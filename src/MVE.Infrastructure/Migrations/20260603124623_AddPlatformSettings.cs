using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPlatformSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "StripeSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false),
                    SecretKey = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    PublishableKey = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Currency = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    WebhookSecret = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StripeSettings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "WebsiteSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false),
                    SiteName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    SiteTagline = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: true),
                    PublicBaseUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ContactEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ContactPhone = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    ContactAddress = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    ContactFormRecipientEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    HeaderLogoUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    FooterLogoUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    FaviconUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    TopBarPromo1 = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    TopBarPromo2 = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    TopBarPromo3 = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    FooterTagline = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    FooterCopyrightSuffix = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    SocialFacebookUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    SocialTwitterUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    SocialInstagramUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    SocialYoutubeUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    SocialLinkedInUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    DefaultMetaTitle = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    DefaultMetaDescription = table.Column<string>(type: "nvarchar(800)", maxLength: 800, nullable: true),
                    DefaultMetaKeywords = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    DefaultOgTitle = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    OgDefaultImageUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    RobotsMeta = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    GoogleSiteVerification = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    BingSiteVerification = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    ThemeColor = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    OgSiteName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    TwitterSite = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    StructuredDataJsonLd = table.Column<string>(type: "nvarchar(max)", maxLength: 8000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WebsiteSettings", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "StripeSettings");

            migrationBuilder.DropTable(
                name: "WebsiteSettings");
        }
    }
}
