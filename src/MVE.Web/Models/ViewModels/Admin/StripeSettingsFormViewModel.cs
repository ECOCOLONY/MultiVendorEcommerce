using System.ComponentModel.DataAnnotations;

namespace Web.Models.ViewModels.Admin;

public class StripeSettingsFormViewModel
{
    /// <summary>Optional: set only when rotating the secret key.</summary>
    [StringLength(500)]
    public string? NewSecretKey { get; set; }

    [StringLength(500)]
    public string PublishableKey { get; set; } = "";

    [Required, StringLength(16, MinimumLength = 3)]
    public string Currency { get; set; } = "usd";

    [StringLength(500)]
    public string? WebhookSecret { get; set; }

    public bool HasSecretKey { get; set; }
}
