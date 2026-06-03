using Domain.Common;

namespace Domain.Entities;

public class WishlistItem : BaseAuditableEntity
{
    public string CustomerUserId { get; set; } = string.Empty;

    public Guid ProductId { get; set; }
    public Product Product { get; set; } = null!;
}
