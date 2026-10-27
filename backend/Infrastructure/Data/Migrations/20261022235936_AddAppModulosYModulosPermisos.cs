using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Infrastructure.Data.Migrations
{
    /// <summary>
    /// Catálogo global de módulos (app_modulos) y acceso por rol (modulos_permisos).
    /// Solo contiene estas tablas: las de zonas empresariales ya las crea 20261022120000 (migración manual sin snapshot).
    /// </summary>
    public partial class AddAppModulosYModulosPermisos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddUniqueConstraint(
                name: "AK_rol_id_organizacion_id_rol",
                schema: "public",
                table: "rol",
                columns: new[] { "id_organizacion", "id_rol" });

            migrationBuilder.CreateTable(
                name: "app_modulos",
                schema: "public",
                columns: table => new
                {
                    id_app_modulo = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    codigo = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    nombre = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    descripcion = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    ruta = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    icono = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: true),
                    orden = table.Column<int>(type: "integer", nullable: false),
                    activo = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    creado_en = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_app_modulos", x => x.id_app_modulo);
                    table.CheckConstraint("ck_app_modulos_codigo", "codigo ~ '^[a-z][a-z0-9_]*$'");
                });

            migrationBuilder.CreateTable(
                name: "modulos_permisos",
                schema: "public",
                columns: table => new
                {
                    id_modulo_permiso = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    id_rol = table.Column<int>(type: "integer", nullable: false),
                    id_app_modulo = table.Column<int>(type: "integer", nullable: false),
                    asignado_en = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    id_usuario_asigna = table.Column<int>(type: "integer", nullable: true),
                    id_organizacion = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_modulos_permisos", x => x.id_modulo_permiso);
                    table.ForeignKey(
                        name: "FK_modulos_permisos_app_modulos_id_app_modulo",
                        column: x => x.id_app_modulo,
                        principalSchema: "public",
                        principalTable: "app_modulos",
                        principalColumn: "id_app_modulo",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_modulos_permisos_organizacion_id_organizacion",
                        column: x => x.id_organizacion,
                        principalSchema: "public",
                        principalTable: "organizacion",
                        principalColumn: "id_organizacion",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_modulos_permisos_rol_id_organizacion_id_rol",
                        columns: x => new { x.id_organizacion, x.id_rol },
                        principalSchema: "public",
                        principalTable: "rol",
                        principalColumns: new[] { "id_organizacion", "id_rol" },
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_modulos_permisos_usuario_id_usuario_asigna",
                        column: x => x.id_usuario_asigna,
                        principalSchema: "public",
                        principalTable: "usuario",
                        principalColumn: "id_usuario",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_app_modulos_codigo",
                schema: "public",
                table: "app_modulos",
                column: "codigo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_modulos_permisos_id_app_modulo",
                schema: "public",
                table: "modulos_permisos",
                column: "id_app_modulo");

            migrationBuilder.CreateIndex(
                name: "IX_modulos_permisos_id_organizacion_id_rol",
                schema: "public",
                table: "modulos_permisos",
                columns: new[] { "id_organizacion", "id_rol" });

            migrationBuilder.CreateIndex(
                name: "IX_modulos_permisos_id_rol_id_app_modulo",
                schema: "public",
                table: "modulos_permisos",
                columns: new[] { "id_rol", "id_app_modulo" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_modulos_permisos_id_usuario_asigna",
                schema: "public",
                table: "modulos_permisos",
                column: "id_usuario_asigna");


            // Catálogo inicial: solo módulos con API o pantalla existentes. `ruta` solo para pantallas implementadas.
            migrationBuilder.Sql("""
                INSERT INTO public.app_modulos (codigo, nombre, descripcion, ruta, icono, orden, activo, creado_en) VALUES
                ('dashboard',       'Dashboard',          'Resumen operativo de accesos y visitantes.',        '/dashboard',      'dashboard',      10,  true, now()),
                ('control_accesos', 'Control de accesos', 'Movimientos de entrada/salida y permanencias.',     NULL,              'accesos',        20,  true, now()),
                ('visitas',         'Visitas',            'Registro y consulta de visitas.',                   NULL,              'visitas',        30,  true, now()),
                ('autorizaciones',  'Autorizaciones',     'Autorizaciones de ingreso, QR e invitaciones.',     NULL,              'autorizaciones', 40,  true, now()),
                ('personas',        'Personas',           'Registro reutilizable de personas.',                NULL,              'personas',       50,  true, now()),
                ('vehiculos',       'Vehículos',          'Vehículos y sus movimientos.',                      NULL,              'vehiculos',      60,  true, now()),
                ('dispositivos',    'Dispositivos',       'Equipos y dispositivos y sus movimientos.',         NULL,              'dispositivos',   70,  true, now()),
                ('alertas',         'Alertas',            'Alertas y notificaciones.',                         NULL,              'alertas',        80,  true, now()),
                ('sedes',           'Sedes y zonas',      'Sedes, áreas, zonas y zonas empresariales.',        NULL,              'sede',           90,  true, now()),
                ('auditoria',       'Auditoría',          'Consulta de la bitácora de auditoría.',             NULL,              'reportes',       100, true, now()),
                ('configuracion',   'Configuración',      'Parámetros, catálogos y plantillas.',               NULL,              'configuracion',  110, true, now()),
                ('administracion',  'Administración',     'Usuarios, organizaciones, roles y sus permisos.',   '/administracion', 'administracion', 120, true, now());
                """);

            // Traslado explícito desde rol_permiso: el prefijo del código de permiso determina el módulo.
            // Permisos 'saas.*' (plataforma) no se trasladan: el administrador general no depende de asignaciones.
            migrationBuilder.Sql("""
                INSERT INTO public.modulos_permisos (id_organizacion, id_rol, id_app_modulo, asignado_en)
                SELECT DISTINCT rp.id_organizacion, rp.id_rol, am.id_app_modulo, now()
                FROM public.rol_permiso rp
                JOIN public.permiso p ON p.id_permiso = rp.id_permiso AND p.activo
                JOIN (VALUES
                    ('dashboard', 'dashboard'),
                    ('accesos', 'control_accesos'), ('movimientos', 'control_accesos'), ('permanencias', 'control_accesos'),
                    ('visitas', 'visitas'),
                    ('autorizaciones', 'autorizaciones'), ('qr', 'autorizaciones'), ('invitaciones', 'autorizaciones'),
                    ('personas', 'personas'),
                    ('vehiculos', 'vehiculos'),
                    ('dispositivos', 'dispositivos'),
                    ('alertas', 'alertas'), ('notificaciones', 'alertas'),
                    ('sedes', 'sedes'), ('areas', 'sedes'), ('zonas', 'sedes'), ('zonas_empresariales', 'sedes'),
                    ('auditoria', 'auditoria'),
                    ('configuracion', 'configuracion'), ('parametros', 'configuracion'),
                    ('usuarios', 'administracion'), ('roles', 'administracion'), ('permisos', 'administracion'), ('membresias', 'administracion')
                ) AS mapa(prefijo, modulo) ON mapa.prefijo = split_part(p.codigo, '.', 1)
                JOIN public.app_modulos am ON am.codigo = mapa.modulo
                ON CONFLICT (id_rol, id_app_modulo) DO NOTHING;
                """);

            // El dashboard no exigía permiso antes de esta migración: se conserva para los roles activos existentes.
            migrationBuilder.Sql("""
                INSERT INTO public.modulos_permisos (id_organizacion, id_rol, id_app_modulo, asignado_en)
                SELECT r.id_organizacion, r.id_rol, am.id_app_modulo, now()
                FROM public.rol r
                JOIN public.app_modulos am ON am.codigo = 'dashboard'
                WHERE r.activo
                ON CONFLICT (id_rol, id_app_modulo) DO NOTHING;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "modulos_permisos", schema: "public");
            migrationBuilder.DropTable(name: "app_modulos", schema: "public");
            migrationBuilder.DropUniqueConstraint(name: "AK_rol_id_organizacion_id_rol", schema: "public", table: "rol");
        }
    }
}
