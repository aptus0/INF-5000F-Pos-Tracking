using SamerHub.Core.Entities;
using SamerHub.Core.Interfaces;

namespace SamerHub.Infrastructure.Persistence;

public sealed class InMemoryProductRepository : IProductRepository
{
    private readonly List<Product> _products = [];

    public Task<IReadOnlyList<Product>> GetAllAsync(CancellationToken ct)
        => Task.FromResult<IReadOnlyList<Product>>(_products.Where(p => p.IsActive).ToList());

    public Task<Product?> GetByIdAsync(Guid id, CancellationToken ct)
        => Task.FromResult(_products.FirstOrDefault(p => p.Id == id));

    public Task<Product> AddAsync(Product product, CancellationToken ct)
    {
        if (product.Id == Guid.Empty)
        {
            product.Id = Guid.NewGuid();
        }
        _products.Add(product);
        return Task.FromResult(product);
    }

    public Task<Product> UpdateAsync(Product product, CancellationToken ct)
    {
        var existing = _products.FirstOrDefault(p => p.Id == product.Id);
        if (existing is not null)
        {
            var index = _products.IndexOf(existing);
            _products[index] = product;
        }
        return Task.FromResult(product);
    }

    public Task DeleteAsync(Guid id, CancellationToken ct)
    {
        var product = _products.FirstOrDefault(p => p.Id == id);
        if (product is not null)
        {
            product.IsActive = false;
        }
        return Task.CompletedTask;
    }
}
