Project: Cyclyst
1. Purpose & Scope
The goal of this tool is to perform Static Analysis on C# source code or compiled assemblies to identify relationships between architectural elements. The primary objective is the identification of Circular Dependencies at the class and namespace level.

2. Technical Stack (Constraints)
- Language: C# / .NET 10+
- Analysis Engine: Roslyn (Microsoft.CodeAnalysis)
- Input: .sln or .csproj file paths.
- Output Formats:
  1. SVG
  2. DOT/Graphviz (for high-density mapping)
  3. JSON (for custom front-end rendering)
  4. Mermaid.js (for GitHub README compatibility)
  5. JSON (for custom front-end rendering)

3. Functional Requirements (FR)
- FR-1 Source Code Analysis and Dependency Graph: The project must use Roslyn to walk the Syntax Tree of a provided solution. 
It must	Identify using statements, constructor injections, and inheritance link. 
It must create a dependency graph using Namespace Grouping	Logic to aggregate classes into their parent namespaces for high-level viewing.

- FR-4 Cycle Detection	Implement a Directed Graph algorithm to find strongly connected components.
- FR-5 CLI Interface	A command-line interface to trigger scans (e.g., analyze --path ./MyProj).

