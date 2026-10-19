using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ConecsIA.Migrations.Migraciones
{
    /// <inheritdoc />
    public partial class RolesConfigurables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<List<Guid>>(
                name: "roles",
                table: "membresia",
                type: "uuid[]",
                nullable: false,
                // Membresías existentes: sin roles (conservan sus claves directas).
                defaultValueSql: "'{}'");

            migrationBuilder.CreateTable(
                name: "rol",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    organizacion_id = table.Column<Guid>(type: "uuid", nullable: false),
                    nombre = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    nombre_normalizado = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    descripcion = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: true),
                    permisos = table.Column<List<string>>(type: "text[]", nullable: false),
                    plantilla = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    activo = table.Column<bool>(type: "boolean", nullable: false),
                    creado_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    actualizado_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    inactivado_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_rol", x => x.id);
                    table.UniqueConstraint("ak_rol_organizacion_id_id", x => new { x.organizacion_id, x.id });
                });

            migrationBuilder.CreateIndex(
                name: "ix_rol_organizacion_id_nombre_normalizado",
                table: "rol",
                columns: new[] { "organizacion_id", "nombre_normalizado" },
                unique: true);

            // FK entre módulos (ADR-001): se crea a mano, como en PrimeraVerticalSedes.
            migrationBuilder.AddForeignKey(
                name: "fk_rol_organizacion_organizacion_id",
                table: "rol",
                column: "organizacion_id",
                principalTable: "organizacion",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(name: "fk_rol_organizacion_organizacion_id", table: "rol");

            migrationBuilder.DropTable(
                name: "rol");

            migrationBuilder.DropColumn(
                name: "roles",
                table: "membresia");
        }
    }
}
