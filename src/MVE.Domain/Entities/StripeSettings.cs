namespace Domain.Entities;

/// <summary>Singleton platform row (Id = <see cref="SingletonId"/>) — Stripe API keys and defaults.</summary>
public class StripeSettings
{
    public const int SingletonId = 1;

    public int Id { get; set; }

    /// <summary>Stripe secret API key (sk_live_… / sk_test_…).</summary>
    public string SecretKey { get; set; } = "";

    /// <summary>Publishable key for client-side Stripe (pk_…).</summary>
    public string PublishableKey { get; set; } = "";

    /// <summary>Default charge currency (ISO, lowercase, e.g. usd, try).</summary>
    public string Currency { get; set; } = "usd";

    /// <summary>Optional signing secret for Stripe webhooks (whsec_…).</summary>
    public string? WebhookSecret { get; set; }
}
