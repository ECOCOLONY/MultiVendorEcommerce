using Application.Common.Interfaces;
using Application.Settings;
using Domain.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Settings.Commands.UpdateWebsiteSettings;

public sealed record UpdateWebsiteSettingsCommand(PublicWebsiteSettingsDto Data) : IRequest<Unit>;

public sealed class UpdateWebsiteSettingsCommandValidator : AbstractValidator<UpdateWebsiteSettingsCommand>
{
    public UpdateWebsiteSettingsCommandValidator()
    {
        RuleFor(x => x.Data.SiteName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Data.PublicBaseUrl).MaximumLength(500);
        RuleFor(x => x.Data.ContactEmail).MaximumLength(256).EmailAddress().When(x => !string.IsNullOrWhiteSpace(x.Data.ContactEmail));
        RuleFor(x => x.Data.ContactFormRecipientEmail).MaximumLength(256).EmailAddress()
            .When(x => !string.IsNullOrWhiteSpace(x.Data.ContactFormRecipientEmail));
    }
}

public sealed class UpdateWebsiteSettingsCommandHandler : IRequestHandler<UpdateWebsiteSettingsCommand, Unit>
{
    private readonly IApplicationDbContext _db;

    public UpdateWebsiteSettingsCommandHandler(IApplicationDbContext db) => _db = db;

    public async Task<Unit> Handle(UpdateWebsiteSettingsCommand request, CancellationToken ct)
    {
        var d = request.Data;
        var row = await _db.WebsiteSettings.FirstOrDefaultAsync(w => w.Id == WebsiteSettings.SingletonId, ct);
        if (row is null)
        {
            row = new WebsiteSettings { Id = WebsiteSettings.SingletonId };
            _db.WebsiteSettings.Add(row);
        }

        row.SiteName = d.SiteName.Trim();
        row.SiteTagline = NullIfWs(d.SiteTagline);
        row.PublicBaseUrl = NullIfWs(d.PublicBaseUrl);
        row.ContactEmail = NullIfWs(d.ContactEmail);
        row.ContactPhone = NullIfWs(d.ContactPhone);
        row.ContactAddress = NullIfWs(d.ContactAddress);
        row.ContactFormRecipientEmail = NullIfWs(d.ContactFormRecipientEmail);
        row.HeaderLogoUrl = NullIfWs(d.HeaderLogoUrl);
        row.FooterLogoUrl = NullIfWs(d.FooterLogoUrl);
        row.FaviconUrl = NullIfWs(d.FaviconUrl);
        row.TopBarPromo1 = NullIfWs(d.TopBarPromo1);
        row.TopBarPromo2 = NullIfWs(d.TopBarPromo2);
        row.TopBarPromo3 = NullIfWs(d.TopBarPromo3);
        row.FooterTagline = NullIfWs(d.FooterTagline);
        row.FooterCopyrightSuffix = NullIfWs(d.FooterCopyrightSuffix);
        row.SocialFacebookUrl = NullIfWs(d.SocialFacebookUrl);
        row.SocialTwitterUrl = NullIfWs(d.SocialTwitterUrl);
        row.SocialInstagramUrl = NullIfWs(d.SocialInstagramUrl);
        row.SocialYoutubeUrl = NullIfWs(d.SocialYoutubeUrl);
        row.SocialLinkedInUrl = NullIfWs(d.SocialLinkedInUrl);
        row.DefaultMetaTitle = NullIfWs(d.DefaultMetaTitle);
        row.DefaultMetaDescription = NullIfWs(d.DefaultMetaDescription);
        row.DefaultMetaKeywords = NullIfWs(d.DefaultMetaKeywords);
        row.DefaultOgTitle = NullIfWs(d.DefaultOgTitle);
        row.OgDefaultImageUrl = NullIfWs(d.OgDefaultImageUrl);
        row.RobotsMeta = NullIfWs(d.RobotsMeta);
        row.GoogleSiteVerification = NullIfWs(d.GoogleSiteVerification);
        row.BingSiteVerification = NullIfWs(d.BingSiteVerification);
        row.ThemeColor = NullIfWs(d.ThemeColor);
        row.OgSiteName = NullIfWs(d.OgSiteName);
        row.TwitterSite = NullIfWs(d.TwitterSite);
        row.StructuredDataJsonLd = NullIfWs(d.StructuredDataJsonLd);

        await _db.SaveChangesAsync(ct);
        return Unit.Value;
    }

    private static string? NullIfWs(string? s)
    {
        if (string.IsNullOrWhiteSpace(s)) return null;
        var t = s.Trim();
        return t.Length == 0 ? null : t;
    }
}
