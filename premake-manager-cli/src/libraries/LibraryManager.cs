using Octokit;
using Spectre.Console;
using src.config;
using src.modules;
using src.utils;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace src.libraries
{
    internal class LibraryManager
    {

        public static async Task<LibraryConfig> GetLibraryConfig(string githubLink)
        {
            GithubRepo repo = Github.GetRepoFromLink(githubLink);
            try
            {
                Octokit.RepositoryContent config = (await Github.Repositories.Content.GetAllContentsByRef(repo.owner, repo.name, "premakeLibrary.yml", "main"))[0];
                await DownloadUtils.DownloadStatus(config.DownloadUrl, $"Fetching library info: {repo.name}", Path.Combine(PathUtils.GetTempModulePath(repo.name), "premakeLibrary.yml"));
                return new LibraryConfig(Path.Combine(PathUtils.GetTempModulePath(repo.name), "premakeLibrary.yml"));
            } catch (Octokit.NotFoundException) {
                return new LibraryConfig() { name = repo.name, entryPoint = "premake5.lua", description = Github.GetDescription(repo) };
            }
        }

        public static async Task GetLibrariesConfig(string[] githubLinks)
        {
            IList<Task<LibraryConfig>> tasks = new List<Task<LibraryConfig>>();
            foreach (string githubLink in githubLinks)
                tasks.Add(GetLibraryConfig(githubLink));

            await Task.WhenAll(tasks);
        }

        public static async Task InstallLibrary(string githubLink, string version = "*")
        {

            await AnsiConsole.Progress().Columns(new ProgressColumn[]
            {
                            new TaskDescriptionColumn(),
                            new ProgressBarColumn(),
                            new PercentageColumn(),
                            new DownloadedColumn(),
                            new TransferSpeedColumn()
            }).StartAsync(async ctx =>
            {
                await InstallLibraryCtx(ctx, githubLink, version);
            });
        }
        public static async Task InstallLibraries(List<PremakeLibrary> libraries)
        {
            var libraryTuples = libraries
                .Where(m => !string.IsNullOrWhiteSpace(m.library)) // Filter out invalid entries
                .Select(m => (m.library!, m.version))
                .ToList();
            await InstallLibraries(libraryTuples);
        }

        public static async Task InstallLibraries(List<(string githubLink, string version)> modules)
        {
            await AnsiConsole.Progress().Columns(new ProgressColumn[]
            {
                            new TaskDescriptionColumn(),
                            new ProgressBarColumn(),
                            new PercentageColumn(),
                            new DownloadedColumn(),
                            new TransferSpeedColumn()
            }).StartAsync(async ctx =>
            {
                await InstallLibrariesCtx(ctx, modules);
            });
        }

        [RequiresUnreferencedCode("Calls src.config.ConfigReader.ConfigReader(String)")]
        public static async Task InstallLibraryCtx(ProgressContext ctx, string githubLink, string version = "*")
        {
            Config config = ConfigManager.HasConfig() ? ConfigManager.ReadConfig() : new Config();

            if (!githubLink.StartsWith("https://github.com/"))
            {
                string[] repoInfo = githubLink.Split('/');
                if (repoInfo.Length == 2)
                    githubLink = "https://github.com/" + githubLink;
            }

            if (string.IsNullOrEmpty(version))
                version = "*";
            LibraryConfig libconfig = await GetLibraryConfig(githubLink);
            GithubRepo repo = Github.GetRepoFromLink(githubLink);

            string downloadUrl = await ResolveDownloadUrl(repo, version);
            await DownloadUtils.DownloadProgressCtx(ctx, downloadUrl, $"downloading {libconfig.name} library", Path.Combine(PathUtils.GetTempModulePath(repo.name), $"{repo.name}.zip"));
            await ExtractUtils.ExtractZipProgressCtx(ctx, Path.Combine(PathUtils.GetTempModulePath(repo.name), $"{repo.name}.zip"), $"{config.LibrariesPath}/{repo.name}", $"extracting {libconfig.name}");
        }

        [RequiresUnreferencedCode("Calls src.libraries.LibraryManager.InstallLibraryCtx(ProgressContext, String, String)")]
        public static async Task InstallLibrariesCtx(ProgressContext ctx, List<(string githubLink, string version)> modules)
        {
            foreach (var (githubLink, version) in modules)
            {
                await InstallLibraryCtx(ctx, githubLink, version);
            }
        }

        private static async Task<string> ResolveDownloadUrl(GithubRepo repo, string version)
        {
            GitHubClient client = new GitHubClient(new ProductHeaderValue("premake-manager"));

            if (version == "*" || string.IsNullOrWhiteSpace(version))
            {
                Repository repoInfo = await client.Repository.Get(repo.owner, repo.name);
                return Github.FormatZipballUrl(repo, repoInfo.DefaultBranch);
            }

            // Check if it's a branch
            IReadOnlyList<Branch> branches = await client.Repository.Branch.GetAll(repo.owner, repo.name);
            Branch? branch = branches.FirstOrDefault(b => b.Name == version);
            if (branch != null)
                return Github.FormatZipballUrl(repo, branch.Name);


            /* Check if it's a tag */
            IReadOnlyList<Release> releases = await client.Repository.Release.GetAll(repo.owner, repo.name);
            Release? tagRelease = releases.FirstOrDefault(r => r.TagName == version);
            if (tagRelease != null)
                return tagRelease.ZipballUrl;


            /* Assume it's a commit SHA (Octokit throws on invalid SHA so we fetch all commits and match manually) */
            IReadOnlyList<GitHubCommit> commits = await client.Repository.Commit.GetAll(repo.owner, repo.name);
            bool commitExists = commits.Any(c => c.Sha.StartsWith(version));
            if (commitExists)
                return Github.FormatZipballUrl(repo, version);


            throw new InvalidOperationException($"Could not resolve tag, branch, or commit '{version}' for repository {repo.owner}/{repo.name}.");
        }
    }
}
