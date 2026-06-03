using Domain.Common;

namespace Domain.Entities;

public class Review : BaseAuditableEntity
{
    public Guid ProductId { get; set; }
    public Product Product { get; set; } = null!;

    public string CustomerUserId { get; set; } = string.Empty;

    /// <summary>Optional link to the OrderItem that proves this is a verified purchase.</summary>
    public Guid? OrderItemId { get; set; }
    public OrderItem? OrderItem { get; set; }

    public int Rating { get; set; }
    public string? Title { get; set; }
    public string? Comment { get; set; }

    /// <summary>Admin moderation gate. Hide from product page until approved.</summary>
    public bool IsApproved { get; set; }

    // Optional vendor response shown beneath the review.
    public string? VendorReply { get; set; }
    public DateTime? VendorRepliedAtUtc { get; set; }
}
