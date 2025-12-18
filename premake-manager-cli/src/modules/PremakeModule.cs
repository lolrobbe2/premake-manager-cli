using Octokit;
using Spectre.Console;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YamlDotNet.Serialization;
#nullable enable
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
        [YamlIgnore]
        public string repo { get => getRepo(); set => setRepo(value); }

        [YamlIgnore]
        public string owner { get => getOwner(); set => setOwner(value); }

        public string version { get; set; } = "*";

        public string entryPoint { get; set; }

        [YamlIgnore]
        public string? module { get; set; } = "";
        public PremakeModule()
        {

        }
        public PremakeModule(string version, string module)
        {
            this.version = version;
            this.module = module;
        }
        public async Task<ModuleInfo> GetInfo() 
        {
            RepositoryContent gitRepo = await AnsiConsole.Status().StartAsync("Fetching module info", async ctx =>
            {
                ctx.Spinner(Spinner.Known.Aesthetic);
                ctx.SpinnerStyle(Style.Parse("green"));
                return (await Github.Instance.Repository.Content.GetAllContentsByRef(owner, repo, "premakeModule.yml", "master"))[0];
            });

            return new ModuleInfo();
        }
        private void setRepo(string repo)
        {
            this.module = $"{getOwner()}/{repo}";
        }
        private string getRepo()
        {
            if (string.IsNullOrEmpty(module))
                return string.Empty;
            return module.Split("/")[1] ?? string.Empty;
        }
        private void setOwner(string owner)
        {
            this.module = $"{owner}/{getRepo()}";
        }

        private string getOwner() 
        {
            if (string.IsNullOrEmpty(module))
                return string.Empty;
            return module?.Split("/")[0] ?? string.Empty;
        }

        public string getLink()
        {
            return "https://github.com/" + owner + "/" + repo;
        }
    }
}
