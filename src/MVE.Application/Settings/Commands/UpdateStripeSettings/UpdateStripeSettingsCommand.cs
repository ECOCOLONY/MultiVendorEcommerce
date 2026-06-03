using Application.Common.Interfaces;
using Domain.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Settings.Commands.UpdateStripeSettings;

public sealed record UpdateStripeSettingsCommand(
    string? NewSecretKey,
    string PublishableKey,
    string Currency,
    string? WebhookSecret
) : IRequest<Unit>;

public sealed class UpdateStripeSettingsCommandValidator : AbstractValidator<UpdateStripeSettingsCommand>
{
    public UpdateStripeSettingsCommandValidator()
    {
        RuleFor(x => x.PublishableKey).MaximumLength(500);
        RuleFor(x => x.Currency).NotEmpty().MaximumLength(16);
        RuleFor(x => x.WebhookSecret).MaximumLength(500);
        RuleFor(x => x.NewSecretKey).MaximumLength(500);
    }
}

public sealed class UpdateStripeSettingsCommandHandler : IRequestHandler<UpdateStripeSettingsCommand, Unit>
{
    private readonly IApplicationDbContext _db;

    public UpdateStripeSettingsCommandHandler(IApplicationDbContext db) => _db = db;

    public async Task<Unit> Handle(UpdateStripeSettingsCommand request, CancellationToken ct)
    {
        var row = await _db.StripeSettings.FirstOrDefaultAsync(s => s.Id == StripeSettings.SingletonId, ct);
        if (row is null)
        {
            row = new StripeSettings { Id = StripeSettings.SingletonId };
            _db.StripeSettings.Add(row);
        }

        if (!string.IsNullOrWhiteSpace(request.NewSecretKey))
            row.SecretKey = request.NewSecretKey.Trim();

        row.PublishableKey = (request.PublishableKey ?? "").Trim();
        row.Currency = string.IsNullOrWhiteSpace(request.Currency) ? "usd" : request.Currency.Trim().ToLowerInvariant();

        if (string.IsNullOrWhiteSpace(request.WebhookSecret))
            row.WebhookSecret = null;
        else
            row.WebhookSecret = request.WebhookSecret.Trim();

        await _db.SaveChangesAsync(ct);
        return Unit.Value;
    }
}
