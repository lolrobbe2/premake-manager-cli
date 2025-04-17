using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YamlDotNet.Serialization.NamingConventions;
using YamlDotNet.Serialization;
using System.IO;

namespace src.config
{
    internal class ConfigReader
    {
        public string version { get; set; }
        public IList<PremakeModule> modules { get; set; }
        public ConfigReader(string path = "")
        {
            string configPath;

            if (string.IsNullOrWhiteSpace(path))
                configPath = Path.Combine(Directory.GetCurrentDirectory(), "premakeConfig.yml");
            else
                configPath = Path.Combine(path, "premakeConfig.yml");
            

            string deserializedConfig = File.Exists(configPath) ? File.ReadAllText(configPath) : "";

            //If the desirialized config is not null or whitespace we want to read it.
            if (!string.IsNullOrWhiteSpace(deserializedConfig))
            {
                var deserializer = new DeserializerBuilder()
                    .WithNamingConvention(CamelCaseNamingConvention.Instance)  // see height_in_inches in sample yml 
                    .Build();
               ConfigReader tempInstance = deserializer.Deserialize<ConfigReader>(deserializedConfig);

               modules = tempInstance.modules;
               version = tempInstance.version;
            }
        }
    }
}
