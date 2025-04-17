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
    /// <summary>
    /// This class is a builder style class but writes to the premakeConfig.yml instead of returning a class instance
    /// </summary>
    internal class ConfigWriter
    {
        private string version {  get; set; }
        private IList<PremakeModule> modules { get; set; }
        ConfigWriter()
        {
        }
        /// <summary>
        /// Sets the version of the config
        /// </summary>
        /// <param name="versionTag"></param>
        /// <returns></returns>
        public ConfigWriter SetVersion(string versionTag)
        {
            this.version = versionTag;
            return this;
        }

        /// <summary>
        /// Adds a module to the Configuration
        /// </summary>
        /// <param name="module"></param>
        /// <returns></returns>
        public ConfigWriter AddModule(PremakeModule module)
        {
            modules.Add(module);
            return this;
        }

        /// <summary>
        /// Removes a Module from the configuration
        /// </summary>
        /// <param name="moduleName"></param>
        /// <returns></returns>
        public ConfigWriter RemoveModule(string moduleName)
        {
            PremakeModule foundModule = modules.First(module => module.name.Equals(moduleName));
            modules.Remove(foundModule);
            return this;
        }

        public async Task Write(string path = "") 
        {
            var serializer = new SerializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .Build();

            string serializedConfig = serializer.Serialize(this);

            string outputPath;

            if (string.IsNullOrWhiteSpace(path))
                outputPath = Path.Combine(Directory.GetCurrentDirectory(), "premakeConfig.yml");    
            else
                outputPath = Path.Combine(path, "premakeConfig.yml");
            
            await File.WriteAllTextAsync(outputPath, serializedConfig);
        }
    }
}
