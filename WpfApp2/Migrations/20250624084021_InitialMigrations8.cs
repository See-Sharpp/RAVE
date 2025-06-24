using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WpfApp2.Migrations
{
    /// <inheritdoc />
    public partial class InitialMigrations8 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Embedding",
                table: "AllExes",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Embedding",
                table: "AllExes");
        }
    }
}
