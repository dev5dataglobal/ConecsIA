using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Infrastructure;
using ConecsIA.Infrastructure.Data;

#nullable disable

namespace Infrastructure.Data.Migrations;

[DbContext(typeof(ConecsiaDbContext))]
[Migration("20261022120000_AddBusinessZonesAndAuthorizationScopes")]
public partial class AddBusinessZonesAndAuthorizationScopes : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(name: "zona_empresarial", schema: "public", columns: table => new
        {
            id_zona_empresarial = table.Column<int>(type: "integer", nullable: false).Annotation("Npgsql:ValueGenerationStrategy", Npgsql.EntityFrameworkCore.PostgreSQL.Metadata.NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
            nombre = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
            descripcion = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
            activa = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
            creada_en = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
        }, constraints: table => table.PrimaryKey("PK_zona_empresarial", x => x.id_zona_empresarial));
        migrationBuilder.CreateIndex("IX_zona_empresarial_nombre", "zona_empresarial", "nombre", "public", unique: true);
        migrationBuilder.CreateTable(name: "zona_empresarial_organizacion", schema: "public", columns: table => new
        {
            id_organizacion = table.Column<int>(type: "integer", nullable: false),
            id_zona_empresarial = table.Column<int>(type: "integer", nullable: false),
            es_administradora = table.Column<bool>(type: "boolean", nullable: false),
            activa = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true)
        }, constraints: table => { table.PrimaryKey("PK_zona_empresarial_organizacion", x => new { x.id_organizacion, x.id_zona_empresarial }); table.ForeignKey("FK_zona_empresarial_organizacion_zona_empresarial", x => x.id_zona_empresarial, "zona_empresarial", "id_zona_empresarial", "public", onDelete: ReferentialAction.Restrict); table.ForeignKey("FK_zona_empresarial_organizacion_organizacion", x => x.id_organizacion, "organizacion", "id_organizacion", "public", onDelete: ReferentialAction.Restrict); });
        migrationBuilder.AddColumn<int>(name: "id_zona_empresarial", table: "sede", schema: "public", type: "integer", nullable: true);
        migrationBuilder.AddColumn<bool>(name: "es_comun_zona_empresarial", table: "area", schema: "public", type: "boolean", nullable: false, defaultValue: false);
        migrationBuilder.AddColumn<string>("tipo_emisor", "autorizacion", schema: "public", type: "character varying(2000)", nullable: false, defaultValue: "Empresa");
        migrationBuilder.AddColumn<int>(name: "id_zona_empresarial_emisora", table: "autorizacion", schema: "public", type: "integer", nullable: true);
        migrationBuilder.AddColumn<string>("alcance", "autorizacion", schema: "public", type: "character varying(2000)", nullable: false, defaultValue: "Privado");
        migrationBuilder.CreateIndex("IX_sede_id_zona_empresarial", "sede", "id_zona_empresarial", "public");
        migrationBuilder.CreateIndex("IX_zona_empresarial_organizacion_zona", "zona_empresarial_organizacion", "id_zona_empresarial", "public");
        migrationBuilder.CreateIndex("IX_autorizacion_id_zona_empresarial_emisora", "autorizacion", "id_zona_empresarial_emisora", "public");
        migrationBuilder.AddForeignKey(name: "FK_sede_zona_empresarial", table: "sede", column: "id_zona_empresarial", principalTable: "zona_empresarial", schema: "public", principalSchema: "public", principalColumn: "id_zona_empresarial", onDelete: ReferentialAction.Restrict);
        migrationBuilder.AddForeignKey(name: "FK_autorizacion_zona_empresarial_emisora", table: "autorizacion", column: "id_zona_empresarial_emisora", principalTable: "zona_empresarial", schema: "public", principalSchema: "public", principalColumn: "id_zona_empresarial", onDelete: ReferentialAction.Restrict);
        migrationBuilder.AddCheckConstraint("ck_autorizacion_emisor_alcance", "autorizacion", "(tipo_emisor = 'Empresa' AND id_zona_empresarial_emisora IS NULL AND alcance = 'Privado') OR (tipo_emisor = 'ZonaEmpresarial' AND id_zona_empresarial_emisora IS NOT NULL AND alcance = 'Comun')", "public");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey("FK_autorizacion_zona_empresarial_emisora", "autorizacion", "public"); migrationBuilder.DropForeignKey("FK_sede_zona_empresarial", "sede", "public"); migrationBuilder.DropColumn("alcance", "autorizacion", "public"); migrationBuilder.DropColumn("id_zona_empresarial_emisora", "autorizacion", "public"); migrationBuilder.DropColumn("tipo_emisor", "autorizacion", "public"); migrationBuilder.DropColumn("es_comun_zona_empresarial", "area", "public"); migrationBuilder.DropColumn("id_zona_empresarial", "sede", "public"); migrationBuilder.DropTable("zona_empresarial_organizacion", "public"); migrationBuilder.DropTable("zona_empresarial", "public");
    }
}
