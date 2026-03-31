using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NetAPI.Domain.Entities;
using NetAPI.Domain.ValueObjects;

namespace NetAPI.Infrastructure.Persistence.Configurations;

public class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.HasKey(p => p.Id);

        builder.Property(p => p.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(p => p.Description)
            .HasMaxLength(1000);

        // Map Money value object as owned type (two columns: Price_Amount, Price_Currency)
        builder.OwnsOne(p => p.Price, money =>
        {
            money.Property(m => m.Amount)
                .HasColumnName("Price")
                .HasPrecision(18, 4)
                .IsRequired();

            money.Property(m => m.Currency)
                .HasColumnName("Currency")
                .HasMaxLength(3)
                .IsRequired();
        });

        builder.Property(p => p.StockQuantity).IsRequired();
        builder.Property(p => p.IsActive).IsRequired();
        builder.Property(p => p.CreatedAt).IsRequired();

        // Soft-delete global query filter
        builder.HasQueryFilter(p => !p.IsDeleted);

        builder.ToTable("Products");
    }
}
