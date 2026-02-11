using Spectre.Console;
using Spectre.Console.Cli;
using src.config;
using src.dependencies;
using src.dependencies.types;
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

                         if (!Uri.TryCreate(input, UriKind.Absolute, out Uri? uri))
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
            CommonIndex.WriteFileIndex(index);
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
    internal class CommonAddUriLibCommand : AsyncCommand<CommonAddUriLibCommand.Settings>
    {
        internal class Settings : CommandSettings
        {
            [CommandArgument(0, "<githublink>")]
            [Description("The GitHub link of the library or the owner/repo.")]
            public string githublink { get; set; } = "";
        }

        public override ValidationResult Validate([NotNull] CommandContext context, [NotNull] Settings settings)
        {

            if (!settings.githublink.StartsWith("https://github.com/"))
                return ValidationResult.Error("the link should start with https://github.com/");
            GithubRepo repo = Github.GetRepoFromLink(settings.githublink);
            if (string.IsNullOrEmpty(repo.owner))
                return ValidationResult.Error("the repo owner name should be valid");

            if (string.IsNullOrEmpty(repo.name))
                return ValidationResult.Error("the repo name name should be valid");


            return ValidationResult.Success();

        }

        public override async Task<int> ExecuteAsync(CommandContext context, Settings settings)
        {
            IndexView index = CommonIndex.ReadFileIndex();
            GithubRepo libRepo = Github.GetRepoFromLink(settings.githublink);
            string description = await Github.GetDescription(libRepo);
            IndexLibrary library = new IndexLibrary() { name = libRepo.name, description = description };

            CommonIndex.CreateNewLibrary(ref index, library, libRepo.owner);
            CommonIndex.WriteFileIndex(index);
            return 0;
        }
    }

    internal class CommonAddDependencyCommand : AsyncCommand<CommonAddDependencyCommand.Settings>
    {
        internal class Settings : CommandSettings
        {
            [CommandArgument(0, "[githublink]")]
            [Description("The GitHub link of the library or the owner/repo.")]
            public string? githublink { get; set; } = "";
            [CommandArgument(0, "[OWNER]")]
            [Description("the owner of the dependency (github name)")]
            public string? owner { get; set; }
            [CommandArgument(0, "[REPO]")]
            [Description("the name of the dependency")]
            public string? repo { get; set; }

            [CommandArgument(0, "[RANGE]")]
            [Description("the version range of the dependency")]
            public string? range { get; set; }
        }

        public override ValidationResult Validate([NotNull] CommandContext context, [NotNull] Settings settings)
        {
            if(string.IsNullOrEmpty(settings.githublink))
            {
                settings.githublink = AnsiConsole.Prompt(new TextPrompt<string>("[green]library githublink: [/]"));
            }
            if (settings.githublink!.StartsWith("https://github.com/"))
                return ValidationResult.Error("the link should start with https://github.com/");
            GithubRepo repo = Github.GetRepoFromLink(settings.githublink);
            if (string.IsNullOrEmpty(repo.owner))
                return ValidationResult.Error("the repo owner name should be valid");

            if (string.IsNullOrEmpty(repo.name))
                return ValidationResult.Error("the repo name name should be valid");

            if (string.IsNullOrEmpty(settings.owner))
            {
                settings.owner = AnsiConsole.Prompt(new TextPrompt<string>("[green]owner: [/]"));
            }
            if (string.IsNullOrEmpty(settings.repo))
            {
                settings.owner = AnsiConsole.Prompt(new TextPrompt<string>("[green]repo: [/]"));
            }
            if (string.IsNullOrEmpty(settings.range))
            {
                settings.range = AnsiConsole.Prompt(new TextPrompt<string>("[green]range: [/]"));
            }
            return ValidationResult.Success();

        }

        public override async Task<int> ExecuteAsync(CommandContext context, Settings settings)
        {
            GithubRepo libRepo = Github.GetRepoFromLink(settings.githublink!);
            await DependenciesManager.AddLibraryDependency(libRepo, new LibraryDependency { name = $"{settings.owner}/{settings.repo}", version = settings.range! });
            return 0;
        }
    }
}
