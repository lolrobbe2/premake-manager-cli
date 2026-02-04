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

        public static void WriteFileIndex(IndexView index, string filePath = "premakeIndex.yml")
        {
            YamlSerializer.Serialize(index, filePath);
        }

        public static MemoryStream? ReadLibraryFile(IndexView index, GithubRepo repo)
        {
            if (index.libraries.TryGetValue(repo.owner, out IList<IndexLibrary>? libraries))
            {
                if (null == libraries.Where((lib) => lib.name == repo.name).FirstOrDefault())
                    return null;
                GithubRepo remote = Github.GetRepoFromLink(index.remote);
                return ExtractUtils.ReadFile(PathUtils.GetRemotePath(remote.owner, remote.name), $"{repo}-main/{repo.owner}/{repo.name}/premake5.lua".ToLower());
            }
            return null; //could not find library.
        }

        /// <summary>
        /// This function will attempt to Extract a premake5.lua from the given index
        /// </summary>
        /// <param name="index"></param>
        /// <param name="repo"></param>
        /// <returns></returns>
        public static async Task<bool> ExtractLibraryFile(IndexView index, GithubRepo repo)
        {
            MemoryStream? stream = ReadLibraryFile(index, repo);
            if (stream == null) return false;

            using (FileStream libraryFile = File.OpenWrite(await LibraryManager.GetLibraryPath(repo)))
            {
                stream.WriteTo(libraryFile);
            }
            return true;
        }
        #endregion

        #region REMOTE
        public static IndexView ReadRemoteIndex(string owner, string repo)
        {
            return YamlSerializer.Deserialize<IndexView>(owner, repo, "premakeIndex.yml");
        }
        public static IndexView ReadRemoteLocalIndex(string owner, string repo)
        {
            return YamlSerializer.Deserialize<IndexView>(ExtractUtils.ReadFile(PathUtils.GetRemotePath(owner, repo), $"{repo}-main/premakeIndex.yml")!);
        }
        #endregion

        #region CREATE
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
                index.libraries[library.name] = libraries;
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
        #endregion

    }
}
