# Dummy C# projects with known cycles
Creating a "BadProject" sample within this folder is a great way to verify the tool.

Sample A: Clean project (No cycles).

Sample B: Namespace cycle (Namespace A -> B -> A).

Sample C: Deep class cycle (Class A -> B -> C -> A).