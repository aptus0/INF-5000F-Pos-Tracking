using Microsoft.EntityFrameworkCore;
using SamerHub.Core.Entities;
using SamerHub.Core.Interfaces;

namespace SamerHub.Infrastructure.Persistence;

public sealed class EfProductRepository(SamerHubDbContext dbContext) : IProductRepository
{
    public async Task<IReadOnlyList<Product>> GetAllAsync(CancellationToken ct)
    {
        if (!await dbContext.Products.AnyAsync(ct))
        {
            dbContext.Products.AddRange(RepositoryDefaults.CreateProducts());
            await dbContext.SaveChangesAsync(ct);
        }

        return await dbContext.Products.Where(x => x.IsActive).OrderBy(x => x.CategoryName).ThenBy(x => x.Name).ToListAsync(ct);
    }

    public Task<Product?> GetByIdAsync(Guid id, CancellationToken ct) =>
        dbContext.Products.SingleOrDefaultAsync(x => x.Id == id, ct);

    public async Task<Product> AddAsync(Product product, CancellationToken ct)
    {
        if (product.Id == Guid.Empty)
        {
            product.Id = Guid.NewGuid();
        }

        dbContext.Products.Add(product);
        await dbContext.SaveChangesAsync(ct);
        return product;
    }

    public async Task<Product> UpdateAsync(Product product, CancellationToken ct)
    {
        dbContext.Products.Update(product);
        await dbContext.SaveChangesAsync(ct);
        return product;
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct)
    {
        var product = await dbContext.Products.SingleOrDefaultAsync(x => x.Id == id, ct);
        if (product is null)
        {
            return;
        }

        product.IsActive = false;
        await dbContext.SaveChangesAsync(ct);
    }
}
