using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ConecsIA.Migrations.Migraciones
{
    /// <inheritdoc />
    public partial class SesionesUsuario : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "sesion_usuario",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    usuario_id = table.Column<Guid>(type: "uuid", nullable: false),
                    iniciada_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    expira_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    revocada_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_sesion_usuario", x => x.id);
                    table.ForeignKey(
                        name: "fk_sesion_usuario_usuario_usuario_id",
                        column: x => x.usuario_id,
                        principalTable: "usuario",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_sesion_usuario_usuario_id_revocada_en",
                table: "sesion_usuario",
                columns: new[] { "usuario_id", "revocada_en" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "sesion_usuario");
        }
    }
}
