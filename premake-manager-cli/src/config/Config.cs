using src.libraries;
using src.modules;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace src.config
{
    internal sealed class Config
    {
        [YamlMember(Alias = "version")]
        public string Version { get; set; } = string.Empty;

        [YamlMember(Alias = "modulesPath")]
        public string? ModulesPath { get; set; }

        [YamlMember(Alias = "librariesPath")]
        public string? LibrariesPath { get; set; }

        [YamlMember(Alias = "modules")]
        public IDictionary<string, PremakeModule>? Modules { get; set; }

        [YamlMember(Alias = "libraries")]
        public IDictionary<string, PremakeLibrary>? Libraries { get; set; }


        /// <summary>
        /// Adds a module to the Configuration
        /// </summary>
        /// <param name="module"></param>
        /// <returns></returns>
        public Config AddModule(PremakeModule module)
        {
            if(Modules == null)
                Modules = new Dictionary<string, PremakeModule>();
            if (string.IsNullOrEmpty(module.version))
                module.version = "*";
            Modules.Add(module.module!, module);
            return this;
        }

        /// <summary>
        /// Removes a Module from the configuration
        /// </summary>
        /// <param name="moduleName">the name of the module to remove</param>
        /// <returns>This</returns>
        public Config RemoveModule(string moduleName)
        {
            if (Modules == null) return this;
            PremakeModule foundModule = Modules.First(module =>
            {
                if (module.Key.Equals(moduleName, StringComparison.OrdinalIgnoreCase))
                {
                    moduleName = module.Key;
                    return true;
                }
                return false;
            }).Value;
            if (foundModule != null)
                Modules.Remove(moduleName);
            return this;
        }
        /// <summary>
        /// Adds a module to the Configuration
        /// </summary>
        /// <param name="module"></param>
        /// <returns></returns>
        public Config AddLibrary(PremakeLibrary library)
        {
            if (Libraries == null)
               Libraries = new Dictionary<string, PremakeLibrary>();
            if (string.IsNullOrEmpty(library.version))
                library.version = "*";
            Libraries.Add(library.library!, library);
            return this;
        }

        /// <summary>
        /// Removes a Library from the configuration
        /// </summary>
        /// <param name="libraryName"></param>
        /// <returns></returns>
        public Config RemoveLibrary(string libraryName)
        {
            if (Libraries == null) return this;
            PremakeLibrary foundLibrary = Libraries.First(library =>
            {
                if (library.Key.Equals(libraryName, StringComparison.OrdinalIgnoreCase))
                {
                    libraryName = library.Key;
                    return true;
                }
                return false;
            }).Value;
            if (foundLibrary != null)
                Libraries.Remove(libraryName);
            return this;
        }
    }
}
