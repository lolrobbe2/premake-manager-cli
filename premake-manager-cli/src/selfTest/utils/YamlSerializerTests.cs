using src.utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace src.selfTest.utils
{
    internal sealed class TestModel
    {
        public required string Name { get; set; }
        public int Value { get; set; }
    }

    internal class YamlSerializerTests : ITestClass
    {
        public IEnumerable<(string TestName, Func<Task> Action)> GetTests()
        {
            // Test 1: Deserialize known YAML from stream
            yield return ("Deserialize known YAML from stream", async () =>
            {
                string yaml =
    @"name: test
value: 123
";

                byte[] data = System.Text.Encoding.UTF8.GetBytes(yaml);
                using MemoryStream stream = new MemoryStream(data);

                TestModel model = YamlSerializer.Deserialize<TestModel>(stream);

                if (model == null)
                    throw new Exception("Expected non-null model");

                if (model.Name != "test")
                    throw new Exception("Incorrect Name value");

                if (model.Value != 123)
                    throw new Exception("Incorrect Value value");

                await Task.CompletedTask;
            }
            );

            // Test 2: Serialize produces expected YAML fields
            yield return ("Serialize produces expected YAML fields", async () =>
            {
                TestModel model = new TestModel
                {
                    Name = "hello",
                    Value = 5
                };

                using Stream stream = YamlSerializer.Serialize(model);
                using StreamReader reader = new StreamReader(stream);

                string yaml = reader.ReadToEnd();

                if (!yaml.Contains("name: hello"))
                    throw new Exception("Serialized YAML missing name");

                if (!yaml.Contains("value: 5"))
                    throw new Exception("Serialized YAML missing value");

                await Task.CompletedTask;
            }
            );

            // Test 3: Deserialize from file
            yield return ("Deserialize YAML from file", async () =>
            {
                string yaml =
    @"name: file
value: 9
";

                string path = Path.Combine(Path.GetTempPath(), "yaml_test.yml");
                File.WriteAllText(path, yaml);

                TestModel model = YamlSerializer.Deserialize<TestModel>(path);

                if (model == null)
                    throw new Exception("Expected non-null model");

                if (model.Name != "file")
                    throw new Exception("Incorrect Name from file");

                if (model.Value != 9)
                    throw new Exception("Incorrect Value from file");

                File.Delete(path);

                await Task.CompletedTask;
            }
            );
        }
    }

}
