using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Infrastructure.Data.Migrations
{
    /// <summary>
    /// ADR-029: registro de acceso unificado. Esquema aditivo (categorías, horario base, zona horaria de la organización,
    /// origen del registro) y traslado de datos sin pérdida: toda visita queda vinculada a un registro de acceso. No se borra
    /// ninguna tabla ni columna. Bitácora: public.migracion_accesos_adr029.
    /// </summary>
    public partial class RegistroAccesoUnificado : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_autorizacion_horario_id_autorizacion_dia_semana",
                schema: "public",
                table: "autorizacion_horario");

            migrationBuilder.DropIndex(
                name: "IX_autorizacion_id_organizacion_id_persona",
                schema: "public",
                table: "autorizacion");

            migrationBuilder.AddColumn<string>(
                name: "zona_horaria",
                schema: "public",
                table: "organizacion",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "America/Bogota");

            migrationBuilder.AddColumn<int>(
                name: "id_categoria_acceso",
                schema: "public",
                table: "autorizacion",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "id_visita_origen",
                schema: "public",
                table: "autorizacion",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "origen",
                schema: "public",
                table: "autorizacion",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Manual");

            migrationBuilder.AddColumn<bool>(
                name: "usa_horario_base",
                schema: "public",
                table: "autorizacion",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "agente_usuario",
                schema: "public",
                table: "auditoria",
                type: "character varying(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "categoria_acceso",
                schema: "public",
                columns: table => new
                {
                    id_categoria_acceso = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    codigo = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    nombre = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    descripcion = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    flujo_aprobacion = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    modalidades_permitidas = table.Column<string[]>(type: "text[]", nullable: false),
                    exige_anfitrion = table.Column<bool>(type: "boolean", nullable: false),
                    exige_motivo = table.Column<bool>(type: "boolean", nullable: false),
                    permite_vehiculos = table.Column<bool>(type: "boolean", nullable: false),
                    permite_dispositivos = table.Column<bool>(type: "boolean", nullable: false),
                    orden = table.Column<int>(type: "integer", nullable: false),
                    activo = table.Column<bool>(type: "boolean", nullable: false),
                    creado_en = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    id_organizacion = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_categoria_acceso", x => x.id_categoria_acceso);
                    table.UniqueConstraint("AK_categoria_acceso_id_organizacion_id_categoria_acceso", x => new { x.id_organizacion, x.id_categoria_acceso });
                    table.CheckConstraint("ck_categoria_acceso_flujo", "flujo_aprobacion IN ('Automatico','Anfitrion','Seguridad')");
                    table.CheckConstraint("ck_categoria_acceso_modalidades", "cardinality(modalidades_permitidas) > 0 AND modalidades_permitidas <@ ARRAY['Unica','Diaria','Temporal','Recurrente','Permanente']::text[]");
                    table.ForeignKey(
                        name: "FK_categoria_acceso_organizacion_id_organizacion",
                        column: x => x.id_organizacion,
                        principalSchema: "public",
                        principalTable: "organizacion",
                        principalColumn: "id_organizacion",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "horario_base",
                schema: "public",
                columns: table => new
                {
                    id_horario_base = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    id_sede = table.Column<int>(type: "integer", nullable: true),
                    dia_semana = table.Column<int>(type: "integer", nullable: false),
                    hora_inicio = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    hora_fin = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    id_organizacion = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_horario_base", x => x.id_horario_base);
                    table.CheckConstraint("ck_horario_base_dia", "dia_semana BETWEEN 1 AND 7");
                    table.CheckConstraint("ck_horario_base_horas", "hora_fin > hora_inicio");
                    table.ForeignKey(
                        name: "FK_horario_base_organizacion_id_organizacion",
                        column: x => x.id_organizacion,
                        principalSchema: "public",
                        principalTable: "organizacion",
                        principalColumn: "id_organizacion",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_horario_base_sede_id_organizacion_id_sede",
                        columns: x => new { x.id_organizacion, x.id_sede },
                        principalSchema: "public",
                        principalTable: "sede",
                        principalColumns: new[] { "id_organizacion", "id_sede" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "plantilla_categoria_acceso",
                schema: "public",
                columns: table => new
                {
                    id_plantilla_categoria_acceso = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    id_tipo_organizacion = table.Column<int>(type: "integer", nullable: false),
                    codigo = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    nombre = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    descripcion = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    flujo_aprobacion = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    modalidades_permitidas = table.Column<string[]>(type: "text[]", nullable: false),
                    exige_anfitrion = table.Column<bool>(type: "boolean", nullable: false),
                    permite_vehiculos = table.Column<bool>(type: "boolean", nullable: false),
                    permite_dispositivos = table.Column<bool>(type: "boolean", nullable: false),
                    orden = table.Column<int>(type: "integer", nullable: false),
                    activo = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_plantilla_categoria_acceso", x => x.id_plantilla_categoria_acceso);
                    table.CheckConstraint("ck_plantilla_categoria_acceso_flujo", "flujo_aprobacion IN ('Automatico','Anfitrion','Seguridad')");
                    table.ForeignKey(
                        name: "FK_plantilla_categoria_acceso_tipo_organizacion_id_tipo_organi~",
                        column: x => x.id_tipo_organizacion,
                        principalSchema: "public",
                        principalTable: "tipo_organizacion",
                        principalColumn: "id_tipo_organizacion",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_movimiento_acceso_id_organizacion_id_autorizacion",
                schema: "public",
                table: "movimiento_acceso",
                columns: new[] { "id_organizacion", "id_autorizacion" });

            migrationBuilder.CreateIndex(
                name: "IX_autorizacion_horario_id_autorizacion_dia_semana",
                schema: "public",
                table: "autorizacion_horario",
                columns: new[] { "id_autorizacion", "dia_semana" });

            migrationBuilder.CreateIndex(
                name: "IX_autorizacion_id_organizacion_id_categoria_acceso",
                schema: "public",
                table: "autorizacion",
                columns: new[] { "id_organizacion", "id_categoria_acceso" });

            migrationBuilder.CreateIndex(
                name: "IX_autorizacion_id_organizacion_id_persona_fecha_inicio",
                schema: "public",
                table: "autorizacion",
                columns: new[] { "id_organizacion", "id_persona", "fecha_inicio" });

            migrationBuilder.CreateIndex(
                name: "IX_autorizacion_id_organizacion_id_visita_origen",
                schema: "public",
                table: "autorizacion",
                columns: new[] { "id_organizacion", "id_visita_origen" });

            migrationBuilder.AddCheckConstraint(
                name: "ck_autorizacion_origen",
                schema: "public",
                table: "autorizacion",
                sql: "origen IN ('Manual','Politica','Migracion')");

            migrationBuilder.CreateIndex(
                name: "IX_auditoria_id_organizacion_entidad_id_registro",
                schema: "public",
                table: "auditoria",
                columns: new[] { "id_organizacion", "entidad", "id_registro" });

            migrationBuilder.CreateIndex(
                name: "IX_auditoria_id_organizacion_ocurrido_en",
                schema: "public",
                table: "auditoria",
                columns: new[] { "id_organizacion", "ocurrido_en" });

            migrationBuilder.CreateIndex(
                name: "IX_categoria_acceso_id_organizacion_codigo",
                schema: "public",
                table: "categoria_acceso",
                columns: new[] { "id_organizacion", "codigo" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_categoria_acceso_id_organizacion_id_categoria_acceso",
                schema: "public",
                table: "categoria_acceso",
                columns: new[] { "id_organizacion", "id_categoria_acceso" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_horario_base_id_organizacion_id_sede_dia_semana",
                schema: "public",
                table: "horario_base",
                columns: new[] { "id_organizacion", "id_sede", "dia_semana" });

            migrationBuilder.CreateIndex(
                name: "IX_plantilla_categoria_acceso_id_tipo_organizacion_codigo",
                schema: "public",
                table: "plantilla_categoria_acceso",
                columns: new[] { "id_tipo_organizacion", "codigo" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_autorizacion_categoria_acceso_id_organizacion_id_categoria_~",
                schema: "public",
                table: "autorizacion",
                columns: new[] { "id_organizacion", "id_categoria_acceso" },
                principalSchema: "public",
                principalTable: "categoria_acceso",
                principalColumns: new[] { "id_organizacion", "id_categoria_acceso" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_autorizacion_visita_id_organizacion_id_visita_origen",
                schema: "public",
                table: "autorizacion",
                columns: new[] { "id_organizacion", "id_visita_origen" },
                principalSchema: "public",
                principalTable: "visita",
                principalColumns: new[] { "id_organizacion", "id_visita" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_movimiento_acceso_autorizacion_id_organizacion_id_autorizac~",
                schema: "public",
                table: "movimiento_acceso",
                columns: new[] { "id_organizacion", "id_autorizacion" },
                principalSchema: "public",
                principalTable: "autorizacion",
                principalColumns: new[] { "id_organizacion", "id_autorizacion" },
                onDelete: ReferentialAction.Restrict);

            // Bitácora fuera del modelo EF. Revisión: SELECT * FROM public.migracion_accesos_adr029 ORDER BY id_registro;
            migrationBuilder.Sql("""
                CREATE TABLE public.migracion_accesos_adr029 (
                    id_registro bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
                    origen varchar(40) NOT NULL,
                    resultado varchar(20) NOT NULL CHECK (resultado IN ('Migrado', 'Historico', 'Pendiente')),
                    id_organizacion integer NULL,
                    id_visita integer NULL,
                    id_autorizacion integer NULL,
                    id_autorizacion_objeto integer NULL,
                    detalle varchar(500) NOT NULL,
                    registrado_en timestamptz NOT NULL DEFAULT now()
                );
                """);

            // --- Tipos de organización y plantillas de categorías (Propuesta, ADR-029) ---
            migrationBuilder.Sql("""
                INSERT INTO public.tipo_organizacion (nombre, descripcion, activo)
                SELECT t.nombre, NULL, true
                FROM (VALUES ('Empresa'), ('Conjunto residencial'), ('Club'), ('Universidad'), ('Coworking'), ('Parque industrial')) AS t(nombre)
                WHERE NOT EXISTS (SELECT 1 FROM public.tipo_organizacion x WHERE lower(x.nombre) = lower(t.nombre));

                INSERT INTO public.plantilla_categoria_acceso
                    (id_tipo_organizacion, codigo, nombre, descripcion, flujo_aprobacion, modalidades_permitidas, exige_anfitrion, permite_vehiculos, permite_dispositivos, orden, activo)
                SELECT t.id_tipo_organizacion, p.codigo, p.nombre, NULL, p.flujo, p.modalidades, p.anfitrion, true, true, p.orden, true
                FROM (VALUES
                    ('Empresa', 'VISITANTE', 'Visitante', 'Anfitrion', ARRAY['Unica','Diaria','Temporal'], true, 1),
                    ('Empresa', 'CONTRATISTA', 'Contratista', 'Seguridad', ARRAY['Temporal','Recurrente'], true, 2),
                    ('Empresa', 'PROVEEDOR', 'Proveedor', 'Seguridad', ARRAY['Unica','Recurrente'], false, 3),
                    ('Empresa', 'RECURRENTE', 'Acceso recurrente', 'Seguridad', ARRAY['Recurrente','Permanente'], true, 4),
                    ('Conjunto residencial', 'VISITANTE', 'Visitante', 'Anfitrion', ARRAY['Unica','Diaria','Temporal'], true, 1),
                    ('Conjunto residencial', 'DOMICILIARIO', 'Domiciliario', 'Automatico', ARRAY['Unica'], true, 2),
                    ('Conjunto residencial', 'TRABAJADOR', 'Trabajador', 'Seguridad', ARRAY['Recurrente','Permanente'], true, 3),
                    ('Club', 'VISITANTE', 'Invitado', 'Seguridad', ARRAY['Unica','Diaria'], true, 1),
                    ('Club', 'PROVEEDOR', 'Proveedor', 'Seguridad', ARRAY['Unica','Recurrente'], false, 2),
                    ('Club', 'RECURRENTE', 'Acceso recurrente', 'Seguridad', ARRAY['Recurrente','Permanente'], false, 3),
                    ('Universidad', 'VISITANTE', 'Visitante', 'Seguridad', ARRAY['Unica','Diaria','Temporal'], false, 1),
                    ('Universidad', 'ESTUDIANTE_EXTERNO', 'Estudiante externo', 'Seguridad', ARRAY['Temporal','Recurrente'], false, 2),
                    ('Universidad', 'CONTRATISTA', 'Contratista', 'Seguridad', ARRAY['Temporal','Recurrente'], true, 3),
                    ('Coworking', 'VISITANTE', 'Invitado', 'Automatico', ARRAY['Unica','Diaria'], true, 1),
                    ('Coworking', 'RECURRENTE', 'Miembro', 'Seguridad', ARRAY['Recurrente','Permanente'], false, 2),
                    ('Coworking', 'PROVEEDOR', 'Proveedor', 'Seguridad', ARRAY['Unica','Recurrente'], false, 3),
                    ('Parque industrial', 'CONTRATISTA', 'Contratista', 'Seguridad', ARRAY['Temporal','Recurrente'], true, 1),
                    ('Parque industrial', 'TRANSPORTADOR', 'Transportador', 'Seguridad', ARRAY['Unica','Recurrente'], false, 2),
                    ('Parque industrial', 'VISITANTE', 'Visitante', 'Anfitrion', ARRAY['Unica','Diaria','Temporal'], true, 3)
                ) AS p(tipo, codigo, nombre, flujo, modalidades, anfitrion, orden)
                JOIN public.tipo_organizacion t ON lower(t.nombre) = lower(p.tipo)
                ON CONFLICT (id_tipo_organizacion, codigo) DO NOTHING;
                """);

            // --- Categorías por organización: plantillas de su tipo + VISITANTE y RECURRENTE garantizadas para trasladar lo existente ---
            // La política visitas.permite_sin_autorizacion = true se traduce en flujo Automatico para VISITANTE.
            migrationBuilder.Sql("""
                INSERT INTO public.categoria_acceso
                    (id_organizacion, codigo, nombre, descripcion, flujo_aprobacion, modalidades_permitidas, exige_anfitrion, exige_motivo, permite_vehiculos, permite_dispositivos, orden, activo, creado_en)
                SELECT o.id_organizacion, p.codigo, p.nombre, p.descripcion, p.flujo_aprobacion, p.modalidades_permitidas, p.exige_anfitrion, true,
                       p.permite_vehiculos, p.permite_dispositivos, p.orden, p.activo, now()
                FROM public.organizacion o
                JOIN public.plantilla_categoria_acceso p ON p.id_tipo_organizacion = o.id_tipo_organizacion
                ON CONFLICT (id_organizacion, codigo) DO NOTHING;

                INSERT INTO public.categoria_acceso
                    (id_organizacion, codigo, nombre, descripcion, flujo_aprobacion, modalidades_permitidas, exige_anfitrion, exige_motivo, permite_vehiculos, permite_dispositivos, orden, activo, creado_en)
                SELECT o.id_organizacion, c.codigo, c.nombre, NULL, c.flujo, c.modalidades, true, true, true, true, c.orden, true, now()
                FROM public.organizacion o
                CROSS JOIN (VALUES ('VISITANTE', 'Visitante', 'Anfitrion', ARRAY['Unica','Diaria','Temporal'], 90),
                                   ('RECURRENTE', 'Acceso recurrente', 'Seguridad', ARRAY['Recurrente','Permanente'], 91)) AS c(codigo, nombre, flujo, modalidades, orden)
                ON CONFLICT (id_organizacion, codigo) DO NOTHING;

                UPDATE public.categoria_acceso c SET flujo_aprobacion = 'Automatico'
                FROM public.parametro_organizacion p
                WHERE c.codigo = 'VISITANTE' AND p.id_organizacion = c.id_organizacion
                  AND p.clave = 'visitas.permite_sin_autorizacion' AND lower(p.valor) = 'true';

                -- D-42: la clave nueva conserva el valor de la anterior (la anterior se mantiene hasta la Fase 3).
                INSERT INTO public.parametro_organizacion (id_organizacion, clave, valor, actualizado_en)
                SELECT id_organizacion, 'accesos.permite_permanente', valor, now()
                FROM public.parametro_organizacion WHERE clave = 'autorizaciones.permite_permanente'
                ON CONFLICT (id_organizacion, clave) DO NOTHING;

                -- Autorizaciones existentes: categoría según la modalidad.
                UPDATE public.autorizacion a SET id_categoria_acceso = c.id_categoria_acceso
                FROM public.categoria_acceso c
                WHERE c.id_organizacion = a.id_organizacion
                  AND c.codigo = CASE WHEN a.modalidad IN ('Recurrente','Permanente') THEN 'RECURRENTE' ELSE 'VISITANTE' END
                  AND a.id_categoria_acceso IS NULL;
                """);

            // --- Visitas sin autorización: un registro de acceso Único por visita (origen Migracion) ---
            // Fecha = fecha esperada o fecha local de creación en la sede; franja = la esperada si es válida.
            // Estado: Pendiente ⇒ Pendiente; Autorizada/Ingreso/Finalizada (política) ⇒ Aprobada sin decisor; Rechazada/Cancelada/Vencida se conservan.
            migrationBuilder.Sql("""
                INSERT INTO public.autorizacion
                    (id_organizacion, id_persona, id_sede, id_area, id_membresia_responsable, id_usuario_crea, tipo_emisor, alcance, modalidad,
                     fecha_inicio, fecha_fin, hora_inicio, hora_fin, cupo_ingresos, ingresos_consumidos, motivo, observacion, estado,
                     decidido_en, motivo_decision, version, creado_en, id_categoria_acceso, usa_horario_base, origen, id_visita_origen)
                SELECT v.id_organizacion, v.id_persona, v.id_sede, v.id_area, v.id_membresia_responsable, v.id_usuario_registra, 'Empresa', 'Privado', 'Unica',
                       f.fecha, f.fecha,
                       CASE WHEN v.hora_esperada_fin > v.hora_esperada_inicio THEN v.hora_esperada_inicio END,
                       CASE WHEN v.hora_esperada_fin > v.hora_esperada_inicio THEN v.hora_esperada_fin END,
                       1,
                       (SELECT count(*) FROM public.movimiento_acceso m WHERE m.id_organizacion = v.id_organizacion AND m.id_visita = v.id_visita
                           AND m.tipo_movimiento = 'Entrada' AND m.resultado = 'Confirmado'),
                       v.motivo,
                       'Migrado desde la visita #' || v.id_visita || COALESCE(' (política: ' || v.politica_aplicada || ')', ''),
                       CASE v.estado WHEN 'Pendiente' THEN 'Pendiente' WHEN 'Rechazada' THEN 'Rechazada' WHEN 'Cancelada' THEN 'Cancelada'
                                     WHEN 'Vencida' THEN 'Vencida' ELSE 'Aprobada' END,
                       CASE WHEN v.estado = 'Pendiente' THEN NULL ELSE COALESCE(v.actualizado_en, v.creado_en) END,
                       CASE WHEN v.estado IN ('Rechazada','Cancelada','Vencida') THEN COALESCE(v.motivo_cierre, 'Migrado desde la visita #' || v.id_visita) END,
                       0, v.creado_en, c.id_categoria_acceso, false, 'Migracion', v.id_visita
                FROM public.visita v
                JOIN public.sede s ON s.id_sede = v.id_sede
                JOIN public.categoria_acceso c ON c.id_organizacion = v.id_organizacion AND c.codigo = 'VISITANTE'
                CROSS JOIN LATERAL (SELECT COALESCE(v.fecha_esperada, (v.creado_en AT TIME ZONE COALESCE(NULLIF(s.zona_horaria, ''), 'UTC'))::date) AS fecha) f
                WHERE v.id_autorizacion IS NULL;

                INSERT INTO public.autorizacion_transicion (id_organizacion, id_autorizacion, estado_anterior, estado_nuevo, id_usuario_actor, observacion, ocurrido_en)
                SELECT a.id_organizacion, a.id_autorizacion, '', a.estado, NULL, 'Migración ADR-029 desde la visita #' || a.id_visita_origen, now()
                FROM public.autorizacion a WHERE a.origen = 'Migracion';

                UPDATE public.visita v SET id_autorizacion = a.id_autorizacion
                FROM public.autorizacion a
                WHERE a.origen = 'Migracion' AND a.id_organizacion = v.id_organizacion AND a.id_visita_origen = v.id_visita AND v.id_autorizacion IS NULL;

                UPDATE public.movimiento_acceso m SET id_autorizacion = v.id_autorizacion
                FROM public.visita v
                WHERE m.id_autorizacion IS NULL AND v.id_organizacion = m.id_organizacion AND v.id_visita = m.id_visita AND v.id_autorizacion IS NOT NULL;

                INSERT INTO public.migracion_accesos_adr029 (origen, resultado, id_organizacion, id_visita, id_autorizacion, detalle)
                SELECT 'visita', 'Migrado', a.id_organizacion, a.id_visita_origen, a.id_autorizacion,
                       'Registro de acceso Único creado para la visita sin autorización; estado ' || a.estado || '.'
                FROM public.autorizacion a WHERE a.origen = 'Migracion';

                INSERT INTO public.migracion_accesos_adr029 (origen, resultado, id_organizacion, id_visita, detalle)
                SELECT 'visita', 'Pendiente', v.id_organizacion, v.id_visita, 'La visita quedó sin registro de acceso.'
                FROM public.visita v WHERE v.id_autorizacion IS NULL;

                INSERT INTO public.migracion_accesos_adr029 (origen, resultado, id_organizacion, id_autorizacion_objeto, detalle)
                SELECT 'autorizacion_objeto', 'Historico', o.id_organizacion, o.id_autorizacion_objeto,
                       'Autorización de objeto sin registro de acceso de la persona: se conserva sin vínculo.'
                FROM public.autorizacion_objeto o WHERE o.id_autorizacion IS NULL;

                INSERT INTO public.auditoria (id_organizacion, tipo_actor, id_usuario, accion, entidad, id_registro, resultado, valor_anterior, valor_nuevo, ocurrido_en)
                SELECT a.id_organizacion, 'Sistema', NULL, 'Migrar', 'Autorizacion', a.id_autorizacion::text, 'Exitoso',
                       json_build_object('idVisita', a.id_visita_origen)::text,
                       json_build_object('estado', a.estado, 'modalidad', a.modalidad, 'fecha', a.fecha_inicio, 'origen', 'ADR-029')::text, now()
                FROM public.autorizacion a WHERE a.origen = 'Migracion';
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Revierte el traslado: desvincula y elimina los registros creados por la migración. La auditoría es inmutable y se conserva.
            migrationBuilder.Sql("""
                UPDATE public.movimiento_acceso m SET id_autorizacion = NULL
                FROM public.autorizacion a
                WHERE a.origen = 'Migracion' AND m.id_organizacion = a.id_organizacion AND m.id_autorizacion = a.id_autorizacion;
                UPDATE public.visita v SET id_autorizacion = NULL
                FROM public.autorizacion a
                WHERE a.origen = 'Migracion' AND v.id_organizacion = a.id_organizacion AND v.id_autorizacion = a.id_autorizacion;
                DELETE FROM public.autorizacion_transicion t USING public.autorizacion a
                WHERE a.origen = 'Migracion' AND t.id_organizacion = a.id_organizacion AND t.id_autorizacion = a.id_autorizacion;
                DELETE FROM public.autorizacion WHERE origen = 'Migracion';
                DELETE FROM public.parametro_organizacion WHERE clave = 'accesos.permite_permanente';
                DROP TABLE IF EXISTS public.migracion_accesos_adr029;
                """);

            migrationBuilder.DropForeignKey(
                name: "FK_autorizacion_categoria_acceso_id_organizacion_id_categoria_~",
                schema: "public",
                table: "autorizacion");

            migrationBuilder.DropForeignKey(
                name: "FK_autorizacion_visita_id_organizacion_id_visita_origen",
                schema: "public",
                table: "autorizacion");

            migrationBuilder.DropForeignKey(
                name: "FK_movimiento_acceso_autorizacion_id_organizacion_id_autorizac~",
                schema: "public",
                table: "movimiento_acceso");

            migrationBuilder.DropTable(
                name: "categoria_acceso",
                schema: "public");

            migrationBuilder.DropTable(
                name: "horario_base",
                schema: "public");

            migrationBuilder.DropTable(
                name: "plantilla_categoria_acceso",
                schema: "public");

            migrationBuilder.DropIndex(
                name: "IX_movimiento_acceso_id_organizacion_id_autorizacion",
                schema: "public",
                table: "movimiento_acceso");

            migrationBuilder.DropIndex(
                name: "IX_autorizacion_horario_id_autorizacion_dia_semana",
                schema: "public",
                table: "autorizacion_horario");

            migrationBuilder.DropIndex(
                name: "IX_autorizacion_id_organizacion_id_categoria_acceso",
                schema: "public",
                table: "autorizacion");

            migrationBuilder.DropIndex(
                name: "IX_autorizacion_id_organizacion_id_persona_fecha_inicio",
                schema: "public",
                table: "autorizacion");

            migrationBuilder.DropIndex(
                name: "IX_autorizacion_id_organizacion_id_visita_origen",
                schema: "public",
                table: "autorizacion");

            migrationBuilder.DropCheckConstraint(
                name: "ck_autorizacion_origen",
                schema: "public",
                table: "autorizacion");

            migrationBuilder.DropIndex(
                name: "IX_auditoria_id_organizacion_entidad_id_registro",
                schema: "public",
                table: "auditoria");

            migrationBuilder.DropIndex(
                name: "IX_auditoria_id_organizacion_ocurrido_en",
                schema: "public",
                table: "auditoria");

            migrationBuilder.DropColumn(
                name: "zona_horaria",
                schema: "public",
                table: "organizacion");

            migrationBuilder.DropColumn(
                name: "id_categoria_acceso",
                schema: "public",
                table: "autorizacion");

            migrationBuilder.DropColumn(
                name: "id_visita_origen",
                schema: "public",
                table: "autorizacion");

            migrationBuilder.DropColumn(
                name: "origen",
                schema: "public",
                table: "autorizacion");

            migrationBuilder.DropColumn(
                name: "usa_horario_base",
                schema: "public",
                table: "autorizacion");

            migrationBuilder.DropColumn(
                name: "agente_usuario",
                schema: "public",
                table: "auditoria");

            migrationBuilder.CreateIndex(
                name: "IX_autorizacion_horario_id_autorizacion_dia_semana",
                schema: "public",
                table: "autorizacion_horario",
                columns: new[] { "id_autorizacion", "dia_semana" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_autorizacion_id_organizacion_id_persona",
                schema: "public",
                table: "autorizacion",
                columns: new[] { "id_organizacion", "id_persona" });
        }
    }
}
