using System.ComponentModel.DataAnnotations;

namespace Web.Models.ViewModels.Account;

public class RegisterVendorViewModel
{
    [Required, StringLength(120, MinimumLength = 2), Display(Name = "Full name")]
    public string FullName { get; set; } = string.Empty;

    [Required, EmailAddress, Display(Name = "Email")]
    public string Email { get; set; } = string.Empty;

    [Required, StringLength(100, MinimumLength = 6), DataType(DataType.Password), Display(Name = "Password")]
    public string Password { get; set; } = string.Empty;

    [DataType(DataType.Password), Compare(nameof(Password), ErrorMessage = "Passwords do not match.")]
    [Display(Name = "Confirm password")]
    public string ConfirmPassword { get; set; } = string.Empty;

    [Required, StringLength(200, MinimumLength = 2), Display(Name = "Business name")]
    public string BusinessName { get; set; } = string.Empty;

    [Required, StringLength(200, MinimumLength = 2), Display(Name = "Store name")]
    public string StoreName { get; set; } = string.Empty;

    [StringLength(50), Display(Name = "Tax number (optional)")]
    public string? TaxNumber { get; set; }
}
