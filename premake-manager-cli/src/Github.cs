using Octokit;
using Octokit.Caching;
using Octokit.Internal;
using Spectre.Console;
using src.utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
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
            var connection = new Connection(
                new ProductHeaderValue("premake-manager-cli"),
                GitHubClient.GitHubApiUrl,
                new InMemoryCredentialStore(new Credentials("token")),
                new CachingHttpClient(new HttpClientAdapter(() => HttpMessageHandlerFactory.CreateDefault(new WebProxy())), new CacheProvider()),
                new SimpleJsonSerializer());

            GitHubClient client = new GitHubClient(
                connection
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
        internal static async Task<Repository> GetRepo(GithubRepo repo)
        {
            Repository repository = await Repositories.Get(repo.owner, repo.name);
            return repository;
        }
        internal static async Task<string> GetDescription(GithubRepo repo)
        {
            return (await GetRepo(repo)).Description;
        }
        internal static async Task<IReadOnlyList<Release>> GetRepoVersions(GithubRepo repo)
        {
            var releases = await Repositories.Release.GetAll(repo.owner, repo.name);
            return releases;
        }

        internal static async Task<IReadOnlyList<GitHubCommit>> GetRepoCommits(GithubRepo repo)
        {
            var commits = await Repositories.Commit.GetAll(repo.owner, repo.name);
            return commits;
        }
        internal static async Task<IReadOnlyList<RepositoryTag>> GetRepoTags(GithubRepo repo)
        {
            return await Repositories.GetAllTags(repo.owner, repo.name);
        }
        internal static async Task<Octokit.RepositoryContent> GetAllContentsByRef(GithubRepo repo, string path, string branch)
        {

            return (await Github.Repositories.Content.GetAllContentsByRef(repo.owner, repo.name, path, branch))[0];
        }
        internal static async Task<IReadOnlyList<Branch>> GetBranches(GithubRepo repo)
        {
                return await _instance.Repository.Branch.GetAll(repo.owner, repo.name);
        }
    }
}
