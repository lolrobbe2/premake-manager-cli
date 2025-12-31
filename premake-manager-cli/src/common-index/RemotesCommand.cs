using Octokit;
using Spectre.Console;
using Spectre.Console.Cli;
using src.version;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace src.common_index
{
    /// <summary>
    /// This command shows all the remotes
    /// </summary>
    internal class RemotesViewCommand : AsyncCommand
    {
        public override Task<int> ExecuteAsync(CommandContext context)
        {
            Table table = new Table()
             .Border(TableBorder.Rounded)
             .Title("[bold]Configured Remotes[/]");

            table.AddColumn(new TableColumn("Owner").LeftAligned());
            table.AddColumn(new TableColumn("Repository").LeftAligned());
            table.AddColumn(new TableColumn("Enabled").Centered());

            foreach (Remote item in RemotesManager.Remotes)
            {
                string enabledText = item.Enabled
                    ? "[green]Yes[/]"
                    : "[red]No[/]";

                string ownerLink =
                    $"[link=https://github.com/{item.Owner}]{item.Owner}[/]";

                string repoLink =
                    $"[link=https://github.com/{item.Owner}/{item.Repo}]{item.Repo}[/]";


                table.AddRow(
                    ownerLink,
                    repoLink,
                    enabledText
                );
            }

            AnsiConsole.Write(table);
            return Task.FromResult(0);
        }
    }

    internal class RemotesAddCommand : AsyncCommand<RemotesAddCommand.Settings>
    {
        public override async Task<int> ExecuteAsync(CommandContext context, Settings settings)
        {
            await RemotesManager.AddRemote(settings.owner!, settings.repo!);
            AnsiConsole.MarkupLine($"[green] Added new remote: {settings.owner}/{settings.repo}[/]");
            return 0;
        }
        public override ValidationResult Validate([NotNull] CommandContext context, [NotNull] Settings settings)
        {
            if (string.IsNullOrEmpty(settings.owner))
            {
                settings.owner = AnsiConsole.Prompt(new TextPrompt<string>("[green]owner: [/]"));
            }

            if (string.IsNullOrEmpty(settings.repo))
            {
                settings.repo = AnsiConsole.Prompt(new TextPrompt<string>("[green]repo: [/]"));
            }
            return ValidationResult.Success();
        }
        internal class Settings : CommandSettings
        {
            [CommandArgument(0, "[OWNER]")]
            [Description("owner of the repo")]
            public string? owner { get; set; }

            [CommandArgument(0, "[REPO]")]
            [Description("name of the repo")]
            public string? repo { get; set; }
        }
    }

    internal class RemotesRemoveCommand : AsyncCommand<RemotesRemoveCommand.Settings>
    {
        public override async Task<int> ExecuteAsync(CommandContext context, Settings settings)
        {
            await RemotesManager.RemoveRemote(settings.owner!, settings.repo!);
            AnsiConsole.MarkupLine($"[green] Remove remote: {settings.owner}/{settings.repo}[/]");
            return 0;
        }
        public override ValidationResult Validate([NotNull] CommandContext context, [NotNull] Settings settings)
        {
            if (string.IsNullOrEmpty(settings.owner))
            {
                settings.owner = AnsiConsole.Prompt(new TextPrompt<string>("[green]owner: [/]"));
            }

            if (string.IsNullOrEmpty(settings.repo))
            {
                settings.repo = AnsiConsole.Prompt(new TextPrompt<string>("[green]repo: [/]"));
            }
            return ValidationResult.Success();
        }
        internal class Settings : CommandSettings
        {
            [CommandArgument(0, "[OWNER]")]
            [Description("owner of the repo")]
            public string? owner { get; set; }

            [CommandArgument(0, "[REPO]")]
            [Description("name of the repo")]
            public string? repo { get; set; }
        }
    }
}
