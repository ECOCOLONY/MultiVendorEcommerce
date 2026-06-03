using System.ComponentModel.DataAnnotations;
using Domain.Enums;

namespace Web.Models.ViewModels.Coupons;

public class CouponFormViewModel
{
    public Guid? Id { get; set; }

    /// <summary>True for create+edit on /Admin/Coupons; false for /Vendor/Coupons</summary>
    public bool IsAdmin { get; set; }

    [Required, StringLength(40, MinimumLength = 2)]
    public string Code { get; set; } = string.Empty;

    [Required, Display(Name = "Discount type")]
    public CouponDiscountType DiscountType { get; set; }

    [Required, Range(0.01, 1_000_000), Display(Name = "Discount value")]
    public decimal DiscountValue { get; set; }

    [Range(0, 1_000_000), Display(Name = "Minimum order amount")]
    public decimal MinimumOrderAmount { get; set; }

    [Display(Name = "Max total uses")]
    public int? MaxUses { get; set; }

    [Display(Name = "Max uses per customer")]
    public int? MaxUsesPerCustomer { get; set; }

    [DataType(DataType.DateTime), Display(Name = "Starts at")]
    public DateTime? StartsAtUtc { get; set; }

    [DataType(DataType.DateTime), Display(Name = "Expires at")]
    public DateTime? ExpiresAtUtc { get; set; }

    [Display(Name = "Active")]
    public bool IsActive { get; set; } = true;
}
