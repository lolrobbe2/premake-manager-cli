using src.dependencies.types;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
//TODO USE STREAMS => can be tested
namespace src.dependencies
{
    [YamlSerializable]
    internal class Dependencies
    {
        [YamlMember]
        IList<LibraryDependency>? libraries;
    }
    internal class DependenciesManager
    {


    }
}
