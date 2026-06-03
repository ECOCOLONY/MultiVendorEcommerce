using System.ComponentModel.DataAnnotations;

namespace Web.Models.ViewModels.Vendor.Products;

public class VariantInputViewModel
{
    [Required, StringLength(80)]
    public string Sku { get; set; } = string.Empty;

    [StringLength(120)]
    public string? Name { get; set; }

    [StringLength(50)]
    public string? Color { get; set; }

    [StringLength(20)]
    public string? Size { get; set; }

    [Range(0.01, 1_000_000), DataType(DataType.Currency)]
    public decimal Price { get; set; }

    [Range(0, 100_000), Display(Name = "Stock")]
    public int StockQuantity { get; set; }
}
