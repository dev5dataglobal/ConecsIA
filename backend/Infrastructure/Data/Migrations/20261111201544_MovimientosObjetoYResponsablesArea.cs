using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Infrastructure.Data.Migrations
{
    /// <summary>
    /// ADR-030: reemplaza las tablas sin uso movimiento_vehiculo y movimiento_dispositivo (vacías, sin FK ni restricciones)
    /// por movimiento_objeto, con FK compuestas, CHECK y trigger de solo inserción; area_responsable gana FK compuestas y la
    /// trazabilidad de quién asignó o cambió cada designación. Verificar antes de aplicar que ambas tablas estén vacías.
    /// </summary>
    public partial class MovimientosObjetoYResponsablesArea : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Salvaguarda: las tablas que se retiran nunca tuvieron endpoints funcionales; si alguna base tiene filas, se detiene.
            migrationBuilder.Sql("""
                DO $$
                BEGIN
                    IF EXISTS (SELECT 1 FROM public.movimiento_vehiculo) OR EXISTS (SELECT 1 FROM public.movimiento_dispositivo) THEN
                        RAISE EXCEPTION 'movimiento_vehiculo o movimiento_dispositivo tienen filas: trasládalas a movimiento_objeto antes de aplicar esta migración (ADR-030).';
                    END IF;
                END $$;
                """);

            migrationBuilder.DropTable(
                name: "movimiento_dispositivo",
                schema: "public");

            migrationBuilder.DropTable(
                name: "movimiento_vehiculo",
                schema: "public");

            migrationBuilder.AddColumn<DateTime>(
                name: "actualizado_en",
                schema: "public",
                table: "area_responsable",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "asignado_en",
                schema: "public",
                table: "area_responsable",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "now()");

            migrationBuilder.AddColumn<int>(
                name: "id_usuario_actualiza",
                schema: "public",
                table: "area_responsable",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "id_usuario_asigna",
                schema: "public",
                table: "area_responsable",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "movimiento_objeto",
                schema: "public",
                columns: table => new
                {
                    id_movimiento_objeto = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    tipo_objeto = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    id_vehiculo = table.Column<int>(type: "integer", nullable: true),
                    id_dispositivo = table.Column<int>(type: "integer", nullable: true),
                    id_autorizacion_objeto = table.Column<int>(type: "integer", nullable: false),
                    id_autorizacion = table.Column<int>(type: "integer", nullable: false),
                    id_visita = table.Column<int>(type: "integer", nullable: false),
                    id_persona_responsable = table.Column<int>(type: "integer", nullable: false),
                    id_sede = table.Column<int>(type: "integer", nullable: false),
                    id_punto_acceso = table.Column<int>(type: "integer", nullable: false),
                    id_movimiento_acceso = table.Column<long>(type: "bigint", nullable: true),
                    id_movimiento_entrada = table.Column<long>(type: "bigint", nullable: true),
                    id_usuario_registra = table.Column<int>(type: "integer", nullable: false),
                    tipo_movimiento = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    ocurrido_en = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    observacion = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    id_organizacion = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_movimiento_objeto", x => x.id_movimiento_objeto);
                    table.UniqueConstraint("AK_movimiento_objeto_id_organizacion_id_movimiento_objeto", x => new { x.id_organizacion, x.id_movimiento_objeto });
                    table.CheckConstraint("ck_movimiento_objeto_objeto", "(tipo_objeto = 'Vehiculo' AND id_vehiculo IS NOT NULL AND id_dispositivo IS NULL) OR (tipo_objeto = 'Dispositivo' AND id_dispositivo IS NOT NULL AND id_vehiculo IS NULL)");
                    table.CheckConstraint("ck_movimiento_objeto_salida", "(tipo_movimiento = 'Salida') = (id_movimiento_entrada IS NOT NULL)");
                    table.CheckConstraint("ck_movimiento_objeto_tipo", "tipo_movimiento IN ('Entrada','Salida')");
                    table.ForeignKey(
                        name: "FK_movimiento_objeto_autorizacion_id_organizacion_id_autorizac~",
                        columns: x => new { x.id_organizacion, x.id_autorizacion },
                        principalSchema: "public",
                        principalTable: "autorizacion",
                        principalColumns: new[] { "id_organizacion", "id_autorizacion" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_movimiento_objeto_autorizacion_objeto_id_organizacion_id_au~",
                        columns: x => new { x.id_organizacion, x.id_autorizacion_objeto },
                        principalSchema: "public",
                        principalTable: "autorizacion_objeto",
                        principalColumns: new[] { "id_organizacion", "id_autorizacion_objeto" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_movimiento_objeto_dispositivo_id_organizacion_id_dispositivo",
                        columns: x => new { x.id_organizacion, x.id_dispositivo },
                        principalSchema: "public",
                        principalTable: "dispositivo",
                        principalColumns: new[] { "id_organizacion", "id_dispositivo" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_movimiento_objeto_movimiento_acceso_id_organizacion_id_movi~",
                        columns: x => new { x.id_organizacion, x.id_movimiento_acceso },
                        principalSchema: "public",
                        principalTable: "movimiento_acceso",
                        principalColumns: new[] { "id_organizacion", "id_movimiento" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_movimiento_objeto_movimiento_objeto_id_organizacion_id_movi~",
                        columns: x => new { x.id_organizacion, x.id_movimiento_entrada },
                        principalSchema: "public",
                        principalTable: "movimiento_objeto",
                        principalColumns: new[] { "id_organizacion", "id_movimiento_objeto" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_movimiento_objeto_organizacion_id_organizacion",
                        column: x => x.id_organizacion,
                        principalSchema: "public",
                        principalTable: "organizacion",
                        principalColumn: "id_organizacion",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_movimiento_objeto_persona_id_organizacion_id_persona_respon~",
                        columns: x => new { x.id_organizacion, x.id_persona_responsable },
                        principalSchema: "public",
                        principalTable: "persona",
                        principalColumns: new[] { "id_organizacion", "id_persona" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_movimiento_objeto_punto_acceso_id_organizacion_id_sede_id_p~",
                        columns: x => new { x.id_organizacion, x.id_sede, x.id_punto_acceso },
                        principalSchema: "public",
                        principalTable: "punto_acceso",
                        principalColumns: new[] { "id_organizacion", "id_sede", "id_punto_acceso" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_movimiento_objeto_usuario_id_usuario_registra",
                        column: x => x.id_usuario_registra,
                        principalSchema: "public",
                        principalTable: "usuario",
                        principalColumn: "id_usuario",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_movimiento_objeto_vehiculo_id_organizacion_id_vehiculo",
                        columns: x => new { x.id_organizacion, x.id_vehiculo },
                        principalSchema: "public",
                        principalTable: "vehiculo",
                        principalColumns: new[] { "id_organizacion", "id_vehiculo" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_movimiento_objeto_visita_id_organizacion_id_visita",
                        columns: x => new { x.id_organizacion, x.id_visita },
                        principalSchema: "public",
                        principalTable: "visita",
                        principalColumns: new[] { "id_organizacion", "id_visita" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_area_responsable_id_organizacion_id_membresia",
                schema: "public",
                table: "area_responsable",
                columns: new[] { "id_organizacion", "id_membresia" });

            migrationBuilder.CreateIndex(
                name: "IX_area_responsable_id_usuario_actualiza",
                schema: "public",
                table: "area_responsable",
                column: "id_usuario_actualiza");

            migrationBuilder.CreateIndex(
                name: "IX_area_responsable_id_usuario_asigna",
                schema: "public",
                table: "area_responsable",
                column: "id_usuario_asigna");

            migrationBuilder.CreateIndex(
                name: "IX_movimiento_objeto_id_organizacion_id_autorizacion",
                schema: "public",
                table: "movimiento_objeto",
                columns: new[] { "id_organizacion", "id_autorizacion" });

            migrationBuilder.CreateIndex(
                name: "IX_movimiento_objeto_id_organizacion_id_autorizacion_objeto",
                schema: "public",
                table: "movimiento_objeto",
                columns: new[] { "id_organizacion", "id_autorizacion_objeto" });

            migrationBuilder.CreateIndex(
                name: "IX_movimiento_objeto_id_organizacion_id_dispositivo_ocurrido_en",
                schema: "public",
                table: "movimiento_objeto",
                columns: new[] { "id_organizacion", "id_dispositivo", "ocurrido_en" });

            migrationBuilder.CreateIndex(
                name: "IX_movimiento_objeto_id_organizacion_id_movimiento_acceso",
                schema: "public",
                table: "movimiento_objeto",
                columns: new[] { "id_organizacion", "id_movimiento_acceso" });

            migrationBuilder.CreateIndex(
                name: "IX_movimiento_objeto_id_organizacion_id_movimiento_entrada",
                schema: "public",
                table: "movimiento_objeto",
                columns: new[] { "id_organizacion", "id_movimiento_entrada" },
                unique: true,
                filter: "id_movimiento_entrada IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_movimiento_objeto_id_organizacion_id_persona_responsable",
                schema: "public",
                table: "movimiento_objeto",
                columns: new[] { "id_organizacion", "id_persona_responsable" });

            migrationBuilder.CreateIndex(
                name: "IX_movimiento_objeto_id_organizacion_id_sede_id_punto_acceso",
                schema: "public",
                table: "movimiento_objeto",
                columns: new[] { "id_organizacion", "id_sede", "id_punto_acceso" });

            migrationBuilder.CreateIndex(
                name: "IX_movimiento_objeto_id_organizacion_id_sede_ocurrido_en",
                schema: "public",
                table: "movimiento_objeto",
                columns: new[] { "id_organizacion", "id_sede", "ocurrido_en" });

            migrationBuilder.CreateIndex(
                name: "IX_movimiento_objeto_id_organizacion_id_vehiculo_ocurrido_en",
                schema: "public",
                table: "movimiento_objeto",
                columns: new[] { "id_organizacion", "id_vehiculo", "ocurrido_en" });

            migrationBuilder.CreateIndex(
                name: "IX_movimiento_objeto_id_organizacion_id_visita",
                schema: "public",
                table: "movimiento_objeto",
                columns: new[] { "id_organizacion", "id_visita" });

            migrationBuilder.CreateIndex(
                name: "IX_movimiento_objeto_id_usuario_registra",
                schema: "public",
                table: "movimiento_objeto",
                column: "id_usuario_registra");

            migrationBuilder.AddForeignKey(
                name: "FK_area_responsable_area_id_organizacion_id_area",
                schema: "public",
                table: "area_responsable",
                columns: new[] { "id_organizacion", "id_area" },
                principalSchema: "public",
                principalTable: "area",
                principalColumns: new[] { "id_organizacion", "id_area" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_area_responsable_membresia_organizacion_id_organizacion_id_~",
                schema: "public",
                table: "area_responsable",
                columns: new[] { "id_organizacion", "id_membresia" },
                principalSchema: "public",
                principalTable: "membresia_organizacion",
                principalColumns: new[] { "id_organizacion", "id_membresia" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_area_responsable_organizacion_id_organizacion",
                schema: "public",
                table: "area_responsable",
                column: "id_organizacion",
                principalSchema: "public",
                principalTable: "organizacion",
                principalColumn: "id_organizacion",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_area_responsable_usuario_id_usuario_actualiza",
                schema: "public",
                table: "area_responsable",
                column: "id_usuario_actualiza",
                principalSchema: "public",
                principalTable: "usuario",
                principalColumn: "id_usuario",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_area_responsable_usuario_id_usuario_asigna",
                schema: "public",
                table: "area_responsable",
                column: "id_usuario_asigna",
                principalSchema: "public",
                principalTable: "usuario",
                principalColumn: "id_usuario",
                onDelete: ReferentialAction.Restrict);

            // Historial de movimientos de objetos de solo inserción (ADR-014/ADR-030): una salida es una fila nueva que
            // referencia su entrada; nunca se modifica ni borra un movimiento.
            migrationBuilder.Sql("""
                CREATE TRIGGER tr_movimiento_objeto_inmutable BEFORE UPDATE OR DELETE ON public.movimiento_objeto
                    FOR EACH ROW EXECUTE FUNCTION public.impedir_modificacion_historial();
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP TRIGGER IF EXISTS tr_movimiento_objeto_inmutable ON public.movimiento_objeto;");

            migrationBuilder.DropForeignKey(
                name: "FK_area_responsable_area_id_organizacion_id_area",
                schema: "public",
                table: "area_responsable");

            migrationBuilder.DropForeignKey(
                name: "FK_area_responsable_membresia_organizacion_id_organizacion_id_~",
                schema: "public",
                table: "area_responsable");

            migrationBuilder.DropForeignKey(
                name: "FK_area_responsable_organizacion_id_organizacion",
                schema: "public",
                table: "area_responsable");

            migrationBuilder.DropForeignKey(
                name: "FK_area_responsable_usuario_id_usuario_actualiza",
                schema: "public",
                table: "area_responsable");

            migrationBuilder.DropForeignKey(
                name: "FK_area_responsable_usuario_id_usuario_asigna",
                schema: "public",
                table: "area_responsable");

            migrationBuilder.DropTable(
                name: "movimiento_objeto",
                schema: "public");

            migrationBuilder.DropIndex(
                name: "IX_area_responsable_id_organizacion_id_membresia",
                schema: "public",
                table: "area_responsable");

            migrationBuilder.DropIndex(
                name: "IX_area_responsable_id_usuario_actualiza",
                schema: "public",
                table: "area_responsable");

            migrationBuilder.DropIndex(
                name: "IX_area_responsable_id_usuario_asigna",
                schema: "public",
                table: "area_responsable");

            migrationBuilder.DropColumn(
                name: "actualizado_en",
                schema: "public",
                table: "area_responsable");

            migrationBuilder.DropColumn(
                name: "asignado_en",
                schema: "public",
                table: "area_responsable");

            migrationBuilder.DropColumn(
                name: "id_usuario_actualiza",
                schema: "public",
                table: "area_responsable");

            migrationBuilder.DropColumn(
                name: "id_usuario_asigna",
                schema: "public",
                table: "area_responsable");

            migrationBuilder.CreateTable(
                name: "movimiento_dispositivo",
                schema: "public",
                columns: table => new
                {
                    id_movimiento_dispositivo = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    id_dispositivo = table.Column<int>(type: "integer", nullable: false),
                    id_organizacion = table.Column<int>(type: "integer", nullable: false),
                    id_persona_responsable = table.Column<int>(type: "integer", nullable: false),
                    id_sede = table.Column<int>(type: "integer", nullable: false),
                    id_usuario_registra = table.Column<int>(type: "integer", nullable: false),
                    id_visita = table.Column<int>(type: "integer", nullable: false),
                    id_zona_punto_acceso = table.Column<int>(type: "integer", nullable: false),
                    observacion = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    ocurrido_en = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    tipo_movimiento = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_movimiento_dispositivo", x => x.id_movimiento_dispositivo);
                });

            migrationBuilder.CreateTable(
                name: "movimiento_vehiculo",
                schema: "public",
                columns: table => new
                {
                    id_movimiento_vehiculo = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    id_movimiento = table.Column<long>(type: "bigint", nullable: false),
                    id_organizacion = table.Column<int>(type: "integer", nullable: false),
                    id_vehiculo = table.Column<int>(type: "integer", nullable: false),
                    id_visita = table.Column<int>(type: "integer", nullable: false),
                    ocurrido_en = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    tipo_movimiento = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_movimiento_vehiculo", x => x.id_movimiento_vehiculo);
                });
        }
    }
}
