using Spectre.Console;
using Spectre.Console.Cli;
using src.version;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace src
{
    public class main
    {
        static async Task Main(string[] args)
        {
            var app = new CommandApp();
            app.Configure(config =>
            {
                config.CaseSensitivity(CaseSensitivity.None);
                config.SetApplicationName("premake manager");
                config.AddCommand<Info>("info");
                config.AddBranch("version", branch =>
                {
                    branch.SetDescription("Manage premake versions");
                    // Add commands under the "version" branch
                    branch.AddCommand<VersionListCommand>("list")
                        .WithDescription("Lists all available versions.")
                        .WithExample(new[] { "version", "list" });

                    branch.AddCommand<VersionInstallCommand>("install")
                        .WithDescription("Installs the version");
                });
                
            });

            bool running = true;
            while (running)
            {
                try
                {
                    // Prompt the user to enter a command
                    var input = AnsiConsole.Prompt(
                        new TextPrompt<string>("Enter a [green]command[/]:"));
                    
                    // Exit the loop if the user types "exit"
                    if (input == "exit")
                    {
                        running = false;
                        AnsiConsole.MarkupLine("[bold red]Exiting...[/]");
                        break;
                    }

                    // Run the command
                    await app.RunAsync(input.Split(" ")).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    AnsiConsole.MarkupLine($"[bold red]Error:[/] {ex.Message}");
                }
            }
        }
    }
}
