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
        
        public static async Task GetModuleInfo(string githubLink) 
        {
            GithubRepo repo = Github.GetRepoFromLink(githubLink);
            Octokit.RepositoryContent config = (await Github.Repositories.Content.GetAllContentsByRef(repo.owner, repo.name, "premakeModule.yml","main"))[0];
           await DownloadUtils.DownloadStatus(config.DownloadUrl, $"Fetching module info: {repo.name}",Path.Combine(PathUtils.GetTempModuleInfoPath(repo.name),"premakeModule.yml"));
        }
        
    }
}
