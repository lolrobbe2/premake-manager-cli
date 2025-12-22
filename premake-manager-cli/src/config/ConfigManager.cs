using src.utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace src.config
{
    internal class ConfigManager
    {
        public static bool HasConfig(string? filePath = null)
        {
            string usedPath = filePath ?? Path.Combine(Directory.GetCurrentDirectory(), "premakeConfig.yml");
            return Path.Exists(usedPath);
        }
        public static Config ReadConfig(Stream stream)
        {
            return YamlSerializer.Deserialize<Config>(stream);
        }
        public static Config ReadConfig(string? filePath = null)
        {
            string usedPath = filePath  ?? Path.Combine(Directory.GetCurrentDirectory(),"premakeConfig.yml");
            return YamlSerializer.Deserialize<Config>(usedPath);
        }
        public static Stream WriteConfig(Config config)
        {
            return YamlSerializer.Serialize(config);
        }
        public static void WriteConfig(Config config, string? filePath)
        {
            string usedPath = filePath ?? Path.Combine(Directory.GetCurrentDirectory(), "premakeConfig.yml");

            YamlSerializer.Serialize(config, usedPath);
        }
    }
}
