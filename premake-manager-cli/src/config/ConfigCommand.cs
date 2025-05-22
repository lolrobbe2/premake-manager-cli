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
            ConfigReader config = new ConfigReader();
            await VersionManager.SetVersion(config.version);
            await ModuleManager.InstallModules(config.modules.Values.ToList());
            return 0;
        }
    }

    internal class ConfigSetVersionCommand : AsyncCommand<ConfigSetVersionCommand.Settings>
    {
        internal class Settings : CommandSettings
        {
            [CommandArgument(0, "[VERSION]")]
            [Description("version to install")]
            public required string name { get; set; }
        }
        public override async Task<int> ExecuteAsync(CommandContext context, Settings settings)
        {
            await VersionManager.SetVersion(settings.name);
            return 0;
        }

        public override ValidationResult Validate([NotNull] CommandContext context, [NotNull] Settings settings)
        {
            IReadOnlyList<Release> releases = VersionManager.GetVersions().ConfigureAwait(true).GetAwaiter().GetResult();
            Release? release = null;
            if (settings.name == null)
            {

                string selectedTag = AnsiConsole.Prompt(
                    new SelectionPrompt<string>()
                       .Title("Select a [green]Premake version[/]:")
                       .PageSize(10)
                       .AddChoices(releases.Select(r => r.TagName))
                );

                settings.name = selectedTag;
            }
            release = releases.FirstOrDefault(release => release.TagName.Equals(settings.name));
            if (release == null)
                return ValidationResult.Error($"Release with tag '{settings.name}' was not found. Please provide a valid release tag.");
            return ValidationResult.Success();
        }
    }

    internal class ConfigViewCommand : AsyncCommand
    {
        public async override Task<int> ExecuteAsync(CommandContext context)
        {
            ConfigReader config = new ConfigReader();

            var mainTable = new Table()
             .AddColumn("Owner")
             .AddColumn("Modules");
            mainTable.Border = TableBorder.Rounded;
            mainTable.ShowRowSeparators = true;

            var groupedModules = config.modules
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
                mainTable.AddRow(new Markup($"[bold green][link=https://github.com/{group.Key}] {group.Key}[/][/]"),subTable);
            }
            AnsiConsole.Write(mainTable);  // Render the main table with the owner row
            return 0;
        }
    }
}
