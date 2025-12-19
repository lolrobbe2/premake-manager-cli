using Spectre.Console;
using Spectre.Console.Cli;
using System;
using System.Collections.Generic;
using System.ComponentModel;
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
}
