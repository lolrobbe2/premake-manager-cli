using Spectre.Console;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace src.selfTest
{
    internal class TestRunner
    {
        private readonly Dictionary<string, List<(string TestName, Func<Task> Action)>> _groups
            = new Dictionary<string, List<(string, Func<Task>)>>();

        public void AddTest(string groupName, string testName, Func<Task> testAction)
        {
            if (string.IsNullOrWhiteSpace(groupName))
                throw new ArgumentException("Group name cannot be empty", nameof(groupName));
            if (string.IsNullOrWhiteSpace(testName))
                throw new ArgumentException("Test name cannot be empty", nameof(testName));
            if (testAction == null)
                throw new ArgumentNullException(nameof(testAction));

            if (!_groups.ContainsKey(groupName))
                _groups[groupName] = new List<(string, Func<Task>)>();

            _groups[groupName].Add((testName, testAction));
        }

        public void AddGroup(string groupName, IEnumerable<(string TestName, Func<Task> Action)> tests)
        {
            if (string.IsNullOrWhiteSpace(groupName))
                throw new ArgumentException("Group name cannot be empty", nameof(groupName));
            if (tests == null)
                throw new ArgumentNullException(nameof(tests));

            if (!_groups.ContainsKey(groupName))
                _groups[groupName] = new List<(string, Func<Task>)>();

            _groups[groupName].AddRange(tests);
        }

        public void AddTestClass<T>() where T : ITestClass, new()
        {
            var instance = new T();
            var groupName = typeof(T).Name;
            AddGroup(groupName, instance.GetTests());
        }

        public async Task RunAllAsync() => await RunGroupsAsync(_groups.Keys);

        /// <summary>
        /// Runs only a specific group by name
        /// </summary>
        public async Task RunGroupAsync(string groupName)
        {
            if (string.IsNullOrWhiteSpace(groupName))
                throw new ArgumentException("Group name cannot be empty", nameof(groupName));

            if (!_groups.ContainsKey(groupName))
            {
                AnsiConsole.MarkupLine($"[yellow]No such group: {groupName}[/]");
                return;
            }

            await RunGroupsAsync(new[] { groupName });
        }

        /// <summary>
        /// Core runner for one or more groups
        /// </summary>
        private async Task RunGroupsAsync(IEnumerable<string> groupNames)
        {
            var groupsToRun = new Dictionary<string, List<(string TestName, Func<Task> Action)>>();
            foreach (var name in groupNames)
                if (_groups.ContainsKey(name))
                    groupsToRun[name] = _groups[name];

            if (groupsToRun.Count == 0)
            {
                AnsiConsole.MarkupLine("[yellow]No tests to run.[/]");
                return;
            }

            AnsiConsole.MarkupLine("[bold green]Running self-tests...[/]");

            var totalTests = 0;
            foreach (var g in groupsToRun.Values)
                totalTests += g.Count;

            await AnsiConsole.Progress()
                .AutoClear(false)
                .HideCompleted(false)
                .Columns(new ProgressColumn[]
                {
                    new TaskDescriptionColumn(),
                    new ProgressBarColumn { CompletedStyle = new Style(foreground: Color.Green) },
                    new PercentageColumn(),
                    new SpinnerColumn()
                })
                .StartAsync(async ctx =>
                {
                    var progressTask = ctx.AddTask("[green]Executing tests...[/]", maxValue: totalTests);

                    foreach (var group in groupsToRun)
                    {
                        string groupName = group.Key;

                        // Create a panel for the group
                        var panel = new Panel(new Align(new Markup($"[bold blue]{groupName} ({group.Value.Count})[/]"), HorizontalAlignment.Center))
                        {
                            Border = BoxBorder.Rounded,
                            Padding = new Padding(1, 1),
                            Header = new PanelHeader("Test Group", Justify.Center),
                            Expand = true
                        };

                        AnsiConsole.Write(panel);

                        foreach (var (testName, action) in group.Value)
                        {
                            try
                            {
                                ctx.Refresh();
                                AnsiConsole.MarkupLine($"[blue]Running test:[/] {testName}");
                                await action();
                                AnsiConsole.MarkupLine($"[green]✔ Test passed:[/] {groupName} / {testName}");
                            }
                            catch (Exception ex)
                            {
                                AnsiConsole.MarkupLine($"[red]✖ Test failed:[/] {groupName} / {testName} - {ex.Message}");
                            }

                            progressTask.Increment(1);
                        }
                    }

                });

            AnsiConsole.MarkupLine("[bold green]Selected tests completed.[/]");
        }

        public static void AssertFileExists(string filePath, string? expectedContent = null)
        {
            if (!File.Exists(filePath))
                throw new Exception($"File does not exist: {filePath}");

            if (expectedContent != null)
            {
                var content = File.ReadAllText(filePath);
                if (content != expectedContent)
                    throw new Exception($"File content does not match expected text.\nExpected:\n{expectedContent}\nActual:\n{content}");
            }
        }

        internal string[] GetAllGroupNames()
        {
            return _groups.Keys.ToArray();
        }
    }
}
