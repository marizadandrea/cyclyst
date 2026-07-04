using System;
using System.CommandLine;
using Cyclyst.Core.Exporters;
using Cyclyst.Cli.Handlers;

namespace Cyclyst.Cli.Commands;

public enum ExportType
{
    HtmlSvg,
    Mermaid,
    DrawIo
}

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
            Description = "Target folder for the report",
            DefaultValueFactory = _ => "output"
        };
        outputOption.Aliases.Add("-o");

        var includeOption = new Option<string[]>("--namespaces")
        {
            Description = "Namespaces to analyze; dependencies from those namespaces are included",
            AllowMultipleArgumentsPerToken = true,
            Arity = ArgumentArity.ZeroOrMore,
            DefaultValueFactory = _ => Array.Empty<string>()
        };
        includeOption.Aliases.Add("-n");

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

        var exportOption = new Option<ExportType>("--export-type")
        {
            Description = "Export format: HtmlSvg, Mermaid, or DrawIo",
            DefaultValueFactory = _ => ExportType.HtmlSvg
        };
        exportOption.Aliases.Add("-x");

        var command = new Command("analyze", "Analyzes a project or solution for dependency cycles")
        {
            pathArgument,
            outputOption,
            includeOption,
            excludeOption,
            levelOption,
            exportOption
        };

        command.SetAction(parseResult =>
        {
            var path = parseResult.GetValue(pathArgument)!;
            var output = parseResult.GetValue(outputOption)!;
            var includes = parseResult.GetValue(includeOption)!;
            var excludes = parseResult.GetValue(excludeOption)!;
            var level = parseResult.GetValue(levelOption);
            var exportType = parseResult.GetValue(exportOption);

            var handler = new AnalysisHandler();
            handler.RunAsync(path, output, excludes, includes, level, exportType).GetAwaiter().GetResult();
        });

        return command;
    }
}
