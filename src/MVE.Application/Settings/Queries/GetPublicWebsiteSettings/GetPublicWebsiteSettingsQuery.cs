using Application.Common.Interfaces;
using Application.Settings;
using Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Settings.Queries.GetPublicWebsiteSettings;

public sealed record GetPublicWebsiteSettingsQuery : IRequest<PublicWebsiteSettingsDto>;

public sealed class GetPublicWebsiteSettingsQueryHandler
    : IRequestHandler<GetPublicWebsiteSettingsQuery, PublicWebsiteSettingsDto>
{
    private readonly IApplicationDbContext _db;

    public GetPublicWebsiteSettingsQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<PublicWebsiteSettingsDto> Handle(GetPublicWebsiteSettingsQuery request, CancellationToken ct)
    {
        var row = await _db.WebsiteSettings.AsNoTracking()
            .FirstOrDefaultAsync(w => w.Id == WebsiteSettings.SingletonId, ct);

        if (row is null)
            return new PublicWebsiteSettingsDto();

        return Map(row);
    }

    internal static PublicWebsiteSettingsDto Map(WebsiteSettings row) => new()
    {
        SiteName = row.SiteName,
        SiteTagline = row.SiteTagline,
        PublicBaseUrl = row.PublicBaseUrl,
        ContactEmail = row.ContactEmail,
        ContactPhone = row.ContactPhone,
        ContactAddress = row.ContactAddress,
        ContactFormRecipientEmail = row.ContactFormRecipientEmail,
        HeaderLogoUrl = row.HeaderLogoUrl,
        FooterLogoUrl = row.FooterLogoUrl,
        FaviconUrl = row.FaviconUrl,
        TopBarPromo1 = row.TopBarPromo1,
        TopBarPromo2 = row.TopBarPromo2,
        TopBarPromo3 = row.TopBarPromo3,
        FooterTagline = row.FooterTagline,
        FooterCopyrightSuffix = row.FooterCopyrightSuffix,
        SocialFacebookUrl = row.SocialFacebookUrl,
        SocialTwitterUrl = row.SocialTwitterUrl,
        SocialInstagramUrl = row.SocialInstagramUrl,
        SocialYoutubeUrl = row.SocialYoutubeUrl,
        SocialLinkedInUrl = row.SocialLinkedInUrl,
        DefaultMetaTitle = row.DefaultMetaTitle,
        DefaultMetaDescription = row.DefaultMetaDescription,
        DefaultMetaKeywords = row.DefaultMetaKeywords,
        DefaultOgTitle = row.DefaultOgTitle,
        OgDefaultImageUrl = row.OgDefaultImageUrl,
        RobotsMeta = row.RobotsMeta,
        GoogleSiteVerification = row.GoogleSiteVerification,
        BingSiteVerification = row.BingSiteVerification,
        ThemeColor = row.ThemeColor,
        OgSiteName = row.OgSiteName,
        TwitterSite = row.TwitterSite,
        StructuredDataJsonLd = row.StructuredDataJsonLd
    };
}
