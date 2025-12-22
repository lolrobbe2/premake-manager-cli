using Octokit;
using Spectre.Console;
using Spectre.Console.Cli;
using src.modules;
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
    internal class ConfigCommand : AsyncCommand
    {
        public async override Task<int> ExecuteAsync(CommandContext context)
        {
            Config config = ConfigManager.HasConfig() ? ConfigManager.ReadConfig() : new Config();

            //TODO should we auto install the correct version of premake?
            await VersionManager.SetVersion(config.Version);
            if(config.Modules != null)
                await ModuleManager.InstallModules(config.Modules.Values.ToList());
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

            var mainTable = new Table()
             .AddColumn("Owner")
             .AddColumn("Modules");
            mainTable.Border = TableBorder.Rounded;
            mainTable.ShowRowSeparators = true;

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
                    mainTable.AddRow(new Markup($"[bold green][link=https://github.com/{group.Key}] {group.Key}[/][/]"), subTable);
                }
                AnsiConsole.Write(mainTable);  // Render the main table with the owner row
               
            }
            return 0;
        }
    }
}
