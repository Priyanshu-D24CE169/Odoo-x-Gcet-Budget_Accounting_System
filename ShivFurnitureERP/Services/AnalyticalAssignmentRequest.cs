using System;
using System.Collections.Generic;

namespace ShivFurnitureERP.Services;

public record AnalyticalAssignmentRequest
{
    public AnalyticalAssignmentRequest(
        int? partnerId,
        IReadOnlyCollection<int>? partnerTagIds,
        int? productId,
        int? productCategoryId,
        AnalyticalAssignmentSource source)
    {
        PartnerId = partnerId;
        PartnerTagIds = partnerTagIds ?? Array.Empty<int>();
        ProductId = productId;
        ProductCategoryId = productCategoryId;
        Source = source;
    }

    public int? PartnerId { get; }
    public IReadOnlyCollection<int> PartnerTagIds { get; }
    public int? ProductId { get; }
    public int? ProductCategoryId { get; }
    public AnalyticalAssignmentSource Source { get; }

    public static readonly AnalyticalAssignmentRequest Empty = new(null, Array.Empty<int>(), null, null, AnalyticalAssignmentSource.Unknown);
}
