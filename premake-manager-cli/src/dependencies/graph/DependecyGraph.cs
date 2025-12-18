using Spectre.Console;
using src.dependencies.types;
using System;
using System.Collections.Generic;
using System.Linq;

namespace src.dependencies.graph
{
    internal class DependencyGraph
    {
        private readonly Dictionary<LibraryDependency, HashSet<LibraryDependency>> _graph
            = new Dictionary<LibraryDependency, HashSet<LibraryDependency>>();

        public DependencyGraph(LibraryDependency[] libraries)
        {
            if (libraries == null)
                throw new ArgumentNullException(nameof(libraries));

            foreach (var lib in libraries)
                if (!_graph.ContainsKey(lib))
                    _graph[lib] = new HashSet<LibraryDependency>();
        }

        public void AddDependency(LibraryDependency library, LibraryDependency dependsOn)
        {
            if (!_graph.ContainsKey(library))
                _graph[library] = new HashSet<LibraryDependency>();

            if (!_graph.ContainsKey(dependsOn))
                _graph[dependsOn] = new HashSet<LibraryDependency>();

            _graph[library].Add(dependsOn);
        }

        public IReadOnlyCollection<LibraryDependency> GetDependencies(LibraryDependency library) =>
            _graph.TryGetValue(library, out var deps) ? deps : Array.Empty<LibraryDependency>();

        public IReadOnlyCollection<LibraryDependency> GetAllLibraries() => _graph.Keys;

        public IReadOnlyDictionary<string, List<LibraryDependency>> GetDependencyConflicts(bool showProgress = false)
        {
            // Include all libraries: keys + dependencies
            var allDeps = _graph.Keys
                .Concat(_graph.SelectMany(kv => kv.Value))
                .ToList();

            var conflicts = new Dictionary<string, List<LibraryDependency>>();

            if (showProgress)
            {
                AnsiConsole.Progress()
                    .AutoClear(false)
                    .Start(ctx =>
                    {
                        var task = ctx.AddTask("Checking dependencies...", maxValue: allDeps.Count);

                        foreach (var group in allDeps.GroupBy(lib => lib.name))
                        {
                            var versions = group.Select(lib => lib.VersionRange).Distinct().ToList();
                            if (versions.Count > 1 && VersionRange.ConflictsWithAll(versions))
                                conflicts[group.Key] = group.ToList();

                            task.Increment(1);
                        }
                    });
            }
            else
            {
                foreach (var group in allDeps.GroupBy(lib => lib.name))
                {
                    var versions = group.Select(lib => lib.VersionRange).Distinct().ToList();
                    if (versions.Count > 1 && VersionRange.ConflictsWithAll(versions))
                        conflicts[group.Key] = group.ToList();
                }
            }

            return conflicts;
        }

        /// <summary>
        /// Returns a flat list of all libraries with the best matching version,
        /// resolving any compatible version ranges. Throws if no compatible version exists.
        /// </summary>
        public IReadOnlyCollection<LibraryDependency> GetResolvedLibraries()
        {
            // Include all libraries
            var allDeps = _graph.Keys.Concat(_graph.SelectMany(kv => kv.Value)).ToList();

            var resolved = new List<LibraryDependency>();

            // Group by library name
            var groups = allDeps.GroupBy(lib => lib.name);
            foreach (var group in groups)
            {
                var ranges = group.Select(lib => lib.VersionRange).ToList();

                // Check for conflicts
                if (VersionRange.ConflictsWithAll(ranges))
                    continue; //skip the conflicts

                // Pick the "best" version: choose the highest lower bound
                long bestVersion = ranges
                    .Where(r => !r.AnyVersion)
                    .Max(r => r.LowerBound ?? 0);

                // If all are *, just pick *
                var finalRange = ranges.FirstOrDefault(r => r.AnyVersion)
                                 ?? new VersionRange(bestVersion.ToString());

                resolved.Add(new LibraryDependency
                {
                    name = group.Key,
                    version = finalRange.ToString()
                });
            }

            return resolved;
        }
    }
}
