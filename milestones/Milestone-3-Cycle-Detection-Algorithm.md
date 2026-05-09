# Milestone 3: Cycle Detection Algorithm
The Prompt:

"Now that we can map dependencies, we need to detect cycles.

Task: In Cyclyst.Engine.Roslyn, implement a CycleDetector class. Use Tarjan's algorithm or a simple Depth-First Search (DFS) to scan the DependencyGraph.

Update the unit test: add a third class, ClassC, and make ClassC depend back on ClassA. The CycleDetector must be able to return a list containing the circular path: ClassA -> ClassB -> ClassC -> ClassA."

3. Algorithmic Implementation (The SCC Core)To identify circular dependencies, you must implement an algorithm that identifies sets of vertices where every vertex is reachable from every other vertex in that set.Algorithm Choice:Tarjan’s Algorithm: Preferred for its efficiency ($O(V + E)$) and the fact that it finds SCCs in a single pass using a stack.Kosaraju’s Algorithm: An alternative two-pass approach (DFS on $G$, then DFS on the transpose $G^T$).Cycle Detection Logic:An SCC containing more than one vertex ($|SCC| > 1$) represents a Circular Dependency.A single vertex with a self-loop (Class A references Class A) is also a circular dependency.Depth-First Search (DFS) Stack Management: The implementation must handle deep recursion or use an iterative approach to avoid StackOverflowException on massive C# projects (e.g., 10,000+ classes).