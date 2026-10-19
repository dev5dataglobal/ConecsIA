using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ConecsIA.Migrations.Migraciones
{
    /// <inheritdoc />
    public partial class CredencialUsuario : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "bloqueado_hasta",
                table: "usuario",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "hash_contrasena",
                table: "usuario",
                type: "character varying(512)",
                maxLength: 512,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "intentos_fallidos",
                table: "usuario",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "sello_seguridad",
                table: "usuario",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "");

            // Usuarios existentes: sello propio para que la revocación funcione desde el primer ingreso.
            migrationBuilder.Sql("UPDATE usuario SET sello_seguridad = replace(gen_random_uuid()::text, '-', '') WHERE sello_seguridad = '';");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "bloqueado_hasta",
                table: "usuario");

            migrationBuilder.DropColumn(
                name: "hash_contrasena",
                table: "usuario");

            migrationBuilder.DropColumn(
                name: "intentos_fallidos",
                table: "usuario");

            migrationBuilder.DropColumn(
                name: "sello_seguridad",
                table: "usuario");
        }
    }
}
