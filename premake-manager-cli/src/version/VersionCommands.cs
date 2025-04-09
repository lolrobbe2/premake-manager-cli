using Spectre.Console;
using Spectre.Console.Cli;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Octokit;
using System.ComponentModel;
#nullable enable
namespace src.version
{
    public class VersionListCommand : AsyncCommand
    {
        // Execute the command
        public override async Task<int> ExecuteAsync(CommandContext context)
        {
            IReadOnlyList<Release> releases = await VersionManager.GetVersions();
            var table = new Table();
            table.Border = TableBorder.Rounded;
            table.AddColumn("[bold yellow]Tag Name[/]");
            table.AddColumn("[bold green]Release Name[/]");
            table.AddColumn("[bold blue]Published At[/]");

            // Add rows to the table
            foreach (var release in releases)
            {
                table.AddRow(
                    $"[yellow]{release.TagName}[/]",
                    $"[green]{release.Name ?? "No Name"}[/]",
                    $"[blue]{release.PublishedAt?.ToString("yyyy-MM-dd") ?? "Unknown"}[/]"
                );
            }
            AnsiConsole.Write(table);
            return 0; // Return success code
        }
    }

    public class VersionInstallCommand : AsyncCommand<VersionInstallCommand.Settings>
    {
        // Define settings for the command
        public class Settings : CommandSettings
        {
            [CommandArgument(0,"[VERSION]")]
            [Description("version to install")]
            public required string name { get; set; }
        }
        public override async Task<int> ExecuteAsync([NotNull] CommandContext context, [NotNull] Settings settings)
        {
            await VersionManager.InstallRelease(settings.name);
            return 0;
        }
        public override ValidationResult Validate([NotNull] CommandContext context, [NotNull] Settings settings)
        {

            IReadOnlyList<Release> releases = VersionManager.GetVersions().ConfigureAwait(true).GetAwaiter().GetResult();
            Release? release = releases.FirstOrDefault(release => release.TagName.Equals(settings.name));
            if (release == null)
                return ValidationResult.Error($"Release with tag '{settings.name}' was not found. Please provide a valid release tag.");
            return ValidationResult.Success();
        }
    }
}
