using src.dependencies.graph;
using src.dependencies.types;
using src.selfTest;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

internal class DependencyGraphTests : ITestClass
{
    public IEnumerable<(string TestName, Func<Task> Action)> GetTests()
    {
        // Test 1: Basic graph construction
        yield return ("Graph Initialization", async () =>
        {
            var libA = new LibraryDependency { name = "glfw/glfw", version = ">=3.3.0" };
            var libB = new LibraryDependency { name = "vulkan/vulkan", version = ">=1.2.0" };
            var libs = new LibraryDependency[] { libA, libB };

            var graph = new DependencyGraph(libs);

            if (graph.GetAllLibraries().Count != 2)
                throw new Exception("Graph should contain exactly 2 libraries");

            await Task.CompletedTask;
        }
        );

        // Test 2: Adding dependencies
        yield return ("Add Dependencies", async () =>
        {
            var libA = new LibraryDependency { name = "glfw/glfw", version = ">=3.3.0" };
            var libB = new LibraryDependency { name = "vulkan/vulkan", version = ">=1.2.0" };
            var libC = new LibraryDependency { name = "ocornut/imgui", version = "=1.90.0" };

            var graph = new DependencyGraph(new[] { libA, libB, libC });
            graph.AddDependency(libC, libA);
            graph.AddDependency(libC, libB);

            var deps = graph.GetDependencies(libC);
            if (deps.Count != 2)
                throw new Exception("imgui should have 2 dependencies");

            await Task.CompletedTask;
        }
        );

        // Test 3: Detect version conflicts
        yield return ("Version Conflict Detection", async () =>
        {
            var vulkanA = new LibraryDependency { name = "vulkan/vulkan", version = ">=1.2.0" };
            var vulkanB = new LibraryDependency { name = "vulkan/vulkan", version = "<1.2.0" };
            var libs = new LibraryDependency[] { vulkanA, vulkanB };

            var graph = new DependencyGraph(libs);

            var conflicts = graph.GetDependencyConflicts(false);
            if (!conflicts.ContainsKey("vulkan/vulkan"))
                throw new Exception("Expected a conflict for vulkan/vulkan");

            await Task.CompletedTask;
        }
        );

        // Test 3: Detect version conflicts
        yield return ("Version No Conflict Detection", async () =>
        {
            var vulkanA = new LibraryDependency { name = "vulkan/vulkan", version = ">=1.2.0" };
            var vulkanB = new LibraryDependency { name = "vulkan/vulkan", version = "<1.3.0" };
            var libs = new LibraryDependency[] { vulkanA, vulkanB };

            var graph = new DependencyGraph(libs);

            var conflicts = graph.GetDependencyConflicts(false);
            if (conflicts.ContainsKey("vulkan/vulkan"))
                throw new Exception("Did not expect a conflict for vulkan/vulkan");

            await Task.CompletedTask;
        }
        );
        yield return ("Resolved Libraries Skips Conflicts", async () =>
        {
            // Library A: no conflict
            var libA1 = new LibraryDependency { name = "lib/A", version = ">=1.0.0" };
            var libA2 = new LibraryDependency { name = "lib/A", version = "<2.0.0" };

            // Library B: conflict
            var libB1 = new LibraryDependency { name = "lib/B", version = "=1.0.0" };
            var libB2 = new LibraryDependency { name = "lib/B", version = "=2.0.0" };

            var libraries = new[] { libA1, libA2, libB1, libB2 };
            var graph = new DependencyGraph(libraries);

            var resolved = graph.GetResolvedLibraries(); // function skips conflicts

            // Check that lib/A is included
            if (!resolved.Any(l => l.name == "lib/A"))
                throw new Exception("Expected lib/A to be included");

            // Check that lib/B is skipped due to conflict
            if (resolved.Any(l => l.name == "lib/B"))
                throw new Exception("Expected lib/B to be skipped due to conflict");

            await Task.CompletedTask;
        }
        );

    }
}
