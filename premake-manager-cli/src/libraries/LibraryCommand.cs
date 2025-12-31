using Spectre.Console;
using Spectre.Console.Cli;
using src.config;
using src.modules;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace src.libraries
{
    internal class LibraryInfoCommand : AsyncCommand<LibraryInfoCommand.Settings>
    {
        public override async Task<int> ExecuteAsync(CommandContext context, Settings settings)
        {
            LibraryConfig config = await LibraryManager.GetLibraryConfig(settings.githublink);
            config.PrintConfig();
            return 0;
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
        internal class Settings : CommandSettings
        {
            [CommandArgument(0, "<githublink>")]
            [Description("The GitHub link of the library.")]
            public string githublink { get; set; } = "";
        }
    }

    internal class LibraryInstallCommand : AsyncCommand<LibraryInstallCommand.Settings>
    {
        internal class Settings : CommandSettings
        {
            [CommandArgument(0, "<githublink>")]
            [Description("The GitHub link of the library.")]
            public string githublink { get; set; } = "";

            [CommandArgument(1, "[version]")]
            [Description("The version of the library")]
            public string? version { get; set; }
        }
        public override async Task<int> ExecuteAsync(CommandContext context, Settings settings)
        {
            await LibraryManager.InstallLibrary(settings.githublink, settings.version);
            return 0;
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
    }

    internal class LibraryAddCommand : AsyncCommand<LibraryAddCommand.Settings>
    {
        internal class Settings : CommandSettings
        {
            [CommandArgument(0, "<githublink>")]
            [Description("The GitHub link of the library or the owner/repo.")]
            public string githublink { get; set; } = "";

            [CommandArgument(1, "[version]")]
            [Description("The version of the library")]
            public string? version { get; set; }
        }

        public override async Task<int> ExecuteAsync(CommandContext context, Settings settings)
        {
            Config config = ConfigManager.HasConfig() ? ConfigManager.ReadConfig() : new Config();
            
            string[] libraryString = settings.githublink.Replace("https://github.com/", "").Split('/');
            await AnsiConsole.Status().StartAsync("Adding library", async ctx =>
            {
                ctx.Spinner(Spinner.Known.Aesthetic);
                ctx.SpinnerStyle(Style.Parse("green"));
                
                config.AddLibrary(new PremakeLibrary() { owner = libraryString[0], repo = libraryString[1], version = settings.version });
            });
            ConfigManager.WriteConfig(config,null);
            return 0;
        }
    }

    internal class LibraryRemoveCommand : AsyncCommand<LibraryRemoveCommand.Settings>
    {
        internal class Settings : CommandSettings
        {
            [CommandArgument(0, "[githublink]")]
            [Description("The GitHub link of the library or the owner/repo.")]
            public string? githublink { get; set; } = "";
        }

        [RequiresUnreferencedCode("Calls src.config.ConfigReader.ConfigReader(String)")]
        public override ValidationResult Validate([NotNull] CommandContext context, [NotNull] Settings settings)
        {
            if (string.IsNullOrEmpty(settings.githublink))
            {
                Config config = ConfigManager.HasConfig() ? ConfigManager.ReadConfig() : new Config();

                if (config.Libraries != null)
                {
                    string selectedLink = AnsiConsole.Prompt(
                        new SelectionPrompt<string>()
                           .Title("Select a [green]Library to remove[/]:")
                           .PageSize(10)
                           .AddChoices(config.Libraries.Values.Select(m => m.getLink()))
                    );
                    settings.githublink = selectedLink;
                } else {
                    AnsiConsole.MarkupLine("[red] No libraries to remove [/]");
                }
            }
            else
            {
                if (!settings.githublink.StartsWith("https://github.com/"))
                    return ValidationResult.Error("the link should start with https://github.com/");
                GithubRepo repo = Github.GetRepoFromLink(settings.githublink);
                if (string.IsNullOrEmpty(repo.owner))
                    return ValidationResult.Error("the repo owner name should be valid");

                if (string.IsNullOrEmpty(repo.name))
                    return ValidationResult.Error("the repo name name should be valid");

            }
            return ValidationResult.Success();

        }

        public override async Task<int> ExecuteAsync(CommandContext context, Settings settings)
        {
            Config config = ConfigManager.HasConfig() ? ConfigManager.ReadConfig() : new Config();

            string libraryString = settings.githublink!.Replace("https://github.com/", "");

            await AnsiConsole.Status().StartAsync("Removing Library", async ctx =>
            {
                ctx.Spinner(Spinner.Known.Aesthetic);
                ctx.SpinnerStyle(Style.Parse("green"));
                config.RemoveLibrary(libraryString);
            });

            ConfigManager.WriteConfig(config, null);
            return 0;
        }
    }
}
