# Milestone 5: The CLI Entry Point

"Let's build the CLI. In Cyclyst.Cli, use Spectre.Console to create a command-line interface.

Task: The user should be able to run cyclyst analyze <path-to-project>.

It should use MSBuildWorkspace to load the real .csproj or .sln.

It should print a progress spinner while analyzing.

It should output the Mermaid diagram code to the console and save it to a report.md file."


To build the CLI for Cyclyst, the agent needs to integrate the Roslyn-based analysis with a polished user experience. Since you've requested both System.CommandLine and Spectre.Console, the best architectural approach is to use System.CommandLine for the core parsing and Spectre.Console for the high-fidelity UI (spinners, tables, and status updates).

Here are the tasks for the AI code agent.

Task 1: Project Scaffolding & Dependencies
Location: src/Cyclyst.Cli/
Objective: Initialize the project and link internal dependencies.

Initialize Console Project: Create a new .NET console app in the specified folder.

Install NuGets:

System.CommandLine (2.0.7 or equivalent stable 2.0.x).

Spectre.Console.Cli (0.55.0).

Microsoft.CodeAnalysis.Workspaces.MSBuild (to support MSBuildWorkspace).

Project References: Add references to Cyclyst.Core, Cyclyst.Analysis.Roslyn, and Cyclyst.Exporters.

Task 2: Configure MSBuild Workspace Environment
Location: src/Cyclyst.Cli/Program.cs
Objective: Ensure the CLI can actually "see" and compile .csproj or .sln files.

MSBuild Locator: The agent must implement MSBuildLocator.RegisterDefaults(); before any Roslyn code is called. This is a common pitfall where MSBuildWorkspace fails to load because it can't find the SDK.

Validation Logic: Add a check to verify the provided path exists and ends in .csproj or .sln.


Task 3: Implement the Analyze Command
Location: src/Cyclyst.Cli/Commands/AnalyzeCommand.cs
Objective: Build the primary entry point for the user.

Command Definition: Use System.CommandLine to define the analyze command.

Argument: <path> (The path to the solution/project).

Options:

--output (Target folder for the HTML/SVG).

--exclude (List of namespaces to ignore).

--level (Namespace or Class).

Handler Logic: Map these CLI arguments to the ExportOptions created in the previous phase.


Create the CLI for Cyclyst integrating the dependency cyclic analysis with a polished user experience.  
Use System.CommandLine for the core parsing and Spectre.Console for the high-fidelity UI (spinners, tables, and status updates).

Here are the tasks:

Task 1: Project Scaffolding & Dependencies
Location: src/Cyclyst.Cli/
Objective: Initialize the project and link internal dependencies.

Initialize Console Project: Create a new .NET console app in the specified folder.

Install NuGets:

System.CommandLine (2.0.7-beta.21567.1 or equivalent stable 2.0.x).

Spectre.Console.Cli (0.55.0).

Microsoft.CodeAnalysis.Workspaces.MSBuild (to support MSBuildWorkspace).

Project References: Add references to Cyclyst.Core, Cyclyst.Analysis.Roslyn, and Cyclyst.Exporters.

Task 2: Configure MSBuild Workspace Environment
Location: src/Cyclyst.Cli/Program.cs
Objective: Ensure the CLI can actually "see" and compile .csproj or .sln files.

MSBuild Locator: The agent must implement MSBuildLocator.RegisterDefaults(); before any Roslyn code is called. This is a common pitfall where MSBuildWorkspace fails to load because it can't find the SDK.

Validation Logic: Add a check to verify the provided path exists and ends in .csproj or .sln.

Task 3: Implement the Analyze Command
Location: src/Cyclyst.Cli/Commands/AnalyzeCommand.cs
Objective: Build the primary entry point for the user.

Command Definition: Use System.CommandLine to define the analyze command.

Argument: <path> (The path to the solution/project).

Options:

--output (Target folder for the HTML/SVG).

--exclude (List of namespaces to ignore).

--level (Namespace or Class).

Handler Logic: Map these CLI arguments to the ExportOptions created in the previous phase.

Task 4: Interactive Progress & Orchestration
Location: src/Cyclyst.Cli/Handlers/AnalysisHandler.cs
Objective: Use Spectre.Console to provide real-time feedback during the heavy lifting.

The Spinner: Wrap the MSBuildWorkspace loading and the TarjanCycleDetector logic in a AnsiConsole.Status() context.

Text: "Loading Solution..." -> "Walking Syntax Trees..." -> "Detecting Cycles..."

Success Reporting: Once analysis is complete, use an AnsiConsole.MarkupLine to print a success message in green.

Export Notification: Specifically for the HtmlSvgExporter, output a bolded link to the output directory:

📂 Report Generated: [link=file://C:/path/to/output]view-report.html[/link]


To build the CLI for Cyclyst, the agent needs to integrate the Roslyn-based analysis with a polished user experience. Since you've requested both System.CommandLine and Spectre.Console, the best architectural approach is to use System.CommandLine for the core parsing and Spectre.Console for the high-fidelity UI (spinners, tables, and status updates).

Here are the tasks for the AI code agent.

Task 1: Project Scaffolding & Dependencies
Location: src/Cyclyst.Cli/
Objective: Initialize the project and link internal dependencies.

Initialize Console Project: Create a new .NET console app in the specified folder.

Install NuGets:

System.CommandLine (2.0.7-beta.21567.1 or equivalent stable 2.0.x).

Spectre.Console.Cli (0.55.0).

Microsoft.CodeAnalysis.Workspaces.MSBuild (to support MSBuildWorkspace).

Project References: Add references to Cyclyst.Core, Cyclyst.Analysis.Roslyn, and Cyclyst.Exporters.

Task 2: Configure MSBuild Workspace Environment
Location: src/Cyclyst.Cli/Program.cs
Objective: Ensure the CLI can actually "see" and compile .csproj or .sln files.

MSBuild Locator: The agent must implement MSBuildLocator.RegisterDefaults(); before any Roslyn code is called. This is a common pitfall where MSBuildWorkspace fails to load because it can't find the SDK.

Validation Logic: Add a check to verify the provided path exists and ends in .csproj or .sln.

Task 3: Implement the Analyze Command
Location: src/Cyclyst.Cli/Commands/AnalyzeCommand.cs
Objective: Build the primary entry point for the user.

Command Definition: Use System.CommandLine to define the analyze command.

Argument: <path> (The path to the solution/project).

Options:

--output (Target folder for the HTML/SVG).

--exclude (List of namespaces to ignore).

--level (Namespace or Class).

Handler Logic: Map these CLI arguments to the ExportOptions created in the previous phase.

Task 4: Interactive Progress & Orchestration
Location: src/Cyclyst.Cli/Handlers/AnalysisHandler.cs
Objective: Use Spectre.Console to provide real-time feedback during the heavy lifting.

The Spinner: Wrap the MSBuildWorkspace loading and the TarjanCycleDetector logic in a AnsiConsole.Status() context.

Text: "Loading Solution..." -> "Walking Syntax Trees..." -> "Detecting Cycles..."

Success Reporting: Once analysis is complete, use an AnsiConsole.MarkupLine to print a success message in green.

Export Notification: Specifically for the HtmlSvgExporter, output a bolded link to the output directory:

📂 Report Generated: [link=file://C:/path/to/output]view-report.html[/link]

Task 5: Error Boundary & Feedback
Location: src/Cyclyst.Cli/
Objective: Prevent raw stack traces from ruining the user experience.

Spectre Exceptions: Use AnsiConsole.WriteException for unhandled errors to provide a clean, readable breakdown of what went wrong (e.g., missing NuGet packages or malformed C# code).

Missing SDK Warning: If MSBuildWorkspace fails to find a valid toolset, provide an actionable message suggesting the user runs dotnet workload restore.


To build the CLI for Cyclyst, the agent needs to integrate the Roslyn-based analysis with a polished user experience. Since you've requested both System.CommandLine and Spectre.Console, the best architectural approach is to use System.CommandLine for the core parsing and Spectre.Console for the high-fidelity UI (spinners, tables, and status updates).

Here are the tasks for the AI code agent.

Task 1: Project Scaffolding & Dependencies
Location: src/Cyclyst.Cli/
Objective: Initialize the project and link internal dependencies.

Initialize Console Project: Create a new .NET console app in the specified folder.

Install NuGets:

System.CommandLine (2.0.7-beta.21567.1 or equivalent stable 2.0.x).

Spectre.Console.Cli (0.55.0).

Microsoft.CodeAnalysis.Workspaces.MSBuild (to support MSBuildWorkspace).

Project References: Add references to Cyclyst.Core, Cyclyst.Analysis.Roslyn, and Cyclyst.Exporters.

Task 2: Configure MSBuild Workspace Environment
Location: src/Cyclyst.Cli/Program.cs
Objective: Ensure the CLI can actually "see" and compile .csproj or .sln files.

MSBuild Locator: The agent must implement MSBuildLocator.RegisterDefaults(); before any Roslyn code is called. This is a common pitfall where MSBuildWorkspace fails to load because it can't find the SDK.

Validation Logic: Add a check to verify the provided path exists and ends in .csproj or .sln.

Task 3: Implement the Analyze Command
Location: src/Cyclyst.Cli/Commands/AnalyzeCommand.cs
Objective: Build the primary entry point for the user.

Command Definition: Use System.CommandLine to define the analyze command.

Argument: <path> (The path to the solution/project).

Options:

--output (Target folder for the HTML/SVG).

--exclude (List of namespaces to ignore).

--level (Namespace or Class).

Handler Logic: Map these CLI arguments to the ExportOptions created in the previous phase.

Task 4: Interactive Progress & Orchestration
Location: src/Cyclyst.Cli/Handlers/AnalysisHandler.cs
Objective: Use Spectre.Console to provide real-time feedback during the heavy lifting.

The Spinner: Wrap the MSBuildWorkspace loading and the TarjanCycleDetector logic in a AnsiConsole.Status() context.

Text: "Loading Solution..." -> "Walking Syntax Trees..." -> "Detecting Cycles..."

Success Reporting: Once analysis is complete, use an AnsiConsole.MarkupLine to print a success message in green.

Export Notification: Specifically for the HtmlSvgExporter, output a bolded link to the output directory:

📂 Report Generated: [link=file://C:/path/to/output]view-report.html[/link]

Task 5: Error Boundary & Feedback
Location: src/Cyclyst.Cli/
Objective: Prevent raw stack traces from ruining the user experience.

Spectre Exceptions: Use AnsiConsole.WriteException for unhandled errors to provide a clean, readable breakdown of what went wrong (e.g., missing NuGet packages or malformed C# code).

Missing SDK Warning: If MSBuildWorkspace fails to find a valid toolset, provide an actionable message suggesting the user runs dotnet workload restore.

Suggested Execution Flow for the Agent:
C#
// Example Logic Flow the agent should follow:
var rootCommand = new RootCommand("Cyclyst: Dependency Cycle Detector");
var analyzeCommand = new Command("analyze", "Analyzes a project for cycles");

analyzeCommand.SetHandler(async (string path, string output) => 
{
    await AnsiConsole.Status()
        .StartAsync("Analyzing project...", async ctx => 
        {
            // 1. MSBuildWorkspace Logic
            // 2. Roslyn Scanner Logic
            // 3. Tarjan Cycle Detection
            // 4. Exporter Logic
            ctx.Status("Writing HTML/SVG Report...");
        });
    
    AnsiConsole.MarkupLine($"[green]Analysis Complete![/] Results saved to: [yellow]{output}[/]");
}, pathArg, outputOption);

