# FR-1 Source Code Analysis and dependency graph
The project must use Roslyn to walk the Syntax Tree of a provided solution.
This SyntaxWalker will allow to identify Using directives, class declarations, and member types creating a Metadata Mapping that will be used to create a dependency graph. This dependency graph will work using Namespace Grouping Logic to aggregate classes into their parent namespaces for high-level viewing.

# Metadata Mapping:
Extract the following dependency types to build a complete graph:
Inheritance: class A : B (A depends on B).
Interface Implementation: class A : IB (A depends on IB).
Field/Property Types: If A has a property of type B.
Method Signatures: Parameters or return types involving other classes.
Instantiations: new B() inside a method of A.

# Graph Construction Requirements
The tool must transform the extracted metadata into a formal Directed Graph $G = (V, E)$.

## Node Definition ($V$):
- Class Level: Each unique fully qualified class name is a vertex.
- Namespace Level: An abstraction layer where all class-level nodes are collapsed into their parent namespace vertex.

## Edge Definition ($E$):
- A directed edge $e = (u, v)$ exists if $u$ references $v$.
- Multi-edge Handling: If Class A references Class B five times, it should be represented as a single directed edge for SCC purposes.

## Granularity Toggle: 
The system must support "Namespace Aggregation." If NamespaceA.Class1 depends on NamespaceB.Class2, a directed edge must be drawn from NamespaceA to NamespaceB.

# Relation between Metadata and Graph Construction 
To identify cycles, the graph algorithm cares about connectivity and identity. The metadata must capture:

## The Node (Vertex) Metadata
Unique Identifier: A stable ID (usually the Fully Qualified Name) to distinguish NamespaceA.Class1 from NamespaceB.Class1.

### Element Type
An enumeration (Class, Interface, Struct, Namespace) to allow the algorithm to filter or aggregate nodes.

### Scope Metadata
A parent-child relationship (e.g., Class belongs to Namespace) to allow the "collapsing" of class-level nodes into namespace-level nodes.

### The Edge (Relationship) Metadata
Source & Target IDs: The IDs of the two nodes involved.

### Dependency Strength/Type
Whether it is a hard link (Inheritance) or a soft link (Method Parameter). This is useful for "weighting" cycles or ignoring certain types of dependencies.

