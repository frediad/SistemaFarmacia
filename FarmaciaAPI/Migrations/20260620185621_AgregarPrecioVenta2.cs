using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FarmaciaAPI.Migrations
{
    /// <inheritdoc />
    public partial class AgregarPrecioVenta2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "PrecioVenta2",
                table: "Productos",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PrecioVenta2",
                table: "Productos");
        }
    }
}
