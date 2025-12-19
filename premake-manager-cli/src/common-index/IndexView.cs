using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YamlDotNet.Serialization;

namespace src.common_index
{
    internal class IndexLibrary
    {
        public string? description {  get; set; }
        public required string name {  get; set; }
    }
    /// <summary>
    /// Representation of the common-index yaml
    /// </summary>
    [YamlSerializable]
    internal class IndexView
    {
        [YamlMember(Order = 0)]
        public required string remote {  get; set; }
        [YamlMember(Order = 1)]
        public required IDictionary<string, IList<IndexLibrary>> libraries;
    }
}
