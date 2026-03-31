using Microsoft.EntityFrameworkCore;
using NetAPI.Domain.Entities;
using NetAPI.Domain.Interfaces;

namespace NetAPI.Infrastructure.Persistence.Repositories;

public class ProductRepository : Repository<Product>, IProductRepository
{
    public ProductRepository(AppDbContext context) : base(context) { }

    public async Task<Product?> GetByNameAsync(string name, CancellationToken cancellationToken = default)
        => await _dbSet
            .FirstOrDefaultAsync(p => p.Name == name, cancellationToken);

    public async Task<IReadOnlyList<Product>> GetActiveProductsAsync(CancellationToken cancellationToken = default)
        => await _dbSet
            .Where(p => p.IsActive)
            .OrderBy(p => p.Name)
            .ToListAsync(cancellationToken);
}
