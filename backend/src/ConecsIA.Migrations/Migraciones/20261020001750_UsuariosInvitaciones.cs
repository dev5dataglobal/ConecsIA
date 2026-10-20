using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ConecsIA.Migrations.Migraciones
{
    /// <inheritdoc />
    public partial class UsuariosInvitaciones : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "pendiente_de_aceptar",
                table: "membresia",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "invitacion",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    organizacion_id = table.Column<Guid>(type: "uuid", nullable: false),
                    usuario_id = table.Column<Guid>(type: "uuid", nullable: false),
                    membresia_id = table.Column<Guid>(type: "uuid", nullable: false),
                    hash_token = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    estado = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    creada_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    creada_por = table.Column<Guid>(type: "uuid", nullable: true),
                    expira_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    aceptada_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_invitacion", x => x.id);
                    table.ForeignKey(
                        name: "fk_invitacion_membresia_membresia_id",
                        column: x => x.membresia_id,
                        principalTable: "membresia",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_invitacion_usuario_usuario_id",
                        column: x => x.usuario_id,
                        principalTable: "usuario",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_invitacion_hash_token",
                table: "invitacion",
                column: "hash_token",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_invitacion_membresia_id",
                table: "invitacion",
                column: "membresia_id");

            migrationBuilder.CreateIndex(
                name: "ix_invitacion_organizacion_id_estado",
                table: "invitacion",
                columns: new[] { "organizacion_id", "estado" });

            migrationBuilder.CreateIndex(
                name: "ix_invitacion_usuario_id",
                table: "invitacion",
                column: "usuario_id");

            // FK entre módulos (ADR-001): se crea a mano.
            migrationBuilder.AddForeignKey(
                name: "fk_invitacion_organizacion_organizacion_id",
                table: "invitacion",
                column: "organizacion_id",
                principalTable: "organizacion",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(name: "fk_invitacion_organizacion_organizacion_id", table: "invitacion");

            migrationBuilder.DropTable(
                name: "invitacion");

            migrationBuilder.DropColumn(
                name: "pendiente_de_aceptar",
                table: "membresia");
        }
    }
}
