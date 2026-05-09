# Milestone 3: Cycle Detection Algorithm
The Prompt:

"Now that we can map dependencies, we need to detect cycles translating the abstract DependencyGraph into a collection of Strongly Connected Components (SCCs).

Task 1: Define Analysis Result Models
Folder: src/Cyclyst.Core/Models/

CycleResult: Create a class (or record) that represents a single circular dependency.

Properties: IReadOnlyList<string> NodeIds (the members of the cycle), CycleType (Class or Namespace).

AnalysisReport: Create a class to aggregate the findings.

Properties: DateTime Timestamp, List<CycleResult> DetectedCycles, int TotalNodesAnalyzed.

Task 2: Implement Tarjan’s AlgorithmFolder: src/Cyclyst.Core/Analysis/Create a class TarjanCycleDetector that implements ICycleDetector.Internal State Management: The algorithm must track:int index: A counter to assign discovery orders.Stack<string> stack: To keep track of nodes in the current search tree.Dictionary<string, int> indices: Mapping Node ID to discovery index.Dictionary<string, int> lowlink: Mapping Node ID to the smallest index reachable.HashSet<string> onStack: For $O(1)$ lookup of whether a node is on the current stack.The Algorithm Logic:Iterate through all nodes in the DependencyGraph.If a node has not been visited, trigger the DFS search.Robustness Requirement: Use an Iterative DFS (using an explicit Stack object) instead of standard recursion to ensure the tool can handle massive enterprise codebases without triggering a StackOverflowException.

Task 3: Implement Cycle Identification LogicFolder: src/Cyclyst.Core/Analysis/Inside the detector, implement the logic to identify if an SCC qualifies as a "Circular Dependency":Rule A (Standard Cycle): If the identified SCC contains more than one node ($|SCC| > 1$), it is a circular dependency.Rule B (Self-Loop): If the SCC contains only one node ($|SCC| = 1$), the detector must check the DependencyGraph edges. If that node has an edge pointing to itself ($A \to A$), it must be reported as a cycle.Output: Map these identified SCCs into the CycleResult objects defined in Task 1.


Task 4: Unit Test: Multi-Node and Self-Loop Detection
Folder: src/Cyclyst.Tests/

Create a test class CycleDetectorTests.

Test Case 1: Simple Circularity

Input: A graph with edges A -> B and B -> A.

Assert: One CycleResult is returned containing both A and B.

Test Case 2: Self-Loop

Input: A graph with one node A and one edge A -> A.

Assert: One CycleResult is returned containing A.

Test Case 3: Complex Graph (DAG)

Input: A graph A -> B -> C (no cycles).

Assert: Zero CycleResult objects are returned.

Test Case 4: Scale Test

Input: Generate a synthetic chain of 5,000 nodes with a cycle at the very end.

Assert: The algorithm completes without a StackOverflowException.

Technical Implementation Guide for the Agent
When the agent implements the iterative version of Tarjan's, it should follow this pattern to maintain state:

C#
// Logic hint for the AI Agent
public class TarjanCycleDetector : ICycleDetector
{
    public IEnumerable<CycleResult> DetectCycles(DependencyGraph graph)
    {
        // 1. Initialize dictionaries for lowlinks and indices
        // 2. Foreach node in graph...
        // 3. Push to 'workStack' for iterative DFS
        // 4. On node pop: if lowlink == index, we found an SCC
        // 5. Yield return CycleResult if |SCC| > 1 or Self-Loop detected
    }
}

Summary of Success Criteria for this MilestoneEfficiency: The analysis must run in $O(V + E)$ time complexity.Accuracy: Must distinguish between a simple reference and a circular loop.Stability: Must pass the 5,000-node scale test using an iterative stack approach.