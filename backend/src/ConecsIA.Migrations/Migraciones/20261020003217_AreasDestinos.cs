using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ConecsIA.Migrations.Migraciones
{
    /// <inheritdoc />
    public partial class AreasDestinos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "area",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    organizacion_id = table.Column<Guid>(type: "uuid", nullable: false),
                    sede_id = table.Column<Guid>(type: "uuid", nullable: false),
                    nombre = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    nombre_normalizado = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    tipo = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    estado = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    creada_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    actualizada_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    inactivada_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_area", x => x.id);
                    table.UniqueConstraint("ak_area_organizacion_id_id", x => new { x.organizacion_id, x.id });
                    table.ForeignKey(
                        name: "fk_area_sede_organizacion_id_sede_id",
                        columns: x => new { x.organizacion_id, x.sede_id },
                        principalTable: "sede",
                        principalColumns: new[] { "organizacion_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "area_asignacion",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    organizacion_id = table.Column<Guid>(type: "uuid", nullable: false),
                    area_id = table.Column<Guid>(type: "uuid", nullable: false),
                    usuario_id = table.Column<Guid>(type: "uuid", nullable: false),
                    funcion = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    desde_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    asignada_por = table.Column<Guid>(type: "uuid", nullable: true),
                    hasta_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    retirada_por = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_area_asignacion", x => x.id);
                    table.ForeignKey(
                        name: "fk_area_asignacion_area_organizacion_id_area_id",
                        columns: x => new { x.organizacion_id, x.area_id },
                        principalTable: "area",
                        principalColumns: new[] { "organizacion_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_area_organizacion_id_sede_id_nombre_normalizado",
                table: "area",
                columns: new[] { "organizacion_id", "sede_id", "nombre_normalizado" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_area_asignacion_organizacion_id_area_id",
                table: "area_asignacion",
                columns: new[] { "organizacion_id", "area_id" });

            migrationBuilder.CreateIndex(
                name: "ix_area_asignacion_organizacion_id_usuario_id",
                table: "area_asignacion",
                columns: new[] { "organizacion_id", "usuario_id" });

            // FK entre módulos (ADR-001): se crea a mano.
            migrationBuilder.AddForeignKey(
                name: "fk_area_asignacion_usuario_usuario_id",
                table: "area_asignacion",
                column: "usuario_id",
                principalTable: "usuario",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(name: "fk_area_asignacion_usuario_usuario_id", table: "area_asignacion");

            migrationBuilder.DropTable(
                name: "area_asignacion");

            migrationBuilder.DropTable(
                name: "area");
        }
    }
}
