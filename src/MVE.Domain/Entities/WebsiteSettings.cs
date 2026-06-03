namespace Domain.Entities;

/// <summary>Singleton storefront row (Id = <see cref="SingletonId"/>) â€” branding, contact, SEO.</summary>
public class WebsiteSettings
{
    public const int SingletonId = 1;

    public int Id { get; set; }

    public string SiteName { get; set; } = "MVE";
    public string? SiteTagline { get; set; }

    /// <summary>Optional absolute base URL (https://â€¦) for Stripe redirects, canonical, and OG URLs when the server host does not match the public URL.</summary>
    public string? PublicBaseUrl { get; set; }

    public string? ContactEmail { get; set; }
    public string? ContactPhone { get; set; }
    public string? ContactAddress { get; set; }

    /// <summary>Recipient for the public contact form (defaults to ContactEmail when empty).</summary>
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

    /// <summary>Optional JSON-LD snippet injected in the public layout.</summary>
    public string? StructuredDataJsonLd { get; set; }
}
