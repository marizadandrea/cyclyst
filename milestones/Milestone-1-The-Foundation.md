Milestone 1: The Foundation (Core & Project Setup)

The Prompt:

"I want to start a C# project named Cyclyst. Follow the folder structure we discussed: a src folder containing Cyclyst.Core, Cyclyst.Engine.Roslyn, Cyclyst.Exporters, and Cyclyst.Cli.

Task 1: Create the folder structure and empty .csproj files for all four projects.
Task 2: In Cyclyst.Core, define the domain models: Node (representing a class/namespace), Edge (representing a dependency), and a DependencyGraph class that holds a collection of both.
Task 3: In Cyclyst.Core, define an interface ICodeAnalyzer with a method AnalyzeAsync(string path) that returns a DependencyGraph."


Ask for "Dry Runs": Before it writes a 200-line file, ask: "Briefly explain the logic you will use to resolve cross-project references before you write the code."

