using Octokit;
using Semver;
using Spectre.Console;
using src.common_index;
using src.dependencies.graph;
using src.dependencies.types;
using src.libraries;
using src.utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
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
        public static async Task AddLibraryDependency(GithubRepo repo, LibraryDependency dependency)
        {
            string LibraryDirectory = Path.Combine([Directory.GetCurrentDirectory(), "libraries", repo.owner, repo.name]).ToLower();
            string LibraryDependencies = Path.Combine(LibraryDirectory, "premakeDependencies.yml");

            #region EXTRACT_UPDATE
            Dependencies dependencies = Path.Exists(LibraryDependencies) ? YamlSerializer.Deserialize<Dependencies>(LibraryDependencies): new Dependencies();
            
            if(dependencies.libraries == null)
                dependencies.libraries = new List<LibraryDependency>();
            dependencies.libraries.Add(dependency);
            #endregion
            
            YamlSerializer.Serialize(dependencies, LibraryDependencies);
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

        private static async Task<IList<LibraryDependency>> GatherDependencies(PremakeLibrary library)
        {
            List<LibraryDependency> libraryDependencies = new List<LibraryDependency>();

            IList<LibraryDependency> dependencies = await GatherLibraryDependencies(library);
            libraryDependencies.AddRange(dependencies);
            libraryDependencies.AddRange(await GatherSubDependencies(dependencies));

            return libraryDependencies;
        }
        private static async Task<IList<LibraryDependency>> GatherSubDependencies(IList<LibraryDependency> dependencies)
        {
            List<LibraryDependency> libraryDependencies = new List<LibraryDependency>();
            foreach (var library in dependencies)
            {
                IList<LibraryDependency> subDependencies = await GatherLibrarySubDependencies(library);
                libraryDependencies.AddRange(subDependencies);
                libraryDependencies.AddRange(await GatherSubDependencies(subDependencies));
            }
            return libraryDependencies;
        }
        #region INDIVIDUAL
        private static async Task<IList<LibraryDependency>> GatherLibraryDependencies(PremakeLibrary library)
        {
            //NOTE: dependencies must either be defined in the remoteIndex or in the github repo itself
            //1) check local remotes.
            //2) check github for dependencies
            GithubRepo repo = Github.GetRepoFromLink(library.getLink());
            #region CHECK_REMOTES
            Dependencies? dependencies = RemotesManager.GetDependencies(repo);
            if (dependencies is not null)
            {
                return dependencies.libraries;
            }
            #endregion

            #region CHECK_GITHUB
            /* DO NOT SUPPORT THIS AS WE WILL QUICKLY HOT THE RATE LIMIT */
            /*
            string LibraryGitDirectory = Path.Combine(["libraries", repo.owner, repo.name]).ToLower();
            string LibraryGitDependencies = Path.Combine(LibraryGitDirectory, "premakeDependencies.yml");
            try
            {
                return (await YamlSerializer.Deserialize<Dependencies>(repo.owner, repo.name, LibraryGitDependencies)).libraries;
            }
            catch (HttpRequestException)
            {
                return new List<LibraryDependency>(); //return empty list when no dependencies where found.
            }
            */
            return new List<LibraryDependency>();
            #endregion
        }

        private static async Task<IList<LibraryDependency>> GatherLibrarySubDependencies(LibraryDependency library)
        {
            //NOTE: dependencies must either be defined in the remoteIndex or in the github repo itself
            //1) check local remotes.
            //2) check github for dependencies
            GithubRepo repo = Github.GetRepoFromLink(library.name);
            #region CHECK_REMOTES
            Dependencies? dependencies  = RemotesManager.GetDependencies(repo);
            if(dependencies is not null)
            {
                return dependencies.libraries;
            }
            #endregion

            #region CHECK_GITHUB
            /* DO NOT SUPPORT THIS AS WE WILL QUICKLY HOT THE RATE LIMIT */
            /*
            string LibraryGitDirectory = Path.Combine(["libraries", repo.owner, repo.name]).ToLower();
            string LibraryGitDependencies = Path.Combine(LibraryGitDirectory, "premakeDependencies.yml");
            try
            {
                return (await YamlSerializer.Deserialize<Dependencies>(repo.owner, repo.name, LibraryGitDependencies)).libraries;
            }
            catch (HttpRequestException)
            {
                return new List<LibraryDependency>(); //return empty list when no dependencies where found.
            }
            */
            return new List<LibraryDependency>();
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
        public static async Task<DependencyGraph> GetDependencyGraph(IList<PremakeLibrary> libraries)
        {
            IList<KeyValuePair<PremakeLibrary, LibraryDependency>> rootDependencies = libraries.Select((library) => new KeyValuePair<PremakeLibrary, LibraryDependency>(library, new LibraryDependency() { name = library.library!, version = library.version })).ToList();
            DependencyGraph graph = new DependencyGraph(rootDependencies.Select((dep) => dep.Value).ToArray());
            foreach (KeyValuePair<PremakeLibrary, LibraryDependency> dependency in rootDependencies)
            {
                graph.AddDependencies(dependency.Value, await GatherDependencies(dependency.Key));
            }
            return graph;

        }
        static async Task<PremakeLibrary> GetLibVersions(Regex regex, LibraryDependency library)
        {
            GithubRepo repo = Github.GetRepoFromLink(library.name);
            SemVersionRange range = SemVersionRange.Parse(library.version);
            try
            {
                #region BATCH_CALC_SEMVERS
                // fetch all tags
                var versions = await Github.GetRepoTags(repo);

                // parse all valid SemVersions in one pass
                int batchSize = 50; // Adjust based on your API limits
                var batches = versions.Chunk(batchSize);

                var tasks = batches.Select(async batch =>
                {
                    // Process the batch in parallel or sequentially depending on your needs
                    return batch.Select(tag =>
                    {
                        Match match = regex.Match(tag.Name);
                        if (match.Success && SemVersion.TryParse(match.Groups[1].Value, out SemVersion tmp))
                        {
                            return (Success: true, Version: tmp);
                        }
                        return (Success: false, Version: null);
                    })
                    .Where(t => t.Success)
                    .Select(t => t.Version)
                    .ToList();
                });
                #endregion
                // Wait for all batches to finish
                var semVersions = (await Task.WhenAll(tasks)).SelectMany(x => x).ToList();

                // find the first version in range
                SemVersion firstInRange = semVersions.FirstOrDefault(v => range.Contains(v));

                if (firstInRange != null)
                {
                    return new PremakeLibrary($"v{firstInRange.ToString()}", library.name);
                }
            }
            catch (Exception)
            {
                AnsiConsole.WriteLine($"Library not found");
            }
            //NO VERSION FOUND FOR RANGE
            throw new InvalidOperationException("No valid version found for provided range");
        }
        /// <summary>
        /// This function fetches the versions from Github and resolves the correct version (if possible).
        /// </summary>
        /// <param name="graph">the dependecy graph to resolve the versions of</param>
        /// <returns>a dictionary indexed, via the library name</returns>
        public static async Task<PremakeLibrary[]> GetVersionsFromGraph(DependencyGraph graph)
        {
            var(libraries,conflict) = graph.GetResolvedLibraries();
            IList<PremakeLibrary> resultLibraries = new List<PremakeLibrary>();
            Regex regex = new Regex(@"(\d+\.\d+\.\d+)", RegexOptions.IgnoreCase);

            var tasks = libraries.Select(library => GetLibVersions(regex, library));

            // 2. Start them all and wait for the whole group to complete
            resultLibraries = await Task.WhenAll(tasks);
            return resultLibraries.ToArray();
        }
        #endregion
    }
}
