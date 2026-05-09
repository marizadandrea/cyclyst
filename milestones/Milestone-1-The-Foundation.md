Milestone 1: The Foundation (Core & Project Setup)

I want to start a to implement the Cyclyst.Core project for the C# Cyclyst application.
The scope of the  Cyclyst application is defined in the "software-requirements-specification.md" file.

I want you to implement the objects required in the metadata defined in the file "FR-1-Source-Code-Analysis-and-Dependency-Graph.md" following this tasks:

Task 1: Create the csproj "Cyclyst.Core" in the folder  src/Cyclyst.Core

Task 2: Define Domain Enumerations
Folder: src/Cyclyst.Core/Models/

Create ElementType.cs: Include Class, Interface, Namespace, Struct, Enum, Record.

Create DependencyType.cs: Include Inheritance, Implementation, Field, Property, MethodParameter, LocalVariable.

Task 3: Define Core Model Objects
Folder: src/Cyclyst.Core/Models/

NodeMetadata: Define a class containing Id (string), Name (string), Type (ElementType), and ParentId (string?).

EdgeMetadata: Define a class containing SourceId (string), TargetId (string), and Relation (DependencyType).

DependencyGraph: Define a container class that holds a HashSet<NodeMetadata> and a HashSet<EdgeMetadata>. It should include a method GetAdjacencyList() which returns a Dictionary<string, IEnumerable<string>> optimized for the SCC algorithm.

Task 4: Define Analysis Interfaces
Folder: src/Cyclyst.Core/Analysis/

IScanner: Define an interface with a method Task<DependencyGraph> ScanAsync(string path). This will be the contract for both the Roslyn (source) and Cecil (assembly) implementations.

ICycleDetector: Define an interface that takes a DependencyGraph and returns a collection of StronglyConnectedComponent objects.

IAnalysisConfiguration: Define an interface to handle exclusion rules (e.g., IgnoreNamespaces, MaxDepth).

Task 5: Define Exporter Interfaces
Folder: src/Cyclyst.Core/Exporters/

IGraphExporter: Define an interface with a method Task ExportAsync(DependencyGraph graph, string outputPath).

ICycleReporter: Define an interface specifically for outputting circular dependency violations (e.g., to JSON for CI/CD or Mermaid for documentation).

General considerations
Ensure all Model objects are immutable where possible (using record in C#) to prevent side effects during the multi-threaded scanning phase.

Data Structure for the SCC Algorithm
To ensure the AI agent creates a DependencyGraph compatible with SCC algorithms, the following mapping must be provided:
Metadata Property | Graph Equivalent | Usage in Tarjan's
Node.Id | Vertex ($V$ ) | Used as the key in the "Visited" and "LowLink" dictionaries.
Edge.TargetId | Successor | Iterated during the DFS traversal of each node.
Node.Type | Filter |Used to aggregate Class nodes into Namespace nodes before running the DFS.