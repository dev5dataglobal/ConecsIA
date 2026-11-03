using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class SesionesRefrescoYRecuperacionContrasena : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "recuperacion_contrasena",
                schema: "public",
                columns: table => new
                {
                    id_recuperacion_contrasena = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    id_usuario = table.Column<int>(type: "integer", nullable: false),
                    token_hash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    creado_en = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    expira_en = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    usado_en = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_recuperacion_contrasena", x => x.id_recuperacion_contrasena);
                    table.ForeignKey(
                        name: "FK_recuperacion_contrasena_usuario_id_usuario",
                        column: x => x.id_usuario,
                        principalSchema: "public",
                        principalTable: "usuario",
                        principalColumn: "id_usuario",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "sesion_refresco",
                schema: "public",
                columns: table => new
                {
                    id_sesion_refresco = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    id_usuario = table.Column<int>(type: "integer", nullable: false),
                    token_hash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    recordar = table.Column<bool>(type: "boolean", nullable: false),
                    sello_seguridad = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    creado_en = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    expira_en = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    revocado_en = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    id_reemplazo = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sesion_refresco", x => x.id_sesion_refresco);
                    table.ForeignKey(
                        name: "FK_sesion_refresco_usuario_id_usuario",
                        column: x => x.id_usuario,
                        principalSchema: "public",
                        principalTable: "usuario",
                        principalColumn: "id_usuario",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_recuperacion_contrasena_id_usuario",
                schema: "public",
                table: "recuperacion_contrasena",
                column: "id_usuario");

            migrationBuilder.CreateIndex(
                name: "IX_recuperacion_contrasena_token_hash",
                schema: "public",
                table: "recuperacion_contrasena",
                column: "token_hash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_sesion_refresco_id_usuario",
                schema: "public",
                table: "sesion_refresco",
                column: "id_usuario");

            migrationBuilder.CreateIndex(
                name: "IX_sesion_refresco_token_hash",
                schema: "public",
                table: "sesion_refresco",
                column: "token_hash",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "recuperacion_contrasena",
                schema: "public");

            migrationBuilder.DropTable(
                name: "sesion_refresco",
                schema: "public");
        }
    }
}
