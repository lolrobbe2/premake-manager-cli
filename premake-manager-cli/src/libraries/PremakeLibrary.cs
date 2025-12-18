using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YamlDotNet.Serialization;

namespace src.libraries
{
    internal class PremakeLibrary
    {
        [YamlIgnore]
        public string repo { get => getRepo(); set => setRepo(value); }

        [YamlIgnore]
        public string owner { get => getOwner(); set => setOwner(value); }

        public string version { get; set; } = "*";

        [YamlIgnore]
        public string? library { get; set; } = "";
        public PremakeLibrary()
        {

        }
        public PremakeLibrary(string version, string library)
        {
            this.version = version;
            this.library = library;
        }
        private void setRepo(string repo)
        {
            this.library = $"{getOwner()}/{repo}";
        }
        private string getRepo()
        {
            if (string.IsNullOrEmpty(library))
                return string.Empty;
            return library.Split("/")[1] ?? string.Empty;
        }
        private void setOwner(string owner)
        {
            this.library = $"{owner}/{getRepo()}";
        }

        private string getOwner()
        {
            if (string.IsNullOrEmpty(library))
                return string.Empty;
            return library?.Split("/")[0] ?? string.Empty;
        }

        public string getLink()
        {
            return "https://github.com/" + owner + "/" + repo;
        }
    }
}
