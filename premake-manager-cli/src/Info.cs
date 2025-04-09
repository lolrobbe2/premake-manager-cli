using Spectre.Console;
using Spectre.Console.Cli;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace src
{
    internal class Info : AsyncCommand
    {
        public override async Task<int> ExecuteAsync(CommandContext context)
        {
            AnsiConsole.WriteLine("this application has the ability to manage premake versions");
            return 0;
        }
    }
}
