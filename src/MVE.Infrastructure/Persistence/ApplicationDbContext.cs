using Application.Common.Interfaces;
using Domain.Entities;
using Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>, IApplicationDbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    public DbSet<Vendor>          Vendors          => Set<Vendor>();
    public DbSet<VendorStore>     VendorStores     => Set<VendorStore>();
    public DbSet<Product>         Products         => Set<Product>();
    public DbSet<ProductImage>    ProductImages    => Set<ProductImage>();
    public DbSet<ProductVariant>  ProductVariants  => Set<ProductVariant>();
    public DbSet<Category>        Categories       => Set<Category>();
    public DbSet<Cart>            Carts            => Set<Cart>();
    public DbSet<CartItem>        CartItems        => Set<CartItem>();
    public DbSet<Order>           Orders           => Set<Order>();
    public DbSet<OrderItem>       OrderItems       => Set<OrderItem>();
    public DbSet<Shipment>        Shipments        => Set<Shipment>();
    public DbSet<Payment>         Payments         => Set<Payment>();
    public DbSet<Address>         Addresses        => Set<Address>();
    public DbSet<Coupon>          Coupons          => Set<Coupon>();
    public DbSet<Review>          Reviews          => Set<Review>();
    public DbSet<WishlistItem>    WishlistItems    => Set<WishlistItem>();
    public DbSet<Notification>    Notifications    => Set<Notification>();
    public DbSet<AuditLog>        AuditLogs        => Set<AuditLog>();
    public DbSet<RefreshToken>    RefreshTokens    => Set<RefreshToken>();
    public DbSet<StripeSettings>  StripeSettings   => Set<StripeSettings>();
    public DbSet<WebsiteSettings>  WebsiteSettings  => Set<WebsiteSettings>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);

        // Identity table renames for cleaner schema.
        builder.Entity<ApplicationUser>(b => b.ToTable("Users"));
        builder.Entity<IdentityRole>(b => b.ToTable("Roles"));
        builder.Entity<IdentityUserRole<string>>(b => b.ToTable("UserRoles"));
        builder.Entity<IdentityUserClaim<string>>(b => b.ToTable("UserClaims"));
        builder.Entity<IdentityUserLogin<string>>(b => b.ToTable("UserLogins"));
        builder.Entity<IdentityRoleClaim<string>>(b => b.ToTable("RoleClaims"));
        builder.Entity<IdentityUserToken<string>>(b => b.ToTable("UserTokens"));
    }
}
