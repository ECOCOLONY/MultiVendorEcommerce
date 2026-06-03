using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public class ProductVariantConfiguration : IEntityTypeConfiguration<ProductVariant>
{
    public void Configure(EntityTypeBuilder<ProductVariant> b)
    {
        b.HasQueryFilter(v => !v.IsDeleted);
        b.Property(v => v.Sku).IsRequired().HasMaxLength(80);
        b.Property(v => v.Name).HasMaxLength(120);
        b.Property(v => v.Color).HasMaxLength(50);
        b.Property(v => v.Size).HasMaxLength(20);
        b.Property(v => v.Price).HasPrecision(18, 2);
        b.HasIndex(v => v.Sku).IsUnique();
        b.HasIndex(v => v.ProductId);
        b.HasOne(v => v.Product).WithMany(p => p.Variants).HasForeignKey(v => v.ProductId).OnDelete(DeleteBehavior.Cascade);
    }
}
