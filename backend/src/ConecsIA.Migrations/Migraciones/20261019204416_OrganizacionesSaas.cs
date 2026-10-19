using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ConecsIA.Migrations.Migraciones
{
    /// <inheritdoc />
    public partial class OrganizacionesSaas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<List<string>>(
                name: "permisos_saas",
                table: "usuario",
                type: "text[]",
                nullable: false,
                defaultValueSql: "'{}'");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "actualizada_en",
                table: "organizacion",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<string>(
                name: "contacto_administrativo",
                table: "organizacion",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "identificacion_legal",
                table: "organizacion",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "inactivada_en",
                table: "organizacion",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "nombre_normalizado",
                table: "organizacion",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "tipo",
                table: "organizacion",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "otro");

            // xmin es una columna de sistema de PostgreSQL: el token de concurrencia no se agrega.

            migrationBuilder.AddColumn<string>(
                name: "zona_horaria",
                table: "organizacion",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            // Filas existentes: nombre normalizado como en Organizacion.NormalizarNombre y fecha de actualización.
            migrationBuilder.Sql("""
                UPDATE organizacion
                SET nombre_normalizado = lower(regexp_replace(btrim(nombre), '\s+', ' ', 'g')),
                    actualizada_en = creada_en;
                """);

            migrationBuilder.CreateIndex(
                name: "ix_organizacion_nombre_normalizado",
                table: "organizacion",
                column: "nombre_normalizado",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_organizacion_nombre_normalizado",
                table: "organizacion");

            migrationBuilder.DropColumn(
                name: "permisos_saas",
                table: "usuario");

            migrationBuilder.DropColumn(
                name: "actualizada_en",
                table: "organizacion");

            migrationBuilder.DropColumn(
                name: "contacto_administrativo",
                table: "organizacion");

            migrationBuilder.DropColumn(
                name: "identificacion_legal",
                table: "organizacion");

            migrationBuilder.DropColumn(
                name: "inactivada_en",
                table: "organizacion");

            migrationBuilder.DropColumn(
                name: "nombre_normalizado",
                table: "organizacion");

            migrationBuilder.DropColumn(
                name: "tipo",
                table: "organizacion");

            migrationBuilder.DropColumn(
                name: "zona_horaria",
                table: "organizacion");
        }
    }
}
