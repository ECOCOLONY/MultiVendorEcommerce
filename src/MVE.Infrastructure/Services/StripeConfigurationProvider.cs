using Application.Common.Configuration;
using Application.Common.Interfaces;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Infrastructure.Services;

public sealed class StripeConfigurationProvider : IStripeConfigurationProvider
{
    private readonly IApplicationDbContext _db;
    private readonly StripeOptions _fallback;

    public StripeConfigurationProvider(IApplicationDbContext db, IOptions<StripeOptions> fallback)
    {
        _db = db;
        _fallback = fallback.Value;
    }

    public async Task<StripeConfigurationSnapshot> GetAsync(CancellationToken cancellationToken = default)
    {
        var row = await _db.StripeSettings.AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == StripeSettings.SingletonId, cancellationToken);

        var secret = row?.SecretKey?.Trim() ?? "";
        var publish = row?.PublishableKey?.Trim() ?? "";
        var currency = row?.Currency?.Trim() ?? "";

        if (string.IsNullOrWhiteSpace(secret))
            secret = (_fallback.SecretKey ?? "").Trim();
        if (string.IsNullOrWhiteSpace(publish))
            publish = (_fallback.PublishableKey ?? "").Trim();
        if (string.IsNullOrWhiteSpace(currency))
            currency = (_fallback.Currency ?? "").Trim();

        currency = string.IsNullOrWhiteSpace(currency) ? "usd" : currency.ToLowerInvariant();

        return new StripeConfigurationSnapshot(secret, publish, currency);
    }
}
