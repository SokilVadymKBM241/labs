using MigrationCacheDemo.Api.Models;

namespace MigrationCacheDemo.Api.Repositories
{
    public interface IProductRepository
    {
        Task<Product?> GetByIdAsync(Guid id);
        Task<List<Product>> GetAllAsync();
        Task<Product> CreateAsync(Product product);
        Task<Product?> UpdateAsync(Guid id, UpdateProductRequest request);
        Task<bool> DeleteAsync(Guid id);
    }
}
