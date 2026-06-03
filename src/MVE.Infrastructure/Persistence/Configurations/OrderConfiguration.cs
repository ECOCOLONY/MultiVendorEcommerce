using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> b)
    {
        b.HasQueryFilter(o => !o.IsDeleted);
        b.Property(o => o.CustomerUserId).IsRequired().HasMaxLength(450);
        b.Property(o => o.OrderNumber).IsRequired().HasMaxLength(40);
        b.HasIndex(o => o.OrderNumber).IsUnique();
        b.HasIndex(o => o.CustomerUserId);

        b.Property(o => o.Subtotal).HasPrecision(18, 2);
        b.Property(o => o.TaxAmount).HasPrecision(18, 2);
        b.Property(o => o.ShippingAmount).HasPrecision(18, 2);
        b.Property(o => o.DiscountAmount).HasPrecision(18, 2);
        b.Property(o => o.Total).HasPrecision(18, 2);

        b.Property(o => o.ShippingFullName).HasMaxLength(200);
        b.Property(o => o.ShippingPhone).HasMaxLength(50);
        b.Property(o => o.ShippingLine1).HasMaxLength(200);
        b.Property(o => o.ShippingLine2).HasMaxLength(200);
        b.Property(o => o.ShippingCity).HasMaxLength(100);
        b.Property(o => o.ShippingState).HasMaxLength(100);
        b.Property(o => o.ShippingPostalCode).HasMaxLength(20);
        b.Property(o => o.ShippingCountry).HasMaxLength(100);
        b.Property(o => o.RefundReason).HasMaxLength(2000);

        // Two FKs to Address — break cascade cycles.
        b.HasOne(o => o.ShippingAddress).WithMany().HasForeignKey(o => o.ShippingAddressId).OnDelete(DeleteBehavior.NoAction);
        b.HasOne(o => o.BillingAddress).WithMany().HasForeignKey(o => o.BillingAddressId).OnDelete(DeleteBehavior.NoAction);

        b.HasOne(o => o.Coupon).WithMany(c => c.Orders).HasForeignKey(o => o.CouponId).OnDelete(DeleteBehavior.SetNull);
    }
}

public class OrderItemConfiguration : IEntityTypeConfiguration<OrderItem>
{
    public void Configure(EntityTypeBuilder<OrderItem> b)
    {
        b.HasQueryFilter(i => !i.IsDeleted);
        b.Property(i => i.ProductName).IsRequired().HasMaxLength(200);
        b.Property(i => i.VariantName).HasMaxLength(120);
        b.Property(i => i.UnitPrice).HasPrecision(18, 2);
        b.Property(i => i.LineTotal).HasPrecision(18, 2);
        b.Property(i => i.CommissionPercent).HasPrecision(5, 2);
        b.Property(i => i.CommissionAmount).HasPrecision(18, 2);
        b.Property(i => i.VendorNetAmount).HasPrecision(18, 2);

        b.HasIndex(i => i.OrderId);
        b.HasIndex(i => i.VendorStoreId);

        b.HasOne(i => i.Order).WithMany(o => o.Items).HasForeignKey(i => i.OrderId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne(i => i.ProductVariant).WithMany(v => v.OrderItems).HasForeignKey(i => i.ProductVariantId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(i => i.VendorStore).WithMany().HasForeignKey(i => i.VendorStoreId).OnDelete(DeleteBehavior.Restrict);
    }
}
