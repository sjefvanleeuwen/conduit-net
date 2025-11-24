using System.CommandLine;
using Spectre.Console;

namespace Conduit.Cli;

class Program
{
    static async Task<int> Main(string[] args)
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;

        if (args.Length == 0 || args.Contains("--help") || args.Contains("-h") || args.Contains("-?"))
        {
            AnsiConsole.MarkupLine("[bold cyan] ⢀⣀ ⢀⡀ ⣀⡀ ⢀⣸ ⡀⢀ ⠄ ⣰⡀[/]");
            AnsiConsole.MarkupLine("[bold cyan] ⠣⠤ ⠣⠜ ⠇⠸ ⠣⠼ ⠣⠼ ⠇ ⠘⠤[/]");
            AnsiConsole.WriteLine();
        }

        var rootCommand = new RootCommand("Conduit CLI (cn) - Unified management tool for the Conduit ecosystem.");

        // Registry Group
        var registryCommand = new Command("registry", "Manages packages and interactions with the Conduit Service Registry.");
        registryCommand.AddAlias("reg");

        var publishCommand = new Command("publish", "Build and push the current project as a .cnp.");
        publishCommand.SetHandler(() =>
        {
            AnsiConsole.Status()
                .Start("Publishing package...", ctx => 
                {
                    ctx.Spinner(Spinner.Known.Dots);
                    ctx.SpinnerStyle(Style.Parse("green"));
                    Thread.Sleep(2000); // Simulate work
                    AnsiConsole.MarkupLine("[yellow]Publishing package... (Not implemented)[/]");
                });
        });
        registryCommand.AddCommand(publishCommand);

        var installCommand = new Command("install", "Download and extract a package.");
        var packageArgument = new Argument<string>("package", "The package name to install.");
        installCommand.AddArgument(packageArgument);
        installCommand.SetHandler((package) =>
        {
            AnsiConsole.MarkupLine($"[green]Installing package:[/] [bold white]{package}[/] [yellow](Not implemented)[/]");
        }, packageArgument);
        registryCommand.AddCommand(installCommand);

        rootCommand.AddCommand(registryCommand);

        // Node Group
        var nodeCommand = new Command("node", "Manages local or remote Conduit Nodes.");
        var startCommand = new Command("start", "Start a node in the current directory.");
        startCommand.SetHandler(() =>
        {
            AnsiConsole.MarkupLine("[blue]Starting node... (Not implemented)[/]");
        });
        nodeCommand.AddCommand(startCommand);
        rootCommand.AddCommand(nodeCommand);

        return await rootCommand.InvokeAsync(args);
    }
}
