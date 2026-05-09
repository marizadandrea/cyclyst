using System;
using System.CommandLine;
using Cyclyst.Core.Exporters;
using Cyclyst.Cli.Handlers;

namespace Cyclyst.Cli.Commands;

public static class AnalyzeCommandFactory
{
    public static Command Create()
    {
        var pathArgument = new Argument<string>("path")
        {
            Description = "The path to the .csproj or .sln to analyze"
        };

        var outputOption = new Option<string>("--output")
        {
            Description = "Target folder for the HTML/SVG report",
            DefaultValueFactory = _ => "output"
        };
        outputOption.Aliases.Add("-o");

        var excludeOption = new Option<string[]>("--exclude")
        {
            Description = "Namespaces to ignore",
            AllowMultipleArgumentsPerToken = true,
            Arity = ArgumentArity.ZeroOrMore,
            DefaultValueFactory = _ => Array.Empty<string>()
        };
        excludeOption.Aliases.Add("-e");

        var levelOption = new Option<GroupingLevel>("--level")
        {
            Description = "Grouping level for the report: Namespace or Class",
            DefaultValueFactory = _ => GroupingLevel.Namespace
        };
        levelOption.Aliases.Add("-l");

        var command = new Command("analyze", "Analyzes a project or solution for dependency cycles")
        {
            pathArgument,
            outputOption,
            excludeOption,
            levelOption
        };

        command.SetAction(parseResult =>
        {
            var path = parseResult.GetValue(pathArgument)!;
            var output = parseResult.GetValue(outputOption)!;
            var excludes = parseResult.GetValue(excludeOption)!;
            var level = parseResult.GetValue(levelOption);

            var handler = new AnalysisHandler();
            handler.RunAsync(path, output, excludes, level).GetAwaiter().GetResult();
        });

        return command;
    }
}
