using src.dependencies.types;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace src.dependencies
{
    [YamlSerializable]
    internal class Dependencies
    {
        [YamlMember]
        IList<LibraryDependency>? dependencies;
    }
    internal class DependenciesReader
    {
        private readonly IDeserializer _deserializer;

        public DependenciesReader()
        {
            _deserializer = new DeserializerBuilder()
                .WithNamingConvention(CamelCaseNamingConvention.Instance)
                .IgnoreUnmatchedProperties()
                .Build();
        }

        public T Deserialize<T>(Stream yamlStream)
        {
            if (yamlStream == null)
            {
                throw new ArgumentNullException(nameof(yamlStream));
            }

            using (StreamReader reader = new StreamReader(yamlStream, leaveOpen: true))
            {
                return _deserializer.Deserialize<T>(reader);
            }
        }
    }
}
