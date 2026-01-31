using System.Linq;
using Microsoft.EntityFrameworkCore;
using ShivFurnitureERP.Data;
using ShivFurnitureERP.Models;

namespace ShivFurnitureERP.Services;

public class AnalyticalRuleEngine : IAnalyticalRuleEngine
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<AnalyticalRuleEngine> _logger;

    public AnalyticalRuleEngine(ApplicationDbContext dbContext, ILogger<AnalyticalRuleEngine> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<AnalyticalAssignmentResult?> ResolveAsync(AnalyticalAssignmentRequest request, CancellationToken cancellationToken = default)
    {
        if (request is null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        var tagIds = request.PartnerTagIds ?? Array.Empty<int>();

        var candidates = await _dbContext.AutoAnalyticalModels
            .AsNoTracking()
            .Where(m => !m.IsArchived && m.Status == AnalyticalModelStatus.Confirmed)
            .ToListAsync(cancellationToken);

        AnalyticalAssignmentResult? bestResult = null;

        foreach (var model in candidates)
        {
            var score = CalculateMatchScore(model, request, tagIds);
            if (score <= 0)
            {
                continue;
            }

            var result = new AnalyticalAssignmentResult(model.AnalyticalAccountId, model.ModelId, score, request.Source);
            if (bestResult is null || result.Priority > bestResult.Priority)
            {
                bestResult = result;
            }
        }

        if (bestResult is null)
        {
            _logger.LogDebug("No analytical model matched for source {Source}.", request.Source);
        }
        else
        {
            _logger.LogInformation("Analytical model {ModelId} selected with priority {Priority} for source {Source}.", bestResult.ModelId, bestResult.Priority, request.Source);
        }

        return bestResult;
    }

    private static int CalculateMatchScore(AutoAnalyticalModel model, AnalyticalAssignmentRequest request, IReadOnlyCollection<int> tagIds)
    {
        var score = 0;

        if (model.PartnerId.HasValue && request.PartnerId.HasValue && model.PartnerId.Value == request.PartnerId.Value)
        {
            score += 2;
        }

        if (model.PartnerTagId.HasValue && tagIds.Contains(model.PartnerTagId.Value))
        {
            score += 1;
        }

        if (model.ProductId.HasValue && request.ProductId.HasValue && model.ProductId.Value == request.ProductId.Value)
        {
            score += 3;
        }

        if (model.ProductCategoryId.HasValue && request.ProductCategoryId.HasValue && model.ProductCategoryId.Value == request.ProductCategoryId.Value)
        {
            score += 1;
        }

        return score;
    }
}
