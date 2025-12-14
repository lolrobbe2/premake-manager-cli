using Spectre.Console;
using Spectre.Console.Cli;
using src.selfTest;
using src.selfTest.config;
using System.ComponentModel;
using System.Threading.Tasks;
namespace src.selfTest
{
    internal class SelfTestCommand : AsyncCommand
    {
        public async override Task<int> ExecuteAsync(CommandContext context)
        {
            var runner = new TestRunner();

            // Register all test classes
            runner.AddTestClass<DependencyGraphTests>();
            runner.AddTestClass<ConfigWriterTests>();

            await runner.RunAllAsync();
            return 0;
        }
    }

    internal class SelfTestGroupCommand : AsyncCommand<SelfTestGroupCommand.Settings>
    {
        public class Settings : CommandSettings
        {
            [CommandOption("-g|--group <GROUPNAME>")]
            [Description("The name of the test group to run")]
            public string GroupName { get; set; } = "";
        }

        public async override Task<int> ExecuteAsync(CommandContext context, Settings settings)
        {
            var runner = new TestRunner();

            // Register all test classes
            runner.AddTestClass<DependencyGraphTests>();
            runner.AddTestClass<ConfigWriterTests>();

            // Determine which group to run
            string groupToRun = settings.GroupName;

            if (string.IsNullOrWhiteSpace(groupToRun))
            {
                var allGroups = runner.GetAllGroupNames();
                if (allGroups.Length == 0)
                {
                    AnsiConsole.MarkupLine("[red]No test groups available to run.[/]");
                    return 1;
                }

                groupToRun = AnsiConsole.Prompt(
                    new SelectionPrompt<string>()
                        .Title("Select a test group to run:")
                        .PageSize(10)
                        .AddChoices(allGroups)
                );
            }

            await runner.RunGroupAsync(groupToRun);
            return 0;
        }
    }

}