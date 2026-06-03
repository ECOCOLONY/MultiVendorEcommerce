using Domain.Common;

namespace Domain.Entities;

public class Address : BaseAuditableEntity
{
    public string UserId { get; set; } = string.Empty;

    public string? Label { get; set; }
    public string Line1 { get; set; } = string.Empty;
    public string? Line2 { get; set; }
    public string City { get; set; } = string.Empty;
    public string? State { get; set; }
    public string PostalCode { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public bool IsDefault { get; set; }
}
