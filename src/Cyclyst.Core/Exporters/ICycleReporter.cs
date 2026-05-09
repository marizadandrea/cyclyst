using System.Collections.Generic;
using System.Threading.Tasks;
using Cyclyst.Core.Models;

namespace Cyclyst.Core.Exporters;

public interface ICycleReporter
{
    Task ReportAsync(IEnumerable<CycleResult> cycles, string outputPath);
}