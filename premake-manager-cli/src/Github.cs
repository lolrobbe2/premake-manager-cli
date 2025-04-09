using Octokit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace src
{
    internal class Github
    {
        /* Static instance of the GitHubClient */
        private static readonly GitHubClient _instance;

        // Static constructor to initialize the instance
        static Github()
        {
            _instance = new GitHubClient(new ProductHeaderValue("MyApp"));
        }

        // Public static property to access the instance
        public static GitHubClient Instance => _instance;
        public static IRepositoriesClient Repositories => _instance.Repository;
    }
}
