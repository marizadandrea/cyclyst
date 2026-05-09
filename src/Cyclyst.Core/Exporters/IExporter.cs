using System.Threading.Tasks;
using Cyclyst.Core.Models;

namespace Cyclyst.Core.Exporters;

public interface IExporter
{
    Task ExportAsync(DependencyGraph graph, string outputPath, ExportOptions options);
}
