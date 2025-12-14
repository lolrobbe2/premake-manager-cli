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
            if (string.IsNullOrWhiteSpace(settings.GroupName))
            {
                AnsiConsole.MarkupLine("[red]Please provide a group name using -g or --group[/]");
                return 1;
            }

            var runner = new TestRunner();

            // Register all test classes
            runner.AddTestClass<DependencyGraphTests>();
            runner.AddTestClass<ConfigWriterTests>();

            await runner.RunGroupAsync(settings.GroupName);
            return 0;
        }
    }
}