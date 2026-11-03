using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Data.Migrations
{
    /// <summary>
    /// ADR-028: la zona empresarial se resuelve desde la sede (se retira el vínculo zona-organización) y se retiran las zonas
    /// de acceso (los puntos quedan directamente en su sede). Orden: columnas nuevas, bitácora, traslado de datos y, al final,
    /// retiro de llaves, tablas y columnas. Lo que no se traslada sin ambigüedad queda en public.migracion_zonas_adr028.
    /// </summary>
    public partial class ZonasEmpresarialesPorSedeYRetiroZonasAcceso : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "es_comun_zona_empresarial",
                schema: "public",
                table: "punto_acceso",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "zona_historica",
                schema: "public",
                table: "movimiento_acceso",
                type: "character varying(150)",
                maxLength: 150,
                nullable: true);

            // Bitácora fuera del modelo EF: conserva lo retirado y lo que no pudo trasladarse sin ambigüedad.
            // Pendientes: SELECT * FROM public.migracion_zonas_adr028 WHERE resultado = 'Pendiente' AND resuelto_en IS NULL;
            migrationBuilder.Sql("""
                CREATE TABLE public.migracion_zonas_adr028 (
                    id_registro bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
                    origen varchar(40) NOT NULL,
                    resultado varchar(20) NOT NULL CHECK (resultado IN ('Migrado', 'Pendiente', 'Retirado', 'Historico')),
                    id_organizacion integer NULL,
                    id_zona_empresarial integer NULL,
                    es_administradora boolean NULL,
                    id_sede integer NULL,
                    id_zona_acceso integer NULL,
                    nombre_zona_acceso varchar(150) NULL,
                    id_punto_acceso integer NULL,
                    id_autorizacion integer NULL,
                    id_restriccion integer NULL,
                    detalle varchar(500) NOT NULL,
                    registrado_en timestamptz NOT NULL DEFAULT now(),
                    resuelto_en timestamptz NULL,
                    observacion_resolucion varchar(500) NULL
                );
                CREATE INDEX ix_migracion_zonas_adr028_pendiente ON public.migracion_zonas_adr028 (resultado, id_organizacion) WHERE resuelto_en IS NULL;
                """);

            // --- Zona empresarial: del vínculo con la organización a la sede ---
            // Solo se traslada si es inequívoco: la organización tiene exactamente una sede, sin zona, y un único vínculo activo.
            // Con varias sedes (o ninguna) no se asigna la zona: queda Pendiente en la bitácora.
            migrationBuilder.Sql("""
                UPDATE public.sede s
                SET id_zona_empresarial = l.id_zona_empresarial
                FROM public.zona_empresarial_organizacion l
                WHERE l.activa
                  AND s.id_organizacion = l.id_organizacion
                  AND s.id_zona_empresarial IS NULL
                  AND (SELECT count(*) FROM public.sede x WHERE x.id_organizacion = l.id_organizacion) = 1
                  AND (SELECT count(*) FROM public.zona_empresarial_organizacion k WHERE k.id_organizacion = l.id_organizacion AND k.activa) = 1;

                INSERT INTO public.migracion_zonas_adr028 (origen, resultado, id_organizacion, id_zona_empresarial, es_administradora, id_sede, detalle)
                SELECT 'zona_empresarial_organizacion',
                       CASE WHEN NOT l.activa THEN 'Historico'
                            WHEN EXISTS (SELECT 1 FROM public.sede s WHERE s.id_organizacion = l.id_organizacion AND s.id_zona_empresarial = l.id_zona_empresarial) THEN 'Migrado'
                            ELSE 'Pendiente' END,
                       l.id_organizacion, l.id_zona_empresarial, l.es_administradora,
                       (SELECT min(s.id_sede) FROM public.sede s WHERE s.id_organizacion = l.id_organizacion AND s.id_zona_empresarial = l.id_zona_empresarial),
                       CASE WHEN NOT l.activa THEN 'Vínculo inactivo: solo se conserva como historia.'
                            WHEN EXISTS (SELECT 1 FROM public.sede s WHERE s.id_organizacion = l.id_organizacion AND s.id_zona_empresarial = l.id_zona_empresarial) THEN 'La zona quedó resuelta desde la sede de la organización.'
                            WHEN NOT EXISTS (SELECT 1 FROM public.sede s WHERE s.id_organizacion = l.id_organizacion) THEN 'La organización no tiene sedes: asigne la zona al crear la sede correspondiente.'
                            WHEN (SELECT count(*) FROM public.sede s WHERE s.id_organizacion = l.id_organizacion) > 1 THEN 'La organización tiene varias sedes: indique cuáles pertenecen a la zona.'
                            WHEN (SELECT count(*) FROM public.zona_empresarial_organizacion k WHERE k.id_organizacion = l.id_organizacion AND k.activa) > 1 THEN 'La organización estaba vinculada a varias zonas y tiene una sola sede: elija la zona de la sede.'
                            ELSE 'La única sede de la organización ya pertenece a otra zona: revise la asignación.' END
                FROM public.zona_empresarial_organizacion l;
                """);

            // --- Zonas de acceso: historia legible antes de retirarlas ---
            migrationBuilder.Sql("""
                UPDATE public.movimiento_acceso m SET zona_historica = z.nombre
                FROM public.zona z
                WHERE z.id_organizacion = m.id_organizacion AND z.id_zona = m.id_zona;

                INSERT INTO public.migracion_zonas_adr028 (origen, resultado, id_organizacion, id_sede, id_zona_acceso, nombre_zona_acceso, detalle)
                SELECT 'zona', 'Retirado', z.id_organizacion, z.id_sede, z.id_zona, z.nombre,
                       CASE WHEN z.activo THEN 'Zona de acceso retirada (estaba activa).' ELSE 'Zona de acceso retirada (estaba inactiva).' END
                FROM public.zona z;

                INSERT INTO public.migracion_zonas_adr028 (origen, resultado, id_organizacion, id_sede, id_zona_acceso, nombre_zona_acceso, id_punto_acceso, detalle)
                SELECT 'punto_acceso', 'Migrado', p.id_organizacion, p.id_sede, z.id_zona, z.nombre, p.id_punto_acceso,
                       'El punto de acceso se conserva en su sede; se retira su zona.'
                FROM public.punto_acceso p
                JOIN public.zona z ON z.id_organizacion = p.id_organizacion AND z.id_zona = p.id_zona;

                -- ADR-028: la restricción por zona se elimina; la autorización vale en todos los puntos de su sede.
                INSERT INTO public.migracion_zonas_adr028 (origen, resultado, id_organizacion, id_sede, id_zona_acceso, nombre_zona_acceso, id_autorizacion, detalle)
                SELECT 'autorizacion_zona', 'Retirado', az.id_organizacion, z.id_sede, z.id_zona, z.nombre, az.id_autorizacion,
                       'Restricción por zona retirada: la autorización (' || a.estado || ') queda válida en todos los puntos de su sede.'
                FROM public.autorizacion_zona az
                JOIN public.zona z ON z.id_organizacion = az.id_organizacion AND z.id_zona = az.id_zona
                JOIN public.autorizacion a ON a.id_organizacion = az.id_organizacion AND a.id_autorizacion = az.id_autorizacion;

                INSERT INTO public.migracion_zonas_adr028 (origen, resultado, id_organizacion, detalle)
                SELECT 'parametro_organizacion', 'Retirado', p.id_organizacion,
                       'Política autorizaciones.exige_zonas = ' || p.valor || ' retirada con las zonas de acceso.'
                FROM public.parametro_organizacion p WHERE p.clave = 'autorizaciones.exige_zonas';
                DELETE FROM public.parametro_organizacion WHERE clave = 'autorizaciones.exige_zonas';

                -- Una restricción de persona por zona pasa a la sede de la zona: más amplia, nunca más permisiva.
                INSERT INTO public.migracion_zonas_adr028 (origen, resultado, id_organizacion, id_sede, id_zona_acceso, nombre_zona_acceso, id_restriccion, detalle)
                SELECT 'restriccion_acceso',
                       CASE WHEN z.id_zona IS NULL AND r.id_sede IS NULL THEN 'Pendiente' ELSE 'Migrado' END,
                       r.id_organizacion, coalesce(r.id_sede, z.id_sede), r.id_zona, z.nombre, r.id_restriccion,
                       CASE WHEN z.id_zona IS NULL AND r.id_sede IS NULL THEN 'La zona de la restricción no existe y la restricción no tiene sede: defina su alcance.'
                            WHEN r.id_sede IS NULL THEN 'Restricción por zona trasladada a la sede de la zona.'
                            ELSE 'La restricción conserva su sede; se retira la zona.' END
                FROM public.restriccion_acceso r
                LEFT JOIN public.zona z ON z.id_organizacion = r.id_organizacion AND z.id_zona = r.id_zona
                WHERE r.id_zona IS NOT NULL;
                UPDATE public.restriccion_acceso r SET id_sede = z.id_sede
                FROM public.zona z
                WHERE r.id_sede IS NULL AND z.id_organizacion = r.id_organizacion AND z.id_zona = r.id_zona;
                """);

            // --- Retiro: solo después de trasladar los datos y la historia ---
            migrationBuilder.DropForeignKey(
                name: "FK_movimiento_acceso_zona_id_organizacion_id_zona",
                schema: "public",
                table: "movimiento_acceso");

            migrationBuilder.DropForeignKey(
                name: "FK_punto_acceso_zona_id_organizacion_id_sede_id_zona",
                schema: "public",
                table: "punto_acceso");

            migrationBuilder.DropTable(
                name: "autorizacion_zona",
                schema: "public");

            migrationBuilder.DropTable(
                name: "zona_empresarial_organizacion",
                schema: "public");

            migrationBuilder.DropTable(
                name: "zona",
                schema: "public");

            migrationBuilder.DropIndex(
                name: "IX_punto_acceso_id_organizacion_id_sede_id_zona",
                schema: "public",
                table: "punto_acceso");

            migrationBuilder.DropIndex(
                name: "IX_punto_acceso_id_organizacion_id_zona",
                schema: "public",
                table: "punto_acceso");

            migrationBuilder.DropIndex(
                name: "IX_movimiento_acceso_id_organizacion_id_zona",
                schema: "public",
                table: "movimiento_acceso");

            migrationBuilder.DropColumn(
                name: "id_zona",
                schema: "public",
                table: "restriccion_acceso");

            migrationBuilder.DropColumn(
                name: "id_zona",
                schema: "public",
                table: "punto_acceso");

            migrationBuilder.DropColumn(
                name: "id_zona",
                schema: "public",
                table: "movimiento_acceso");

            migrationBuilder.AddForeignKey(
                name: "FK_punto_acceso_sede_id_organizacion_id_sede",
                schema: "public",
                table: "punto_acceso",
                columns: new[] { "id_organizacion", "id_sede" },
                principalSchema: "public",
                principalTable: "sede",
                principalColumns: new[] { "id_organizacion", "id_sede" },
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Las zonas de acceso, sus restricciones y los vínculos zona-organización se retiraron con traslado de datos;
            // recrear las tablas vacías dejaría puntos y movimientos sin zona válida. La historia queda en
            // public.migracion_zonas_adr028. Para revertir, restaure el respaldo previo a esta migración.
            throw new NotSupportedException("ZonasEmpresarialesPorSedeYRetiroZonasAcceso no es reversible: restaure el respaldo previo.");
        }
    }
}
