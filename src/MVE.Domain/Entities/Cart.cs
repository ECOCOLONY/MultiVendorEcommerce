using Domain.Common;

namespace Domain.Entities;

public class Cart : BaseAuditableEntity
{
    public string CustomerUserId { get; set; } = string.Empty;

    public ICollection<CartItem> Items { get; set; } = new List<CartItem>();
}
