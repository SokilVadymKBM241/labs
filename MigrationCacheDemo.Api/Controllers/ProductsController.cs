using Microsoft.AspNetCore.Mvc;
using MigrationCacheDemo.Api.Models;
using MigrationCacheDemo.Api.Services;
using System.Data;

namespace MigrationCacheDemo.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProductsController : ControllerBase
    {
        private readonly IProductService _productService;
        private readonly ILogger<ProductsController> _logger;

        public ProductsController(IProductService productService, ILogger<ProductsController> logger)
        {
            _productService = productService;
            _logger = logger;
        }

        /// <summary>
        /// Отримати всі продукти (з кешуванням)
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<List<Product>>> GetAllProducts()
        {
            _logger.LogInformation("📋 Запит на отримання всіх продуктів");
            var products = await _productService.GetAllProductsAsync();
            return Ok(products);
        }

        /// <summary>
        /// Отримати продукт за ID (з кешуванням)
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<Product>> GetProduct(Guid id)
        {
            _logger.LogInformation("🔍 Запит на отримання продукту {ProductId}", id);
            var product = await _productService.GetProductAsync(id);
            return product != null ? Ok(product) : NotFound($"Продукт з ID {id} не знайдено");
        }

        /// <summary>
        /// Створити новий продукт
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<Product>> CreateProduct(CreateProductRequest request)
        {
            _logger.LogInformation("➕ Створення нового продукту: {ProductName}", request.Name);

            if (string.IsNullOrWhiteSpace(request.Name))
            {
                return BadRequest("Назва продукту не може бути пустою");
            }

            if (request.Price <= 0)
            {
                return BadRequest("Ціна повинна бути більше 0");
            }

            var product = await _productService.CreateProductAsync(request);
            return CreatedAtAction(nameof(GetProduct), new { id = product.Id }, product);
        }

        /// <summary>
        /// Оновити продукт
        /// </summary>
        [HttpPut("{id}")]
        public async Task<ActionResult<Product>> UpdateProduct(Guid id, UpdateProductRequest request)
        {
            _logger.LogInformation("✏️ Оновлення продукту {ProductId}", id);

            var product = await _productService.UpdateProductAsync(id, request);
            return product != null ? Ok(product) : NotFound($"Продукт з ID {id} не знайдено");
        }

        /// <summary>
        /// Видалити продукт
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteProduct(Guid id)
        {
            _logger.LogInformation("🗑️ Видалення продукту {ProductId}", id);

            var result = await _productService.DeleteProductAsync(id);
            return result ? NoContent() : NotFound($"Продукт з ID {id} не знайдено");
        }

        /// <summary>
        /// Очистити весь кеш
        /// </summary>
        [HttpPost("clear-cache")]
        public ActionResult ClearCache()
        {
            _logger.LogInformation("🧹 Очищення кешу");
            _productService.ClearCache();
            return Ok("Кеш очищено успішно");
        }
        /// <summary>
        /// Діагностика: структура таблиці Products
        /// </summary>
        [HttpGet("debug/table-structure")]
        public async Task<ActionResult> GetTableStructure()
        {
            try
            {
                var connectionString = "Data Source=products.db"; // Тимчасово хардкод
                using var connection = new Microsoft.Data.Sqlite.SqliteConnection(connectionString);
                await connection.OpenAsync();

                using var cmd = connection.CreateCommand();
                cmd.CommandText = "PRAGMA table_info(Products);";
                using var reader = await cmd.ExecuteReaderAsync();

                var columns = new List<object>();
                while (await reader.ReadAsync())
                {
                    columns.Add(new
                    {
                        Name = reader.GetString("name"),
                        Type = reader.GetString("type"),
                        NotNull = reader.GetInt32("notnull") == 1,
                        Position = reader.GetInt32("cid")
                    });
                }

                return Ok(new
                {
                    Message = "Поточна структура таблиці Products:",
                    Columns = columns
                });
            }
            catch (Exception ex)
            {
                return BadRequest($"Помилка: {ex.Message}");
            }
        }

    }
}
