using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Web.Models.ViewModels.Admin.Categories;

public class CategoryFormViewModel
{
    public Guid? Id { get; set; }

    [Required, StringLength(120, MinimumLength = 2)]
    public string Name { get; set; } = string.Empty;

    [StringLength(500)]
    public string? Description { get; set; }

    [StringLength(500), Display(Name = "Icon URL")]
    public string? IconUrl { get; set; }

    [Display(Name = "Parent category")]
    public Guid? ParentCategoryId { get; set; }

    [Range(0, 9999), Display(Name = "Display order")]
    public int DisplayOrder { get; set; }

    [Display(Name = "Active")]
    public bool IsActive { get; set; } = true;

    public IEnumerable<SelectListItem>? ParentCategories { get; set; }
}
