# Milestone 3: Cycle Detection Algorithm
The Prompt:

"Now that we can map dependencies, we need to detect cycles.

Task: In Cyclyst.Engine.Roslyn, implement a CycleDetector class. Use Tarjan's algorithm or a simple Depth-First Search (DFS) to scan the DependencyGraph.

Update the unit test: add a third class, ClassC, and make ClassC depend back on ClassA. The CycleDetector must be able to return a list containing the circular path: ClassA -> ClassB -> ClassC -> ClassA."

