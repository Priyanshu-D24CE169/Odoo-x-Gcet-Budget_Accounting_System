using System.Threading;
using System.Threading.Tasks;

namespace ShivFurnitureERP.Services;

public interface IAnalyticalRuleEngine
{
    Task<AnalyticalAssignmentResult?> ResolveAsync(AnalyticalAssignmentRequest request, CancellationToken cancellationToken = default);
}
