using Octokit;
using Spectre.Console;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace src.modules
{

    internal struct ModuleInfo
    {
        public string name; //module name
        public string discription;
        public string entryPoint; //entrypoint to load in the premake system file.
    }
    internal class PremakeModule
    {
        public string name {  get; set; }
        public string version { get; set; }
        public string git {  get; set; }

        public async Task<ModuleInfo> GetInfo() 
        {
            string[] splitLink = git.Split("/");
            string owner = splitLink[splitLink.Length - 1];
            string repo = splitLink[splitLink.Length - 2];
            RepositoryContent gitRepo = await AnsiConsole.Status().StartAsync("Fetching module info", async ctx =>
            {
                ctx.Spinner(Spinner.Known.Aesthetic);
                ctx.SpinnerStyle(Style.Parse("green"));
                return (await Github.Instance.Repository.Content.GetAllContentsByRef(owner, repo, "premakeModule.yml", "master"))[0];
            });

            return new ModuleInfo();
        }
    }
}
