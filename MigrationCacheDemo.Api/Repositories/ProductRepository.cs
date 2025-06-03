using Microsoft.Data.Sqlite;
using MigrationCacheDemo.Api.Models;
using System.Data;

namespace MigrationCacheDemo.Api.Repositories
{
    public class ProductRepository : IProductRepository
    {
        private readonly string _connectionString;

        public ProductRepository(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection")!;
        }

        public async Task<Product?> GetByIdAsync(Guid id)
        {
            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();

            using var command = new SqliteCommand(
                "SELECT Id, Name, Price, CreatedAt FROM Products WHERE Id = @id", connection);
            command.Parameters.AddWithValue("@id", id.ToString());

            using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return new Product
                {
                    Id = Guid.Parse(reader.GetString("Id")),
                    Name = reader.GetString("Name"),
                    Price = reader.GetDecimal("Price"),
                    CreatedAt = reader.GetDateTime("CreatedAt"),
                    Description = null // Завжди null поки немає колонки
                };
            }

            return null;
        }


        public async Task<List<Product>> GetAllAsync()
        {
            var products = new List<Product>();
            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();

            using var command = new SqliteCommand(
                "SELECT Id, Name, Price, CreatedAt FROM Products ORDER BY CreatedAt DESC", connection);

            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                products.Add(new Product
                {
                    Id = Guid.Parse(reader.GetString("Id")),
                    Name = reader.GetString("Name"),
                    Price = reader.GetDecimal("Price"),
                    CreatedAt = reader.GetDateTime("CreatedAt"),
                    Description = null // Завжди null поки немає колонки
                });
            }

            return products;
        }

        public async Task<Product> CreateAsync(Product product)
        {
            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();

            using var command = new SqliteCommand(
                "INSERT INTO Products (Id, Name, Price, CreatedAt, Description) VALUES (@id, @name, @price, @createdAt, @description)",
                connection);

            command.Parameters.AddWithValue("@id", product.Id.ToString());
            command.Parameters.AddWithValue("@name", product.Name);
            command.Parameters.AddWithValue("@price", product.Price);
            command.Parameters.AddWithValue("@createdAt", product.CreatedAt);
            command.Parameters.AddWithValue("@description", product.Description ?? (object)DBNull.Value);

            await command.ExecuteNonQueryAsync();
            return product;
        }

        public async Task<Product?> UpdateAsync(Guid id, UpdateProductRequest request)
        {
            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();

            using var command = new SqliteCommand(
                "UPDATE Products SET Name = @name, Price = @price, Description = @description WHERE Id = @id",
                connection);

            command.Parameters.AddWithValue("@id", id.ToString());
            command.Parameters.AddWithValue("@name", request.Name);
            command.Parameters.AddWithValue("@price", request.Price);
            command.Parameters.AddWithValue("@description", request.Description ?? (object)DBNull.Value);

            var rowsAffected = await command.ExecuteNonQueryAsync();
            return rowsAffected > 0 ? await GetByIdAsync(id) : null;
        }

        public async Task<bool> DeleteAsync(Guid id)
        {
            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();

            using var command = new SqliteCommand(
                "DELETE FROM Products WHERE Id = @id", connection);
            command.Parameters.AddWithValue("@id", id.ToString());

            var rowsAffected = await command.ExecuteNonQueryAsync();
            return rowsAffected > 0;
        }
    }
}
