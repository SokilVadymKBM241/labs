using FluentMigrator;

namespace MigrationCacheDemo.Migrations.Migrations // ← Перевірте цей namespace
{
    [Migration(20250603001)]
    public class CreateProductTable : Migration
    {
        public override void Up()
        {
            Execute.Sql("PRAGMA foreign_keys = ON;"); // Для SQLite

            Create.Table("Products")
                .WithColumn("Id").AsString(36).PrimaryKey() // Guid як string для SQLite
                .WithColumn("Name").AsString(100).NotNullable()
                .WithColumn("Price").AsDecimal(18, 2).NotNullable()
                .WithColumn("CreatedAt").AsDateTime().NotNullable();
        }

        public override void Down()
        {
            Delete.Table("Products");
        }
    }
}
