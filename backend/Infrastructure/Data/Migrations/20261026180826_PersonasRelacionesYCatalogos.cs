using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class PersonasRelacionesYCatalogos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_persona_id_organizacion_id_tipo_persona",
                schema: "public",
                table: "persona");

            migrationBuilder.AddColumn<string>(
                name: "patron_validacion",
                schema: "public",
                table: "tipo_documento",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "codigo_pais",
                schema: "public",
                table: "persona_telefono",
                type: "character varying(5)",
                maxLength: 5,
                nullable: false,
                defaultValue: "+57");

            migrationBuilder.AddUniqueConstraint(
                name: "AK_vehiculo_id_organizacion_id_vehiculo",
                schema: "public",
                table: "vehiculo",
                columns: new[] { "id_organizacion", "id_vehiculo" });

            migrationBuilder.CreateIndex(
                name: "IX_tipo_persona_id_organizacion_nombre",
                schema: "public",
                table: "tipo_persona",
                columns: new[] { "id_organizacion", "nombre" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_tipo_documento_codigo",
                schema: "public",
                table: "tipo_documento",
                column: "codigo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_persona_vehiculo_id_organizacion_id_vehiculo",
                schema: "public",
                table: "persona_vehiculo",
                columns: new[] { "id_organizacion", "id_vehiculo" });

            migrationBuilder.CreateIndex(
                name: "UX_persona_telefono_numero_activo",
                schema: "public",
                table: "persona_telefono",
                columns: new[] { "id_organizacion", "id_persona", "codigo_pais", "numero" },
                unique: true,
                filter: "activo");

            migrationBuilder.CreateIndex(
                name: "UX_persona_telefono_principal",
                schema: "public",
                table: "persona_telefono",
                columns: new[] { "id_organizacion", "id_persona" },
                unique: true,
                filter: "principal AND activo");

            migrationBuilder.CreateIndex(
                name: "IX_persona_id_archivo_foto",
                schema: "public",
                table: "persona",
                column: "id_archivo_foto");

            migrationBuilder.CreateIndex(
                name: "IX_persona_id_organizacion_id_tipo_persona",
                schema: "public",
                table: "persona",
                columns: new[] { "id_organizacion", "id_tipo_persona" });

            migrationBuilder.AddForeignKey(
                name: "FK_persona_archivo_id_archivo_foto",
                schema: "public",
                table: "persona",
                column: "id_archivo_foto",
                principalSchema: "public",
                principalTable: "archivo",
                principalColumn: "id_archivo",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_persona_organizacion_id_organizacion",
                schema: "public",
                table: "persona",
                column: "id_organizacion",
                principalSchema: "public",
                principalTable: "organizacion",
                principalColumn: "id_organizacion",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_persona_telefono_persona_id_organizacion_id_persona",
                schema: "public",
                table: "persona_telefono",
                columns: new[] { "id_organizacion", "id_persona" },
                principalSchema: "public",
                principalTable: "persona",
                principalColumns: new[] { "id_organizacion", "id_persona" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_persona_vehiculo_persona_id_organizacion_id_persona",
                schema: "public",
                table: "persona_vehiculo",
                columns: new[] { "id_organizacion", "id_persona" },
                principalSchema: "public",
                principalTable: "persona",
                principalColumns: new[] { "id_organizacion", "id_persona" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_persona_vehiculo_vehiculo_id_organizacion_id_vehiculo",
                schema: "public",
                table: "persona_vehiculo",
                columns: new[] { "id_organizacion", "id_vehiculo" },
                principalSchema: "public",
                principalTable: "vehiculo",
                principalColumns: new[] { "id_organizacion", "id_vehiculo" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_tipo_persona_organizacion_id_organizacion",
                schema: "public",
                table: "tipo_persona",
                column: "id_organizacion",
                principalSchema: "public",
                principalTable: "organizacion",
                principalColumn: "id_organizacion",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_vehiculo_organizacion_id_organizacion",
                schema: "public",
                table: "vehiculo",
                column: "id_organizacion",
                principalSchema: "public",
                principalTable: "organizacion",
                principalColumn: "id_organizacion",
                onDelete: ReferentialAction.Restrict);
            // Catálogo global de tipos de documento (Colombia). No modifica registros existentes.
            // Patrones sobre el número normalizado (recortado y en mayúsculas); provisionales hasta D-09.
            migrationBuilder.Sql("""
                INSERT INTO public.tipo_documento (codigo, nombre, pais_emisor, patron_validacion, activo) VALUES
                  ('CC',  'Cédula de ciudadanía',             'Colombia', '^[0-9]{3,10}$',            true),
                  ('TI',  'Tarjeta de identidad',             'Colombia', '^[0-9]{6,11}$',            true),
                  ('CE',  'Cédula de extranjería',            'Colombia', '^[A-Z0-9]{3,12}$',         true),
                  ('PA',  'Pasaporte',                        NULL,       '^[A-Z0-9]{5,20}$',         true),
                  ('PPT', 'Permiso por protección temporal',  'Colombia', '^[0-9]{5,15}$',            true),
                  ('NIT', 'NIT',                              'Colombia', '^[0-9]{6,10}(-?[0-9])?$',  true)
                ON CONFLICT (codigo) DO NOTHING;
                """);

            // Tipos de persona iniciales para cada organización existente (solo los que falten).
            migrationBuilder.Sql("""
                INSERT INTO public.tipo_persona (id_organizacion, nombre, activo)
                SELECT o.id_organizacion, t.nombre, true
                FROM public.organizacion o
                CROSS JOIN (VALUES ('Empleado'), ('Visitante'), ('Contratista'), ('Proveedor'), ('Residente'), ('Cliente')) AS t(nombre)
                WHERE NOT EXISTS (SELECT 1 FROM public.tipo_persona x WHERE x.id_organizacion = o.id_organizacion AND upper(x.nombre) = upper(t.nombre));
                """);

            // Pantalla del módulo de Personas.
            migrationBuilder.Sql("UPDATE public.app_modulos SET ruta = '/personas' WHERE codigo = 'personas' AND ruta IS NULL;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("UPDATE public.app_modulos SET ruta = NULL WHERE codigo = 'personas' AND ruta = '/personas';");
            migrationBuilder.DropForeignKey(
                name: "FK_persona_archivo_id_archivo_foto",
                schema: "public",
                table: "persona");

            migrationBuilder.DropForeignKey(
                name: "FK_persona_organizacion_id_organizacion",
                schema: "public",
                table: "persona");

            migrationBuilder.DropForeignKey(
                name: "FK_persona_telefono_persona_id_organizacion_id_persona",
                schema: "public",
                table: "persona_telefono");

            migrationBuilder.DropForeignKey(
                name: "FK_persona_vehiculo_persona_id_organizacion_id_persona",
                schema: "public",
                table: "persona_vehiculo");

            migrationBuilder.DropForeignKey(
                name: "FK_persona_vehiculo_vehiculo_id_organizacion_id_vehiculo",
                schema: "public",
                table: "persona_vehiculo");

            migrationBuilder.DropForeignKey(
                name: "FK_tipo_persona_organizacion_id_organizacion",
                schema: "public",
                table: "tipo_persona");

            migrationBuilder.DropForeignKey(
                name: "FK_vehiculo_organizacion_id_organizacion",
                schema: "public",
                table: "vehiculo");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_vehiculo_id_organizacion_id_vehiculo",
                schema: "public",
                table: "vehiculo");

            migrationBuilder.DropIndex(
                name: "IX_tipo_persona_id_organizacion_nombre",
                schema: "public",
                table: "tipo_persona");

            migrationBuilder.DropIndex(
                name: "IX_tipo_documento_codigo",
                schema: "public",
                table: "tipo_documento");

            migrationBuilder.DropIndex(
                name: "IX_persona_vehiculo_id_organizacion_id_vehiculo",
                schema: "public",
                table: "persona_vehiculo");

            migrationBuilder.DropIndex(
                name: "UX_persona_telefono_numero_activo",
                schema: "public",
                table: "persona_telefono");

            migrationBuilder.DropIndex(
                name: "UX_persona_telefono_principal",
                schema: "public",
                table: "persona_telefono");

            migrationBuilder.DropIndex(
                name: "IX_persona_id_archivo_foto",
                schema: "public",
                table: "persona");

            migrationBuilder.DropIndex(
                name: "IX_persona_id_organizacion_id_tipo_persona",
                schema: "public",
                table: "persona");

            migrationBuilder.DropColumn(
                name: "patron_validacion",
                schema: "public",
                table: "tipo_documento");

            migrationBuilder.DropColumn(
                name: "codigo_pais",
                schema: "public",
                table: "persona_telefono");

            migrationBuilder.CreateIndex(
                name: "IX_persona_id_organizacion_id_tipo_persona",
                schema: "public",
                table: "persona",
                columns: new[] { "id_organizacion", "id_tipo_persona" },
                unique: true);
        }
    }
}
