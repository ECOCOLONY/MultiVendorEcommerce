using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Web.Models.ViewModels.Vendor.Products;

public class ProductCreateViewModel
{
    [Required, Display(Name = "Store")]
    public Guid VendorStoreId { get; set; }

    [Required, Display(Name = "Category")]
    public Guid CategoryId { get; set; }

    [Required, StringLength(200, MinimumLength = 2)]
    public string Name { get; set; } = string.Empty;

    [StringLength(4000)]
    public string? Description { get; set; }

    [Required, Range(0, 1_000_000), DataType(DataType.Currency), Display(Name = "Base price")]
    public decimal BasePrice { get; set; }

    [Display(Name = "Product images (first one becomes the main image)")]
    public List<IFormFile> ImageFiles { get; set; } = new();

    public List<VariantInputViewModel> Variants { get; set; } = new();

    public IEnumerable<SelectListItem>? Stores { get; set; }
    public IEnumerable<SelectListItem>? Categories { get; set; }
}
