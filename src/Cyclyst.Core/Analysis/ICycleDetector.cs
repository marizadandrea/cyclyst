using System.Collections.Generic;
using Cyclyst.Core.Models;

namespace Cyclyst.Core.Analysis;

public interface ICycleDetector
{
    IEnumerable<StronglyConnectedComponent> DetectCycles(DependencyGraph graph);
}