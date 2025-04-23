using Spectre.Console;
using Spectre.Console.Cli;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YamlDotNet.Serialization;
#nullable enable
namespace src.workspace
{
    internal class WorkspaceCreateCommand : AsyncCommand<WorkspaceCreateCommand.Settings>
    {
        public class Settings : CommandSettings
        {
            [CommandOption("--workspace <NAME>")]
            [Description("Name of the workspace. Use multiple times for multiple workspaces.")]
            public List<string>? workspaces { get; set; }

            [CommandOption("--config <NAME|CONFIGS>")]
            [Description("Comma-separated list of configurations. Applies to the last defined workspace.")]
            public List<string>? configurations { get; set; }

            [CommandOption("--project <NAME|PROJECT_NAME|LOCATION|LANGUAGE>")]
            [Description("Define a project in the format Name:Project_Name:Location:Language. Applies to the last defined workspace.")]
            public List<string>? projects { get; set; } 
        }

        public override async Task<int> ExecuteAsync(CommandContext context, Settings settings)
        {
            if (settings.workspaces != null && settings.configurations != null || settings.projects != null)
                return await RunFromSettings(settings);
            else 
                return await RunInteractive();
        }

        public Task<int> RunFromSettings(Settings settings)
        {
            WorkspaceBuilder builder;

            foreach (var workspaceName in settings.workspaces!)
            {
               
                builder = new WorkspaceBuilder()
                    .StartWorkspace()
                    .SetName(workspaceName);

                IList<string> configurations = settings.configurations!.Where(config => config.StartsWith(workspaceName)).Select(config => config.Split("|").Last()).ToList();
                IList<string> projects = settings.configurations!.Where(config => config.StartsWith(workspaceName)).Select(config => config.Split("|").Last()).ToList();
                foreach (string config in configurations)
                    builder.AddConfiguration(config);

                foreach (string projectStr in projects)
                {
                    IList<string> project = projectStr.Split("|");

                    string name = project[1];
                    string location = project[2];
                    string language = project[3];

                    builder.AddProject(new ProjectBuilder()
                        .SetName(name)
                        .SetLocation(location)
                        .SetLanguage(language)
                        .Build()
                        );
                }
            }
            return Task.FromResult(0);
        }
        public async Task<int> RunInteractive()
        {
            var builder = new WorkspaceBuilder();
            bool addMore = true;

            while (addMore)
            {
             

                AnsiConsole.Write(new Rule($"[grey]({builder.workspaceCount})[/] [green]New Workspace [/]").RuleStyle("grey").Centered());
                
                string name = AnsiConsole.Ask<string>("[blue]Workspace name[/]:");
                builder.StartWorkspace()
                       .SetName(name);

                AnsiConsole.MarkupLine("[blue]Enter configurations one at a time. Leave blank to finish.[/]");
                while (true)
                {
                    string? config = AnsiConsole.Prompt(new TextPrompt<string>(" - [blue]Configuration[/]:").AllowEmpty());
                    if (string.IsNullOrEmpty(config))
                        break;

                    builder.AddConfiguration(config.Trim());
                }

                bool addProject = true;
                while (addProject)
                {
                    AnsiConsole.Write(new Rule($"[red]({builder.currentProjectCount + 1})[/][green]WorkSpace({name})[/] [grey]New Project:[/]").RuleStyle("grey").LeftJustified());
                    string projectName = AnsiConsole.Ask<string>(" - [blue]Name[/]:");
                    string projectLocation = AnsiConsole.Ask<string>(" - [blue]Location[/]:");
                    string projectLanguage = AnsiConsole.Prompt(
                        new SelectionPrompt<string>()
                            .Title(" - [blue]Language[/]:")
                            .AddChoices(new[] { "C", "C++", "C#", "F#" })
                    );
                    AnsiConsole.MarkupLine($" - [blue]Language[/]: {projectLanguage}");
                    builder.AddProject(new ProjectBuilder()
                        .SetName(projectName)
                        .SetLocation(projectLocation)
                        .SetLanguage(projectLanguage)
                        .Build());

                    addProject = AnsiConsole.Confirm("Add another project?", false);
                }

                builder.EndWorkspace();
                addMore = AnsiConsole.Confirm("Add another workspace?", false);
            }
            builder.Build();

            return await Task.FromResult(0);
        }
    }
}
