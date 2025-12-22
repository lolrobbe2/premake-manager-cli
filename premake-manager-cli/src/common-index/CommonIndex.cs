using Spectre.Console;
using src.libraries;
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
        public static IndexView ReadFileIndex(string filePath = "premakeIndex.yml")
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
        public static IndexView ReadRemoteLocalIndex(string owner, string repo)
        {
            return ExtractUtils.Read
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

        public static void CreateNewLibrary(ref IndexView index, IndexLibrary library, string owner)
        {
          
            library.description = library.description!.ToLower();
            library.name = library.name.ToLower();
            owner = owner.ToLower();

            string LibraryDirectory = Path.Combine([Directory.GetCurrentDirectory(), "libraries", owner, library.name]).ToLower();
            
            if (index.libraries.TryGetValue(library.name, out IList<IndexLibrary>? libraries))
            {
                libraries.Add(library);
            }
            else
            {
                libraries = new List<IndexLibrary>();
                libraries.Add(library);
                index.libraries.Add(owner,libraries!);
            }
            YamlSerializer.Serialize(library, Path.Combine(LibraryDirectory,"premakeLib.yml"));
            PathUtils.CreateEmpty(Path.Combine(LibraryDirectory, "premake5.lua"));  
        }
    }
}
