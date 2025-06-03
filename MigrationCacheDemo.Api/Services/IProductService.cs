using MigrationCacheDemo.Api.Models;

namespace MigrationCacheDemo.Api.Services
{
    public interface IProductService
    {
        Task<Product?> GetProductAsync(Guid id);
        Task<List<Product>> GetAllProductsAsync();
        Task<Product> CreateProductAsync(CreateProductRequest request);
        Task<Product?> UpdateProductAsync(Guid id, UpdateProductRequest request);
        Task<bool> DeleteProductAsync(Guid id);
        void ClearCache();
    }
}
