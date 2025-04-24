using Spectre.Console;
using Spectre.Console.Cli;
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
            await ModuleManager.InstallModule(settings.githublink,settings.version);
            return 0;
        }
    }
}
