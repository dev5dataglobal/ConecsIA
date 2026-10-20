using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ConecsIA.Migrations.Migraciones
{
    /// <inheritdoc />
    public partial class ModulosOrganizacion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "organizacion_modulo",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    organizacion_id = table.Column<Guid>(type: "uuid", nullable: false),
                    modulo = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    habilitado = table.Column<bool>(type: "boolean", nullable: false),
                    cambiado_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    cambiado_por = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_organizacion_modulo", x => x.id);
                    table.ForeignKey(
                        name: "fk_organizacion_modulo_organizacion_organizacion_id",
                        column: x => x.organizacion_id,
                        principalTable: "organizacion",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_organizacion_modulo_organizacion_id_modulo",
                table: "organizacion_modulo",
                columns: new[] { "organizacion_id", "modulo" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "organizacion_modulo");
        }
    }
}
