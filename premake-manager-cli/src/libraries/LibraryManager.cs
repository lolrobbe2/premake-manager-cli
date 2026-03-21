using Octokit;
using Semver;
using Spectre.Console;
using src.common_index;
using src.config;
using src.modules;
using src.utils;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace src.libraries
{
    internal class LibraryManager
    {

        public static async Task<LibraryConfig> GetLibraryConfig(string githubLink)
        {
            GithubRepo repo = Github.GetRepoFromLink(githubLink);
            return await GetLibraryConfig(repo);
        }
        public static async Task<LibraryConfig> GetLibraryConfig(GithubRepo repo)
        {
            try
            {
                Octokit.RepositoryContent config = (await Github.Repositories.Content.GetAllContentsByRef(repo.owner, repo.name, "premakeLibrary.yml", "main"))[0];
                await DownloadUtils.DownloadStatus(config.DownloadUrl, $"Fetching library info: {repo.name}", Path.Combine(PathUtils.GetTempModulePath(repo.name), "premakeLibrary.yml"));
                return new LibraryConfig(Path.Combine(PathUtils.GetTempModulePath(repo.name), "premakeLibrary.yml"));
            }
            catch (Octokit.NotFoundException)
            {
                //default for when we are installing from a remote with no LibraryConfig
                return new LibraryConfig() { name = repo.name, entryPoint = "premake5.lua", description = await Github.GetDescription(repo) };
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
            await ExtractUtils.ExtractZipProgressCtx(ctx, Path.Combine(PathUtils.GetTempModulePath(repo.name), $"{repo.name}.zip"), (await GetLibraryPath(repo)).ToLower(), $"extracting {libconfig.name}");
            await RemotesManager.InstallRemotesLibrary(repo);
            
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

            if (SemVersion.TryParse(version, SemVersionStyles.Any, out _))
            {
                using var httpClient = new HttpClient();
                httpClient.DefaultRequestHeaders.Add("User-Agent", "premake-manager");
                // 1. Try the version exactly as provided
                string firstUrl = Github.FormatZipballUrl(repo, version);
                var firstResponse = await httpClient.SendAsync(new HttpRequestMessage(HttpMethod.Head, firstUrl));

                if (firstResponse.IsSuccessStatusCode)
                   return firstUrl;
                

                // 2. If first fails, toggle the 'v' prefix and try again
                string toggledVersion = version.StartsWith("v", StringComparison.OrdinalIgnoreCase)
                    ? version.Substring(1)
                    : $"v{version}";

                string secondUrl = Github.FormatZipballUrl(repo, toggledVersion);
                var secondResponse = await httpClient.SendAsync(new HttpRequestMessage(HttpMethod.Head, secondUrl));

                if (secondResponse.IsSuccessStatusCode)
                    return secondUrl;
            }
            if (version == "*" || string.IsNullOrWhiteSpace(version))
            {
                Repository repoInfo = await Github.GetRepo(repo);
                return Github.FormatZipballUrl(repo, repoInfo.DefaultBranch);
            }
            // Check if it's a branch
            IReadOnlyList<Branch> branches = await Github.GetBranches(repo);
            Branch? branch = branches.FirstOrDefault(b => b.Name == version);
            if (branch != null)
                return Github.FormatZipballUrl(repo, branch.Name);


            /* Assume it's a commit SHA (Octokit throws on invalid SHA so we fetch all commits and match manually) */
            IReadOnlyList<GitHubCommit> commits = await Github.GetRepoCommits(repo);
            bool commitExists = commits.Any(c => c.Sha.StartsWith(version));
            if (commitExists)
                return Github.FormatZipballUrl(repo, version);


            throw new InvalidOperationException($"Could not resolve tag, branch, or commit '{version}' for repository {repo.owner}/{repo.name}.");
        }

        public static async Task<string> GetLibraryPath(GithubRepo library)
        {
            Config config = ConfigManager.HasConfig() ? ConfigManager.ReadConfig() : new Config();
            return $"{config.LibrariesPath ?? "libraries"}/{library.name}";
        }
    }
}
