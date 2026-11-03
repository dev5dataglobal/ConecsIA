using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class DispositivosYAutorizacionesObjeto : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "serial_normalizado",
                schema: "public",
                table: "dispositivo",
                type: "character varying(60)",
                maxLength: 60,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(2000)",
                oldMaxLength: 2000,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "serial",
                schema: "public",
                table: "dispositivo",
                type: "character varying(60)",
                maxLength: 60,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(2000)",
                oldMaxLength: 2000,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "codigo_identificador",
                schema: "public",
                table: "dispositivo",
                type: "character varying(60)",
                maxLength: 60,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(2000)",
                oldMaxLength: 2000);

            migrationBuilder.AddColumn<int>(
                name: "id_usuario_crea",
                schema: "public",
                table: "dispositivo",
                type: "integer",
                nullable: true);

            migrationBuilder.AddUniqueConstraint(
                name: "AK_tipo_dispositivo_id_organizacion_id_tipo_dispositivo",
                schema: "public",
                table: "tipo_dispositivo",
                columns: new[] { "id_organizacion", "id_tipo_dispositivo" });

            migrationBuilder.AddUniqueConstraint(
                name: "AK_dispositivo_id_organizacion_id_dispositivo",
                schema: "public",
                table: "dispositivo",
                columns: new[] { "id_organizacion", "id_dispositivo" });

            migrationBuilder.AddUniqueConstraint(
                name: "AK_autorizacion_id_organizacion_id_autorizacion",
                schema: "public",
                table: "autorizacion",
                columns: new[] { "id_organizacion", "id_autorizacion" });

            migrationBuilder.CreateTable(
                name: "autorizacion_objeto",
                schema: "public",
                columns: table => new
                {
                    id_autorizacion_objeto = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    tipo_objeto = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    id_vehiculo = table.Column<int>(type: "integer", nullable: true),
                    id_dispositivo = table.Column<int>(type: "integer", nullable: true),
                    id_persona = table.Column<int>(type: "integer", nullable: false),
                    id_sede = table.Column<int>(type: "integer", nullable: false),
                    id_area = table.Column<int>(type: "integer", nullable: true),
                    id_autorizacion = table.Column<int>(type: "integer", nullable: true),
                    fecha_inicio = table.Column<DateOnly>(type: "date", nullable: false),
                    fecha_fin = table.Column<DateOnly>(type: "date", nullable: false),
                    hora_inicio = table.Column<TimeOnly>(type: "time without time zone", nullable: true),
                    hora_fin = table.Column<TimeOnly>(type: "time without time zone", nullable: true),
                    motivo = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    estado = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    motivo_decision = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    id_usuario_solicita = table.Column<int>(type: "integer", nullable: false),
                    solicitado_en = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    id_usuario_decide = table.Column<int>(type: "integer", nullable: true),
                    decidido_en = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    id_organizacion = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_autorizacion_objeto", x => x.id_autorizacion_objeto);
                    table.UniqueConstraint("AK_autorizacion_objeto_id_organizacion_id_autorizacion_objeto", x => new { x.id_organizacion, x.id_autorizacion_objeto });
                    table.CheckConstraint("CK_autorizacion_objeto_estado", "estado IN ('Pendiente', 'Aprobada', 'Rechazada', 'Revocada')");
                    table.CheckConstraint("CK_autorizacion_objeto_fechas", "fecha_fin >= fecha_inicio");
                    table.CheckConstraint("CK_autorizacion_objeto_horas", "(hora_inicio IS NULL AND hora_fin IS NULL) OR (hora_inicio IS NOT NULL AND hora_fin IS NOT NULL AND hora_fin > hora_inicio)");
                    table.CheckConstraint("CK_autorizacion_objeto_objeto", "(tipo_objeto = 'Vehiculo' AND id_vehiculo IS NOT NULL AND id_dispositivo IS NULL) OR (tipo_objeto = 'Dispositivo' AND id_dispositivo IS NOT NULL AND id_vehiculo IS NULL)");
                    table.ForeignKey(
                        name: "FK_autorizacion_objeto_area_id_organizacion_id_area",
                        columns: x => new { x.id_organizacion, x.id_area },
                        principalSchema: "public",
                        principalTable: "area",
                        principalColumns: new[] { "id_organizacion", "id_area" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_autorizacion_objeto_autorizacion_id_organizacion_id_autoriz~",
                        columns: x => new { x.id_organizacion, x.id_autorizacion },
                        principalSchema: "public",
                        principalTable: "autorizacion",
                        principalColumns: new[] { "id_organizacion", "id_autorizacion" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_autorizacion_objeto_dispositivo_id_organizacion_id_disposit~",
                        columns: x => new { x.id_organizacion, x.id_dispositivo },
                        principalSchema: "public",
                        principalTable: "dispositivo",
                        principalColumns: new[] { "id_organizacion", "id_dispositivo" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_autorizacion_objeto_organizacion_id_organizacion",
                        column: x => x.id_organizacion,
                        principalSchema: "public",
                        principalTable: "organizacion",
                        principalColumn: "id_organizacion",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_autorizacion_objeto_persona_id_organizacion_id_persona",
                        columns: x => new { x.id_organizacion, x.id_persona },
                        principalSchema: "public",
                        principalTable: "persona",
                        principalColumns: new[] { "id_organizacion", "id_persona" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_autorizacion_objeto_sede_id_organizacion_id_sede",
                        columns: x => new { x.id_organizacion, x.id_sede },
                        principalSchema: "public",
                        principalTable: "sede",
                        principalColumns: new[] { "id_organizacion", "id_sede" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_autorizacion_objeto_usuario_id_usuario_decide",
                        column: x => x.id_usuario_decide,
                        principalSchema: "public",
                        principalTable: "usuario",
                        principalColumn: "id_usuario",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_autorizacion_objeto_usuario_id_usuario_solicita",
                        column: x => x.id_usuario_solicita,
                        principalSchema: "public",
                        principalTable: "usuario",
                        principalColumn: "id_usuario",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_autorizacion_objeto_vehiculo_id_organizacion_id_vehiculo",
                        columns: x => new { x.id_organizacion, x.id_vehiculo },
                        principalSchema: "public",
                        principalTable: "vehiculo",
                        principalColumns: new[] { "id_organizacion", "id_vehiculo" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "autorizacion_objeto_transicion",
                schema: "public",
                columns: table => new
                {
                    id_transicion = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    id_autorizacion_objeto = table.Column<int>(type: "integer", nullable: false),
                    id_sede = table.Column<int>(type: "integer", nullable: false),
                    estado_anterior = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    estado_nuevo = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    motivo = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    id_usuario_actor = table.Column<int>(type: "integer", nullable: false),
                    ocurrido_en = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    id_organizacion = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_autorizacion_objeto_transicion", x => x.id_transicion);
                    table.ForeignKey(
                        name: "FK_autorizacion_objeto_transicion_autorizacion_objeto_id_organ~",
                        columns: x => new { x.id_organizacion, x.id_autorizacion_objeto },
                        principalSchema: "public",
                        principalTable: "autorizacion_objeto",
                        principalColumns: new[] { "id_organizacion", "id_autorizacion_objeto" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_autorizacion_objeto_transicion_sede_id_organizacion_id_sede",
                        columns: x => new { x.id_organizacion, x.id_sede },
                        principalSchema: "public",
                        principalTable: "sede",
                        principalColumns: new[] { "id_organizacion", "id_sede" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_autorizacion_objeto_transicion_usuario_id_usuario_actor",
                        column: x => x.id_usuario_actor,
                        principalSchema: "public",
                        principalTable: "usuario",
                        principalColumn: "id_usuario",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_tipo_dispositivo_id_organizacion_id_tipo_dispositivo",
                schema: "public",
                table: "tipo_dispositivo",
                columns: new[] { "id_organizacion", "id_tipo_dispositivo" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_tipo_dispositivo_id_organizacion_nombre",
                schema: "public",
                table: "tipo_dispositivo",
                columns: new[] { "id_organizacion", "nombre" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_persona_id_usuario_crea",
                schema: "public",
                table: "persona",
                column: "id_usuario_crea");

            migrationBuilder.CreateIndex(
                name: "IX_dispositivo_id_organizacion_codigo_identificador",
                schema: "public",
                table: "dispositivo",
                columns: new[] { "id_organizacion", "codigo_identificador" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_dispositivo_id_organizacion_id_dispositivo",
                schema: "public",
                table: "dispositivo",
                columns: new[] { "id_organizacion", "id_dispositivo" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_dispositivo_id_organizacion_id_persona_responsable",
                schema: "public",
                table: "dispositivo",
                columns: new[] { "id_organizacion", "id_persona_responsable" });

            migrationBuilder.CreateIndex(
                name: "IX_dispositivo_id_organizacion_id_tipo_dispositivo",
                schema: "public",
                table: "dispositivo",
                columns: new[] { "id_organizacion", "id_tipo_dispositivo" });

            migrationBuilder.CreateIndex(
                name: "IX_dispositivo_id_usuario_crea",
                schema: "public",
                table: "dispositivo",
                column: "id_usuario_crea");

            migrationBuilder.CreateIndex(
                name: "IX_autorizacion_objeto_id_organizacion_estado_fecha_inicio",
                schema: "public",
                table: "autorizacion_objeto",
                columns: new[] { "id_organizacion", "estado", "fecha_inicio" });

            migrationBuilder.CreateIndex(
                name: "IX_autorizacion_objeto_id_organizacion_id_area",
                schema: "public",
                table: "autorizacion_objeto",
                columns: new[] { "id_organizacion", "id_area" });

            migrationBuilder.CreateIndex(
                name: "IX_autorizacion_objeto_id_organizacion_id_autorizacion",
                schema: "public",
                table: "autorizacion_objeto",
                columns: new[] { "id_organizacion", "id_autorizacion" });

            migrationBuilder.CreateIndex(
                name: "IX_autorizacion_objeto_id_organizacion_id_autorizacion_objeto",
                schema: "public",
                table: "autorizacion_objeto",
                columns: new[] { "id_organizacion", "id_autorizacion_objeto" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_autorizacion_objeto_id_organizacion_id_dispositivo",
                schema: "public",
                table: "autorizacion_objeto",
                columns: new[] { "id_organizacion", "id_dispositivo" });

            migrationBuilder.CreateIndex(
                name: "IX_autorizacion_objeto_id_organizacion_id_persona",
                schema: "public",
                table: "autorizacion_objeto",
                columns: new[] { "id_organizacion", "id_persona" });

            migrationBuilder.CreateIndex(
                name: "IX_autorizacion_objeto_id_organizacion_id_sede",
                schema: "public",
                table: "autorizacion_objeto",
                columns: new[] { "id_organizacion", "id_sede" });

            migrationBuilder.CreateIndex(
                name: "IX_autorizacion_objeto_id_organizacion_id_vehiculo",
                schema: "public",
                table: "autorizacion_objeto",
                columns: new[] { "id_organizacion", "id_vehiculo" });

            migrationBuilder.CreateIndex(
                name: "IX_autorizacion_objeto_id_usuario_decide",
                schema: "public",
                table: "autorizacion_objeto",
                column: "id_usuario_decide");

            migrationBuilder.CreateIndex(
                name: "IX_autorizacion_objeto_id_usuario_solicita",
                schema: "public",
                table: "autorizacion_objeto",
                column: "id_usuario_solicita");

            migrationBuilder.CreateIndex(
                name: "IX_autorizacion_objeto_transicion_id_organizacion_id_autorizac~",
                schema: "public",
                table: "autorizacion_objeto_transicion",
                columns: new[] { "id_organizacion", "id_autorizacion_objeto" });

            migrationBuilder.CreateIndex(
                name: "IX_autorizacion_objeto_transicion_id_organizacion_id_sede",
                schema: "public",
                table: "autorizacion_objeto_transicion",
                columns: new[] { "id_organizacion", "id_sede" });

            migrationBuilder.CreateIndex(
                name: "IX_autorizacion_objeto_transicion_id_usuario_actor",
                schema: "public",
                table: "autorizacion_objeto_transicion",
                column: "id_usuario_actor");

            migrationBuilder.AddForeignKey(
                name: "FK_dispositivo_organizacion_id_organizacion",
                schema: "public",
                table: "dispositivo",
                column: "id_organizacion",
                principalSchema: "public",
                principalTable: "organizacion",
                principalColumn: "id_organizacion",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_dispositivo_persona_id_organizacion_id_persona_responsable",
                schema: "public",
                table: "dispositivo",
                columns: new[] { "id_organizacion", "id_persona_responsable" },
                principalSchema: "public",
                principalTable: "persona",
                principalColumns: new[] { "id_organizacion", "id_persona" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_dispositivo_tipo_dispositivo_id_organizacion_id_tipo_dispos~",
                schema: "public",
                table: "dispositivo",
                columns: new[] { "id_organizacion", "id_tipo_dispositivo" },
                principalSchema: "public",
                principalTable: "tipo_dispositivo",
                principalColumns: new[] { "id_organizacion", "id_tipo_dispositivo" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_dispositivo_usuario_id_usuario_crea",
                schema: "public",
                table: "dispositivo",
                column: "id_usuario_crea",
                principalSchema: "public",
                principalTable: "usuario",
                principalColumn: "id_usuario",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_persona_usuario_id_usuario_crea",
                schema: "public",
                table: "persona",
                column: "id_usuario_crea",
                principalSchema: "public",
                principalTable: "usuario",
                principalColumn: "id_usuario",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_tipo_dispositivo_organizacion_id_organizacion",
                schema: "public",
                table: "tipo_dispositivo",
                column: "id_organizacion",
                principalSchema: "public",
                principalTable: "organizacion",
                principalColumn: "id_organizacion",
                onDelete: ReferentialAction.Restrict);
            // Auditoría y transiciones append-only (ADR-014): la base rechaza UPDATE y DELETE.
            migrationBuilder.Sql("""
                CREATE OR REPLACE FUNCTION public.impedir_modificacion_historial() RETURNS trigger LANGUAGE plpgsql AS $$
                BEGIN
                    RAISE EXCEPTION 'La tabla % es de solo inserción: no se modifican ni eliminan eventos.', TG_TABLE_NAME
                        USING ERRCODE = 'insufficient_privilege';
                END;
                $$;
                CREATE TRIGGER tr_auditoria_inmutable BEFORE UPDATE OR DELETE ON public.auditoria
                    FOR EACH ROW EXECUTE FUNCTION public.impedir_modificacion_historial();
                CREATE TRIGGER tr_autorizacion_objeto_transicion_inmutable BEFORE UPDATE OR DELETE ON public.autorizacion_objeto_transicion
                    FOR EACH ROW EXECUTE FUNCTION public.impedir_modificacion_historial();
                UPDATE public.app_modulos SET ruta = '/autorizaciones' WHERE codigo = 'autorizaciones' AND ruta IS NULL;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DROP TRIGGER IF EXISTS tr_autorizacion_objeto_transicion_inmutable ON public.autorizacion_objeto_transicion;
                DROP TRIGGER IF EXISTS tr_auditoria_inmutable ON public.auditoria;
                DROP FUNCTION IF EXISTS public.impedir_modificacion_historial();
                UPDATE public.app_modulos SET ruta = NULL WHERE codigo = 'autorizaciones' AND ruta = '/autorizaciones';
                """);
            migrationBuilder.DropForeignKey(
                name: "FK_dispositivo_organizacion_id_organizacion",
                schema: "public",
                table: "dispositivo");

            migrationBuilder.DropForeignKey(
                name: "FK_dispositivo_persona_id_organizacion_id_persona_responsable",
                schema: "public",
                table: "dispositivo");

            migrationBuilder.DropForeignKey(
                name: "FK_dispositivo_tipo_dispositivo_id_organizacion_id_tipo_dispos~",
                schema: "public",
                table: "dispositivo");

            migrationBuilder.DropForeignKey(
                name: "FK_dispositivo_usuario_id_usuario_crea",
                schema: "public",
                table: "dispositivo");

            migrationBuilder.DropForeignKey(
                name: "FK_persona_usuario_id_usuario_crea",
                schema: "public",
                table: "persona");

            migrationBuilder.DropForeignKey(
                name: "FK_tipo_dispositivo_organizacion_id_organizacion",
                schema: "public",
                table: "tipo_dispositivo");

            migrationBuilder.DropTable(
                name: "autorizacion_objeto_transicion",
                schema: "public");

            migrationBuilder.DropTable(
                name: "autorizacion_objeto",
                schema: "public");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_tipo_dispositivo_id_organizacion_id_tipo_dispositivo",
                schema: "public",
                table: "tipo_dispositivo");

            migrationBuilder.DropIndex(
                name: "IX_tipo_dispositivo_id_organizacion_id_tipo_dispositivo",
                schema: "public",
                table: "tipo_dispositivo");

            migrationBuilder.DropIndex(
                name: "IX_tipo_dispositivo_id_organizacion_nombre",
                schema: "public",
                table: "tipo_dispositivo");

            migrationBuilder.DropIndex(
                name: "IX_persona_id_usuario_crea",
                schema: "public",
                table: "persona");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_dispositivo_id_organizacion_id_dispositivo",
                schema: "public",
                table: "dispositivo");

            migrationBuilder.DropIndex(
                name: "IX_dispositivo_id_organizacion_codigo_identificador",
                schema: "public",
                table: "dispositivo");

            migrationBuilder.DropIndex(
                name: "IX_dispositivo_id_organizacion_id_dispositivo",
                schema: "public",
                table: "dispositivo");

            migrationBuilder.DropIndex(
                name: "IX_dispositivo_id_organizacion_id_persona_responsable",
                schema: "public",
                table: "dispositivo");

            migrationBuilder.DropIndex(
                name: "IX_dispositivo_id_organizacion_id_tipo_dispositivo",
                schema: "public",
                table: "dispositivo");

            migrationBuilder.DropIndex(
                name: "IX_dispositivo_id_usuario_crea",
                schema: "public",
                table: "dispositivo");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_autorizacion_id_organizacion_id_autorizacion",
                schema: "public",
                table: "autorizacion");

            migrationBuilder.DropColumn(
                name: "id_usuario_crea",
                schema: "public",
                table: "dispositivo");

            migrationBuilder.AlterColumn<string>(
                name: "serial_normalizado",
                schema: "public",
                table: "dispositivo",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(60)",
                oldMaxLength: 60,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "serial",
                schema: "public",
                table: "dispositivo",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(60)",
                oldMaxLength: 60,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "codigo_identificador",
                schema: "public",
                table: "dispositivo",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(60)",
                oldMaxLength: 60);
        }
    }
}
