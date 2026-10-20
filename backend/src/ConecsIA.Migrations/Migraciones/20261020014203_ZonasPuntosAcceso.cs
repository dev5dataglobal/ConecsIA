using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ConecsIA.Migrations.Migraciones
{
    /// <inheritdoc />
    public partial class ZonasPuntosAcceso : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<List<Guid>>(
                name: "puntos_asignados",
                table: "membresia",
                type: "uuid[]",
                nullable: false,
                // Membresías existentes: sin puntos (operan en cualquier punto de sus sedes).
                defaultValueSql: "'{}'");

            migrationBuilder.CreateTable(
                name: "zona",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    organizacion_id = table.Column<Guid>(type: "uuid", nullable: false),
                    sede_id = table.Column<Guid>(type: "uuid", nullable: false),
                    nombre = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    nombre_normalizado = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    descripcion = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: true),
                    es_punto_acceso = table.Column<bool>(type: "boolean", nullable: false),
                    modo = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    estado = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    creada_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    actualizada_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    inactivada_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_zona", x => x.id);
                    table.UniqueConstraint("ak_zona_organizacion_id_id", x => new { x.organizacion_id, x.id });
                    table.CheckConstraint("ck_zona_modo_solo_en_punto_de_acceso", "es_punto_acceso = (modo IS NOT NULL)");
                    table.ForeignKey(
                        name: "fk_zona_sede_organizacion_id_sede_id",
                        columns: x => new { x.organizacion_id, x.sede_id },
                        principalTable: "sede",
                        principalColumns: new[] { "organizacion_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_zona_organizacion_id_sede_id_nombre_normalizado",
                table: "zona",
                columns: new[] { "organizacion_id", "sede_id", "nombre_normalizado" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "zona");

            migrationBuilder.DropColumn(
                name: "puntos_asignados",
                table: "membresia");
        }
    }
}
