using Application.Common.Interfaces;
using Application.Settings;
using Application.Settings.Queries.GetPublicWebsiteSettings;
using Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Settings.Queries.GetWebsiteSettingsForAdmin;

public sealed record GetWebsiteSettingsForAdminQuery : IRequest<PublicWebsiteSettingsDto>;

public sealed class GetWebsiteSettingsForAdminQueryHandler
    : IRequestHandler<GetWebsiteSettingsForAdminQuery, PublicWebsiteSettingsDto>
{
    private readonly IApplicationDbContext _db;

    public GetWebsiteSettingsForAdminQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<PublicWebsiteSettingsDto> Handle(GetWebsiteSettingsForAdminQuery request, CancellationToken ct)
    {
        var row = await _db.WebsiteSettings.AsNoTracking()
            .FirstOrDefaultAsync(w => w.Id == WebsiteSettings.SingletonId, ct);

        return row is null ? new PublicWebsiteSettingsDto() : GetPublicWebsiteSettingsQueryHandler.Map(row);
    }
}
