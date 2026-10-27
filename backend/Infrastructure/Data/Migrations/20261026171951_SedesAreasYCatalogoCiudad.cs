using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class SedesAreasYCatalogoCiudad : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_area_id_organizacion_id_sede",
                schema: "public",
                table: "area");

            migrationBuilder.AddColumn<bool>(
                name: "tiene_sedes",
                schema: "public",
                table: "organizacion",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "zona_horaria",
                schema: "public",
                table: "ciudad",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "America/Bogota");

            migrationBuilder.CreateIndex(
                name: "IX_sede_id_organizacion_nombre",
                schema: "public",
                table: "sede",
                columns: new[] { "id_organizacion", "nombre" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_area_id_organizacion_id_sede_nombre",
                schema: "public",
                table: "area",
                columns: new[] { "id_organizacion", "id_sede", "nombre" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_sede_id_organizacion_nombre",
                schema: "public",
                table: "sede");

            migrationBuilder.DropIndex(
                name: "IX_area_id_organizacion_id_sede_nombre",
                schema: "public",
                table: "area");

            migrationBuilder.DropColumn(
                name: "tiene_sedes",
                schema: "public",
                table: "organizacion");

            migrationBuilder.DropColumn(
                name: "zona_horaria",
                schema: "public",
                table: "ciudad");

            migrationBuilder.CreateIndex(
                name: "IX_area_id_organizacion_id_sede",
                schema: "public",
                table: "area",
                columns: new[] { "id_organizacion", "id_sede" },
                unique: true);
        }
    }
}
