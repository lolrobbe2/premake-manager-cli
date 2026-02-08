using Semver;
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

        public IReadOnlyDictionary<string, LibraryDependency> GetDependencyConflicts(bool showProgress = false)
        {
            // Include all libraries: keys + dependencies
            var (resolved, conflict) = GetResolvedLibraries();

            return conflict!;
        }

       
        public (IReadOnlyCollection<LibraryDependency>, IReadOnlyDictionary<string,LibraryDependency>) GetResolvedLibraries()
        {
            // Include all libraries
            var allDeps = _graph.Keys.Concat(_graph.SelectMany(kv => kv.Value)).ToList();

            //contains all resolved non conflicting libraries.
            var resolved = new List<LibraryDependency>();
            //contains all conflicting libraries.
            var conflict = new Dictionary<string,LibraryDependency>();

            AnsiConsole.Progress()
                    .AutoClear(false)
                    .Start(ctx =>
                    {
                        // Group by library name
                        var task = ctx.AddTask("Checking dependencies...", maxValue: allDeps.Count);

                        var groups = allDeps.GroupBy(lib => lib.name);

                        foreach (var group in groups)
                        {
                            SemVersionRange final = SemVersionRange.Empty;
                            SemVersionRange[] ranges = group.Select(lib => SemVersionRange.Parse(lib.version)).ToArray();
                            foreach (SemVersionRange range in ranges)
                            {
                                if (final != SemVersionRange.Empty)
                                    final = SemVersionRange.Parse($"{final} {range}");
                                else
                                    final = SemVersionRange.Parse($"{range}");
                                
                                if (final == SemVersionRange.Empty)
                                {

                                    conflict.Add(group.Key, new LibraryDependency()
                                    {
                                        name = group.Key,
                                        version = range.ToString()
                                    });
                                    break;
                                }
                                

                            }
                            if (final == SemVersionRange.Empty)
                                continue;
                            resolved.Add(new LibraryDependency()
                            {
                                name = group.Key,
                                version = final.ToString()
                            });

                        }
                    
                        task.Increment(1);
                        ctx.Refresh();
                    });

            return (resolved,conflict);
        }
    }
}
