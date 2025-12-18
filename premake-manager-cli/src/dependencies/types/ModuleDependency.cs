using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YamlDotNet.Serialization;

namespace src.dependencies.types
{
    [YamlSerializable]
    internal class ModuleDependency
    {
        [YamlMember]
        public string name { get; set; }
        [YamlMember]
        public string version { get; set; }
    }
}
