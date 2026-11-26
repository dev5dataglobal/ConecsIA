using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Data.Migrations
{
    /// <summary>
    /// ADR-029, cierre: retirados los endpoints de Visitas y Autorizaciones, todo registro nuevo pasa por /api/accesos.
    /// Las restricciones se crean NOT VALID: obligan a las filas nuevas (y a las que se modifiquen) sin reescribir el
    /// histórico migrado. Antes se categorizan las autorizaciones que el código anterior creó tras RegistroAccesoUnificado.
    /// </summary>
    public partial class RestriccionesRegistroAccesoUnificado : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                UPDATE public.autorizacion a SET id_categoria_acceso = c.id_categoria_acceso
                FROM public.categoria_acceso c
                WHERE a.id_categoria_acceso IS NULL AND c.id_organizacion = a.id_organizacion
                  AND c.codigo = CASE WHEN a.modalidad IN ('Recurrente','Permanente') THEN 'RECURRENTE' ELSE 'VISITANTE' END;

                -- Todo registro de acceso nuevo tiene categoría.
                ALTER TABLE public.autorizacion ADD CONSTRAINT ck_autorizacion_categoria CHECK (id_categoria_acceso IS NOT NULL) NOT VALID;
                -- Rechazar, revocar y cancelar exigen motivo.
                ALTER TABLE public.autorizacion ADD CONSTRAINT ck_autorizacion_motivo_decision
                    CHECK (estado NOT IN ('Rechazada','Revocada','Cancelada') OR motivo_decision IS NOT NULL) NOT VALID;
                -- Toda visita (episodio) nueva cuelga de un registro de acceso (D-49).
                ALTER TABLE public.visita ADD CONSTRAINT ck_visita_autorizacion CHECK (id_autorizacion IS NOT NULL) NOT VALID;

                -- Visitas y Autorizaciones quedan solo como permisos: sin ruta propia en el menú.
                UPDATE public.app_modulos SET ruta = NULL WHERE codigo IN ('visitas', 'autorizaciones');
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                ALTER TABLE public.visita DROP CONSTRAINT IF EXISTS ck_visita_autorizacion;
                ALTER TABLE public.autorizacion DROP CONSTRAINT IF EXISTS ck_autorizacion_motivo_decision;
                ALTER TABLE public.autorizacion DROP CONSTRAINT IF EXISTS ck_autorizacion_categoria;
                UPDATE public.app_modulos SET ruta = '/visitas' WHERE codigo = 'visitas';
                UPDATE public.app_modulos SET ruta = '/autorizaciones' WHERE codigo = 'autorizaciones';
                """);
        }
    }
}
