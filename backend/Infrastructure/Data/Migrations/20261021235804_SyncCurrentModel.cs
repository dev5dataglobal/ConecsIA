using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class SyncCurrentModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_zona_id_organizacion_id_sede",
                schema: "public",
                table: "zona");

            migrationBuilder.DropIndex(
                name: "IX_persona_id_organizacion_id_tipo_persona",
                schema: "public",
                table: "persona");

            migrationBuilder.DropIndex(
                name: "IX_area_id_organizacion_id_sede",
                schema: "public",
                table: "area");

            migrationBuilder.CreateIndex(
                name: "IX_zona_id_organizacion_id_sede",
                schema: "public",
                table: "zona",
                columns: new[] { "id_organizacion", "id_sede" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_persona_id_organizacion_id_tipo_persona",
                schema: "public",
                table: "persona",
                columns: new[] { "id_organizacion", "id_tipo_persona" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_organizacion_id_archivo_logo",
                schema: "public",
                table: "organizacion",
                column: "id_archivo_logo");

            migrationBuilder.CreateIndex(
                name: "IX_organizacion_id_plan",
                schema: "public",
                table: "organizacion",
                column: "id_plan");

            migrationBuilder.CreateIndex(
                name: "IX_organizacion_id_tipo_organizacion",
                schema: "public",
                table: "organizacion",
                column: "id_tipo_organizacion");

            migrationBuilder.CreateIndex(
                name: "IX_area_id_organizacion_id_sede",
                schema: "public",
                table: "area",
                columns: new[] { "id_organizacion", "id_sede" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_organizacion_archivo_id_archivo_logo",
                schema: "public",
                table: "organizacion",
                column: "id_archivo_logo",
                principalSchema: "public",
                principalTable: "archivo",
                principalColumn: "id_archivo",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_organizacion_plan_id_plan",
                schema: "public",
                table: "organizacion",
                column: "id_plan",
                principalSchema: "public",
                principalTable: "plan",
                principalColumn: "id_plan",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_organizacion_tipo_organizacion_id_tipo_organizacion",
                schema: "public",
                table: "organizacion",
                column: "id_tipo_organizacion",
                principalSchema: "public",
                principalTable: "tipo_organizacion",
                principalColumn: "id_tipo_organizacion",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_organizacion_archivo_id_archivo_logo",
                schema: "public",
                table: "organizacion");

            migrationBuilder.DropForeignKey(
                name: "FK_organizacion_plan_id_plan",
                schema: "public",
                table: "organizacion");

            migrationBuilder.DropForeignKey(
                name: "FK_organizacion_tipo_organizacion_id_tipo_organizacion",
                schema: "public",
                table: "organizacion");

            migrationBuilder.DropIndex(
                name: "IX_zona_id_organizacion_id_sede",
                schema: "public",
                table: "zona");

            migrationBuilder.DropIndex(
                name: "IX_persona_id_organizacion_id_tipo_persona",
                schema: "public",
                table: "persona");

            migrationBuilder.DropIndex(
                name: "IX_organizacion_id_archivo_logo",
                schema: "public",
                table: "organizacion");

            migrationBuilder.DropIndex(
                name: "IX_organizacion_id_plan",
                schema: "public",
                table: "organizacion");

            migrationBuilder.DropIndex(
                name: "IX_organizacion_id_tipo_organizacion",
                schema: "public",
                table: "organizacion");

            migrationBuilder.DropIndex(
                name: "IX_area_id_organizacion_id_sede",
                schema: "public",
                table: "area");

            migrationBuilder.CreateIndex(
                name: "IX_zona_id_organizacion_id_sede",
                schema: "public",
                table: "zona",
                columns: new[] { "id_organizacion", "id_sede" });

            migrationBuilder.CreateIndex(
                name: "IX_persona_id_organizacion_id_tipo_persona",
                schema: "public",
                table: "persona",
                columns: new[] { "id_organizacion", "id_tipo_persona" });

            migrationBuilder.CreateIndex(
                name: "IX_area_id_organizacion_id_sede",
                schema: "public",
                table: "area",
                columns: new[] { "id_organizacion", "id_sede" });
        }
    }
}
