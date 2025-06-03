using FluentMigrator.Runner;
using LazyCache;
using Microsoft.Extensions.DependencyInjection;
using MigrationCacheDemo.Api.Repositories;
using MigrationCacheDemo.Api.Services;
using System.Data;

var builder = WebApplication.CreateBuilder(args);

// Додавання сервісів
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Налаштування рядка підключення SQLite
var connectionString = "Data Source=products.db";

// Додавання кешування (ВИПРАВЛЕНО)
builder.Services.AddMemoryCache();
builder.Services.AddTransient<IAppCache>(provider =>
{
    return new CachingService(); // Конструктор без параметрів
});

// Налаштування конфігурації
builder.Services.AddSingleton<IConfiguration>(provider =>
{
    var configBuilder = new ConfigurationBuilder();
    configBuilder.AddInMemoryCollection(new Dictionary<string, string?>
    {
        ["ConnectionStrings:DefaultConnection"] = connectionString
    });
    return configBuilder.Build();
});

// Додавання FluentMigrator
builder.Services.AddFluentMigratorCore()
    .ConfigureRunner(rb => rb
        .AddSQLite()
        .WithGlobalConnectionString(connectionString)
        .ScanIn(typeof(MigrationCacheDemo.Migrations.Migrations.CreateProductTable).Assembly)
        .For.Migrations())
    .AddLogging(lb => lb.AddFluentMigratorConsole());

// Реєстрація власних сервісів
builder.Services.AddScoped<IProductRepository, ProductRepository>();
builder.Services.AddScoped<IProductService, ProductService>();

var app = builder.Build();


// Перевірка та виконання міграцій з детальним логуванням
Console.WriteLine("🔍 Початок процесу міграцій...");
using (var scope = app.Services.CreateScope())
{
    try
    {
        var runner = scope.ServiceProvider.GetRequiredService<IMigrationRunner>();

        Console.WriteLine("📋 Перевірка поточного стану міграцій...");

        // Перевірити поточну версію
        using var connection = new Microsoft.Data.Sqlite.SqliteConnection(connectionString);
        connection.Open();

        // Перевірити чи існує таблиця VersionInfo
        using var checkCmd = connection.CreateCommand();
        checkCmd.CommandText = "SELECT name FROM sqlite_master WHERE type='table' AND name='VersionInfo';";
        var versionTableExists = checkCmd.ExecuteScalar();

        if (versionTableExists != null)
        {
            using var versionCmd = connection.CreateCommand();
            versionCmd.CommandText = "SELECT Version FROM VersionInfo ORDER BY AppliedOn DESC LIMIT 1;";
            var currentVersion = versionCmd.ExecuteScalar();
            Console.WriteLine($"📊 Поточна версія БД: {currentVersion}");
        }
        else
        {
            Console.WriteLine("📊 Таблиця VersionInfo не існує - БД порожня");
        }

        Console.WriteLine("🚀 Запуск міграцій...");
        runner.MigrateUp();
        Console.WriteLine("✅ Міграції виконано успішно!");

        // Перевірка фінального стану
        using var finalCmd = connection.CreateCommand();
        finalCmd.CommandText = "PRAGMA table_info(Products);";
        using var reader = finalCmd.ExecuteReader();

        Console.WriteLine("📋 Структура таблиці Products:");
        while (reader.Read())
        {
            var columnName = reader.GetString("name");
            var columnType = reader.GetString("type");
            var notNull = reader.GetInt32("notnull");
            Console.WriteLine($"  - {columnName}: {columnType} {(notNull == 1 ? "NOT NULL" : "NULL")}");
        }

        connection.Close();
    }
    catch (Exception ex)
    {
        Console.WriteLine($"❌ КРИТИЧНА ПОМИЛКА під час міграції: {ex.Message}");
        Console.WriteLine($"Деталі: {ex}");
    }
}


// Налаштування pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

Console.WriteLine("🌐 API готове до роботи!");
app.Run();
