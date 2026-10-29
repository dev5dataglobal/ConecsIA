using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class PersonaTipoAccesoYExcepcionVisita : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "excepcion_tipo_acceso",
                schema: "public",
                table: "visita",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "motivo_excepcion",
                schema: "public",
                table: "visita",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "tipo_acceso",
                schema: "public",
                table: "persona",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Extemporaneo");

            migrationBuilder.AddCheckConstraint(
                name: "ck_visita_excepcion_motivo",
                schema: "public",
                table: "visita",
                sql: "NOT excepcion_tipo_acceso OR motivo_excepcion IS NOT NULL");

            migrationBuilder.AddCheckConstraint(
                name: "ck_persona_tipo_acceso",
                schema: "public",
                table: "persona",
                sql: "tipo_acceso IN ('Recurrente','Extemporaneo')");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_visita_excepcion_motivo",
                schema: "public",
                table: "visita");

            migrationBuilder.DropCheckConstraint(
                name: "ck_persona_tipo_acceso",
                schema: "public",
                table: "persona");

            migrationBuilder.DropColumn(
                name: "excepcion_tipo_acceso",
                schema: "public",
                table: "visita");

            migrationBuilder.DropColumn(
                name: "motivo_excepcion",
                schema: "public",
                table: "visita");

            migrationBuilder.DropColumn(
                name: "tipo_acceso",
                schema: "public",
                table: "persona");
        }
    }
}
