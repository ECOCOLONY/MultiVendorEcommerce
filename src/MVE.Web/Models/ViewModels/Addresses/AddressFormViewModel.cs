using System.ComponentModel.DataAnnotations;

namespace Web.Models.ViewModels.Addresses;

public class AddressFormViewModel
{
    public Guid? Id { get; set; }

    [StringLength(50), Display(Name = "Label (e.g. Home, Office)")]
    public string? Label { get; set; }

    [Required, StringLength(200), Display(Name = "Address line 1")]
    public string Line1 { get; set; } = string.Empty;

    [StringLength(200), Display(Name = "Address line 2")]
    public string? Line2 { get; set; }

    [Required, StringLength(100)]
    public string City { get; set; } = string.Empty;

    [StringLength(100)]
    public string? State { get; set; }

    [Required, StringLength(20), Display(Name = "Postal code")]
    public string PostalCode { get; set; } = string.Empty;

    [Required, StringLength(100)]
    public string Country { get; set; } = string.Empty;

    [StringLength(50)]
    public string? Phone { get; set; }

    [Display(Name = "Set as default")]
    public bool IsDefault { get; set; }
}
