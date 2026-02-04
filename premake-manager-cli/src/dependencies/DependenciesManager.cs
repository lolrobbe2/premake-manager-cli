using src.dependencies.graph;
using src.dependencies.types;
using src.libraries;
using src.utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
//TODO USE STREAMS => can be tested
namespace src.dependencies
{
    [YamlSerializable]
    internal class Dependencies
    {
        [YamlMember]
        public IList<LibraryDependency>? libraries;
    }

    //TODO Package Calculus

    //Add  dependencies
    internal class DependenciesManager
    {
        #region LIBRARIES
        /// <summary>
        /// Add a new library dependency to the index.
        /// </summary>
        /// <param name="repo">the repo to add the dependency to</param>
        /// <param name="dependency">dependency to add</param>
        public async void AddLibraryDependency(GithubRepo repo, LibraryDependency dependency)
        {
            string LibraryDirectory = Path.Combine([Directory.GetCurrentDirectory(), "libraries", repo.owner, repo.name]).ToLower();
            string LibraryDependencies = Path.Combine(LibraryDirectory, "premakeDependencies.yml");

            #region EXTRACT_UPDATE
            Dependencies dependencies = Path.Exists(LibraryDependencies) ? YamlSerializer.Deserialize<Dependencies>(LibraryDependencies): new Dependencies();
            
            if(dependencies.libraries == null)
                dependencies.libraries = new List<LibraryDependency>();
            dependencies.libraries.Add(dependency);
            #endregion
            
            YamlSerializer.Serialize(dependencies, LibraryDirectory);
        }
        /// <summary>
        /// Remove a library from the repo dependecies
        /// </summary>
        /// <param name="repo">repo to remove the dependecy from</param>
        /// <param name="name">the name of the dependecy to remove</param>
        public async void RemoveLibraryDependency(GithubRepo repo, string name)
        {
            string LibraryDirectory = Path.Combine([Directory.GetCurrentDirectory(), "libraries", repo.owner, repo.name]).ToLower();
            string LibraryDependencies = Path.Combine(LibraryDirectory, "premakeDependencies.yml");
            if(Path.Exists(LibraryDependencies))
            {
                Dependencies dependencies = YamlSerializer.Deserialize<Dependencies>(LibraryDependencies);
                LibraryDependency? dependency = dependencies.libraries?.First((dependency) => dependency.name == name);
                if (dependency != null)
                    dependencies.libraries?.Remove(dependency);

                YamlSerializer.Serialize(dependencies, LibraryDirectory);
            }
        }
        #endregion

        #region GATHER_DEPENDENCIES

        private static IList<LibraryDependency> GatherDependencies(IList<PremakeLibrary> libraries)
        {
            List<LibraryDependency> libraryDependencies = new List<LibraryDependency>();
            foreach (var library in libraries)
            {
                IList<LibraryDependency> dependencies = GatherLibraryDependencies(library);   
                libraryDependencies.AddRange(dependencies);
                libraryDependencies.AddRange(GatherSubDependencies(dependencies));
            }
            return libraryDependencies;
        }
        private static IList<LibraryDependency> GatherSubDependencies(IList<LibraryDependency> dependencies)
        {
            List<LibraryDependency> libraryDependencies = new List<LibraryDependency>();
            foreach (var library in dependencies)
            {
                IList<LibraryDependency> subDependencies = GatherLibrarySubDependencies(library);
                libraryDependencies.AddRange(subDependencies);
                libraryDependencies.AddRange(GatherSubDependencies(subDependencies));
            }
            return libraryDependencies;
        }
        #region INDIVIDUAL
        private static IList<LibraryDependency> GatherLibraryDependencies(PremakeLibrary library)
        {
            //NOTE: dependencies must either be defined in the remoteIndex or in the github repo itself
            //1) check local remotes.
            //2) check github for dependencies
            #region CHECK_REMOTES
            string LibraryDirectory = Path.Combine([Directory.GetCurrentDirectory(), "libraries", library.owner, library.repo]).ToLower();
            string LibraryDependencies = Path.Combine(LibraryDirectory, "premakeDependencies.yml");

            if (Path.Exists(LibraryDependencies))
                return YamlSerializer.Deserialize<Dependencies>(LibraryDependencies).libraries;
            #endregion

            #region CHECK_GITHUB
                return YamlSerializer.Deserialize<Dependencies>(library.owner, library.repo, LibraryDependencies).libraries;
            #endregion
        }

        private static IList<LibraryDependency> GatherLibrarySubDependencies(LibraryDependency library)
        {
            //NOTE: dependencies must either be defined in the remoteIndex or in the github repo itself
            //1) check local remotes.
            //2) check github for dependencies
            GithubRepo repo = Github.GetRepoFromLink(library.name);
            #region CHECK_REMOTES
            string LibraryDirectory = Path.Combine([Directory.GetCurrentDirectory(), "libraries", repo.owner, repo.name]).ToLower();
            string LibraryDependencies = Path.Combine(LibraryDirectory, "premakeDependencies.yml");

            if (Path.Exists(LibraryDependencies))
                return YamlSerializer.Deserialize<Dependencies>(LibraryDependencies).libraries;
            #endregion

            #region CHECK_GITHUB
            return YamlSerializer.Deserialize<Dependencies>(repo.owner, repo.name, LibraryDependencies).libraries;
            #endregion
        }
        #endregion
        #endregion

        #region GRAPH
        /// <summary>
        /// This function resolves the dependecyGraph for the given list of premake libraries
        /// </summary>
        /// <param name="libraries"></param>
        /// <returns></returns>
        public static DependencyGraph GetDependencyGraph(IList<PremakeLibrary> libraries)
        {
            IList<LibraryDependency> dependencies = GatherDependencies(libraries);
            return new DependencyGraph(dependencies.ToArray());
        }
        /// <summary>
        /// This function fetches the versions from Github and resolves the correct version (if possible).
        /// </summary>
        /// <param name="graph">the dependecy graph to resolve the versions of</param>
        /// <returns>a dictionary indexed, via the library name</returns>
        public static async Task<IDictionary<string, string>> GetVersionsFromGraph(DependencyGraph graph)
        {
            IReadOnlyCollection<LibraryDependency> libraries = graph.GetResolvedLibraries();
            foreach (LibraryDependency library in libraries)
            {
                GithubRepo repo = Github.GetRepoFromLink(library.name);
                var versions = await Github.GetRepoVersions(repo);
                //TODO 

            }
            return new Dictionary<string, string>();
        }
        #endregion
    }
}
