using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class ZonasYPuntosAcceso : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_movimiento_acceso_zona_id_organizacion_id_zona_punto_acceso",
                schema: "public",
                table: "movimiento_acceso");

            migrationBuilder.DropIndex(
                name: "IX_zona_id_organizacion_id_sede",
                schema: "public",
                table: "zona");

            migrationBuilder.RenameColumn(
                name: "id_zona_punto_acceso",
                schema: "public",
                table: "movimiento_acceso",
                newName: "id_zona");

            migrationBuilder.RenameIndex(
                name: "IX_movimiento_acceso_id_organizacion_id_zona_punto_acceso",
                schema: "public",
                table: "movimiento_acceso",
                newName: "IX_movimiento_acceso_id_organizacion_id_zona");

            migrationBuilder.RenameColumn(
                name: "id_zona",
                schema: "public",
                table: "membresia_punto_acceso",
                newName: "id_punto_acceso");

            migrationBuilder.AlterColumn<string>(
                name: "nombre",
                schema: "public",
                table: "zona",
                type: "character varying(150)",
                maxLength: 150,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(2000)",
                oldMaxLength: 2000);

            migrationBuilder.AlterColumn<string>(
                name: "descripcion",
                schema: "public",
                table: "zona",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(2000)",
                oldMaxLength: 2000,
                oldNullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "actualizado_en",
                schema: "public",
                table: "zona",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "creado_en",
                schema: "public",
                table: "zona",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<int>(
                name: "id_usuario_actualiza",
                schema: "public",
                table: "zona",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "id_usuario_crea",
                schema: "public",
                table: "zona",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "id_punto_acceso",
                schema: "public",
                table: "movimiento_acceso",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "nombre_normalizado",
                schema: "public",
                table: "zona",
                type: "character varying(150)",
                maxLength: 150,
                nullable: true,
                computedColumnSql: "upper(btrim(nombre))",
                stored: true);

            migrationBuilder.AddUniqueConstraint(
                name: "AK_zona_id_organizacion_id_sede_id_zona",
                schema: "public",
                table: "zona",
                columns: new[] { "id_organizacion", "id_sede", "id_zona" });

            migrationBuilder.CreateTable(
                name: "punto_acceso",
                schema: "public",
                columns: table => new
                {
                    id_punto_acceso = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    id_sede = table.Column<int>(type: "integer", nullable: false),
                    id_zona = table.Column<int>(type: "integer", nullable: false),
                    nombre = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    descripcion = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    modo_acceso = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    activo = table.Column<bool>(type: "boolean", nullable: false),
                    creado_en = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    id_usuario_crea = table.Column<int>(type: "integer", nullable: true),
                    actualizado_en = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    id_usuario_actualiza = table.Column<int>(type: "integer", nullable: true),
                    nombre_normalizado = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true, computedColumnSql: "upper(btrim(nombre))", stored: true),
                    id_organizacion = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_punto_acceso", x => x.id_punto_acceso);
                    table.UniqueConstraint("AK_punto_acceso_id_organizacion_id_punto_acceso", x => new { x.id_organizacion, x.id_punto_acceso });
                    table.UniqueConstraint("AK_punto_acceso_id_organizacion_id_sede_id_punto_acceso", x => new { x.id_organizacion, x.id_sede, x.id_punto_acceso });
                    table.CheckConstraint("ck_punto_acceso_modo", "modo_acceso IN ('Entrada','Salida','Ambos')");
                    table.ForeignKey(
                        name: "FK_punto_acceso_zona_id_organizacion_id_sede_id_zona",
                        columns: x => new { x.id_organizacion, x.id_sede, x.id_zona },
                        principalSchema: "public",
                        principalTable: "zona",
                        principalColumns: new[] { "id_organizacion", "id_sede", "id_zona" },
                        onDelete: ReferentialAction.Restrict);
                });

            // Datos: cada zona marcada como punto de acceso (ADR-019, reemplazado por ADR-027) pasa a punto_acceso dentro de su
            // zona padre (si es de la misma sede) o de sí misma; asignaciones de operadores y movimientos se remapean.
            migrationBuilder.Sql("""
                UPDATE public.zona SET creado_en = now() WHERE creado_en = '0001-01-01';
                ALTER TABLE public.punto_acceso ADD COLUMN id_zona_origen integer;
                INSERT INTO public.punto_acceso (id_organizacion, id_sede, id_zona, nombre, descripcion, modo_acceso, activo, creado_en, id_zona_origen)
                SELECT z.id_organizacion, z.id_sede, COALESCE(p.id_zona, z.id_zona), z.nombre, z.descripcion, COALESCE(z.modo_acceso, 'Ambos'), z.activo, now(), z.id_zona
                FROM public.zona z
                LEFT JOIN public.zona p ON p.id_organizacion = z.id_organizacion AND p.id_zona = z.id_zona_padre AND p.id_sede = z.id_sede
                WHERE z.es_punto_acceso;
                UPDATE public.membresia_punto_acceso m SET id_punto_acceso = pa.id_punto_acceso
                FROM public.punto_acceso pa WHERE pa.id_organizacion = m.id_organizacion AND pa.id_zona_origen = m.id_punto_acceso;
                DELETE FROM public.membresia_punto_acceso m
                WHERE NOT EXISTS (SELECT 1 FROM public.punto_acceso pa WHERE pa.id_organizacion = m.id_organizacion AND pa.id_punto_acceso = m.id_punto_acceso);
                UPDATE public.movimiento_acceso m SET id_punto_acceso = pa.id_punto_acceso, id_zona = pa.id_zona
                FROM public.punto_acceso pa WHERE pa.id_organizacion = m.id_organizacion AND pa.id_zona_origen = m.id_zona;
                ALTER TABLE public.punto_acceso DROP COLUMN id_zona_origen;
                """);

            migrationBuilder.CreateIndex(
                name: "ux_zona_sede_nombre",
                schema: "public",
                table: "zona",
                columns: new[] { "id_organizacion", "id_sede", "nombre_normalizado" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_movimiento_acceso_id_organizacion_id_punto_acceso",
                schema: "public",
                table: "movimiento_acceso",
                columns: new[] { "id_organizacion", "id_punto_acceso" });

            migrationBuilder.CreateIndex(
                name: "IX_movimiento_acceso_id_organizacion_id_sede_id_punto_acceso",
                schema: "public",
                table: "movimiento_acceso",
                columns: new[] { "id_organizacion", "id_sede", "id_punto_acceso" });

            migrationBuilder.CreateIndex(
                name: "IX_membresia_punto_acceso_id_organizacion_id_punto_acceso",
                schema: "public",
                table: "membresia_punto_acceso",
                columns: new[] { "id_organizacion", "id_punto_acceso" });

            migrationBuilder.CreateIndex(
                name: "IX_autorizacion_zona_id_organizacion_id_zona",
                schema: "public",
                table: "autorizacion_zona",
                columns: new[] { "id_organizacion", "id_zona" });

            migrationBuilder.CreateIndex(
                name: "IX_punto_acceso_id_organizacion_id_punto_acceso",
                schema: "public",
                table: "punto_acceso",
                columns: new[] { "id_organizacion", "id_punto_acceso" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_punto_acceso_id_organizacion_id_sede_id_zona",
                schema: "public",
                table: "punto_acceso",
                columns: new[] { "id_organizacion", "id_sede", "id_zona" });

            migrationBuilder.CreateIndex(
                name: "IX_punto_acceso_id_organizacion_id_zona",
                schema: "public",
                table: "punto_acceso",
                columns: new[] { "id_organizacion", "id_zona" });

            migrationBuilder.CreateIndex(
                name: "ux_punto_acceso_sede_nombre",
                schema: "public",
                table: "punto_acceso",
                columns: new[] { "id_organizacion", "id_sede", "nombre_normalizado" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_autorizacion_zona_zona_id_organizacion_id_zona",
                schema: "public",
                table: "autorizacion_zona",
                columns: new[] { "id_organizacion", "id_zona" },
                principalSchema: "public",
                principalTable: "zona",
                principalColumns: new[] { "id_organizacion", "id_zona" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_membresia_punto_acceso_punto_acceso_id_organizacion_id_punt~",
                schema: "public",
                table: "membresia_punto_acceso",
                columns: new[] { "id_organizacion", "id_punto_acceso" },
                principalSchema: "public",
                principalTable: "punto_acceso",
                principalColumns: new[] { "id_organizacion", "id_punto_acceso" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_movimiento_acceso_punto_acceso_id_organizacion_id_sede_id_p~",
                schema: "public",
                table: "movimiento_acceso",
                columns: new[] { "id_organizacion", "id_sede", "id_punto_acceso" },
                principalSchema: "public",
                principalTable: "punto_acceso",
                principalColumns: new[] { "id_organizacion", "id_sede", "id_punto_acceso" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_movimiento_acceso_zona_id_organizacion_id_zona",
                schema: "public",
                table: "movimiento_acceso",
                columns: new[] { "id_organizacion", "id_zona" },
                principalSchema: "public",
                principalTable: "zona",
                principalColumns: new[] { "id_organizacion", "id_zona" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.DropCheckConstraint(
                name: "ck_zona_modo_acceso",
                schema: "public",
                table: "zona");

            migrationBuilder.DropColumn(
                name: "es_punto_acceso",
                schema: "public",
                table: "zona");

            migrationBuilder.DropColumn(
                name: "modo_acceso",
                schema: "public",
                table: "zona");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_autorizacion_zona_zona_id_organizacion_id_zona",
                schema: "public",
                table: "autorizacion_zona");

            migrationBuilder.DropForeignKey(
                name: "FK_membresia_punto_acceso_punto_acceso_id_organizacion_id_punt~",
                schema: "public",
                table: "membresia_punto_acceso");

            migrationBuilder.DropForeignKey(
                name: "FK_movimiento_acceso_punto_acceso_id_organizacion_id_sede_id_p~",
                schema: "public",
                table: "movimiento_acceso");

            migrationBuilder.DropForeignKey(
                name: "FK_movimiento_acceso_zona_id_organizacion_id_zona",
                schema: "public",
                table: "movimiento_acceso");

            migrationBuilder.DropTable(
                name: "punto_acceso",
                schema: "public");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_zona_id_organizacion_id_sede_id_zona",
                schema: "public",
                table: "zona");

            migrationBuilder.DropIndex(
                name: "ux_zona_sede_nombre",
                schema: "public",
                table: "zona");

            migrationBuilder.DropIndex(
                name: "IX_movimiento_acceso_id_organizacion_id_punto_acceso",
                schema: "public",
                table: "movimiento_acceso");

            migrationBuilder.DropIndex(
                name: "IX_movimiento_acceso_id_organizacion_id_sede_id_punto_acceso",
                schema: "public",
                table: "movimiento_acceso");

            migrationBuilder.DropIndex(
                name: "IX_membresia_punto_acceso_id_organizacion_id_punto_acceso",
                schema: "public",
                table: "membresia_punto_acceso");

            migrationBuilder.DropIndex(
                name: "IX_autorizacion_zona_id_organizacion_id_zona",
                schema: "public",
                table: "autorizacion_zona");

            migrationBuilder.DropColumn(
                name: "nombre_normalizado",
                schema: "public",
                table: "zona");

            migrationBuilder.DropColumn(
                name: "actualizado_en",
                schema: "public",
                table: "zona");

            migrationBuilder.DropColumn(
                name: "creado_en",
                schema: "public",
                table: "zona");

            migrationBuilder.DropColumn(
                name: "id_usuario_actualiza",
                schema: "public",
                table: "zona");

            migrationBuilder.DropColumn(
                name: "id_usuario_crea",
                schema: "public",
                table: "zona");

            migrationBuilder.DropColumn(
                name: "id_punto_acceso",
                schema: "public",
                table: "movimiento_acceso");

            migrationBuilder.RenameColumn(
                name: "id_zona",
                schema: "public",
                table: "movimiento_acceso",
                newName: "id_zona_punto_acceso");

            migrationBuilder.RenameIndex(
                name: "IX_movimiento_acceso_id_organizacion_id_zona",
                schema: "public",
                table: "movimiento_acceso",
                newName: "IX_movimiento_acceso_id_organizacion_id_zona_punto_acceso");

            migrationBuilder.RenameColumn(
                name: "id_punto_acceso",
                schema: "public",
                table: "membresia_punto_acceso",
                newName: "id_zona");

            migrationBuilder.AlterColumn<string>(
                name: "nombre",
                schema: "public",
                table: "zona",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(150)",
                oldMaxLength: 150);

            migrationBuilder.AlterColumn<string>(
                name: "descripcion",
                schema: "public",
                table: "zona",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500,
                oldNullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "es_punto_acceso",
                schema: "public",
                table: "zona",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "modo_acceso",
                schema: "public",
                table: "zona",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_zona_id_organizacion_id_sede",
                schema: "public",
                table: "zona",
                columns: new[] { "id_organizacion", "id_sede" });

            migrationBuilder.AddCheckConstraint(
                name: "ck_zona_modo_acceso",
                schema: "public",
                table: "zona",
                sql: "es_punto_acceso OR modo_acceso IS NULL OR modo_acceso IN ('Entrada','Salida','Ambos')");

            migrationBuilder.AddForeignKey(
                name: "FK_movimiento_acceso_zona_id_organizacion_id_zona_punto_acceso",
                schema: "public",
                table: "movimiento_acceso",
                columns: new[] { "id_organizacion", "id_zona_punto_acceso" },
                principalSchema: "public",
                principalTable: "zona",
                principalColumns: new[] { "id_organizacion", "id_zona" },
                onDelete: ReferentialAction.Restrict);
        }
    }
}
