using FluentMigrator.Runner;
using Microsoft.AspNetCore.Mvc;
using MigrationCacheDemo.Api.Services;
using System.Data;

namespace MigrationCacheDemo.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class MigrationsController : ControllerBase
    {
        private readonly IMigrationRunner _migrationRunner;
        private readonly ILogger<MigrationsController> _logger;

        public MigrationsController(IMigrationRunner migrationRunner, ILogger<MigrationsController> logger)
        {
            _migrationRunner = migrationRunner;
            _logger = logger;
        }

        /// <summary>
        /// Виконати всі міграції
        /// </summary>
        [HttpPost("migrate-up")]
        public ActionResult MigrateUp()
        {
            try
            {
                _logger.LogInformation("🚀 Виконання всіх міграцій...");
                _migrationRunner.MigrateUp();
                _logger.LogInformation("✅ Всі міграції виконано успішно!");
                return Ok("Всі міграції виконано успішно!");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Помилка під час виконання міграцій");
                return BadRequest($"Помилка: {ex.Message}");
            }
        }

        /// <summary>
        /// Відмінити всі міграції
        /// </summary>
        [HttpPost("migrate-down")]
        public ActionResult MigrateDown()
        {
            try
            {
                _logger.LogInformation("⬇️ Відміна всіх міграцій...");
                _migrationRunner.MigrateDown(0);
                _logger.LogInformation("✅ Всі міграції відмінено успішно!");
                return Ok("Всі міграції відмінено успішно!");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Помилка під час відміни міграцій");
                return BadRequest($"Помилка: {ex.Message}");
            }
        }

        /// <summary>
        /// Відмінити міграцію до конкретної версії (з діагностикою)
        /// </summary>
        [HttpPost("rollback/{version}")]
        public ActionResult RollbackToVersion(long version)
        {
            try
            {
                _logger.LogInformation("⏪ Відміна до версії {Version}...", version);

                // Очистити всі кеші перед відміною
                var productService = HttpContext.RequestServices.GetService<IProductService>();
                productService?.ClearCache();
                _logger.LogInformation("🗑️ Кеш очищено перед відміною міграції");

                // Виконати відміну
                _migrationRunner.MigrateDown(version);

                _logger.LogInformation("✅ Відміна до версії {Version} виконана!", version);

                // Перевірити результат
                var connectionString = "Data Source=products.db";
                using var connection = new Microsoft.Data.Sqlite.SqliteConnection(connectionString);
                connection.Open();

                using var cmd = connection.CreateCommand();
                cmd.CommandText = "PRAGMA table_info(Products);";
                using var reader = cmd.ExecuteReader();

                var columns = new List<string>();
                while (reader.Read())
                {
                    columns.Add(reader.GetString("name"));
                }

                return Ok(new
                {
                    Message = $"Відміна до версії {version} виконана успішно!",
                    CurrentColumns = columns
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Помилка під час відміни до версії {Version}", version);
                return BadRequest($"Помилка: {ex.Message}");
            }
        }

        /// <summary>
        /// Міграція до конкретної версії
        /// </summary>
        [HttpPost("migrate-to/{version}")]
        public ActionResult MigrateToVersion(long version)
        {
            try
            {
                _logger.LogInformation("⏩ Міграція до версії {Version}...", version);
                _migrationRunner.MigrateUp(version);
                _logger.LogInformation("✅ Міграція до версії {Version} виконана!", version);
                return Ok($"Міграція до версії {version} виконана успішно!");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Помилка під час міграції до версії {Version}", version);
                return BadRequest($"Помилка: {ex.Message}");
            }
        }

        /// <summary>
        /// Пряме видалення колонки Description (SQLite workaround)
        /// </summary>
        [HttpPost("force-remove-description")]
        public async Task<ActionResult> ForceRemoveDescription()
        {
            try
            {
                var connectionString = "Data Source=products.db";
                using var connection = new Microsoft.Data.Sqlite.SqliteConnection(connectionString);
                await connection.OpenAsync();

                _logger.LogInformation("🔧 Початок примусового видалення колонки Description...");

                // SQLite не підтримує DROP COLUMN, тому пересоздаємо таблицю
                using var transaction = connection.BeginTransaction();

                // 1. Створити тимчасову таблицю
                var createTempTable = @"
                    CREATE TABLE Products_temp (
                        Id TEXT PRIMARY KEY,
                        Name TEXT NOT NULL,
                        Price REAL NOT NULL,
                        CreatedAt TEXT NOT NULL
                    );";

                using var cmd1 = connection.CreateCommand();
                cmd1.CommandText = createTempTable;
                cmd1.Transaction = transaction;
                await cmd1.ExecuteNonQueryAsync();

                // 2. Скопіювати дані (без Description)
                var copyData = @"
                    INSERT INTO Products_temp (Id, Name, Price, CreatedAt)
                    SELECT Id, Name, Price, CreatedAt FROM Products;";

                using var cmd2 = connection.CreateCommand();
                cmd2.CommandText = copyData;
                cmd2.Transaction = transaction;
                await cmd2.ExecuteNonQueryAsync();

                // 3. Видалити стару таблицю
                using var cmd3 = connection.CreateCommand();
                cmd3.CommandText = "DROP TABLE Products;";
                cmd3.Transaction = transaction;
                await cmd3.ExecuteNonQueryAsync();

                // 4. Перейменувати тимчасову таблицю
                using var cmd4 = connection.CreateCommand();
                cmd4.CommandText = "ALTER TABLE Products_temp RENAME TO Products;";
                cmd4.Transaction = transaction;
                await cmd4.ExecuteNonQueryAsync();

                transaction.Commit();

                _logger.LogInformation("✅ Колонка Description примусово видалена!");

                // Очистити кеш
                var productService = HttpContext.RequestServices.GetService<IProductService>();
                productService?.ClearCache();

                return Ok("Колонка Description примусово видалена з таблиці Products!");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Помилка під час примусового видалення колонки");
                return BadRequest($"Помилка: {ex.Message}");
            }
        }
    }
}
