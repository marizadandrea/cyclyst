using System;
using System.CommandLine;
using Spectre.Console;
using Cyclyst.Cli.Commands;
using Microsoft.Build.Locator;

var rootCommand = new RootCommand("Cyclyst: Dependency Cycle Detector")
{
    AnalyzeCommandFactory.Create()
};

try
{
    MSBuildLocator.RegisterDefaults();
    return rootCommand.Parse(args).Invoke();
}
catch (Exception ex)
{
    if (ex.Message.Contains("No usable MSBuild", StringComparison.OrdinalIgnoreCase) ||
        (ex.Message.Contains("MSBuild", StringComparison.OrdinalIgnoreCase) && ex.Message.Contains("toolset", StringComparison.OrdinalIgnoreCase)))
    {
        AnsiConsole.MarkupLine("[red]MSBuild initialization failed. Ensure the .NET SDK is installed and run [yellow]dotnet workload restore[/].[/]");
    }

    AnsiConsole.WriteException(ex, new ExceptionSettings
    {
        Format = ExceptionFormats.ShortenPaths | ExceptionFormats.ShortenTypes
    });

    return 1;
}
