using src.libraries;
using src.modules;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using static Microsoft.VisualStudio.Threading.AsyncReaderWriterLock;

namespace src.config
{
    /// <summary>
    /// This class is a builder style class but writes to the premakeConfig.yml instead of returning a class instance
    /// </summary>
    internal class ConfigWriter
    {
        public string version {  get; set; }

        public IDictionary<string,PremakeModule> modules { get; set; }
        public IDictionary<string, PremakeLibrary> libraries { get; set; }

        public static ConfigWriter FromReader(ConfigReader reader)
        {
            ConfigWriter writer = new();
            writer.version = reader.version;
            writer.modules = reader.modules;
            return writer;
        }
        public ConfigWriter()
        {
            this.modules = new Dictionary<string, PremakeModule>();
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
            if (string.IsNullOrEmpty(module.version))
                module.version = "*";
            modules.Add(module.module,module);
            return this;
        }

        /// <summary>
        /// Removes a Module from the configuration
        /// </summary>
        /// <param name="moduleName"></param>
        /// <returns></returns>
        public ConfigWriter RemoveModule(string moduleName)
        {
            PremakeModule foundModule = modules.First(module =>
            {
                if (module.Key.Equals(moduleName, StringComparison.OrdinalIgnoreCase))
                {
                    moduleName = module.Key;
                    return true;
                }
                return false;
            }).Value;
            if(foundModule != null)
                modules.Remove(moduleName);
            return this;
        }

        /// <summary>
        /// Adds a module to the Configuration
        /// </summary>
        /// <param name="module"></param>
        /// <returns></returns>
        public ConfigWriter AddLibrary(PremakeLibrary library)
        {
            if (string.IsNullOrEmpty(library.version))
                library.version = "*";
            libraries.Add(library.library, library);
            return this;
        }

        /// <summary>
        /// Removes a Library from the configuration
        /// </summary>
        /// <param name="libraryName"></param>
        /// <returns></returns>
        public ConfigWriter RemoveLibrary(string libraryName)
        {
            PremakeLibrary foundLibrary = libraries.First(library =>
            {
                if (library.Key.Equals(libraryName, StringComparison.OrdinalIgnoreCase))
                {
                    libraryName = library.Key;
                    return true;
                }
                return false;
            }).Value;
            if (foundLibrary != null)
                libraries.Remove(libraryName);
            return this;
        }

        public async Task Write(string path = "") 
        {
            var serializer = new SerializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .Build();

            string serializedConfig = serializer.Serialize(this);

            string outputPath;

            if (string.IsNullOrEmpty(path))
                outputPath = Path.Combine(Directory.GetCurrentDirectory(), "premakeConfig.yml");    
            else
                outputPath = Path.Combine(path, "premakeConfig.yml");
            
            await File.WriteAllTextAsync(outputPath, serializedConfig);
        }
    }
}
