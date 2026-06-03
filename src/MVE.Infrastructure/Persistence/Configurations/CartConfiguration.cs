using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public class CartConfiguration : IEntityTypeConfiguration<Cart>
{
    public void Configure(EntityTypeBuilder<Cart> b)
    {
        b.HasQueryFilter(c => !c.IsDeleted);
        b.Property(c => c.CustomerUserId).IsRequired().HasMaxLength(450);
        b.HasIndex(c => c.CustomerUserId).IsUnique();
    }
}

public class CartItemConfiguration : IEntityTypeConfiguration<CartItem>
{
    public void Configure(EntityTypeBuilder<CartItem> b)
    {
        b.HasQueryFilter(i => !i.IsDeleted);
        b.Property(i => i.UnitPrice).HasPrecision(18, 2);
        b.HasOne(i => i.Cart).WithMany(c => c.Items).HasForeignKey(i => i.CartId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne(i => i.ProductVariant).WithMany(v => v.CartItems).HasForeignKey(i => i.ProductVariantId).OnDelete(DeleteBehavior.Restrict);
    }
}
