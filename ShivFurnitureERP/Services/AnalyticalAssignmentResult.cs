namespace ShivFurnitureERP.Services;

public record AnalyticalAssignmentResult(
    int AnalyticalAccountId,
    int ModelId,
    int Priority,
    AnalyticalAssignmentSource Source);
