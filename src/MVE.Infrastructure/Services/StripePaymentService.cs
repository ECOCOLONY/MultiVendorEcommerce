using Application.Common.Interfaces;
using Stripe;
using Stripe.Checkout;

namespace Infrastructure.Services;

public sealed class StripePaymentService : IStripePaymentService
{
    private readonly IStripeConfigurationProvider _config;

    public StripePaymentService(IStripeConfigurationProvider config) => _config = config;

    public async Task<bool> IsConfiguredAsync(CancellationToken cancellationToken = default)
    {
        var c = await _config.GetAsync(cancellationToken);
        return c.IsConfigured;
    }

    public async Task<string?> CreateCheckoutSessionAsync(
        string successUrl,
        string cancelUrl,
        IReadOnlyDictionary<string, string> metadata,
        long amountTotalCents,
        string currency,
        string productTitle,
        string? productDescription,
        CancellationToken cancellationToken = default)
    {
        var cfg = await _config.GetAsync(cancellationToken);
        if (!cfg.IsConfigured) return null;

        var client = new StripeClient(cfg.SecretKey);
        var service = new SessionService(client);

        var options = new SessionCreateOptions
        {
            Mode = "payment",
            SuccessUrl = successUrl,
            CancelUrl = cancelUrl,
            Metadata = metadata.ToDictionary(kv => kv.Key, kv => kv.Value),
            LineItems = new List<SessionLineItemOptions>
            {
                new()
                {
                    Quantity = 1,
                    PriceData = new SessionLineItemPriceDataOptions
                    {
                        Currency = string.IsNullOrWhiteSpace(currency) ? cfg.Currency : currency,
                        UnitAmount = amountTotalCents,
                        ProductData = new SessionLineItemPriceDataProductDataOptions
                        {
                            Name = productTitle,
                            Description = productDescription
                        }
                    }
                }
            }
        };

        var session = await service.CreateAsync(options, cancellationToken: cancellationToken);
        return session.Url;
    }

    public async Task<StripePaidSessionInfo?> TryGetPaidSessionAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        var cfg = await _config.GetAsync(cancellationToken);
        if (!cfg.IsConfigured || string.IsNullOrWhiteSpace(sessionId)) return null;

        var client = new StripeClient(cfg.SecretKey);
        var service = new SessionService(client);
        Session session;
        try
        {
            session = await service.GetAsync(sessionId, cancellationToken: cancellationToken);
        }
        catch (StripeException)
        {
            return null;
        }

        if (session.PaymentStatus != "paid") return null;
        if (string.IsNullOrEmpty(session.PaymentIntentId)) return null;

        IReadOnlyDictionary<string, string> meta = session.Metadata is { Count: > 0 }
            ? session.Metadata.ToDictionary(kv => kv.Key, kv => kv.Value ?? "")
            : new Dictionary<string, string>();

        return new StripePaidSessionInfo(session.PaymentIntentId, session.AmountTotal ?? 0L, meta);
    }

    public async Task<(bool Ok, string? RefundId, string? Error)> RefundPaymentIntentAsync(
        string paymentIntentId,
        CancellationToken cancellationToken = default)
    {
        var cfg = await _config.GetAsync(cancellationToken);
        if (!cfg.IsConfigured)
            return (false, null, "Stripe is not configured.");

        if (string.IsNullOrWhiteSpace(paymentIntentId))
            return (false, null, "Missing payment reference.");

        try
        {
            var client = new StripeClient(cfg.SecretKey);
            var service = new RefundService(client);
            var refund = await service.CreateAsync(
                new RefundCreateOptions { PaymentIntent = paymentIntentId },
                cancellationToken: cancellationToken);
            return (true, refund.Id, null);
        }
        catch (StripeException ex)
        {
            return (false, null, ex.Message);
        }
    }
}
