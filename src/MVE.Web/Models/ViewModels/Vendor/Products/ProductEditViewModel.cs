using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Web.Models.ViewModels.Vendor.Products;

public class ProductEditViewModel
{
    [Required]
    public Guid Id { get; set; }

    [Required, Display(Name = "Category")]
    public Guid CategoryId { get; set; }

    [Required, StringLength(200, MinimumLength = 2)]
    public string Name { get; set; } = string.Empty;

    [StringLength(4000)]
    public string? Description { get; set; }

    [Required, Range(0, 1_000_000), DataType(DataType.Currency), Display(Name = "Base price")]
    public decimal BasePrice { get; set; }

    [Display(Name = "Published")]
    public bool IsPublished { get; set; }

    public string? StoreName { get; set; }
    public string? Slug { get; set; }
    public string? ApprovalStatus { get; set; }

    public IEnumerable<SelectListItem>? Categories { get; set; }
}
