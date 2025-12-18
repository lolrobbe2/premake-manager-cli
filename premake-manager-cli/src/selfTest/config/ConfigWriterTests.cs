using src.selfTest;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace src.selfTest.config
{
    internal class ConfigWriterTests : ITestClass
    {
        public IEnumerable<(string TestName, Func<Task> Action)> GetTests()
        {
            yield return ("Modules non null default constructor",async () => {
                var writer = new src.config.ConfigWriter();
                if(writer.modules == null)
                {
                    throw new Exception("Modules cannot be null");
                }
            });

            yield return ("SetVersion", async () => {
                var writer = new src.config.ConfigWriter();
                string version = "v5.0.0-beta7";
                writer.SetVersion(version);
                if(writer.version != version)
                {
                    throw new Exception($"set version should be equal to {version}");
                }
            }
            );

            yield return ("AddModule", async () => {

                var module = new modules.PremakeModule(">=3.3.0", "glfw");

                var writer = new src.config.ConfigWriter()
                           .AddModule(module);
                if (writer.modules[module.module!].module != module.module && writer.modules[module.module!].version != module.version)
                {
                    throw new Exception($"the added modules parameters should match");
                }
            }
            );

            yield return ("Write Config File Test", async () =>
            {
                var tempPath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "selftest_config");
                System.IO.Directory.CreateDirectory(tempPath);

                var writer = new src.config.ConfigWriter()
                    .SetVersion("1.0.0")
                    .AddModule(new src.modules.PremakeModule { module = "glfw", version = ">=3.3.0" });

                await writer.Write(tempPath);

                var filePath = System.IO.Path.Combine(tempPath, "premakeConfig.yml");
                TestRunner.AssertFileExists(filePath);

                System.IO.File.Delete(filePath);
            }
            );
        }
    }
}
