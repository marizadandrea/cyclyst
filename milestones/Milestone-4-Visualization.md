# Milestone 4: Visualization (Mermaid.js)
The Prompt:

"We need to see the results. In Cyclyst.Exporters, implement a MermaidExporter.

Task: Create a class that takes a DependencyGraph and converts it into a Mermaid.js class diagram string.

If an edge is part of a detected cycle, format it specifically (e.g., using a different arrow style or note) so it stands out in the visualization."


4. Analysis & Reporting RequirementsOnce the SCCs are identified, the data must be made actionable for an architect.RequirementDescriptionCycle IsolationThe tool must be able to output the specific path that creates the cycle (e.g., A -> B -> C -> A).Level SwitchingUsers must be able to run the analysis at the Namespace level to see architectural flaws, then drill down to the Class level to find the specific code causing it.Exclusion RulesAbility to ignore "System" or "Third-party" namespaces (e.g., System.*, Microsoft.*) so only project-specific circularities are reported.Output FormatsSupport for JSON/XML (for CI/CD pipelines) and a visual format (like Mermaid.js or Graphviz) to see the clusters.