# cyclyst
This repository provides a framework for generating interactive visual maps of C# projects. By analyzing Namespaces, Classes, and Structures, Cyclyst helps developers understand the underlying "skeleton" of their code and identify problematic patterns.

# Core Features
Hierarchical Mapping: Visualize the flow from high-level Projects down to granular Classes.

Cycle Detection: Automatically highlight Cyclic Dependencies (A → B → A) that violate the Dependency Inversion Principle.

Structure Audit: Identify "God Objects" or overly coupled namespaces that need refactoring.

Exportable Graphs: Generate visualizations in formats like Mermaid.js, Graphviz (DOT), or interactive SVG.

