using Spectre.Console;
using Spectre.Console.Cli;
using src.config;
using src.utils;
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
#if DEBUG
            bool interactive = true;
#else
            bool interactive = false;
#endif
            if (args.Length > 0 && args[0] == "--interactive")
                interactive = true;

            Console.CancelKeyPress += (sender, e) =>
            {
                PathUtils.ClearDirectory(PathUtils.GetTempPath());

            };
            AppDomain.CurrentDomain.ProcessExit += (sender, e) =>
            {
                PathUtils.ClearDirectory(PathUtils.GetTempPath());
            };

            Console.OutputEncoding = System.Text.Encoding.UTF8;

            var app = new CommandApp();
            app.Configure(config =>
            {
                config.CaseSensitivity(CaseSensitivity.None);
                config.SetApplicationName("premake manager");
                config.AddCommand<Info>("info");
                config.AddCommand<ConfigCommand>("configure");
                config.AddBranch("config", branch =>
                {
                    branch.SetDescription("manage premakeConfig");
                    branch.AddCommand<ConfigSetVersionCommand>("version");
                    branch.AddCommand<ConfigViewCommand>("view");
                });
                config.AddBranch("version", branch =>
                {
                    branch.SetDescription("Manage premake versions");
                    // Add commands under the "version" branch
                    branch.AddCommand<VersionListCommand>("list")
                        .WithDescription("Lists all available versions.")
                        .WithExample(new[] { "version", "list" });

                    branch.AddCommand<VersionInstallCommand>("install")
                        .WithDescription("Installs the version");

                    branch.AddCommand<VersionSetCommand>("set")
                        .WithDescription("set the premake version");
                });
                config.AddBranch("workspace", branch =>
                {
                    branch.SetDescription("Manage premake workspaces");
                    branch.AddCommand<workspace.WorkspaceCreateCommand>("new")
                          .WithDescription("Create a new workspace with acompanying projects");
                });

                config.AddBranch("module", branch =>
                {
                    branch.SetDescription("Manage premake modules");
                    branch.AddCommand<modules.ModuleInfoCommand>("info")
                          .WithDescription("get the info from a module");

                    branch.AddCommand<modules.ModuleInstallCommand>("install")
                          .WithDescription("install a module given its github link");

                    branch.AddCommand<modules.ModuleAddCommand>("add")
                          .WithDescription("add a module to the confiuration");

                    branch.AddCommand<modules.ModuleRemoveCommand>("remove");
                });

                config.AddBranch("test", branch =>
                {
                    branch.SetDescription("The Premake Manager selftest commands");
                    branch.AddCommand<selfTest.SelfTestCommand>("all")
                        .WithDescription("run all the registered self tests");

                    branch.AddCommand<selfTest.SelfTestGroupCommand>("group").WithDescription("run all the test in a certain group");
                });
                config.AddBranch("index", branch => {
                    branch.SetDescription("All commands for managing the common index");
                    branch.AddCommand<common_index.CommonIndexCommand>("new");

                    branch.AddBranch("add", branch => {
                        branch.AddCommand<common_index.CommonAddLibCommand>("library").WithDescription("Add a library to the local index");
                        branch.AddBranch("uri", uriBranch =>
                        {
                            uriBranch.AddCommand<common_index.CommonAddUriLibCommand>("library").WithDescription("Add a library from a github uri to the local index");
                        });
                    });
                });
                config.AddBranch("remotes", branch => {
                    branch.SetDescription("All commands for managing the localy used remotes");
                    branch.AddCommand<common_index.RemotesViewCommand>("view")
                          .WithDescription("Show all the local remotes");
                    branch.AddCommand<common_index.RemotesAddCommand>("add")
                          .WithDescription("Add a new local remote");
                    branch.AddCommand<common_index.RemotesUpdateCommand>("update")
                        .WithDescription("Update outdated remotes");
                });
            });

            bool running = true;

            if (!interactive)
                await app.RunAsync(args).ConfigureAwait(false);
            else
            {
                IList<string> choices = new List<string>();
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
                        choices.Add(input);
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
}
