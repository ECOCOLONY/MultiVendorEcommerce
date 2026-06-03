using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> b)
    {
        b.HasQueryFilter(p => !p.IsDeleted);
        b.Property(p => p.Name).IsRequired().HasMaxLength(200);
        b.Property(p => p.Slug).IsRequired().HasMaxLength(220);
        b.Property(p => p.Description).HasMaxLength(4000);
        b.Property(p => p.BasePrice).HasPrecision(18, 2);
        b.Property(p => p.AverageRating).HasPrecision(3, 2);

        b.HasIndex(p => p.Slug).IsUnique();
        b.HasIndex(p => p.VendorStoreId);
        b.HasIndex(p => p.CategoryId);

        b.HasOne(p => p.VendorStore).WithMany(s => s.Products).HasForeignKey(p => p.VendorStoreId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(p => p.Category).WithMany(c => c.Products).HasForeignKey(p => p.CategoryId).OnDelete(DeleteBehavior.Restrict);
    }
}
