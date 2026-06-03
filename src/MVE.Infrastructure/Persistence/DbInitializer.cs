using Domain.Entities;
using Domain.Enums;
using Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.Persistence;

internal sealed record SeedVariant(string SkuSuffix, string Name, decimal Price, int Stock, string? Color = null, string? Size = null);

internal sealed record SeedProduct(
    string Name,
    string Description,
    decimal BasePrice,
    string Slug,
    bool Featured = false,
    bool ExtraGalleryImage = false,
    IReadOnlyList<SeedVariant>? Variants = null);

internal sealed record MarketplaceVendorSeed(
    string Email,
    string OwnerFullName,
    string Password,
    string BusinessName,
    string StoreName,
    string CategorySlug,
    string? ContactPhone,
    string StoreDescription,
    params SeedProduct[] Products);

public static class DbInitializer
{
    public static async Task InitializeAsync(IServiceProvider services)
    {
        var db = services.GetRequiredService<ApplicationDbContext>();
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();

        await db.Database.MigrateAsync();

        foreach (var role in Roles.All)
            if (!await roleManager.RoleExistsAsync(role))
                await roleManager.CreateAsync(new IdentityRole(role));

        await EnsurePlatformSettingsAsync(db);

        // ----- Admin -----
        var adminEmail = "admin@shop.com";
        if (await userManager.FindByEmailAsync(adminEmail) is null)
        {
            var admin = new ApplicationUser { UserName = adminEmail, Email = adminEmail, FullName = "Site Administrator", EmailConfirmed = true };
            await userManager.CreateAsync(admin, "Admin123!");
            await userManager.AddToRoleAsync(admin, Roles.Admin);
        }

        // ----- Categories (core set) -----
        if (!await db.Categories.AnyAsync())
        {
            db.Categories.AddRange(
                new Category { Name = "Electronics", Slug = "electronics", DisplayOrder = 1, IsActive = true, IconUrl = "https://picsum.photos/seed/electronics/120/120" },
                new Category { Name = "Fashion",     Slug = "fashion",     DisplayOrder = 2, IsActive = true, IconUrl = "https://picsum.photos/seed/fashion/120/120" },
                new Category { Name = "Home",        Slug = "home",        DisplayOrder = 3, IsActive = true, IconUrl = "https://picsum.photos/seed/home/120/120" },
                new Category { Name = "Books",       Slug = "books",       DisplayOrder = 4, IsActive = true, IconUrl = "https://picsum.photos/seed/books/120/120" },
                new Category { Name = "Sports",      Slug = "sports",      DisplayOrder = 5, IsActive = true, IconUrl = "https://picsum.photos/seed/sports/120/120" }
            );
            await db.SaveChangesAsync();
        }

        // ----- Extra categories (idempotent — for richer marketplace seed) -----
        await EnsureCategoryAsync(db, "Beauty & Care", "beauty", 6, "beauty-cat");
        await EnsureCategoryAsync(db, "Toys & Games", "toys", 7, "toys-cat");
        await EnsureCategoryAsync(db, "Garden & Outdoor", "garden", 8, "garden-cat");
        await EnsureCategoryAsync(db, "Grocery & Pantry", "grocery", 9, "grocery-cat");
        await EnsureCategoryAsync(db, "Automotive", "automotive", 10, "auto-cat");

        // ----- Demo vendors (15 stores, 70+ products) -----
        foreach (var def in MarketplaceSeed.Vendors)
            await SeedDemoVendorAsync(db, userManager, def);

        // ----- Demo customer -----
        var buyerEmail = "demo@shop.com";
        if (await userManager.FindByEmailAsync(buyerEmail) is null)
        {
            var buyer = new ApplicationUser { UserName = buyerEmail, Email = buyerEmail, FullName = "Demo Customer", EmailConfirmed = true };
            await userManager.CreateAsync(buyer, "Demo123!");
            await userManager.AddToRoleAsync(buyer, Roles.Customer);
        }

        // ----- Sample platform coupon -----
        if (!await db.Coupons.AnyAsync(c => c.Code == "WELCOME10"))
        {
            db.Coupons.Add(new Coupon
            {
                Code = "WELCOME10",
                DiscountType = CouponDiscountType.Percentage,
                DiscountValue = 10m,
                MinimumOrderAmount = 0m,
                IsActive = true,
                VendorStoreId = null,
                StartsAtUtc = DateTime.UtcNow.AddDays(-1)
            });
            await db.SaveChangesAsync();
        }
    }

    private static async Task EnsureCategoryAsync(ApplicationDbContext db, string name, string slug, int displayOrder, string picsumSeed)
    {
        if (await db.Categories.AnyAsync(c => c.Slug == slug)) return;
        db.Categories.Add(new Category
        {
            Name = name,
            Slug = slug,
            DisplayOrder = displayOrder,
            IsActive = true,
            IconUrl = $"https://picsum.photos/seed/{picsumSeed}/120/120"
        });
        await db.SaveChangesAsync();
    }

    private static async Task EnsurePlatformSettingsAsync(ApplicationDbContext db)
    {
        if (!await db.StripeSettings.AnyAsync())
        {
            db.StripeSettings.Add(new StripeSettings
            {
                Id = StripeSettings.SingletonId,
                Currency = "usd",
                SecretKey = "",
                PublishableKey = ""
            });
        }

        if (!await db.WebsiteSettings.AnyAsync())
        {
            db.WebsiteSettings.Add(new WebsiteSettings
            {
                Id = WebsiteSettings.SingletonId,
                SiteName = "Ramadan Abdullah",
                SiteTagline = "Independent vendors, one trusted checkout.",
                TopBarPromo1 = "Free shipping over $50",
                TopBarPromo2 = "Secure payments",
                TopBarPromo3 = "Easy returns",
                FooterTagline = "A trusted marketplace where independent vendors meet curious customers — one cart, one checkout, many stores.",
                FooterCopyrightSuffix = "Ramadan Abdullah",
                ContactEmail = "support@example.com",
                DefaultMetaTitle = "Ramadan Abdullah — Shop independent stores",
                DefaultMetaDescription = "Discover products from verified vendors. Secure checkout, fast shipping, and easy returns.",
                DefaultMetaKeywords = "marketplace, multi-vendor, online shopping, independent sellers",
                RobotsMeta = "index, follow",
                OgSiteName = "Ramadan Abdullah"
            });
        }

        await db.SaveChangesAsync();
    }

    private static async Task SeedDemoVendorAsync(ApplicationDbContext db, UserManager<ApplicationUser> userManager, MarketplaceVendorSeed def)
    {
        if (await userManager.FindByEmailAsync(def.Email) is not null)
            return;

        var user = new ApplicationUser { UserName = def.Email, Email = def.Email, FullName = def.OwnerFullName, EmailConfirmed = true };
        var create = await userManager.CreateAsync(user, def.Password);
        if (!create.Succeeded) return;
        await userManager.AddToRoleAsync(user, Roles.Vendor);

        var storeSlug = SlugifyStore(def.StoreName);
        var vendor = new Vendor
        {
            OwnerUserId = user.Id,
            BusinessName = def.BusinessName,
            IsApproved = true,
            DefaultCommissionPercent = 10m
        };
        var store = new VendorStore
        {
            Vendor = vendor,
            Name = def.StoreName,
            Slug = storeSlug,
            Description = def.StoreDescription,
            IsActive = true,
            LogoUrl = $"https://picsum.photos/seed/{storeSlug}-logo/200/200",
            BannerUrl = $"https://picsum.photos/seed/{storeSlug}-banner/1200/400",
            ContactEmail = def.Email,
            ContactPhone = def.ContactPhone
        };
        db.Vendors.Add(vendor);
        db.VendorStores.Add(store);
        await db.SaveChangesAsync();

        var category = await db.Categories.FirstOrDefaultAsync(c => c.Slug == def.CategorySlug);
        if (category is null) return;

        foreach (var p in def.Products)
        {
            if (await db.Products.AnyAsync(x => x.Slug == p.Slug)) continue;

            var images = BuildProductImages(p);
            var variants = BuildVariants(p);

            var product = new Product
            {
                VendorStoreId = store.Id,
                CategoryId = category.Id,
                Name = p.Name,
                Slug = p.Slug,
                Description = p.Description,
                BasePrice = p.BasePrice,
                IsPublished = true,
                IsFeatured = p.Featured,
                ApprovalStatus = ProductApprovalStatus.Approved,
                Images = images,
                Variants = variants
            };
            db.Products.Add(product);
        }

        await db.SaveChangesAsync();
    }

    private static List<ProductImage> BuildProductImages(SeedProduct p)
    {
        var alt = p.Name;
        var list = new List<ProductImage>
        {
            new() { Url = $"https://picsum.photos/seed/{p.Slug}-1/800/600", AltText = alt, IsMain = true,  SortOrder = 0 },
            new() { Url = $"https://picsum.photos/seed/{p.Slug}-2/800/600", AltText = alt, IsMain = false, SortOrder = 1 }
        };
        if (p.Featured || p.ExtraGalleryImage)
            list.Add(new ProductImage { Url = $"https://picsum.photos/seed/{p.Slug}-3/800/600", AltText = alt, IsMain = false, SortOrder = 2 });
        return list;
    }

    private static List<ProductVariant> BuildVariants(SeedProduct p)
    {
        if (p.Variants is { Count: > 0 })
        {
            var list = new List<ProductVariant>();
            foreach (var v in p.Variants)
            {
                var sku = $"{p.Slug.ToUpperInvariant().Replace("-", "")}-{v.SkuSuffix}";
                list.Add(new ProductVariant
                {
                    Sku = sku,
                    Name = v.Name,
                    Color = v.Color,
                    Size = v.Size,
                    Price = v.Price,
                    StockQuantity = v.Stock,
                    IsActive = true
                });
            }
            return list;
        }

        return new List<ProductVariant>
        {
            new()
            {
                Sku = $"{p.Slug.ToUpperInvariant().Replace("-", "")}-DEF",
                Name = "Default",
                Price = p.BasePrice,
                StockQuantity = 25,
                IsActive = true
            }
        };
    }

    private static string SlugifyStore(string name) =>
        string.Join('-', name.ToLowerInvariant().Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            .Replace(".", "");
}

/// <summary>All marketplace demo stores and products (runs on startup after migrations; idempotent per vendor email).</summary>
internal static class MarketplaceSeed
{
    public static readonly IReadOnlyList<MarketplaceVendorSeed> Vendors =
    [
        new("techgear@shop.com", "Alex Rivera", "Demo123!", "TechGear Co.", "TechGear", "electronics",
            "+1-555-0101",
            "Cutting-edge electronics, audio, and smart accessories. Fast shipping, genuine warranty.",
            new SeedProduct("Wireless Earbuds Pro", "Bluetooth 5.3, ANC, 24h case battery. Clear calls, low latency.", 79.99m, "earbuds-pro", true, true),
            new SeedProduct("Smart Watch X1", "AMOLED display, GPS, SpO₂, 7-day battery. Swim-proof.", 149.00m, "smart-watch-x1", true, true),
            new SeedProduct("4K Action Camera", "Waterproof housing, 60fps 4K, RockSteady stabilization.", 199.50m, "action-camera-4k"),
            new SeedProduct("Bluetooth Speaker Mini", "IPX5, 12h playtime, stereo pairing.", 39.90m, "bt-speaker-mini"),
            new SeedProduct("Mechanical Keyboard 75%", "Hot-swap PCB, south-facing RGB, gasket mount.", 89.00m, "mech-keyboard-75"),
            new SeedProduct("USB-C Hub 7-in-1", "HDMI 4K, SD/microSD, 100W PD passthrough.", 45.00m, "usb-c-hub-7in1")),

        new("stylehub@shop.com", "Sara Chen", "Demo123!", "StyleHub LLC", "StyleHub", "fashion",
            "+1-555-0102",
            "Modern apparel and accessories — curated fits for work, weekend, and everywhere between.",
            new SeedProduct("Classic Leather Jacket", "Buttery leather, tailored silhouette, satin lining.", 229.00m, "leather-jacket-classic", true, true),
            new SeedProduct("Organic Cotton Tee 3-Pack", "GOTS cotton. Tagless, pre-shrunk.", 34.99m, "cotton-tee-3pack", false, false,
                new[]
                {
                    new SeedVariant("S-WHT", "S / White", 34.99m, 20, "White", "S"),
                    new SeedVariant("M-WHT", "M / White", 34.99m, 30, "White", "M"),
                    new SeedVariant("M-BLK", "M / Black", 34.99m, 25, "Black", "M"),
                    new SeedVariant("L-BLK", "L / Black", 34.99m, 18, "Black", "L")
                }),
            new SeedProduct("Canvas Court Sneakers", "Vulcanized sole, cushioned insole, unisex.", 59.50m, "canvas-sneakers"),
            new SeedProduct("Merino Wool Beanie", "Temperature regulating, itch-free.", 18.00m, "wool-beanie"),
            new SeedProduct("Roll-Top Denim Backpack", "16\" laptop sleeve, water-resistant.", 69.95m, "denim-backpack")),

        new("homecomfort@shop.com", "Jordan Miller", "Demo123!", "HomeComfort Inc.", "HomeComfort", "home",
            "+1-555-0103",
            "Soft linens, smart storage, and everyday homeware to make your space calm and organized.",
            new SeedProduct("Linen Duvet Set — Queen", "Stone-washed European linen. Includes 2 shams.", 189.00m, "linen-duvet-queen", true, true),
            new SeedProduct("Ceramic Table Lamp", "Warm 2700K LED included, touch dimmer.", 49.00m, "ceramic-table-lamp"),
            new SeedProduct("Bamboo Bath Mat", "Non-slip backing, quick-dry waffle weave.", 32.00m, "bamboo-bath-mat"),
            new SeedProduct("Glass Food Storage — 12pc", "Oven-safe borosilicate, BPA-free lids.", 44.50m, "glass-storage-12pc"),
            new SeedProduct("Velvet Throw Blanket", "50x60in, machine washable.", 36.00m, "velvet-throw-blanket")),

        new("bookworm@shop.com", "Emily Walsh", "Demo123!", "BookWorm Depot LLC", "BookWorm Depot", "books",
            "+1-555-0104",
            "Bestsellers, indie picks, and study essentials. We pack every order with care.",
            new SeedProduct("The Art of Readable Code", "Classic software craftsmanship — practices that scale.", 42.00m, "art-readable-code", true, true),
            new SeedProduct("National Parks — Photo Treasury", "Oversized hardcover, 200+ plates.", 55.00m, "parks-photo-treasury"),
            new SeedProduct("Lined Journal — Vegan Leather", "192 pages, lay-flat binding.", 22.00m, "lined-journal-vegan"),
            new SeedProduct("Children's Illustrated Atlas", "Ages 6–10, updated countries & flags.", 28.00m, "kids-atlas-illus"),
            new SeedProduct("Cookbook — One-Pan Wonders", "120 weeknight dinners, nutritional callouts.", 31.00m, "cookbook-one-pan")),

        new("prosports@shop.com", "Marcus Webb", "Demo123!", "Pro Sports Outlet", "Pro Sports Outlet", "sports",
            "+1-555-0105",
            "Gear for training days and game days — from court to trail.",
            new SeedProduct("Carbon Trail Running Shoes", "8mm drop, rock plate, Vibram-style outsole.", 139.00m, "carbon-trail-shoes", true, true),
            new SeedProduct("Adjustable Dumbbells 25lb Pair", "Quick dial, compact stand included.", 199.00m, "adj-dumbbells-25"),
            new SeedProduct("Pickleball Paddle Set", "2 paddles, 4 balls, carry bag.", 64.99m, "pickleball-set"),
            new SeedProduct("Yoga Mat Pro", "6mm TPE, alignment lines, carrying strap.", 48.00m, "yoga-mat-pro"),
            new SeedProduct("Insulated Hydration Pack", "2L bladder, trail-grade zippers.", 72.00m, "hydration-pack-2l")),

        new("glowbeauty@shop.com", "Priya Nair", "Demo123!", "Glow Beauty Lab", "Glow Beauty Lab", "beauty",
            "+1-555-0106",
            "Clean-formulation skincare and pro tools — glow responsibly.",
            new SeedProduct("Vitamin C Brightening Serum", "15% L-ascorbic, ferulic acid, daily AM use.", 38.00m, "vitamin-c-serum-v2", true, true),
            new SeedProduct("Hyaluronic Dew Moisturizer", "Multi-weight HA, niacinamide, fragrance-free.", 29.00m, "ha-dew-moisturizer"),
            new SeedProduct("Silk Heatless Curl Set", "Satin rod + scrunchies, overnight curls.", 24.00m, "heatless-curl-set"),
            new SeedProduct("Latte Makeup Brush 12pc", "Dense synthetic, travel tin.", 42.00m, "brush-set-latte-12")),

        new("gadgetbay@shop.com", "Chris Okonkwo", "Demo123!", "GadgetBay Ltd.", "Gadget Bay", "electronics",
            "+1-555-0107",
            "Budget-friendly tech that punches above its weight — tested by our team.",
            new SeedProduct("Budget Android Tablet 10\"", "1080p, dual speakers, kid mode.", 129.00m, "budget-tablet-10", true, true),
            new SeedProduct("Noise-Canceling Headphones Lite", "40h battery, fold-flat, USB-C charge.", 79.00m, "nc-headphones-lite"),
            new SeedProduct("Wireless Charging Stand", "15W Mag-style + AirPods spot.", 35.00m, "wireless-stand-15w"),
            new SeedProduct("RGB Gaming Mouse", "26K DPI, 80g, PTFE feet.", 39.00m, "rgb-gaming-mouse")),

        new("freshharvest@shop.com", "Olivia Santos", "Demo123!", "Fresh Harvest Co.", "Fresh Harvest Co.", "grocery",
            "+1-555-0108",
            "Pantry staples and small-batch specialties sourced with transparency.",
            new SeedProduct("Single-Origin Coffee Beans 12oz", "Medium roast, notes of cocoa & cherry.", 18.50m, "coffee-single-origin-12", true, true),
            new SeedProduct("Extra Virgin Olive Oil 750ml", "Cold-pressed, harvest date on label.", 22.00m, "evoo-750ml"),
            new SeedProduct("Artisan Pasta Variety 6pk", "Bronze die, slow-dried semolina.", 27.00m, "pasta-artisan-6"),
            new SeedProduct("Raw Wildflower Honey 16oz", "Unfiltered, glass jar.", 19.99m, "honey-wildflower-16")),

        new("littleplay@shop.com", "Tom Brennan", "Demo123!", "Little Play Toys", "Little Play Toys", "toys",
            "+1-555-0109",
            "STEM, pretend play, and cozy plush — safety-tested for curious kids.",
            new SeedProduct("Magnetic Building Tiles 100pc", "Strong magnets, compatible with major brands.", 79.00m, "magtiles-100", true, true),
            new SeedProduct("Wooden Train Starter Set", "28 track pieces, engine + 3 cars.", 54.00m, "wood-train-starter"),
            new SeedProduct("Plush Dino — Jumbo", "Machine washable, embroidered eyes.", 36.00m, "plush-dino-jumbo"),
            new SeedProduct("Doctor Pretend Play Kit", "12 tools, carrying case, age 3+.", 29.00m, "doctor-pretend-kit")),

        new("greenthumb@shop.com", "Rachel Green", "Demo123!", "Green Thumb Supply", "Green Thumb Garden", "garden",
            "+1-555-0110",
            "Everything for balconies, backyards, and hobby greenhouses.",
            new SeedProduct("Raised Bed Kit 4x4", "Cedar, hardware included, no tools drill.", 119.00m, "raised-bed-4x4", true, true),
            new SeedProduct("Expandable Hose 75ft", "Brass fittings, 8-pattern nozzle.", 49.00m, "hose-expand-75"),
            new SeedProduct("Heirloom Vegetable Seed Vault", "30 non-GMO varieties, storage tin.", 34.00m, "seed-vault-heirloom"),
            new SeedProduct("Solar Path Lights — 8pk", "Warm white, dusk-to-dawn sensor.", 59.00m, "solar-path-8pk")),

        new("autoparts@shop.com", "Daniel Kowalski", "Demo123!", "Auto Parts Direct", "Auto Parts Direct", "automotive",
            "+1-555-0111",
            "OEM-style replacement parts and pro-grade fluids for DIYers.",
            new SeedProduct("Synthetic Oil 5W-30 — 5qt", "Dexos-approved, extended drain.", 28.99m, "syn-oil-5w30-5qt", true, true),
            new SeedProduct("Cabin Air Filter — Universal Fit", "HEPA-grade media, install guide QR.", 16.00m, "cabin-filter-uni"),
            new SeedProduct("LED Headlight Bulb Pair H11", "Cool white 6000K, CAN-bus friendly.", 44.00m, "led-bulb-h11-pair"),
            new SeedProduct("All-Weather Floor Liners — 4pc", "Laser measured, deep channels.", 89.00m, "floor-liners-4pc")),

        new("nordicliving@shop.com", "Ingrid Svensson", "Demo123!", "Nordic Living AB", "Nordic Living", "home",
            "+1-555-0112",
            "Scandinavian minimalism — natural materials, calm palettes.",
            new SeedProduct("Oak Floating Shelf Trio", "Hidden bracket, 24/36/48in lengths.", 94.00m, "oak-shelf-trio", true, true),
            new SeedProduct("Wool Knit Pouf", "Handmade cover, recycled fill.", 110.00m, "wool-pouf-knit"),
            new SeedProduct("Stoneware Dinner Plate Set", "4 matte plates, dishwasher safe.", 58.00m, "stoneware-plates-4")),

        new("campus@shop.com", "Kevin Park", "Demo123!", "Campus Supply Co.", "Campus Supply Co.", "books",
            "+1-555-0113",
            "Textbooks, desk organizers, and dorm hacks — semester ready.",
            new SeedProduct("Scientific Calculator CX", "Graphing, exam-approved modes.", 89.00m, "sci-calc-cx", true, true),
            new SeedProduct("Desk Organizer Mesh 6pc", "Drawer + vertical sorter.", 24.00m, "desk-mesh-6pc"),
            new SeedProduct("Bluebook Exam Notebook 12pk", "College ruled perforated.", 14.00m, "bluebook-12pk")),

        new("athleisure@shop.com", "Mia Foster", "Demo123!", "Athleisure Hub LLC", "Athleisure Hub", "fashion",
            "+1-555-0114",
            "Performance fabrics for studio, street, and sofa Sundays.",
            new SeedProduct("High-Waist Leggings 28\"", "Squat-proof, side pockets, 4-way stretch.", 54.00m, "hw-leggings-28", true, false,
                new[]
                {
                    new SeedVariant("XS-BLK", "XS / Black", 54.00m, 12, "Black", "XS"),
                    new SeedVariant("S-BLK", "S / Black", 54.00m, 22, "Black", "S"),
                    new SeedVariant("M-NAV", "M / Navy", 52.00m, 12, "Navy", "M")
                }),
            new SeedProduct("Performance Tank", "Sweat-wicking mesh back panel.", 29.00m, "perform-tank"),
            new SeedProduct("Quarter-Zip Fleece", "Mid-weight, recycled poly.", 62.00m, "qz-fleece-recycled")),

        new("techvault@shop.com", "Samir Haddad", "Demo123!", "TechVault Accessories", "Tech Vault", "electronics",
            "+1-555-0115",
            "Cases, cables, power — the small things that save your day.",
            new SeedProduct("Laptop Sleeve 14\" — Recycled", "Felted fiber, magnetic flap.", 39.00m, "laptop-sleeve-14-recycle", true, true),
            new SeedProduct("Braided USB-C Cable 2m", "100W e-mark, aluminum housings.", 19.00m, "usbc-braid-2m"),
            new SeedProduct("GaN Charger 65W 3-Port", "Foldable prongs, PPS support.", 45.00m, "gan-charger-65w"),
            new SeedProduct("Webcam Privacy Pack 3pk", "Ultra-thin sliders.", 9.00m, "webcam-cover-3pk"))
    ];
}
