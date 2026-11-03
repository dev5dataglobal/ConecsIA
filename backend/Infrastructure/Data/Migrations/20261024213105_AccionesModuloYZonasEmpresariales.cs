using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Data.Migrations
{
    /// <summary>
    /// 1. Acciones por módulo (consultar / añadir / editar) en modulos_permisos. Las filas existentes quedan con
    ///    las tres acciones para conservar el acceso que ya tenían.
    /// 2. Divide «administracion» en usuarios, perfiles, organizaciones y zonas_empresariales; los roles que tenían
    ///    «administracion» reciben los cuatro con las tres acciones. «administracion» se inactiva (no se borra).
    /// 3. Reglas de zona empresarial: una organización superior activa por zona y una zona activa por empresa.
    ///    Falla (sin cambios) si los datos actuales ya las incumplen.
    /// </summary>
    public partial class AccionesModuloYZonasEmpresariales : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // El valor por defecto true en BD solo rellena filas existentes y altas manuales; EF siempre envía las tres columnas.
            foreach (var columna in new[] { "puede_consultar", "puede_anadir", "puede_editar" })
            {
                migrationBuilder.AddColumn<bool>(
                    name: columna,
                    schema: "public",
                    table: "modulos_permisos",
                    type: "boolean",
                    nullable: false,
                    defaultValue: true);
            }

            migrationBuilder.Sql("""
                INSERT INTO public.app_modulos (codigo, nombre, descripcion, ruta, icono, orden, activo, creado_en) VALUES
                  ('usuarios', 'Usuarios', 'Cuentas de acceso de la organización y sus perfiles.', '/administracion/usuarios', 'personas', 120, true, now()),
                  ('perfiles', 'Perfiles', 'Perfiles de acceso y sus permisos por módulo.', '/administracion/perfiles', 'autorizaciones', 121, true, now()),
                  ('organizaciones', 'Organizaciones', 'Datos de la organización; el administrador de plataforma gestiona todas.', '/administracion/organizaciones', 'sede', 122, true, now()),
                  ('zonas_empresariales', 'Zonas empresariales', 'Organizaciones superiores que agrupan empresas de una zona empresarial.', '/administracion/zonas-empresariales', 'administracion', 123, true, now())
                ON CONFLICT (codigo) DO NOTHING;

                INSERT INTO public.modulos_permisos (id_organizacion, id_rol, id_app_modulo, asignado_en, puede_consultar, puede_anadir, puede_editar)
                SELECT mp.id_organizacion, mp.id_rol, nuevo.id_app_modulo, now(), mp.puede_consultar, mp.puede_anadir, mp.puede_editar
                FROM public.modulos_permisos mp
                JOIN public.app_modulos viejo ON viejo.id_app_modulo = mp.id_app_modulo AND viejo.codigo = 'administracion'
                CROSS JOIN public.app_modulos nuevo
                WHERE nuevo.codigo IN ('usuarios', 'perfiles', 'organizaciones', 'zonas_empresariales')
                ON CONFLICT (id_rol, id_app_modulo) DO NOTHING;

                UPDATE public.app_modulos SET activo = false WHERE codigo = 'administracion';
                """);

            migrationBuilder.CreateIndex(
                name: "UX_zona_empresarial_organizacion_administradora",
                schema: "public",
                table: "zona_empresarial_organizacion",
                column: "id_zona_empresarial",
                unique: true,
                filter: "es_administradora AND activa");

            migrationBuilder.CreateIndex(
                name: "UX_zona_empresarial_organizacion_empresa",
                schema: "public",
                table: "zona_empresarial_organizacion",
                column: "id_organizacion",
                unique: true,
                filter: "NOT es_administradora AND activa");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "UX_zona_empresarial_organizacion_administradora",
                schema: "public",
                table: "zona_empresarial_organizacion");

            migrationBuilder.DropIndex(
                name: "UX_zona_empresarial_organizacion_empresa",
                schema: "public",
                table: "zona_empresarial_organizacion");

            // Se reactiva «administracion»; las asignaciones de los módulos nuevos se conservan (sin efecto al bajar).
            migrationBuilder.Sql("UPDATE public.app_modulos SET activo = true WHERE codigo = 'administracion';");

            foreach (var columna in new[] { "puede_consultar", "puede_anadir", "puede_editar" })
            {
                migrationBuilder.DropColumn(name: columna, schema: "public", table: "modulos_permisos");
            }
        }
    }
}
