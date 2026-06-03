using Domain.Common;

namespace Domain.Entities;

public class ProductImage : BaseAuditableEntity
{
    public Guid ProductId { get; set; }
    public Product Product { get; set; } = null!;

    public string Url { get; set; } = string.Empty;
    public string? AltText { get; set; }

    /// <summary>Exactly one image per product should have IsMain = true (used as thumbnail).</summary>
    public bool IsMain { get; set; }

    public int SortOrder { get; set; }
}
