using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public class VendorConfiguration : IEntityTypeConfiguration<Vendor>
{
    public void Configure(EntityTypeBuilder<Vendor> b)
    {
        b.HasQueryFilter(v => !v.IsDeleted);
        b.Property(v => v.OwnerUserId).IsRequired().HasMaxLength(450);
        b.Property(v => v.BusinessName).IsRequired().HasMaxLength(200);
        b.Property(v => v.TaxNumber).HasMaxLength(50);
        b.Property(v => v.DefaultCommissionPercent).HasPrecision(5, 2);
        b.HasIndex(v => v.OwnerUserId);
    }
}

public class VendorStoreConfiguration : IEntityTypeConfiguration<VendorStore>
{
    public void Configure(EntityTypeBuilder<VendorStore> b)
    {
        b.HasQueryFilter(s => !s.IsDeleted);
        b.Property(s => s.Name).IsRequired().HasMaxLength(200);
        b.Property(s => s.Slug).HasMaxLength(220);
        b.Property(s => s.Description).HasMaxLength(2000);
        b.Property(s => s.LogoUrl).HasMaxLength(500);
        b.Property(s => s.BannerUrl).HasMaxLength(500);
        b.Property(s => s.ContactEmail).HasMaxLength(256);
        b.Property(s => s.ContactPhone).HasMaxLength(50);
        b.HasIndex(s => s.Slug).IsUnique();
        b.HasOne(s => s.Vendor).WithMany(v => v.Stores).HasForeignKey(s => s.VendorId).OnDelete(DeleteBehavior.Restrict);
    }
}
