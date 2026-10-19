using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ConecsIA.Migrations.Migraciones
{
    /// <inheritdoc />
    public partial class PrimeraVerticalSedes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "auditoria_evento",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    organizacion_id = table.Column<Guid>(type: "uuid", nullable: true),
                    actor_usuario_id = table.Column<Guid>(type: "uuid", nullable: true),
                    instante = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    accion = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    tipo_objeto = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    objeto_id = table.Column<Guid>(type: "uuid", nullable: true),
                    resultado = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    valores_anteriores = table.Column<string>(type: "jsonb", nullable: true),
                    valores_nuevos = table.Column<string>(type: "jsonb", nullable: true),
                    trace_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_auditoria_evento", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "organizacion",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    nombre = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    estado = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    creada_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_organizacion", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "sede",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    organizacion_id = table.Column<Guid>(type: "uuid", nullable: false),
                    nombre = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    nombre_normalizado = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    direccion = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: true),
                    ciudad = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    pais = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    zona_horaria = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    estado = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    creada_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    actualizada_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    inactivada_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_sede", x => x.id);
                    table.UniqueConstraint("ak_sede_organizacion_id_id", x => new { x.organizacion_id, x.id });
                });

            migrationBuilder.CreateTable(
                name: "usuario",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    correo = table.Column<string>(type: "character varying(254)", maxLength: 254, nullable: false),
                    nombre = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    activo = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_usuario", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "membresia",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    usuario_id = table.Column<Guid>(type: "uuid", nullable: false),
                    organizacion_id = table.Column<Guid>(type: "uuid", nullable: false),
                    activa = table.Column<bool>(type: "boolean", nullable: false),
                    permisos = table.Column<List<string>>(type: "text[]", nullable: false),
                    alcance_total_sedes = table.Column<bool>(type: "boolean", nullable: false),
                    sedes_asignadas = table.Column<List<Guid>>(type: "uuid[]", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_membresia", x => x.id);
                    table.ForeignKey(
                        name: "fk_membresia_usuario_usuario_id",
                        column: x => x.usuario_id,
                        principalTable: "usuario",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_auditoria_evento_organizacion_id_instante",
                table: "auditoria_evento",
                columns: new[] { "organizacion_id", "instante" });

            migrationBuilder.CreateIndex(
                name: "ix_auditoria_evento_tipo_objeto_objeto_id",
                table: "auditoria_evento",
                columns: new[] { "tipo_objeto", "objeto_id" });

            migrationBuilder.CreateIndex(
                name: "ix_membresia_organizacion_id",
                table: "membresia",
                column: "organizacion_id");

            migrationBuilder.CreateIndex(
                name: "ix_membresia_usuario_id_organizacion_id",
                table: "membresia",
                columns: new[] { "usuario_id", "organizacion_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_sede_organizacion_id_nombre_normalizado",
                table: "sede",
                columns: new[] { "organizacion_id", "nombre_normalizado" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_usuario_correo",
                table: "usuario",
                column: "correo",
                unique: true);

            // FKs entre módulos (ADR-001 impide navegaciones entre ensamblados): se crean a mano.
            // EF no las conoce en el modelo; una migración futura no las elimina.
            migrationBuilder.AddForeignKey(
                name: "fk_sede_organizacion_organizacion_id",
                table: "sede",
                column: "organizacion_id",
                principalTable: "organizacion",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_membresia_organizacion_organizacion_id",
                table: "membresia",
                column: "organizacion_id",
                principalTable: "organizacion",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            // ADR-014 / HU-008: la auditoría es append-only para el rol de la aplicación.
            migrationBuilder.Sql("""
                DO $$
                BEGIN
                    IF EXISTS (SELECT 1 FROM pg_roles WHERE rolname = 'conecsia_app') THEN
                        REVOKE UPDATE, DELETE, TRUNCATE ON auditoria_evento FROM conecsia_app;
                    END IF;
                END
                $$;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(name: "fk_sede_organizacion_organizacion_id", table: "sede");
            migrationBuilder.DropForeignKey(name: "fk_membresia_organizacion_organizacion_id", table: "membresia");

            migrationBuilder.DropTable(
                name: "auditoria_evento");

            migrationBuilder.DropTable(
                name: "membresia");

            migrationBuilder.DropTable(
                name: "organizacion");

            migrationBuilder.DropTable(
                name: "sede");

            migrationBuilder.DropTable(
                name: "usuario");
        }
    }
}
