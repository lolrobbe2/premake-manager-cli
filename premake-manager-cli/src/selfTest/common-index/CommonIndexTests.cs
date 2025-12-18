using src.common_index;
using src.selfTest;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace src.selfTest.common_index
{
    internal class CommonIndexTests : ITestClass
    {
        public IEnumerable<(string TestName, Func<Task> Action)> GetTests()
        {
            yield return ("WriteStreamIndex produces non-empty stream with grouped libraries", async () =>
            {
                IndexView index = new IndexView
                {
                    remote = "https://example.com",
                    libraries = new Dictionary<string, IList<IndexLibrary>>
        {
            { "premake", new List<IndexLibrary> {
                new IndexLibrary { name = "premake-core", description = "Core premake functionality" },
                new IndexLibrary { name = "premake-utils" }
            }},
            { "community", new List<IndexLibrary> {
                new IndexLibrary { name = "imgui" },
                new IndexLibrary { name = "spdlog", description = "Fast logging library" }
            }}
        }
                };

                Stream stream = CommonIndex.WriteStreamIndex(index);

                if (stream == null)
                    throw new Exception("Stream is null");

                if (stream.Length == 0)
                    throw new Exception("Stream is empty");

                await Task.CompletedTask;
            }
            );

            yield return ("ReadStreamIndex correctly deserializes grouped libraries", async () =>
            {
                string yaml = @"
remote: https://example.com
libraries:
  premake:
    - name: premake-core
      description: Core premake functionality
    - name: premake-utils
  community:
    - name: imgui
    - name: spdlog
      description: Fast logging library
";

                using MemoryStream stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(yaml));

                IndexView index = CommonIndex.ReadStreamIndex(stream);

                if (index == null)
                    throw new Exception("Deserialized IndexView is null");

                if (index.remote != "https://example.com")
                    throw new Exception("Remote value mismatch");

                if (!index.libraries.ContainsKey("premake") || index.libraries["premake"].Count != 2)
                    throw new Exception("Premake libraries count mismatch");

                if (index.libraries["premake"][0].name != "premake-core" || index.libraries["premake"][0].description != "Core premake functionality")
                    throw new Exception("Premake-core library data mismatch");

                if (!index.libraries.ContainsKey("community") || index.libraries["community"].Count != 2)
                    throw new Exception("Community libraries count mismatch");

                if (index.libraries["community"][1].name != "spdlog" || index.libraries["community"][1].description != "Fast logging library")
                    throw new Exception("Community spdlog library data mismatch");

                await Task.CompletedTask;
            }
            );

        }
    }
}
