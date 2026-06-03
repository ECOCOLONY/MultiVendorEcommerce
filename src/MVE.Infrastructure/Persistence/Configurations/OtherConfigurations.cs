using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public class AddressConfiguration : IEntityTypeConfiguration<Address>
{
    public void Configure(EntityTypeBuilder<Address> b)
    {
        b.HasQueryFilter(a => !a.IsDeleted);
        b.Property(a => a.UserId).IsRequired().HasMaxLength(450);
        b.Property(a => a.Label).HasMaxLength(50);
        b.Property(a => a.Line1).IsRequired().HasMaxLength(200);
        b.Property(a => a.Line2).HasMaxLength(200);
        b.Property(a => a.City).IsRequired().HasMaxLength(100);
        b.Property(a => a.State).HasMaxLength(100);
        b.Property(a => a.PostalCode).IsRequired().HasMaxLength(20);
        b.Property(a => a.Country).IsRequired().HasMaxLength(100);
        b.Property(a => a.Phone).HasMaxLength(50);
        b.HasIndex(a => a.UserId);
    }
}

public class CouponConfiguration : IEntityTypeConfiguration<Coupon>
{
    public void Configure(EntityTypeBuilder<Coupon> b)
    {
        b.HasQueryFilter(c => !c.IsDeleted);
        b.Property(c => c.Code).IsRequired().HasMaxLength(40);
        b.Property(c => c.DiscountValue).HasPrecision(18, 2);
        b.Property(c => c.MinimumOrderAmount).HasPrecision(18, 2);
        b.HasIndex(c => c.Code).IsUnique();
        b.HasOne(c => c.VendorStore).WithMany(s => s.Coupons).HasForeignKey(c => c.VendorStoreId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class PaymentConfiguration : IEntityTypeConfiguration<Payment>
{
    public void Configure(EntityTypeBuilder<Payment> b)
    {
        b.HasQueryFilter(p => !p.IsDeleted);
        b.Property(p => p.Amount).HasPrecision(18, 2);
        b.Property(p => p.Provider).HasMaxLength(80);
        b.Property(p => p.ExternalPaymentId).HasMaxLength(200);
        b.HasOne(p => p.Order).WithMany(o => o.Payments).HasForeignKey(p => p.OrderId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class ShipmentConfiguration : IEntityTypeConfiguration<Shipment>
{
    public void Configure(EntityTypeBuilder<Shipment> b)
    {
        b.HasQueryFilter(s => !s.IsDeleted);
        b.Property(s => s.AssignedDeliveryUserId).HasMaxLength(450);
        b.Property(s => s.Carrier).HasMaxLength(80);
        b.Property(s => s.TrackingNumber).HasMaxLength(120);
        b.HasOne(s => s.Order).WithMany(o => o.Shipments).HasForeignKey(s => s.OrderId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne(s => s.VendorStore).WithMany().HasForeignKey(s => s.VendorStoreId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class ReviewConfiguration : IEntityTypeConfiguration<Review>
{
    public void Configure(EntityTypeBuilder<Review> b)
    {
        b.HasQueryFilter(r => !r.IsDeleted);
        b.Property(r => r.CustomerUserId).IsRequired().HasMaxLength(450);
        b.Property(r => r.Title).HasMaxLength(200);
        b.Property(r => r.Comment).HasMaxLength(2000);
        b.Property(r => r.VendorReply).HasMaxLength(2000);
        b.HasIndex(r => r.ProductId);
        b.HasOne(r => r.Product).WithMany(p => p.Reviews).HasForeignKey(r => r.ProductId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne(r => r.OrderItem).WithMany().HasForeignKey(r => r.OrderItemId).OnDelete(DeleteBehavior.SetNull);
    }
}

public class WishlistItemConfiguration : IEntityTypeConfiguration<WishlistItem>
{
    public void Configure(EntityTypeBuilder<WishlistItem> b)
    {
        b.HasQueryFilter(w => !w.IsDeleted);
        b.Property(w => w.CustomerUserId).IsRequired().HasMaxLength(450);
        b.HasIndex(w => new { w.CustomerUserId, w.ProductId }).IsUnique();
        b.HasOne(w => w.Product).WithMany(p => p.WishlistItems).HasForeignKey(w => w.ProductId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> b)
    {
        b.HasQueryFilter(n => !n.IsDeleted);
        b.Property(n => n.UserId).IsRequired().HasMaxLength(450);
        b.Property(n => n.Title).IsRequired().HasMaxLength(200);
        b.Property(n => n.Body).IsRequired().HasMaxLength(2000);
        b.Property(n => n.ActionUrl).HasMaxLength(500);
        b.HasIndex(n => n.UserId);
    }
}

public class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> b)
    {
        b.Property(a => a.EntityName).IsRequired().HasMaxLength(120);
        b.Property(a => a.EntityId).HasMaxLength(80);
        b.Property(a => a.IpAddress).HasMaxLength(50);
        b.HasIndex(a => a.UserId);
        b.HasIndex(a => a.EntityName);
    }
}

public class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> b)
    {
        b.HasQueryFilter(t => !t.IsDeleted);
        b.Property(t => t.UserId).IsRequired().HasMaxLength(450);
        b.Property(t => t.TokenHash).IsRequired().HasMaxLength(500);
        b.Property(t => t.ReplacedByTokenHash).HasMaxLength(500);
        b.HasIndex(t => t.UserId);
        b.HasIndex(t => t.TokenHash);
    }
}
