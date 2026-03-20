using Octokit;
using Spectre.Console;
using Spectre.Console.Cli;
using src.dependencies;
using src.dependencies.graph;
using src.libraries;
using src.modules;
using src.utils;
using src.version;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YamlDotNet.Serialization;

namespace src.config
{
    /**
     * this commands intializes the workspace and fetches the libraries and modules
     */
    internal class ConfigCommand : AsyncCommand
    {
        public async override Task<int> ExecuteAsync(CommandContext context)
        {
            Config config = ConfigManager.HasConfig() ? ConfigManager.ReadConfig() : new Config();

            PremakeLibrary[] libraries = [];
            PremakeModule[] modules = [];

            //TODO should we auto install the correct version of premake?
            await VersionManager.SetVersion(config.Version);
            if (config.Modules != null)
            {
                AnsiConsole.WriteLine("aquiring graph");
                modules = config.Modules.Values.ToArray();
                await ModuleManager.InstallModules(config.Modules.Values.ToList());
            }
            //TODO dependencies
            if(config.Libraries != null)
            {
                AnsiConsole.WriteLine("aquiring graph");
                DependencyGraph graph = await DependenciesManager.GetDependencyGraph(config.Libraries.Values.ToList());
                var libs = (await DependenciesManager.GetVersionsFromGraph(graph)).Distinct()
                    .ToList();

                libraries = libs.ToArray();
                await LibraryManager.InstallLibraries(libs.ToList());
            }
            PremakeSystemWriter.Write(config, libraries, modules);
            return 0;
        }
    }

    internal class ConfigSetVersionCommand : AsyncCommand<ConfigSetVersionCommand.Settings>
    {
        internal class Settings : CommandSettings
        {
            [CommandArgument(0, "[VERSION]")]
            [Description("version to install")]
            public required string version { get; set; }
        }
        public override async Task<int> ExecuteAsync(CommandContext context, Settings settings)
        {
            await VersionManager.SetVersion(settings.version);
            Config config = ConfigManager.HasConfig() ? ConfigManager.ReadConfig() : new Config();

            config.Version = settings.version;
            ConfigManager.WriteConfig(config,null);
            return 0;
        }

        public override ValidationResult Validate([NotNull] CommandContext context, [NotNull] Settings settings)
        {
            IReadOnlyList<Release> releases = VersionManager.GetVersions().ConfigureAwait(true).GetAwaiter().GetResult();
            Release? release = null;
            if (settings.version == null)
            {

                string selectedTag = AnsiConsole.Prompt(
                    new SelectionPrompt<string>()
                       .Title("Select a [green]Premake version[/]:")
                       .PageSize(10)
                       .AddChoices(releases.Select(r => r.TagName))
                );

                settings.version = selectedTag;
            }
            release = releases.FirstOrDefault(release => release.TagName.Equals(settings.version));
            if (release == null)
                return ValidationResult.Error($"Release with tag '{settings.version}' was not found. Please provide a valid release tag.");
            return ValidationResult.Success();
        }
    }

    internal class ConfigViewCommand : AsyncCommand
    {
        public async override Task<int> ExecuteAsync(CommandContext context)
        {
            Config config = ConfigManager.HasConfig() ? ConfigManager.ReadConfig() : new Config();

            var modulesTable = new Table()
             .AddColumn("Owner")
             .AddColumn("Modules");
            modulesTable.Border = TableBorder.Rounded;
            modulesTable.ShowRowSeparators = true;

            if (config.Modules != null)
            {
                var groupedModules = config.Modules
                    .GroupBy(m => m.Value.owner)
                    .OrderBy(g => g.Key);

                foreach (var group in groupedModules)
                {

                    // Create the subtable for the current owner
                    var subTable = new Table()
                        .AddColumn("Repo")
                        .AddColumn("Version");
                    subTable.Border = TableBorder.Rounded;
                    subTable.ShowRowSeparators = true;
                    // Add rows to the subtable for each module of the owner$


                    foreach (var module in group)
                    {
                        subTable.AddRow($"[link=https://github.com/{group.Key}/{module.Value.repo}] {module.Value.repo}[/]", module.Value.version);
                    }


                    // Add a row in the main table for the owner, with the subtable as its content
                    modulesTable.AddRow(new Markup($"[bold green][link=https://github.com/{group.Key}] {group.Key}[/][/]"), subTable);
                }
                AnsiConsole.Write(modulesTable);  // Render the main table with the owner row

            }

            var librariesTable = new Table()
              .AddColumn("Owner")
              .AddColumn("Libraries");
            librariesTable.Border = TableBorder.Rounded;
            librariesTable.ShowRowSeparators = true;
            if (config.Libraries != null)
            {
                var groupedLibraries = config.Libraries
                    .GroupBy(m => m.Value.owner)
                    .OrderBy(g => g.Key);

                foreach (var group in groupedLibraries)
                {

                    // Create the subtable for the current owner
                    var subTable = new Table()
                        .AddColumn("Repo")
                        .AddColumn("Version");
                    subTable.Border = TableBorder.Rounded;
                    subTable.ShowRowSeparators = true;
                    // Add rows to the subtable for each module of the owner$


                    foreach (var library in group)
                    {
                        subTable.AddRow($"[link=https://github.com/{group.Key}/{library.Value.repo}] {library.Value.repo}[/]", library.Value.version);
                    }


                    // Add a row in the main table for the owner, with the subtable as its content
                    librariesTable.AddRow(new Markup($"[bold green][link=https://github.com/{group.Key}] {group.Key}[/][/]"), subTable);
                }
                AnsiConsole.Write(librariesTable);  // Render the main table with the owner row

            }
            return 0;
        }
    }
}
