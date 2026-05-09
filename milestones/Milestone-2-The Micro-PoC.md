# Milestone 2: The Micro-PoC (Roslyn Logic)
Once the folders are there, force the agent to prove it can actually "read" code before it touches the file system.

The Prompt:

"Now, let's implement the logic for Cyclyst.Engine.Roslyn.

Task: Create a unit test in Cyclyst.Tests.Unit. This test should:

Define a string containing two C# classes: ClassA and ClassB.

ClassA should have a constructor parameter of type ClassB.

Use Microsoft.CodeAnalysis.CSharp (Roslyn) to parse this string.

Successfully return a DependencyGraph where ClassA has an edge pointing to ClassB.

Do not worry about .sln files yet; just make this in-memory test pass."

