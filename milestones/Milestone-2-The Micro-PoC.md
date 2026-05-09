# Milestone 2: The Micro-PoC (Roslyn Logic)
Once the folders are there, force the agent to prove it can actually "read" code before it touches the file system.

The Prompt:

"Now, let's implement the logic for Cyclyst.Engine.Roslyn. 
Roslyn-based analyzer, that will bridge the gap between C# syntax trees and the core DependencyGraph model.

Roslyn will be used  to resolve the exact types of constructor parameters, even if they aren't fully qualified in the source.

Consider this Milestone as a Micro-PoC to proof that the Roslyn can be used to read a c# clasess (in text format) and create a dependency graph.

To perform this follow these tasks:

Task 1: Project Setup & Dependencies
Folder: src/Cyclyst.Analysis.Roslyn/

Create a new Class Library project named Cyclyst.Analysis.Roslyn.

Add a project reference to Cyclyst.Core.

Add the following NuGet packages:

Microsoft.CodeAnalysis.CSharp

Microsoft.CodeAnalysis.CSharp.Workspaces


Task 2: Implement the DependencyHarvester (Syntax Walker)
Folder: src/Cyclyst.Analysis.Roslyn/

Create a class DependencyHarvester that inherits from CSharpSyntaxWalker.

Override VisitClassDeclaration: Capture the current class being visited as the "Source Node."

Override VisitConstructorDeclaration:

Iterate through the ParameterList.

For each parameter, identify its type.

Store this as a dependency (Edge) from the current class to the parameter's type.

Override VisitFieldDeclaration & VisitPropertyDeclaration: Capture these as additional dependencies.

Requirement: The harvester should store a list of discovered EdgeMetadata and NodeMetadata.


Task 3: Implement the RoslynSourceScanner
Folder: src/Cyclyst.Analysis.Roslyn/

Create a class RoslynSourceScanner that implements IScanner from Cyclyst.Core.

Implement ScanAsync(string sourceCode):

Parse the text into a SyntaxTree: CSharpSyntaxTree.ParseText(sourceCode).

Create a CSharpCompilation to enable semantic analysis (resolving types).

Get the SemanticModel for the tree.

Initialize the DependencyHarvester and pass the SemanticModel to it.

Walk the tree: harvester.Visit(tree.GetRoot()).

Return a DependencyGraph populated with the harvester’s findings.


Task 4: Unit Test Implementation
Folder: src/Cyclyst.Tests/

Create a test class RoslynScannerTests.

Test Case: Should_Identify_Constructor_Dependency_Between_ClassA_And_ClassB

Input String:

C#
public class ClassA {
    public ClassA(ClassB dependency) { }
}
public class ClassB { }
Logic:

Instantiate RoslynSourceScanner.

Call ScanAsync with the string above.

Assert that graph.Nodes contains both ClassA and ClassB.

Assert that graph.Edges contains an edge where SourceId == "ClassA" and TargetId == "ClassB".


# Technical Implementation
When resolving types in the DependencyHarvester,  use the SemanticModel to get the ITypeSymbol. This is more robust than looking at the string name of the type:

C#
// Example of what the agent should implement inside the walker
var typeInfo = _semanticModel.GetTypeInfo(parameterSyntax.Type);
var targetTypeName = typeInfo.Type.ToDisplayString(); // Gets Fully Qualified Name


# Refined Folder Structure

src/
├── Cyclyst.Core/                 
│   ├── Models/                   # NodeMetadata.cs, EdgeMetadata.cs, DependencyGraph.cs
│   ├── Analysis/                 # IScanner.cs, ICycleDetector.cs
│   └── Exporters/                
├── Cyclyst.Analysis.Roslyn/      
│   ├── RoslynSourceScanner.cs    # Orchestrates the Roslyn Compilation
│   └── DependencyHarvester.cs    # The SyntaxWalker that finds "new", params, etc.
└── Cyclyst.Tests/
    └── RoslynScannerTests.cs     # The requested Unit Test