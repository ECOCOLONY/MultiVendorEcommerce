namespace Application.Common.Interfaces;

/// <summary>Effective Stripe keys and currency (database with optional appsettings fallback).</summary>
public interface IStripeConfigurationProvider
{
    Task<StripeConfigurationSnapshot> GetAsync(CancellationToken cancellationToken = default);
}

/// <param name="SecretKey">Secret API key; empty when not configured.</param>
public sealed record StripeConfigurationSnapshot(string SecretKey, string PublishableKey, string Currency)
{
    public bool IsConfigured => !string.IsNullOrWhiteSpace(SecretKey);
}
