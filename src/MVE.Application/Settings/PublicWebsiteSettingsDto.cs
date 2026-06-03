namespace Application.Settings;

/// <summary>Read model for public storefront layout, contact page, and SEO.</summary>
public sealed class PublicWebsiteSettingsDto
{
    public string SiteName { get; set; } = "MVE";
    public string? SiteTagline { get; set; }
    public string? PublicBaseUrl { get; set; }

    public string? ContactEmail { get; set; }
    public string? ContactPhone { get; set; }
    public string? ContactAddress { get; set; }
    public string? ContactFormRecipientEmail { get; set; }

    public string? HeaderLogoUrl { get; set; }
    public string? FooterLogoUrl { get; set; }
    public string? FaviconUrl { get; set; }

    public string? TopBarPromo1 { get; set; }
    public string? TopBarPromo2 { get; set; }
    public string? TopBarPromo3 { get; set; }

    public string? FooterTagline { get; set; }
    public string? FooterCopyrightSuffix { get; set; }

    public string? SocialFacebookUrl { get; set; }
    public string? SocialTwitterUrl { get; set; }
    public string? SocialInstagramUrl { get; set; }
    public string? SocialYoutubeUrl { get; set; }
    public string? SocialLinkedInUrl { get; set; }

    public string? DefaultMetaTitle { get; set; }
    public string? DefaultMetaDescription { get; set; }
    public string? DefaultMetaKeywords { get; set; }
    public string? DefaultOgTitle { get; set; }
    public string? OgDefaultImageUrl { get; set; }
    public string? RobotsMeta { get; set; }

    public string? GoogleSiteVerification { get; set; }
    public string? BingSiteVerification { get; set; }
    public string? ThemeColor { get; set; }

    public string? OgSiteName { get; set; }
    public string? TwitterSite { get; set; }

    public string? StructuredDataJsonLd { get; set; }

    public string EffectiveContactRecipient =>
        !string.IsNullOrWhiteSpace(ContactFormRecipientEmail)
            ? ContactFormRecipientEmail!.Trim()
            : (ContactEmail?.Trim() ?? "");
}
