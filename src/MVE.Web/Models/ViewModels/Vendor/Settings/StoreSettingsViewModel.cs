using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace Web.Models.ViewModels.Vendor.Settings;

public class StoreSettingsViewModel
{
    public Guid StoreId { get; set; }

    [Required, StringLength(200, MinimumLength = 2), Display(Name = "Store name")]
    public string Name { get; set; } = string.Empty;

    [Display(Name = "Slug (auto)")]
    public string? Slug { get; set; }

    [StringLength(2000)]
    public string? Description { get; set; }

    [EmailAddress, StringLength(256), Display(Name = "Contact email")]
    public string? ContactEmail { get; set; }

    [StringLength(50), Display(Name = "Contact phone")]
    public string? ContactPhone { get; set; }

    public string? LogoUrl { get; set; }
    public string? BannerUrl { get; set; }

    [Display(Name = "Logo (replace)")]
    public IFormFile? LogoFile { get; set; }

    [Display(Name = "Banner (replace)")]
    public IFormFile? BannerFile { get; set; }
}
