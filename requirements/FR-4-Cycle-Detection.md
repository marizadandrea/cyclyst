# FR-4 Cycle Detection
Implement a Directed Graph algorithm to find strongly connected components.

# Requirements

# Implementation Process
Starts with a Proof-of-Concept (PoC) that will allow to proof that is possible to detect the cycles dependencies.

The Instruction: "Create a temporary string variable containing two C# classes within the same namespace. Class A should have a property of type Class B. Class B should have a constructor parameter of type Class A."

The Goal: Force the agent to successfully identify the link between these two specific symbols using Roslyn without the noise of file I/O or NuGet dependencies.


# Expected Evolution of the Tool
In-Memory
Roslyn Symbol Resolution  ResolutionConfirmation that $A \leftrightarrow B$ is detectable.
2. Single Project	File System & csproj	Mapping relationships within one assembly.
3. Solution Level	Cross-Project references	Mapping how Project A impacts Project B.
4. Visualization	Mermaid/JSON/SVG UI	Making the data digestible for humans.

