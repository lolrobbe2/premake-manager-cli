using src.modules;
using System.Collections.Generic;
using System.IO;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace src.config
{
    internal class ConfigReader
    {
        [YamlMember(Alias = "version")]
        public string version { get; set; } = string.Empty;
        [YamlMember(Alias = "modules")]
        public IDictionary<string, PremakeModule> modules { get; set; } = new Dictionary<string, PremakeModule>();
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
                    if (tempInstance["modules"] == null)
                        return;
                    foreach (var module in tempInstance["modules"])
                        modules.Add(module.Key, new PremakeModule(module.Value["version"],module.Key));     
                }
            }
        }
    }
}
