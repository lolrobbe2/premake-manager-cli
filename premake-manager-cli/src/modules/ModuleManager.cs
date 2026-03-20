using Octokit;
using Semver;
using Spectre.Console;
using src.utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace src.modules
{
    internal class ModuleManager
    {


        public static async Task<ModuleConfig> GetModuleConfig(string githubLink)
        {
            GithubRepo repo = Github.GetRepoFromLink(githubLink);
            RepositoryContent config = await Github.GetAllContentsByRef(repo,"premakeModule.yml", "main");
            await DownloadUtils.DownloadStatus(config.DownloadUrl, $"Fetching module info: {repo.name}", Path.Combine(PathUtils.GetTempModulePath(repo.name), "premakeModule.yml"));
            return new ModuleConfig(Path.Combine(PathUtils.GetTempModulePath(repo.name), "premakeModule.yml"));
        }
        public static async Task<ModuleConfig> GetModuleConfigCtx(ProgressContext ctx,string githubLink)
        {
            GithubRepo repo = Github.GetRepoFromLink(githubLink);
            RepositoryContent config = await Github.GetAllContentsByRef(repo, "premakeModule.yml", "main");
            await DownloadUtils.DownloadProgressCtx(ctx,config.DownloadUrl, $"Fetching module info: {repo.name}", Path.Combine(PathUtils.GetTempModulePath(repo.name), "premakeModule.yml"));
            return new ModuleConfig(Path.Combine(PathUtils.GetTempModulePath(repo.name), "premakeModule.yml"));
        }

        public static async Task GetModulesConfig(string[] githubLinks)
        {
            IList<Task> tasks = new List<Task>();
            foreach (string githubLink in githubLinks)
                tasks.Add(GetModuleConfig(githubLink));

            await Task.WhenAll(tasks);
        }

        public static async Task InstallModule(string githubLink, string version = "*")
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
              await InstallModuleCtx(ctx, githubLink, version);
          });
        }
        public static async Task InstallModules(List<PremakeModule> modules)
        {
            var moduleTuples = modules
                .Where(m => !string.IsNullOrWhiteSpace(m.module)) // Filter out invalid entries
                .Select(m => (m.module!, m.version))
                .ToList();
            await InstallModules(moduleTuples);
        }

        public static async Task InstallModules(List<(string githubLink, string version)> modules)
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
                await InstallModulesCtx(ctx, modules);
            });
        }
        public static async Task InstallModuleCtx(ProgressContext ctx, string githubLink, string version = "*")
        {
            if (!githubLink.StartsWith("https://github.com/"))
            {
                string[] repoInfo = githubLink.Split('/');
                if (repoInfo.Length == 2)
                    githubLink = "https://github.com/" + githubLink;
            }

            if (string.IsNullOrEmpty(version))
                version = "*";
            ModuleConfig config = await GetModuleConfigCtx(ctx,githubLink);
            GithubRepo repo = Github.GetRepoFromLink(githubLink);

            string downloadUrl = await ResolveDownloadUrl(repo, version);
            await DownloadUtils.DownloadProgressCtx(ctx, downloadUrl, $"downloading {config.name} module", Path.Combine(PathUtils.GetTempModulePath(repo.name), $"{repo.name}.zip"));
            await ExtractUtils.ExtractZipProgressCtx(ctx, Path.Combine(PathUtils.GetTempModulePath(repo.name), $"{repo.name}.zip"), Path.Combine(Directory.GetCurrentDirectory(),$"modules/{repo.name}"), $"extracting {config.name}");
        }

        public static async Task InstallModulesCtx(ProgressContext ctx, List<(string githubLink, string version)> modules)
        {
            foreach (var (githubLink, version) in modules)
            {
                await InstallModuleCtx(ctx, githubLink, version);
            }
        }

        private static async Task<string> ResolveDownloadUrl(GithubRepo repo, string version)
        {

            if (SemVersion.TryParse(version, SemVersionStyles.Any, out _))
            {
                return Github.FormatZipballUrl(repo, version);
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
    }
}
