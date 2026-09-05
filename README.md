# cyclyst
This repository provides a framework for generating interactive visual maps of C# projects. By analyzing Namespaces, Classes, and Structures, Cyclyst helps developers understand the underlying "skeleton" of their code and identify problematic patterns.

# Core Features
Hierarchical Mapping: Visualize the flow from high-level Projects down to granular Classes.

Cycle Detection: Automatically highlight Cyclic Dependencies (A → B → A) that violate the Dependency Inversion Principle.

Structure Audit: Identify "God Objects" or overly coupled namespaces that need refactoring.

Exportable Graphs: Generate visualizations in formats like Mermaid.js, Graphviz (DOT), or interactive SVG.

# Warnings
This implementation will be done using IA free agents and therefore is not gurantee

## Dependents Analysis

You can now restrict an analysis to the set of types that reference a given class or namespace (i.e. find dependents).

Usage (CLI):

```bash
cyclyst analyze <path-to-sln-or-csproj> --dependents-of <pattern> -o <output-folder>
```

- `--dependents-of` / `-d`: One or more class/namespace patterns to match. Patterns support `*` as a wildcard (glob-like).
- Matching is applied against the node id, name, and namespace. Example patterns: `MyNamespace.MyClass`, `MyNamespace.*`, `*Controller`.
- The filter is transitive by default — the report will include all types that directly or indirectly reference the target(s).

Examples:

```bash
# Find everything that depends (directly or transitively) on MyNamespace.MyClass
cyclyst analyze MySolution.sln -d MyNamespace.MyClass -o output

# Find dependents of all types in a namespace
cyclyst analyze MyProject.csproj -d MyCompany.MyProduct.* -o output -x Mermaid
```

Notes:

- The dependents filter is applied before cycle detection and export; exporters generate the same report formats but only for the filtered subgraph.
- Currently the CLI offers transitive filtering by default. Programmatically the filter supports a non-transitive mode, but there is no CLI switch for that yet.
- A unit test verifying the behavior was added: [tests/Cyclyst.Tests.Unit/DependentsFilterTests.cs](tests/Cyclyst.Tests.Unit/DependentsFilterTests.cs)