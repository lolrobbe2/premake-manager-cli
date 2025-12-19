using src.utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace src.common_index
{
    
    internal class CommonIndex
    {
        #region STREAM
        public static IndexView ReadStreamIndex(Stream stream)
        {
           return YamlSerializer.Deserialize<IndexView>(stream);
        }
        public static Stream WriteStreamIndex(IndexView index)
        {
            return YamlSerializer.Serialize(index);
        }
        #endregion

        #region FILE
        public static IndexView ReadFileIndex(string filePath)
        {
            return YamlSerializer.Deserialize<IndexView>(filePath);
        }

        public static void WriteFileIndex(IndexView index, string filePath)
        {
            YamlSerializer.Serialize(index, filePath);
        }

        #endregion
        #region REMOTE
        public static IndexView ReadRemoteIndex(string owner, string repo)
        {
            return YamlSerializer.Deserialize<IndexView>(owner, repo, "premakeIndex.yml");
        }

        #endregion
        public static IndexView CreateNew(string remoteName)
        {
            return new IndexView()
            {
                remote = remoteName,
                libraries = new Dictionary<string, IList<IndexLibrary>>()
            };
        }
    }
}
