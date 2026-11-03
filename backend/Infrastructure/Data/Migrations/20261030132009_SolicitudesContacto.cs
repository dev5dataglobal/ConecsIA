using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class SolicitudesContacto : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "solicitud_contacto",
                schema: "public",
                columns: table => new
                {
                    id_solicitud_contacto = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    organizacion = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    nombre_contacto = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    correo = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    telefono = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    tipo_organizacion = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    numero_sedes = table.Column<int>(type: "integer", nullable: true),
                    mensaje = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    estado = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    fecha_creacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_solicitud_contacto", x => x.id_solicitud_contacto);
                    table.CheckConstraint("ck_solicitud_contacto_numero_sedes", "numero_sedes IS NULL OR numero_sedes BETWEEN 1 AND 9999");
                });

            migrationBuilder.CreateIndex(
                name: "IX_solicitud_contacto_fecha_creacion",
                schema: "public",
                table: "solicitud_contacto",
                column: "fecha_creacion");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "solicitud_contacto",
                schema: "public");
        }
    }
}
