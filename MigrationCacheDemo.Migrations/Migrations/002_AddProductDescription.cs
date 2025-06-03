using FluentMigrator;

namespace MigrationCacheDemo.Migrations.Migrations
{
    [Migration(20250603002)]
    public class AddProductDescription : Migration
    {
        public override void Up()
        {
            Console.WriteLine("🔧 Виконується міграція: Додавання поля Description");

            Alter.Table("Products")
                .AddColumn("Description").AsString(500).Nullable();

            Console.WriteLine("✅ Поле Description додано до таблиці Products");
        }

        public override void Down()
        {
            Console.WriteLine("🔧 Виконується відміна: Видалення поля Description");

            Delete.Column("Description").FromTable("Products");

            Console.WriteLine("✅ Поле Description видалено з таблиці Products");
        }
    }
}
