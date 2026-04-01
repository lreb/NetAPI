using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NetAPI.Domain.Entities;
using NetAPI.Domain.ValueObjects;

namespace NetAPI.Infrastructure.Persistence;

public static class AppDbContextSeed
{
    public static async Task SeedAsync(AppDbContext context, ILogger logger)
    {
        try
        {
            if (context.Database.IsNpgsql())
                await context.Database.MigrateAsync();

            if (!await context.Products.AnyAsync())
            {
                var products = new List<Product>
                {
                    Product.Create("Laptop Pro 15", "High-performance laptop", new Money(1299.99m, "USD"), 50),
                    Product.Create("Wireless Mouse", "Ergonomic wireless mouse", new Money(29.99m, "USD"), 200),
                    Product.Create("USB-C Hub", "7-in-1 USB-C hub", new Money(49.99m, "USD"), 150),
                };

                foreach (var product in products)
                    product.ClearDomainEvents(); // suppress events during seed

                await context.Products.AddRangeAsync(products);
                await context.SaveChangesAsync();

                logger.LogInformation("Database seeded with {Count} products.", products.Count);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while seeding the database.");
            throw;
        }
    }
}
