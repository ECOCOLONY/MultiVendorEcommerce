using Application.Common.Interfaces;
using Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Settings.Queries.GetStripeSettingsForAdmin;

public sealed record StripeSettingsAdminDto
{
    public string PublishableKey { get; init; } = "";
    public string Currency { get; init; } = "usd";
    public string? WebhookSecret { get; init; }
    public bool HasSecretKey { get; init; }
}

public sealed record GetStripeSettingsForAdminQuery : IRequest<StripeSettingsAdminDto>;

public sealed class GetStripeSettingsForAdminQueryHandler : IRequestHandler<GetStripeSettingsForAdminQuery, StripeSettingsAdminDto>
{
    private readonly IApplicationDbContext _db;

    public GetStripeSettingsForAdminQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<StripeSettingsAdminDto> Handle(GetStripeSettingsForAdminQuery request, CancellationToken ct)
    {
        var row = await _db.StripeSettings.AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == StripeSettings.SingletonId, ct);

        return new StripeSettingsAdminDto
        {
            PublishableKey = row?.PublishableKey ?? "",
            Currency = string.IsNullOrWhiteSpace(row?.Currency) ? "usd" : row!.Currency,
            WebhookSecret = row?.WebhookSecret,
            HasSecretKey = !string.IsNullOrWhiteSpace(row?.SecretKey)
        };
    }
}
