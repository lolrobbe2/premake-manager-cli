using src.utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YamlDotNet.Serialization;

namespace src.common_index
{
    [YamlSerializable]
    internal sealed class Remote
    {
        [YamlMember(Alias ="owner", Description = "the repo owner", Order = 0)]
        public required string Owner {  get; set; }
        [YamlMember(Alias = "repo", Description = "the repo name", Order = 1)]
        public required string Repo { get; set; }
        [YamlMember(Alias = "enabled", Description = "should the remote be used", Order = 2)]
        public required bool Enabled { get; set; }
    }
    internal sealed class Remotes
    {
        internal required Remote[] remotes {get; set;}
    }

    internal sealed class RemoteIndex
    {
        public required Remote Remote { get; set; }
        public required IndexView Index {  get; set; }
    }
    internal class RemotesManager
    {
        private static readonly Remote DefaultRemote = new Remote() { Owner = "lolrobbe", Repo = "premake-common-registry", Enabled = true };  
        private static Remote[] Remotes {  get => GetRemotes();  }
        static void Intialize()
        {
            
        }

        private static Remote[] GetRemotes() {
            string RemotePaths = Path.Combine(PathUtils.GetRoamingPath(), "premakeRemotes.yaml");
            if (Path.Exists(RemotePaths))
            {
                return YamlSerializer.Deserialize<Remotes>(RemotePaths).remotes;
            }
            return [DefaultRemote];
        }

        private static RemoteIndex[] GetRemoteIndices()
        {
            Remotes.Select((remote)=> new RemoteIndex() { Remote = remote , Index = CommonIndex.ReadRemoteIndex()}).ToList();
        }
    }
}
