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
        [YamlMember(Alias = "version")]
        public string version { get; set; } = string.Empty;
        [YamlMember(Alias = "modules")]
        public IList<PremakeModule> modules { get; set; } = new List<PremakeModule>();
        public ConfigReader(string path = "")
        {
            string configPath;

            if (string.IsNullOrEmpty(path))
                configPath = Path.Combine(Directory.GetCurrentDirectory(), "premakeConfig.yml");
            else
                configPath = Path.Combine(path, "premakeConfig.yml");
            

            string deserializedConfig = File.Exists(configPath) ? File.ReadAllText(configPath) : "";

            //If the desirialized config is not null or whitespace we want to read it.
            if (!string.IsNullOrEmpty(deserializedConfig))
            {
                var deserializer = new DeserializerBuilder()
                    .WithNamingConvention(CamelCaseNamingConvention.Instance)
                    .IgnoreUnmatchedProperties()
                    .Build();
               var tempInstance = deserializer.Deserialize<dynamic>(deserializedConfig);
                if (tempInstance != null)
                {
                    //extract version
                    version = tempInstance["version"] ?? string.Empty;

                    #region EXTRACT_MODULES
                    if (tempInstance["modules"] != null)
                    {
                        modules = new List<PremakeModule>();
                        foreach (var module in tempInstance["modules"])
                        {
                            var premakeModule = new PremakeModule
                            {
                                name = module["name"] ?? string.Empty,
                                git = module["git"] ?? string.Empty,
                                version = module["version"] ?? string.Empty,
                            };
                            modules.Add(premakeModule);
                        }
                    }
                    #endregion
                }
            }
        }
    }
}
