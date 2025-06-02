using Spectre.Console;
using Spectre.Console.Cli;
using src.config;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
#nullable enable
namespace src.modules
{
    internal class ModuleInfoCommand : AsyncCommand<ModuleInfoCommand.Settings>
    {
        public override async Task<int> ExecuteAsync(CommandContext context, Settings settings)
        {
            ModuleConfig config = await ModuleManager.GetModuleConfig(settings.githublink);
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
            [Description("The GitHub link of the module.")]
            public string githublink { get; set; } = "";
        }
    }
    internal class ModuleInstallCommand : AsyncCommand<ModuleInstallCommand.Settings>
    {
        internal class Settings : CommandSettings
        {
            [CommandArgument(0, "<githublink>")]
            [Description("The GitHub link of the module.")]
            public string githublink { get; set; } = "";

            [CommandArgument(1, "[version]")]
            [Description("The version of the module")]
            public string? version { get; set; }
        }
        public override async Task<int> ExecuteAsync(CommandContext context, Settings settings)
        {
            await ModuleManager.InstallModule(settings.githublink, settings.version);
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

    internal class ModuleAddCommand : AsyncCommand<ModuleAddCommand.Settings>
    {
        internal class Settings : CommandSettings
        {
            [CommandArgument(0, "<githublink>")]
            [Description("The GitHub link of the module or the owner/repo.")]
            public string githublink { get; set; } = "";

            [CommandArgument(1, "[version]")]
            [Description("The version of the module")]
            public string? version { get; set; }
        }

        public override async Task<int> ExecuteAsync(CommandContext context, Settings settings)
        {
            ConfigReader config = new ConfigReader();
            string[] moduleString = settings.githublink.Replace("https://github.com/", "").Split('/');
            await AnsiConsole.Status().StartAsync("Adding module", async ctx =>
            {
                ctx.Spinner(Spinner.Known.Aesthetic);
                ctx.SpinnerStyle(Style.Parse("green"));
                await ConfigWriter.FromReader(config).AddModule(new PremakeModule() { owner = moduleString[0], repo = moduleString[1], version = settings.version }).Write();

            });
            return 0;
        }
    }

    internal class ModuleRemoveCommand : AsyncCommand<ModuleRemoveCommand.Settings>
    {
        internal class Settings : CommandSettings
        {
            [CommandArgument(0, "[githublink]")]
            [Description("The GitHub link of the module or the owner/repo.")]
            public string? githublink { get; set; } = "";
        }

        public override ValidationResult Validate([NotNull] CommandContext context, [NotNull] Settings settings)
        {
            if (string.IsNullOrEmpty(settings.githublink)) {
                ConfigReader reader = new ConfigReader();
                IList<PremakeModule> modules = reader.modules.Values.ToList();
                string selectedLink = AnsiConsole.Prompt(
                    new SelectionPrompt<string>()
                       .Title("Select a [green]Module to remove[/]:")
                       .PageSize(10)
                       .AddChoices(modules.Select(m => m.getLink()))
                );
                settings.githublink = selectedLink;
            } else {
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
            ConfigReader config = new ConfigReader();
            string moduleString = settings.githublink!.Replace("https://github.com/", "");

            await AnsiConsole.Status().StartAsync("Removing Module", async ctx =>
            {
                ctx.Spinner(Spinner.Known.Aesthetic);
                ctx.SpinnerStyle(Style.Parse("green"));
                await ConfigWriter.FromReader(config).RemoveModule(moduleString).Write();
            });

            return 0;
        }
    }
}
