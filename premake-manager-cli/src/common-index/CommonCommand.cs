using Spectre.Console;
using Spectre.Console.Cli;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace src.common_index
{
    /// <summary>
    /// Command to create a new index
    /// </summary>
    internal class CommonIndexCommand : AsyncCommand<CommonIndexCommand.Settings>
    {
        internal class Settings : CommandSettings
        {
            [CommandArgument(0, "[REMOTE]")]
            [Description("remote github url of the repo")]
            public string? remote { get; set; }
        }
        public override Task<int> ExecuteAsync(CommandContext context, Settings settings)
        {
            if (string.IsNullOrEmpty(settings.remote))
            {
                TextPrompt<string> prompt = new TextPrompt<string>("Enter the remote githubUrl:")
                     .Validate(input =>
                     {
                         if (string.IsNullOrWhiteSpace(input))
                             return ValidationResult.Error("Url cannot be empty");

                         if (!Uri.TryCreate(input, UriKind.Absolute, out Uri uri))
                             return ValidationResult.Error("Invalid url format");

                         if (!string.Equals(uri.Host, "github.com", StringComparison.OrdinalIgnoreCase))
                             return ValidationResult.Error("Url must be a github.com repository");

                         string[] segments = uri.AbsolutePath.Trim('/').Split('/');
                         if (segments.Length != 2)
                             return ValidationResult.Error("Url must be in the form https://github.com/owner/repo");

                         if (string.IsNullOrWhiteSpace(segments[0]) || string.IsNullOrWhiteSpace(segments[1]))
                             return ValidationResult.Error("Owner and repo must be non-empty");

                         return ValidationResult.Success();
                     });

                settings.remote = AnsiConsole.Prompt(prompt);
            }
            IndexView index = CommonIndex.CreateNew(settings.remote!);
            CommonIndex.WriteFileIndex(index, "premakeIndex.yml");
            AnsiConsole.MarkupLine("[green]✔ Index file created successfully[/]");
            return Task.FromResult(1);
        }
    }

    internal class CommonAddLibCommand : AsyncCommand<CommonAddLibCommand.Settings>
    {
        public override Task<int> ExecuteAsync(CommandContext context, Settings settings)
        {
            IndexView index = CommonIndex.ReadFileIndex();
            IndexLibrary library = new IndexLibrary() { name = settings.repo ?? string.Empty, description = settings.description ?? string.Empty };
            if (string.IsNullOrEmpty(settings.owner))
            {
                settings.owner = AnsiConsole.Prompt(new TextPrompt<string>("[green]owner: [/]"));
            }

            if (string.IsNullOrEmpty(settings.repo))
            {
                library.name = AnsiConsole.Prompt(new TextPrompt<string>("[green]repo: [/]"));
            }

            if (string.IsNullOrEmpty(settings.description))
            {
                library.description = AnsiConsole.Prompt(new TextPrompt<string>("[green]description: [/]"));
            }
            CommonIndex.CreateNewLibrary(ref index, library, settings.owner!);
            return Task.FromResult(0);
        }
        public override ValidationResult Validate([NotNull] CommandContext context, [NotNull] Settings settings)
        {
            if (!Path.Exists("premakeIndex.yml"))
            {
                return ValidationResult.Error("no existing index found!");
            }
            return ValidationResult.Success();
        }
        internal class Settings : CommandSettings
        {
            [CommandArgument(0, "[OWNER]")]
            [Description("the owner of the library (github name)")]
            public string? owner { get; set; }
            [CommandArgument(0, "[REPO]")]
            [Description("the name of the repo")]
            public string? repo { get; set; }

            [CommandArgument(0, "[DESRIPTION]")]
            [Description("the library desciption")]
            public string? description { get; set; }
        }
    }
}
