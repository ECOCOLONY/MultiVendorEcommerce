namespace Application.Common.Interfaces;

/// <summary>Stripe Checkout Session creation, verification, and PaymentIntent refunds.</summary>
public interface IStripePaymentService
{
    /// <summary>True when a secret key is available (database or appsettings fallback).</summary>
    Task<bool> IsConfiguredAsync(CancellationToken cancellationToken = default);

    /// <summary>Creates a Checkout Session and returns its URL (hosted payment page).</summary>
    Task<string?> CreateCheckoutSessionAsync(
        string successUrl,
        string cancelUrl,
        IReadOnlyDictionary<string, string> metadata,
        long amountTotalCents,
        string currency,
        string productTitle,
        string? productDescription,
        CancellationToken cancellationToken = default);

    /// <summary>Returns PaymentIntent id and amount when the session is fully paid; otherwise null.</summary>
    Task<StripePaidSessionInfo?> TryGetPaidSessionAsync(string sessionId, CancellationToken cancellationToken = default);

    /// <summary>Refunds a captured charge by PaymentIntent id. Returns refund id or error message.</summary>
    Task<(bool Ok, string? RefundId, string? Error)> RefundPaymentIntentAsync(
        string paymentIntentId,
        CancellationToken cancellationToken = default);
}

/// <param name="PaymentIntentId">pi_...</param>
/// <param name="AmountTotalCents">Total paid, minor units (matches Session.AmountTotal).</param>
/// <param name="Metadata">Checkout Session metadata copied at payment time.</param>
public sealed record StripePaidSessionInfo(
    string PaymentIntentId,
    long AmountTotalCents,
    IReadOnlyDictionary<string, string> Metadata);
