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
using System.IO;
using src.utils;
#nullable enable
namespace src.version
{
    public class VersionListCommand : AsyncCommand<VersionListCommand.Settings>
    {
        public class Settings : CommandSettings
        {
            [CommandOption("--releases")]
            [Description("List available releases")]
            public bool ShowReleases { get; set; }

            [CommandOption("--installed")]
            [Description("List installed versions")]
            public bool ShowInstalled { get; set; }
        }
        // Execute the command
        public override async Task<int> ExecuteAsync(CommandContext context, Settings settings)
        {
            if (settings.ShowReleases)
                await ListReleases();
            if (settings.ShowInstalled)
                ListInstalled();

            return 0; // Return success code
        }
        private async Task<int> ListReleases()
        {
            IReadOnlyList<Release> releases = await VersionManager.GetVersions();
            var table = new Table();
            table.Border = TableBorder.Rounded;
            table.AddColumn("[bold yellow]Tag Name[/]");
            table.AddColumn("[bold green]Release Name[/]");
            table.AddColumn("[bold cyan]Published At[/]");

            // Add rows to the table
            foreach (var release in releases)
            {
                table.AddRow(
                    $"[yellow]{release.TagName}[/]",
                    $"[green]{release.Name ?? "No Name"}[/]",
                    $"[cyan]{release.PublishedAt?.ToString("yyyy-MM-dd") ?? "Unknown"}[/]"
                );
            }
            AnsiConsole.Write(table);
            return 0;
        }
        public void ListInstalled()
        {
            IList<string> installedVersions = VersionManager.GetPremakeInstalledVersions();
            if (installedVersions == null || installedVersions.Count == 0)
            {
                AnsiConsole.MarkupLine("[yellow]No installed versions found.[/]");
                return;
            }
            AnsiConsole.MarkupLine($"[grey] folder: {PathUtils.GetRoamingPath()}[/]");
            var table = new Table();
            table.Border = TableBorder.Rounded;
            table.AddColumn("[bold cyan]Installed Versions[/]");

            foreach (string version in installedVersions)
            {
                table.AddRow(Path.GetFileName(version));
            }

            AnsiConsole.Write(table);
        }
    }

    public class VersionInstallCommand : AsyncCommand<VersionInstallCommand.Settings>
    {
        // Define settings for the command
        public class Settings : CommandSettings
        {
            [CommandArgument(0, "[VERSION]")]
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
    public class VersionSetCommand : AsyncCommand<VersionSetCommand.Settings>
    {
        public class Settings : CommandSettings
        {
            [CommandArgument(0, "[VERSION]")]
            [Description("version to to set in the config and PATH env variable")]
            public required string name { get; set; }
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

        public override async Task<int> ExecuteAsync([NotNull] CommandContext context, [NotNull] Settings settings)
        {
            await VersionManager.SetVersion(settings.name);
            return 0;
        }

    }
    internal class VersionGetCurrent : AsyncCommand
    {
        public override async Task<int> ExecuteAsync(CommandContext context)
        {
            string? path = VersionManager.GetCurrentVersionPath();
            if (path is not null)
            {
                string[] splitPath = path.Split("/");
                AnsiConsole.MarkupLine($"current version: [green]{splitPath[splitPath.Length - 2]}[/]");
            } else {
                AnsiConsole.MarkupLine("[red] no version is currently set");
            }
                return 0;
        }
    }
}
