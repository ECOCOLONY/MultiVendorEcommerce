namespace Application.Common.Configuration;

/// <summary>Bound from appsettings section "Stripe".</summary>
public sealed class StripeOptions
{
    public const string SectionName = "Stripe";

    public string SecretKey { get; set; } = "";

    public string PublishableKey { get; set; } = "";

    public string Currency { get; set; } = "usd";
}
