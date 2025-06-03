using LazyCache;
using MigrationCacheDemo.Api.Models;
using MigrationCacheDemo.Api.Repositories;

namespace MigrationCacheDemo.Api.Services
{
    public class ProductService : IProductService
    {
        private readonly IAppCache _cache;
        private readonly IProductRepository _repository;
        private readonly ILogger<ProductService> _logger;
        private readonly TimeSpan _cacheDuration = TimeSpan.FromMinutes(30);

        public ProductService(IAppCache cache, IProductRepository repository, ILogger<ProductService> logger)
        {
            _cache = cache;
            _repository = repository;
            _logger = logger;
        }

        public async Task<Product?> GetProductAsync(Guid id)
        {
            var cacheKey = $"product-{id}";

            var product = await _cache.GetOrAddAsync(cacheKey, async () =>
            {
                _logger.LogInformation("🔄 Завантаження продукту {ProductId} з БАЗИ ДАНИХ", id);
                return await _repository.GetByIdAsync(id);
            }, _cacheDuration);

            if (product != null)
            {
                _logger.LogInformation("⚡ Продукт {ProductId} отримано з КЕШУ", id);
            }

            return product;
        }

        public async Task<List<Product>> GetAllProductsAsync()
        {
            const string cacheKey = "all-products";

            var products = await _cache.GetOrAddAsync(cacheKey, async () =>
            {
                _logger.LogInformation("🔄 Завантаження ВСІХ продуктів з БАЗИ ДАНИХ");
                return await _repository.GetAllAsync();
            }, TimeSpan.FromMinutes(15));

            _logger.LogInformation("⚡ Список продуктів отримано з КЕШУ ({Count} елементів)", products.Count);
            return products;
        }

        public async Task<Product> CreateProductAsync(CreateProductRequest request)
        {
            var product = new Product
            {
                Id = Guid.NewGuid(),
                Name = request.Name,
                Price = request.Price,
                CreatedAt = DateTime.UtcNow
            };

            var createdProduct = await _repository.CreateAsync(product);

            // Очистити кеш списку продуктів
            _cache.Remove("all-products");
            _logger.LogInformation("🗑️ Кеш списку продуктів очищено після створення");

            return createdProduct;
        }

        public async Task<Product?> UpdateProductAsync(Guid id, UpdateProductRequest request)
        {
            var updatedProduct = await _repository.UpdateAsync(id, request);

            if (updatedProduct != null)
            {
                // Очистити кеш для цього продукту та загального списку
                _cache.Remove($"product-{id}");
                _cache.Remove("all-products");
                _logger.LogInformation("🗑️ Кеш продукту {ProductId} очищено після оновлення", id);
            }

            return updatedProduct;
        }

        public async Task<bool> DeleteProductAsync(Guid id)
        {
            var result = await _repository.DeleteAsync(id);

            if (result)
            {
                _cache.Remove($"product-{id}");
                _cache.Remove("all-products");
                _logger.LogInformation("🗑️ Кеш продукту {ProductId} очищено після видалення", id);
            }

            return result;
        }

        public void ClearCache()
        {
            // У LazyCache немає методу очистити весь кеш, тому очищаємо відомі ключі
            _cache.Remove("all-products");
            _logger.LogInformation("🗑️ Кеш повністю очищено");
        }
    }
}
