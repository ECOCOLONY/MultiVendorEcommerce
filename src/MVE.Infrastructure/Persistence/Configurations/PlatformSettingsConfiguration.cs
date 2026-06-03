using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public class StripeSettingsConfiguration : IEntityTypeConfiguration<StripeSettings>
{
    public void Configure(EntityTypeBuilder<StripeSettings> b)
    {
        b.ToTable("StripeSettings");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).ValueGeneratedNever();
        b.Property(x => x.SecretKey).IsRequired().HasMaxLength(500);
        b.Property(x => x.PublishableKey).IsRequired().HasMaxLength(500);
        b.Property(x => x.Currency).IsRequired().HasMaxLength(16);
        b.Property(x => x.WebhookSecret).HasMaxLength(500);
    }
}

public class WebsiteSettingsConfiguration : IEntityTypeConfiguration<WebsiteSettings>
{
    public void Configure(EntityTypeBuilder<WebsiteSettings> b)
    {
        b.ToTable("WebsiteSettings");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).ValueGeneratedNever();
        b.Property(x => x.SiteName).IsRequired().HasMaxLength(200);
        b.Property(x => x.SiteTagline).HasMaxLength(400);
        b.Property(x => x.PublicBaseUrl).HasMaxLength(500);

        b.Property(x => x.ContactEmail).HasMaxLength(256);
        b.Property(x => x.ContactPhone).HasMaxLength(80);
        b.Property(x => x.ContactAddress).HasMaxLength(2000);
        b.Property(x => x.ContactFormRecipientEmail).HasMaxLength(256);

        b.Property(x => x.HeaderLogoUrl).HasMaxLength(500);
        b.Property(x => x.FooterLogoUrl).HasMaxLength(500);
        b.Property(x => x.FaviconUrl).HasMaxLength(500);

        b.Property(x => x.TopBarPromo1).HasMaxLength(300);
        b.Property(x => x.TopBarPromo2).HasMaxLength(300);
        b.Property(x => x.TopBarPromo3).HasMaxLength(300);

        b.Property(x => x.FooterTagline).HasMaxLength(4000);
        b.Property(x => x.FooterCopyrightSuffix).HasMaxLength(500);

        b.Property(x => x.SocialFacebookUrl).HasMaxLength(500);
        b.Property(x => x.SocialTwitterUrl).HasMaxLength(500);
        b.Property(x => x.SocialInstagramUrl).HasMaxLength(500);
        b.Property(x => x.SocialYoutubeUrl).HasMaxLength(500);
        b.Property(x => x.SocialLinkedInUrl).HasMaxLength(500);

        b.Property(x => x.DefaultMetaTitle).HasMaxLength(200);
        b.Property(x => x.DefaultMetaDescription).HasMaxLength(800);
        b.Property(x => x.DefaultMetaKeywords).HasMaxLength(500);
        b.Property(x => x.DefaultOgTitle).HasMaxLength(200);
        b.Property(x => x.OgDefaultImageUrl).HasMaxLength(500);
        b.Property(x => x.RobotsMeta).HasMaxLength(120);

        b.Property(x => x.GoogleSiteVerification).HasMaxLength(200);
        b.Property(x => x.BingSiteVerification).HasMaxLength(200);
        b.Property(x => x.ThemeColor).HasMaxLength(32);

        b.Property(x => x.OgSiteName).HasMaxLength(200);
        b.Property(x => x.TwitterSite).HasMaxLength(120);

        b.Property(x => x.StructuredDataJsonLd).HasMaxLength(8000);
    }
}
