using Octokit;
using Spectre.Console;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace src
{
    [DebuggerDisplay("{owner,nq}/{name,nq}")]
    internal struct GithubRepo
    {
        public string owner;
        public string name;
    }
    internal class Github
    {
        /* Static instance of the GitHubClient */
        private static GitHubClient _instance;

        // Static constructor to initialize the instance
        static Github()
        {
            GitHubClient client = new GitHubClient(
                new ProductHeaderValue("premake-manager-cli")
            );

            string? githubToken = Environment.GetEnvironmentVariable("GITHUB_TOKEN");

            if (!string.IsNullOrWhiteSpace(githubToken))
            {
                client.Credentials = new Credentials(githubToken);
            }
            ApiInfo apiInfo = client.GetLastApiInfo();
            var rateLimit = apiInfo?.RateLimit;
            if(rateLimit?.Remaining < 10)
            {
                AnsiConsole.WriteLine("[RED]github client is rate limited[/]");
            }
             
            _instance = client;
        }

        public static async Task SetSession(string session)
        {
            if (!string.IsNullOrWhiteSpace(session))
            {
                _instance.Credentials = new Credentials(session);
                MiscellaneousRateLimit rate = await _instance.RateLimit.GetRateLimits();
            }

        }
        // Public static property to access the instance
        public static GitHubClient Instance => _instance;
        public static IRepositoriesClient Repositories => _instance.Repository;

        public static GithubRepo GetRepoFromLink(string githubLink) 
        {
            string[] splitLink = githubLink.Split("/");
            return new GithubRepo { owner = splitLink[splitLink.Length - 2], name = splitLink[splitLink.Length - 1] };
        }
        public static string FormatZipballUrl(GithubRepo repo, string refName)
        {
            return $"https://github.com/{repo.owner}/{repo.name}/zipball/{refName}";
        }

        internal static async Task<string> GetDescription(GithubRepo repo)
        {
            //TODO add cache.
            Repository repository = await Repositories.Get(repo.owner, repo.name);
            return repository.Description;
        }
        internal static async Task<IReadOnlyList<Release>> GetRepoVersions(GithubRepo repo)
        {
            //TODO add cache.
            return await Repositories.Release.GetAll(repo.owner, repo.name);
        }
        internal static async Task<IReadOnlyList<RepositoryTag>> GetRepoTags(GithubRepo repo)
        {
            //TODO add cache.
            return await Repositories.GetAllTags(repo.owner, repo.name);
        }
    }
}
