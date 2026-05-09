# Milestone 4: Visualization
Once the SCCs are identified, the data must be made actionable for an architect.Requirement

Description
Cycle Isolation
The tool must be able to output the specific path that creates the cycle (e.g., A -> B -> C -> A).

Level Switching
Users must be able to run the analysis at the Namespace level to see architectural flaws, then drill down to the Class level to find the specific code causing it.Exclusion RulesAbility to ignore "System" or "Third-party" namespaces (e.g., System.*, Microsoft.*) so only project-specific circularities are reported.

Output Formats
Support for HTML and SVG

Support for JSON/XML (for CI/CD pipelines) and a visual format (like Mermaid.js or Graphviz) to see the clusters.

To implement the visualization component for Cyclyst, the AI agent needs to bridge the gap between the raw dependency graph and an interactive architectural report. Since you are using C# for the exporters, the most robust approach is to generate a self-contained HTML file that embeds SVG logic (potentially using a lightweight front-end library like D3.js or Cytoscape.js for the layout, or generating Graphviz/DOT code to be rendered to SVG).


Task 1: Define Exporter Abstractions
Location: src/Cyclyst.Core/Exporters/
Objective: Create a standard interface to allow multiple output formats.

Define IExporter Interface:

Method: Task ExportAsync(DependencyGraph graph, string outputPath, ExportOptions options);

Define ExportOptions Model:

bool HighlightCycles: Whether to visually distinguish SCCs.

GroupingLevel Level: Enum (Namespace, Class).

List<string> ExcludedNamespaces: Patterns to ignore (e.g., System.*).


Task 2: Implement Graph Filtering and Aggregation Logic
Location: src/Cyclyst.Exporters/
Objective: Transform the raw Dependency Graph data into the specific view required by the user before rendering.

Exclusion Engine: Implement a filter that removes nodes and edges where the NodeMetadata.Namespace matches the ExcludedNamespaces list (using Regex or glob patterns).

Namespace Aggregator:

If Level is set to Namespace, the agent must group all ClassNodes into a single NamespaceNode.

Edges between classes in different namespaces must be collapsed into a single edge between the parent namespaces.

Weight the edges based on the number of class-level dependencies they represent.

Task 3: Cycle Highlight & Path Identification
Location: src/Cyclyst.Exporters/
Objective: Use the output from the TarjanCycleDetector to annotate the visual graph.

SCC Annotation: For every node and edge in the DependencyGraph, add a property IsPartOfCycle (bool) and SccId (int).

Edge Styling Logic:

If an edge connects two nodes within the same SCC (where SCC size > 1), mark it as "Critical".

In the SVG output, these edges must be rendered with a bold red stroke (e.g., stroke="#FF0000" and stroke-width="3").

Add a "Cycle ID" label to the edge tooltip so architects can see which specific cycle it belongs to.

Task 4: HTML/SVG Generator (The Core Task)
Location: src/Cyclyst.Exporters/
Objective: Generate a standalone HTML file containing the SVG visualization.

Template Engine: Use a StringBuilder or a lightweight templating approach to generate an HTML document.

SVG Structure:

Implement a method to convert the DependencyGraph into a JSON object compatible with a visualization library (like Cytoscape.js or D3.js) which will be embedded in the HTML.

Fallback: If the agent generates raw SVG, it must use a basic "Layered Digraph" layout algorithm to prevent node overlapping.

Visual Drill-down:

Embed a small JavaScript snippet in the HTML to handle the "Level Switching."

The HTML should include a toggle button: "View: Namespace Level" vs "View: Class Level".

Clicking a Namespace node should "expand" it to show the internal class-level cycles (if using an interactive JS library).

Task 5: Interactive Cycle Reporting (Actionable Sidebar)
Location: src/Cyclyst.Exporters/
Objective: Create the "Actionable" part of the requirement.

The SCC List: Alongside the SVG graph, generate an HTML sidebar that lists all detected cycles.

Path Output: For each cycle, list the exact path (e.g., Cyclyst.Core.Models.Node -> Cyclyst.Analysis.IScanner -> Cyclyst.Core.Models.Node).

Interactive Link: When a user clicks a cycle in the sidebar, the corresponding nodes and edges in the SVG must "Pulse" or highlight to isolate them from the rest of the noise.

Task 6: Unit Tests for Exporter
Location: src/Cyclyst.Tests/
Objective: Ensure the exporter handles empty graphs or complex cycles correctly.

Test Case 1: Verify that ExcludedNamespaces (e.g., System.*) are successfully removed from the final HTML output.

Test Case 2: Verify that an edge belonging to a cycle has the specific "Cycle" CSS class or SVG attribute applied.

Test Case 3: Ensure the exporter creates the outputPath directory if it does not exist.



Example Data Mapping for the Agent:
To help the agent understand the visual styling, provide this snippet in the prompt:

Edge Formatting Rules:

Normal Edge: Stroke: Gray (#999), Width: 1pt.

Cycle Edge: Stroke: Crimson (#DC143C), Width: 2.5pt, Stroke-Dasharray: None.

Namespace Edge (Aggregated): Stroke: Blue (#0000FF), Width: Based on dependency count.