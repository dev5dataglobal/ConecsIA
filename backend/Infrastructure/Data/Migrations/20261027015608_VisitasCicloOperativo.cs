using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class VisitasCicloOperativo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_zona_id_organizacion_id_sede",
                schema: "public",
                table: "zona");

            migrationBuilder.DropIndex(
                name: "IX_visita_id_organizacion_id_sede",
                schema: "public",
                table: "visita");

            migrationBuilder.AlterColumn<string>(
                name: "politica_aplicada",
                schema: "public",
                table: "visita",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(2000)",
                oldMaxLength: 2000,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "motivo",
                schema: "public",
                table: "visita",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(2000)",
                oldMaxLength: 2000,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "estado",
                schema: "public",
                table: "visita",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(2000)",
                oldMaxLength: 2000);

            migrationBuilder.AddColumn<DateTime>(
                name: "actualizado_en",
                schema: "public",
                table: "visita",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "fecha_esperada",
                schema: "public",
                table: "visita",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<TimeOnly>(
                name: "hora_esperada_fin",
                schema: "public",
                table: "visita",
                type: "time without time zone",
                nullable: true);

            migrationBuilder.AddColumn<TimeOnly>(
                name: "hora_esperada_inicio",
                schema: "public",
                table: "visita",
                type: "time without time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "id_usuario_actualiza",
                schema: "public",
                table: "visita",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "motivo_cierre",
                schema: "public",
                table: "visita",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "estado",
                schema: "public",
                table: "permanencia",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(2000)",
                oldMaxLength: 2000);

            migrationBuilder.AddColumn<long>(
                name: "duracion_segundos",
                schema: "public",
                table: "permanencia",
                type: "bigint",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "tipo_movimiento",
                schema: "public",
                table: "movimiento_acceso",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(2000)",
                oldMaxLength: 2000);

            migrationBuilder.AlterColumn<string>(
                name: "resultado",
                schema: "public",
                table: "movimiento_acceso",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(2000)",
                oldMaxLength: 2000);

            migrationBuilder.AlterColumn<string>(
                name: "observacion",
                schema: "public",
                table: "movimiento_acceso",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(2000)",
                oldMaxLength: 2000,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "medio",
                schema: "public",
                table: "movimiento_acceso",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(2000)",
                oldMaxLength: 2000);

            migrationBuilder.AddUniqueConstraint(
                name: "AK_visita_id_organizacion_id_visita",
                schema: "public",
                table: "visita",
                columns: new[] { "id_organizacion", "id_visita" });

            migrationBuilder.AddUniqueConstraint(
                name: "AK_movimiento_acceso_id_organizacion_id_movimiento",
                schema: "public",
                table: "movimiento_acceso",
                columns: new[] { "id_organizacion", "id_movimiento" });

            migrationBuilder.AddUniqueConstraint(
                name: "AK_membresia_organizacion_id_organizacion_id_membresia",
                schema: "public",
                table: "membresia_organizacion",
                columns: new[] { "id_organizacion", "id_membresia" });

            migrationBuilder.CreateIndex(
                name: "IX_zona_id_organizacion_id_sede",
                schema: "public",
                table: "zona",
                columns: new[] { "id_organizacion", "id_sede" });

            migrationBuilder.CreateIndex(
                name: "IX_visita_id_organizacion_creado_en",
                schema: "public",
                table: "visita",
                columns: new[] { "id_organizacion", "creado_en" });

            migrationBuilder.CreateIndex(
                name: "IX_visita_id_organizacion_id_autorizacion",
                schema: "public",
                table: "visita",
                columns: new[] { "id_organizacion", "id_autorizacion" });

            migrationBuilder.CreateIndex(
                name: "IX_visita_id_organizacion_id_membresia_responsable",
                schema: "public",
                table: "visita",
                columns: new[] { "id_organizacion", "id_membresia_responsable" });

            migrationBuilder.CreateIndex(
                name: "IX_visita_id_organizacion_id_sede_estado",
                schema: "public",
                table: "visita",
                columns: new[] { "id_organizacion", "id_sede", "estado" });

            migrationBuilder.CreateIndex(
                name: "IX_visita_id_usuario_actualiza",
                schema: "public",
                table: "visita",
                column: "id_usuario_actualiza");

            migrationBuilder.CreateIndex(
                name: "IX_visita_id_usuario_registra",
                schema: "public",
                table: "visita",
                column: "id_usuario_registra");

            migrationBuilder.AddCheckConstraint(
                name: "ck_visita_estado",
                schema: "public",
                table: "visita",
                sql: "estado IN ('Pendiente','Autorizada','Ingreso','Finalizada','Rechazada','Cancelada','Vencida')");

            migrationBuilder.AddCheckConstraint(
                name: "ck_visita_horario_esperado",
                schema: "public",
                table: "visita",
                sql: "hora_esperada_inicio IS NULL OR hora_esperada_fin IS NULL OR hora_esperada_fin > hora_esperada_inicio");

            migrationBuilder.CreateIndex(
                name: "IX_permanencia_id_organizacion_id_movimiento_entrada",
                schema: "public",
                table: "permanencia",
                columns: new[] { "id_organizacion", "id_movimiento_entrada" });

            migrationBuilder.CreateIndex(
                name: "IX_permanencia_id_organizacion_id_movimiento_salida",
                schema: "public",
                table: "permanencia",
                columns: new[] { "id_organizacion", "id_movimiento_salida" });

            migrationBuilder.CreateIndex(
                name: "IX_permanencia_id_organizacion_id_permanencia",
                schema: "public",
                table: "permanencia",
                columns: new[] { "id_organizacion", "id_permanencia" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_permanencia_id_organizacion_id_sede_estado",
                schema: "public",
                table: "permanencia",
                columns: new[] { "id_organizacion", "id_sede", "estado" });

            migrationBuilder.CreateIndex(
                name: "ux_permanencia_abierta_persona_sede",
                schema: "public",
                table: "permanencia",
                columns: new[] { "id_organizacion", "id_persona", "id_sede" },
                unique: true,
                filter: "estado = 'Abierta'");

            migrationBuilder.CreateIndex(
                name: "ux_permanencia_visita",
                schema: "public",
                table: "permanencia",
                columns: new[] { "id_organizacion", "id_visita" },
                unique: true);

            migrationBuilder.AddCheckConstraint(
                name: "ck_permanencia_cierre",
                schema: "public",
                table: "permanencia",
                sql: "(estado = 'Abierta' AND cerrada_en IS NULL AND id_movimiento_salida IS NULL) OR (estado = 'Cerrada' AND cerrada_en IS NOT NULL AND id_movimiento_salida IS NOT NULL AND cerrada_en >= abierta_en AND duracion_segundos >= 0)");

            migrationBuilder.AddCheckConstraint(
                name: "ck_permanencia_estado",
                schema: "public",
                table: "permanencia",
                sql: "estado IN ('Abierta','Cerrada')");

            migrationBuilder.CreateIndex(
                name: "IX_movimiento_acceso_id_organizacion_id_sede_ocurrido_en",
                schema: "public",
                table: "movimiento_acceso",
                columns: new[] { "id_organizacion", "id_sede", "ocurrido_en" });

            migrationBuilder.CreateIndex(
                name: "IX_movimiento_acceso_id_organizacion_id_visita",
                schema: "public",
                table: "movimiento_acceso",
                columns: new[] { "id_organizacion", "id_visita" });

            migrationBuilder.CreateIndex(
                name: "IX_movimiento_acceso_id_usuario_registra",
                schema: "public",
                table: "movimiento_acceso",
                column: "id_usuario_registra");

            migrationBuilder.AddCheckConstraint(
                name: "ck_movimiento_acceso_resultado",
                schema: "public",
                table: "movimiento_acceso",
                sql: "resultado IN ('Confirmado','Denegado')");

            migrationBuilder.AddCheckConstraint(
                name: "ck_movimiento_acceso_tipo",
                schema: "public",
                table: "movimiento_acceso",
                sql: "tipo_movimiento IN ('Entrada','Salida')");

            migrationBuilder.AddForeignKey(
                name: "FK_movimiento_acceso_sede_id_organizacion_id_sede",
                schema: "public",
                table: "movimiento_acceso",
                columns: new[] { "id_organizacion", "id_sede" },
                principalSchema: "public",
                principalTable: "sede",
                principalColumns: new[] { "id_organizacion", "id_sede" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_movimiento_acceso_usuario_id_usuario_registra",
                schema: "public",
                table: "movimiento_acceso",
                column: "id_usuario_registra",
                principalSchema: "public",
                principalTable: "usuario",
                principalColumn: "id_usuario",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_movimiento_acceso_visita_id_organizacion_id_visita",
                schema: "public",
                table: "movimiento_acceso",
                columns: new[] { "id_organizacion", "id_visita" },
                principalSchema: "public",
                principalTable: "visita",
                principalColumns: new[] { "id_organizacion", "id_visita" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_permanencia_movimiento_acceso_id_organizacion_id_movimiento~",
                schema: "public",
                table: "permanencia",
                columns: new[] { "id_organizacion", "id_movimiento_entrada" },
                principalSchema: "public",
                principalTable: "movimiento_acceso",
                principalColumns: new[] { "id_organizacion", "id_movimiento" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_permanencia_movimiento_acceso_id_organizacion_id_movimient~1",
                schema: "public",
                table: "permanencia",
                columns: new[] { "id_organizacion", "id_movimiento_salida" },
                principalSchema: "public",
                principalTable: "movimiento_acceso",
                principalColumns: new[] { "id_organizacion", "id_movimiento" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_permanencia_persona_id_organizacion_id_persona",
                schema: "public",
                table: "permanencia",
                columns: new[] { "id_organizacion", "id_persona" },
                principalSchema: "public",
                principalTable: "persona",
                principalColumns: new[] { "id_organizacion", "id_persona" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_permanencia_sede_id_organizacion_id_sede",
                schema: "public",
                table: "permanencia",
                columns: new[] { "id_organizacion", "id_sede" },
                principalSchema: "public",
                principalTable: "sede",
                principalColumns: new[] { "id_organizacion", "id_sede" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_permanencia_visita_id_organizacion_id_visita",
                schema: "public",
                table: "permanencia",
                columns: new[] { "id_organizacion", "id_visita" },
                principalSchema: "public",
                principalTable: "visita",
                principalColumns: new[] { "id_organizacion", "id_visita" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_visita_autorizacion_id_organizacion_id_autorizacion",
                schema: "public",
                table: "visita",
                columns: new[] { "id_organizacion", "id_autorizacion" },
                principalSchema: "public",
                principalTable: "autorizacion",
                principalColumns: new[] { "id_organizacion", "id_autorizacion" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_visita_membresia_organizacion_id_organizacion_id_membresia_~",
                schema: "public",
                table: "visita",
                columns: new[] { "id_organizacion", "id_membresia_responsable" },
                principalSchema: "public",
                principalTable: "membresia_organizacion",
                principalColumns: new[] { "id_organizacion", "id_membresia" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_visita_usuario_id_usuario_actualiza",
                schema: "public",
                table: "visita",
                column: "id_usuario_actualiza",
                principalSchema: "public",
                principalTable: "usuario",
                principalColumn: "id_usuario",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_visita_usuario_id_usuario_registra",
                schema: "public",
                table: "visita",
                column: "id_usuario_registra",
                principalSchema: "public",
                principalTable: "usuario",
                principalColumn: "id_usuario",
                onDelete: ReferentialAction.Restrict);

            // Pantalla de visitas disponible en el menú (antes «Pronto»).
            migrationBuilder.Sql("UPDATE public.app_modulos SET ruta = '/visitas' WHERE codigo = 'visitas' AND ruta IS NULL;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("UPDATE public.app_modulos SET ruta = NULL WHERE codigo = 'visitas' AND ruta = '/visitas';");

            migrationBuilder.DropForeignKey(
                name: "FK_movimiento_acceso_sede_id_organizacion_id_sede",
                schema: "public",
                table: "movimiento_acceso");

            migrationBuilder.DropForeignKey(
                name: "FK_movimiento_acceso_usuario_id_usuario_registra",
                schema: "public",
                table: "movimiento_acceso");

            migrationBuilder.DropForeignKey(
                name: "FK_movimiento_acceso_visita_id_organizacion_id_visita",
                schema: "public",
                table: "movimiento_acceso");

            migrationBuilder.DropForeignKey(
                name: "FK_permanencia_movimiento_acceso_id_organizacion_id_movimiento~",
                schema: "public",
                table: "permanencia");

            migrationBuilder.DropForeignKey(
                name: "FK_permanencia_movimiento_acceso_id_organizacion_id_movimient~1",
                schema: "public",
                table: "permanencia");

            migrationBuilder.DropForeignKey(
                name: "FK_permanencia_persona_id_organizacion_id_persona",
                schema: "public",
                table: "permanencia");

            migrationBuilder.DropForeignKey(
                name: "FK_permanencia_sede_id_organizacion_id_sede",
                schema: "public",
                table: "permanencia");

            migrationBuilder.DropForeignKey(
                name: "FK_permanencia_visita_id_organizacion_id_visita",
                schema: "public",
                table: "permanencia");

            migrationBuilder.DropForeignKey(
                name: "FK_visita_autorizacion_id_organizacion_id_autorizacion",
                schema: "public",
                table: "visita");

            migrationBuilder.DropForeignKey(
                name: "FK_visita_membresia_organizacion_id_organizacion_id_membresia_~",
                schema: "public",
                table: "visita");

            migrationBuilder.DropForeignKey(
                name: "FK_visita_usuario_id_usuario_actualiza",
                schema: "public",
                table: "visita");

            migrationBuilder.DropForeignKey(
                name: "FK_visita_usuario_id_usuario_registra",
                schema: "public",
                table: "visita");

            migrationBuilder.DropIndex(
                name: "IX_zona_id_organizacion_id_sede",
                schema: "public",
                table: "zona");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_visita_id_organizacion_id_visita",
                schema: "public",
                table: "visita");

            migrationBuilder.DropIndex(
                name: "IX_visita_id_organizacion_creado_en",
                schema: "public",
                table: "visita");

            migrationBuilder.DropIndex(
                name: "IX_visita_id_organizacion_id_autorizacion",
                schema: "public",
                table: "visita");

            migrationBuilder.DropIndex(
                name: "IX_visita_id_organizacion_id_membresia_responsable",
                schema: "public",
                table: "visita");

            migrationBuilder.DropIndex(
                name: "IX_visita_id_organizacion_id_sede_estado",
                schema: "public",
                table: "visita");

            migrationBuilder.DropIndex(
                name: "IX_visita_id_usuario_actualiza",
                schema: "public",
                table: "visita");

            migrationBuilder.DropIndex(
                name: "IX_visita_id_usuario_registra",
                schema: "public",
                table: "visita");

            migrationBuilder.DropCheckConstraint(
                name: "ck_visita_estado",
                schema: "public",
                table: "visita");

            migrationBuilder.DropCheckConstraint(
                name: "ck_visita_horario_esperado",
                schema: "public",
                table: "visita");

            migrationBuilder.DropIndex(
                name: "IX_permanencia_id_organizacion_id_movimiento_entrada",
                schema: "public",
                table: "permanencia");

            migrationBuilder.DropIndex(
                name: "IX_permanencia_id_organizacion_id_movimiento_salida",
                schema: "public",
                table: "permanencia");

            migrationBuilder.DropIndex(
                name: "IX_permanencia_id_organizacion_id_permanencia",
                schema: "public",
                table: "permanencia");

            migrationBuilder.DropIndex(
                name: "IX_permanencia_id_organizacion_id_sede_estado",
                schema: "public",
                table: "permanencia");

            migrationBuilder.DropIndex(
                name: "ux_permanencia_abierta_persona_sede",
                schema: "public",
                table: "permanencia");

            migrationBuilder.DropIndex(
                name: "ux_permanencia_visita",
                schema: "public",
                table: "permanencia");

            migrationBuilder.DropCheckConstraint(
                name: "ck_permanencia_cierre",
                schema: "public",
                table: "permanencia");

            migrationBuilder.DropCheckConstraint(
                name: "ck_permanencia_estado",
                schema: "public",
                table: "permanencia");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_movimiento_acceso_id_organizacion_id_movimiento",
                schema: "public",
                table: "movimiento_acceso");

            migrationBuilder.DropIndex(
                name: "IX_movimiento_acceso_id_organizacion_id_sede_ocurrido_en",
                schema: "public",
                table: "movimiento_acceso");

            migrationBuilder.DropIndex(
                name: "IX_movimiento_acceso_id_organizacion_id_visita",
                schema: "public",
                table: "movimiento_acceso");

            migrationBuilder.DropIndex(
                name: "IX_movimiento_acceso_id_usuario_registra",
                schema: "public",
                table: "movimiento_acceso");

            migrationBuilder.DropCheckConstraint(
                name: "ck_movimiento_acceso_resultado",
                schema: "public",
                table: "movimiento_acceso");

            migrationBuilder.DropCheckConstraint(
                name: "ck_movimiento_acceso_tipo",
                schema: "public",
                table: "movimiento_acceso");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_membresia_organizacion_id_organizacion_id_membresia",
                schema: "public",
                table: "membresia_organizacion");

            migrationBuilder.DropColumn(
                name: "actualizado_en",
                schema: "public",
                table: "visita");

            migrationBuilder.DropColumn(
                name: "fecha_esperada",
                schema: "public",
                table: "visita");

            migrationBuilder.DropColumn(
                name: "hora_esperada_fin",
                schema: "public",
                table: "visita");

            migrationBuilder.DropColumn(
                name: "hora_esperada_inicio",
                schema: "public",
                table: "visita");

            migrationBuilder.DropColumn(
                name: "id_usuario_actualiza",
                schema: "public",
                table: "visita");

            migrationBuilder.DropColumn(
                name: "motivo_cierre",
                schema: "public",
                table: "visita");

            migrationBuilder.DropColumn(
                name: "duracion_segundos",
                schema: "public",
                table: "permanencia");

            migrationBuilder.AlterColumn<string>(
                name: "politica_aplicada",
                schema: "public",
                table: "visita",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "motivo",
                schema: "public",
                table: "visita",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "estado",
                schema: "public",
                table: "visita",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(20)",
                oldMaxLength: 20);

            migrationBuilder.AlterColumn<string>(
                name: "estado",
                schema: "public",
                table: "permanencia",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(20)",
                oldMaxLength: 20);

            migrationBuilder.AlterColumn<string>(
                name: "tipo_movimiento",
                schema: "public",
                table: "movimiento_acceso",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(20)",
                oldMaxLength: 20);

            migrationBuilder.AlterColumn<string>(
                name: "resultado",
                schema: "public",
                table: "movimiento_acceso",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(20)",
                oldMaxLength: 20);

            migrationBuilder.AlterColumn<string>(
                name: "observacion",
                schema: "public",
                table: "movimiento_acceso",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "medio",
                schema: "public",
                table: "movimiento_acceso",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(20)",
                oldMaxLength: 20);

            migrationBuilder.CreateIndex(
                name: "IX_zona_id_organizacion_id_sede",
                schema: "public",
                table: "zona",
                columns: new[] { "id_organizacion", "id_sede" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_visita_id_organizacion_id_sede",
                schema: "public",
                table: "visita",
                columns: new[] { "id_organizacion", "id_sede" });
        }
    }
}
