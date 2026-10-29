using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AutorizacionesAccesoYRetiroTipoAcceso : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_persona_tipo_acceso",
                schema: "public",
                table: "persona");

            migrationBuilder.DropColumn(
                name: "tipo_acceso",
                schema: "public",
                table: "persona");

            migrationBuilder.AddColumn<DateTime>(
                name: "decidido_en",
                schema: "public",
                table: "autorizacion",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<TimeOnly>(
                name: "hora_fin",
                schema: "public",
                table: "autorizacion",
                type: "time",
                nullable: true);

            migrationBuilder.AddColumn<TimeOnly>(
                name: "hora_inicio",
                schema: "public",
                table: "autorizacion",
                type: "time",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "id_usuario_decide",
                schema: "public",
                table: "autorizacion",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "motivo_decision",
                schema: "public",
                table: "autorizacion",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_autorizacion_transicion_id_organizacion_id_autorizacion",
                schema: "public",
                table: "autorizacion_transicion",
                columns: new[] { "id_organizacion", "id_autorizacion" });

            migrationBuilder.CreateIndex(
                name: "IX_autorizacion_horario_id_autorizacion_dia_semana",
                schema: "public",
                table: "autorizacion_horario",
                columns: new[] { "id_autorizacion", "dia_semana" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_autorizacion_horario_id_organizacion_id_autorizacion",
                schema: "public",
                table: "autorizacion_horario",
                columns: new[] { "id_organizacion", "id_autorizacion" });

            migrationBuilder.AddCheckConstraint(
                name: "ck_autorizacion_horario_horas",
                schema: "public",
                table: "autorizacion_horario",
                sql: "hora_fin > hora_inicio");

            migrationBuilder.CreateIndex(
                name: "IX_autorizacion_id_organizacion_estado",
                schema: "public",
                table: "autorizacion",
                columns: new[] { "id_organizacion", "estado" });

            migrationBuilder.CreateIndex(
                name: "IX_autorizacion_id_usuario_crea",
                schema: "public",
                table: "autorizacion",
                column: "id_usuario_crea");

            migrationBuilder.CreateIndex(
                name: "IX_autorizacion_id_usuario_decide",
                schema: "public",
                table: "autorizacion",
                column: "id_usuario_decide");

            migrationBuilder.AddCheckConstraint(
                name: "ck_autorizacion_estado",
                schema: "public",
                table: "autorizacion",
                sql: "estado IN ('Pendiente','Aprobada','Rechazada','Cancelada','Revocada','Vencida','Agotada')");

            migrationBuilder.AddCheckConstraint(
                name: "ck_autorizacion_fechas",
                schema: "public",
                table: "autorizacion",
                sql: "(modalidad = 'Permanente' AND fecha_fin IS NULL) OR (modalidad = 'Recurrente' AND (fecha_fin IS NULL OR fecha_fin >= fecha_inicio)) OR (modalidad IN ('Unica','Diaria') AND fecha_fin = fecha_inicio) OR (modalidad = 'Temporal' AND fecha_fin >= fecha_inicio)");

            migrationBuilder.AddCheckConstraint(
                name: "ck_autorizacion_horas",
                schema: "public",
                table: "autorizacion",
                sql: "(modalidad = 'Temporal' AND hora_inicio IS NOT NULL AND hora_fin IS NOT NULL AND (fecha_fin > fecha_inicio OR hora_fin > hora_inicio)) OR (modalidad IN ('Unica','Diaria') AND ((hora_inicio IS NULL AND hora_fin IS NULL) OR (hora_inicio IS NOT NULL AND hora_fin IS NOT NULL AND hora_fin > hora_inicio))) OR (modalidad IN ('Recurrente','Permanente') AND hora_inicio IS NULL AND hora_fin IS NULL)");

            migrationBuilder.AddCheckConstraint(
                name: "ck_autorizacion_modalidad",
                schema: "public",
                table: "autorizacion",
                sql: "modalidad IN ('Unica','Diaria','Temporal','Recurrente','Permanente')");

            migrationBuilder.AddForeignKey(
                name: "FK_autorizacion_usuario_id_usuario_crea",
                schema: "public",
                table: "autorizacion",
                column: "id_usuario_crea",
                principalSchema: "public",
                principalTable: "usuario",
                principalColumn: "id_usuario",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_autorizacion_usuario_id_usuario_decide",
                schema: "public",
                table: "autorizacion",
                column: "id_usuario_decide",
                principalSchema: "public",
                principalTable: "usuario",
                principalColumn: "id_usuario",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_autorizacion_horario_autorizacion_id_organizacion_id_autori~",
                schema: "public",
                table: "autorizacion_horario",
                columns: new[] { "id_organizacion", "id_autorizacion" },
                principalSchema: "public",
                principalTable: "autorizacion",
                principalColumns: new[] { "id_organizacion", "id_autorizacion" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_autorizacion_transicion_autorizacion_id_organizacion_id_aut~",
                schema: "public",
                table: "autorizacion_transicion",
                columns: new[] { "id_organizacion", "id_autorizacion" },
                principalSchema: "public",
                principalTable: "autorizacion",
                principalColumns: new[] { "id_organizacion", "id_autorizacion" },
                onDelete: ReferentialAction.Restrict);

            // El módulo Control de accesos pasa a tener pantalla (solicitudes de autorización de personas).
            migrationBuilder.Sql("UPDATE public.app_modulos SET ruta = '/control-accesos' WHERE codigo = 'control_accesos' AND ruta IS NULL;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("UPDATE public.app_modulos SET ruta = NULL WHERE codigo = 'control_accesos' AND ruta = '/control-accesos';");

            migrationBuilder.DropForeignKey(
                name: "FK_autorizacion_usuario_id_usuario_crea",
                schema: "public",
                table: "autorizacion");

            migrationBuilder.DropForeignKey(
                name: "FK_autorizacion_usuario_id_usuario_decide",
                schema: "public",
                table: "autorizacion");

            migrationBuilder.DropForeignKey(
                name: "FK_autorizacion_horario_autorizacion_id_organizacion_id_autori~",
                schema: "public",
                table: "autorizacion_horario");

            migrationBuilder.DropForeignKey(
                name: "FK_autorizacion_transicion_autorizacion_id_organizacion_id_aut~",
                schema: "public",
                table: "autorizacion_transicion");

            migrationBuilder.DropIndex(
                name: "IX_autorizacion_transicion_id_organizacion_id_autorizacion",
                schema: "public",
                table: "autorizacion_transicion");

            migrationBuilder.DropIndex(
                name: "IX_autorizacion_horario_id_autorizacion_dia_semana",
                schema: "public",
                table: "autorizacion_horario");

            migrationBuilder.DropIndex(
                name: "IX_autorizacion_horario_id_organizacion_id_autorizacion",
                schema: "public",
                table: "autorizacion_horario");

            migrationBuilder.DropCheckConstraint(
                name: "ck_autorizacion_horario_horas",
                schema: "public",
                table: "autorizacion_horario");

            migrationBuilder.DropIndex(
                name: "IX_autorizacion_id_organizacion_estado",
                schema: "public",
                table: "autorizacion");

            migrationBuilder.DropIndex(
                name: "IX_autorizacion_id_usuario_crea",
                schema: "public",
                table: "autorizacion");

            migrationBuilder.DropIndex(
                name: "IX_autorizacion_id_usuario_decide",
                schema: "public",
                table: "autorizacion");

            migrationBuilder.DropCheckConstraint(
                name: "ck_autorizacion_estado",
                schema: "public",
                table: "autorizacion");

            migrationBuilder.DropCheckConstraint(
                name: "ck_autorizacion_fechas",
                schema: "public",
                table: "autorizacion");

            migrationBuilder.DropCheckConstraint(
                name: "ck_autorizacion_horas",
                schema: "public",
                table: "autorizacion");

            migrationBuilder.DropCheckConstraint(
                name: "ck_autorizacion_modalidad",
                schema: "public",
                table: "autorizacion");

            migrationBuilder.DropColumn(
                name: "decidido_en",
                schema: "public",
                table: "autorizacion");

            migrationBuilder.DropColumn(
                name: "hora_fin",
                schema: "public",
                table: "autorizacion");

            migrationBuilder.DropColumn(
                name: "hora_inicio",
                schema: "public",
                table: "autorizacion");

            migrationBuilder.DropColumn(
                name: "id_usuario_decide",
                schema: "public",
                table: "autorizacion");

            migrationBuilder.DropColumn(
                name: "motivo_decision",
                schema: "public",
                table: "autorizacion");

            migrationBuilder.AddColumn<string>(
                name: "tipo_acceso",
                schema: "public",
                table: "persona",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Extemporaneo");

            migrationBuilder.AddCheckConstraint(
                name: "ck_persona_tipo_acceso",
                schema: "public",
                table: "persona",
                sql: "tipo_acceso IN ('Recurrente','Extemporaneo')");
        }
    }
}
