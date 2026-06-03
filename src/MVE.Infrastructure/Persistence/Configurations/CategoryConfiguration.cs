using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> b)
    {
        b.HasQueryFilter(c => !c.IsDeleted);
        b.Property(c => c.Name).IsRequired().HasMaxLength(120);
        b.Property(c => c.Slug).IsRequired().HasMaxLength(140);
        b.Property(c => c.Description).HasMaxLength(500);
        b.Property(c => c.IconUrl).HasMaxLength(500);
        b.HasIndex(c => c.Slug).IsUnique();
        b.HasOne(c => c.Parent).WithMany(c => c.Children).HasForeignKey(c => c.ParentCategoryId).OnDelete(DeleteBehavior.Restrict);
    }
}
