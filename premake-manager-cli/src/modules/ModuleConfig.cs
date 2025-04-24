using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YamlDotNet.Serialization.NamingConventions;
using YamlDotNet.Serialization;
using Spectre.Console;
#nullable enable

namespace src.modules
{
    internal class ModuleConfig
    {
        public string? name { get; set; } = null;
        public string? description { get; set; } = null;
        public string? entryPoint { get; set; } = null;

        public ModuleConfig(string premakeModulePath)
        {
            string deserializedConfig = File.Exists(premakeModulePath) ? File.ReadAllText(premakeModulePath) : "";

            //If the desirialized config is not null or whitespace we want to read it.
            if (!string.IsNullOrEmpty(deserializedConfig))
            {
                var deserializer = new DeserializerBuilder()
                    .WithNamingConvention(CamelCaseNamingConvention.Instance)
                    .IgnoreUnmatchedProperties()
                    .Build();
                var tempInstance = deserializer.Deserialize<dynamic>(deserializedConfig);
                if (tempInstance != null)
                {
                    //extract version
                    name = tempInstance["name"];
                    description = tempInstance["description"];
                    entryPoint = tempInstance["entrypoint"];
                }
            }
        }

        public void PrintConfig()
        {
            var table = new Table()
                .Border(TableBorder.Rounded)
                .Title("[bold underline green]Module Configuration[/]")
                .AddColumn("[bold]Key[/]")
                .AddColumn("[bold]Value[/]");

            table.AddRow("Name", name ?? "[grey]Not specified[/]");
            table.AddRow("Description", description ?? "[grey]Not specified[/]");
            table.AddRow("Entry Point", entryPoint ?? "[grey]Not specified[/]");

            AnsiConsole.Write(table);
        }

    }
}
