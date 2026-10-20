using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "public");

            migrationBuilder.CreateTable(
                name: "alerta",
                schema: "public",
                columns: table => new
                {
                    id_alerta = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    id_sede = table.Column<int>(type: "integer", nullable: false),
                    tipo_alerta = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    id_persona = table.Column<int>(type: "integer", nullable: true),
                    id_visita = table.Column<int>(type: "integer", nullable: true),
                    id_autorizacion = table.Column<int>(type: "integer", nullable: true),
                    id_movimiento = table.Column<long>(type: "bigint", nullable: true),
                    clave_deduplicacion = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    descripcion = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    estado = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    generada_en = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    id_organizacion = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_alerta", x => x.id_alerta);
                });

            migrationBuilder.CreateTable(
                name: "alerta_transicion",
                schema: "public",
                columns: table => new
                {
                    id_transicion = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    id_alerta = table.Column<int>(type: "integer", nullable: false),
                    estado_anterior = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    estado_nuevo = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    id_usuario_actor = table.Column<int>(type: "integer", nullable: false),
                    observacion = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    ocurrido_en = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    id_organizacion = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_alerta_transicion", x => x.id_transicion);
                });

            migrationBuilder.CreateTable(
                name: "archivo",
                schema: "public",
                columns: table => new
                {
                    id_archivo = table.Column<Guid>(type: "uuid", nullable: false),
                    id_organizacion = table.Column<int>(type: "integer", nullable: true),
                    clave_almacenamiento = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    tipo_contenido = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    tamano_bytes = table.Column<long>(type: "bigint", nullable: false),
                    hash_sha256 = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    id_usuario_carga = table.Column<int>(type: "integer", nullable: false),
                    creado_en = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_archivo", x => x.id_archivo);
                });

            migrationBuilder.CreateTable(
                name: "area_responsable",
                schema: "public",
                columns: table => new
                {
                    id_organizacion = table.Column<int>(type: "integer", nullable: false),
                    id_area = table.Column<int>(type: "integer", nullable: false),
                    id_membresia = table.Column<int>(type: "integer", nullable: false),
                    es_autorizador = table.Column<bool>(type: "boolean", nullable: false),
                    activo = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_area_responsable", x => new { x.id_organizacion, x.id_area, x.id_membresia });
                });

            migrationBuilder.CreateTable(
                name: "auditoria",
                schema: "public",
                columns: table => new
                {
                    id_auditoria = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    id_organizacion = table.Column<int>(type: "integer", nullable: true),
                    tipo_actor = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    id_usuario = table.Column<int>(type: "integer", nullable: true),
                    accion = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    entidad = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    id_registro = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    resultado = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    valor_anterior = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    valor_nuevo = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    direccion_ip = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    id_correlacion = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    ocurrido_en = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_auditoria", x => x.id_auditoria);
                });

            migrationBuilder.CreateTable(
                name: "autorizacion_dispositivo",
                schema: "public",
                columns: table => new
                {
                    id_organizacion = table.Column<int>(type: "integer", nullable: false),
                    id_autorizacion = table.Column<int>(type: "integer", nullable: false),
                    id_dispositivo = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_autorizacion_dispositivo", x => new { x.id_organizacion, x.id_autorizacion, x.id_dispositivo });
                });

            migrationBuilder.CreateTable(
                name: "autorizacion_horario",
                schema: "public",
                columns: table => new
                {
                    id_autorizacion_horario = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    id_autorizacion = table.Column<int>(type: "integer", nullable: false),
                    dia_semana = table.Column<int>(type: "integer", nullable: false),
                    hora_inicio = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    hora_fin = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    id_organizacion = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_autorizacion_horario", x => x.id_autorizacion_horario);
                    table.CheckConstraint("ck_autorizacion_horario_dia", "dia_semana BETWEEN 1 AND 7");
                });

            migrationBuilder.CreateTable(
                name: "autorizacion_transicion",
                schema: "public",
                columns: table => new
                {
                    id_transicion = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    id_autorizacion = table.Column<int>(type: "integer", nullable: false),
                    estado_anterior = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    estado_nuevo = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    id_usuario_actor = table.Column<int>(type: "integer", nullable: true),
                    observacion = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    ocurrido_en = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    id_organizacion = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_autorizacion_transicion", x => x.id_transicion);
                });

            migrationBuilder.CreateTable(
                name: "autorizacion_vehiculo",
                schema: "public",
                columns: table => new
                {
                    id_organizacion = table.Column<int>(type: "integer", nullable: false),
                    id_autorizacion = table.Column<int>(type: "integer", nullable: false),
                    id_vehiculo = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_autorizacion_vehiculo", x => new { x.id_organizacion, x.id_autorizacion, x.id_vehiculo });
                });

            migrationBuilder.CreateTable(
                name: "autorizacion_zona",
                schema: "public",
                columns: table => new
                {
                    id_organizacion = table.Column<int>(type: "integer", nullable: false),
                    id_autorizacion = table.Column<int>(type: "integer", nullable: false),
                    id_zona = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_autorizacion_zona", x => new { x.id_organizacion, x.id_autorizacion, x.id_zona });
                });

            migrationBuilder.CreateTable(
                name: "ciudad",
                schema: "public",
                columns: table => new
                {
                    id_ciudad = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    nombre = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    departamento = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    pais = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    activo = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ciudad", x => x.id_ciudad);
                });

            migrationBuilder.CreateTable(
                name: "dispositivo",
                schema: "public",
                columns: table => new
                {
                    id_dispositivo = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    id_persona_responsable = table.Column<int>(type: "integer", nullable: false),
                    id_tipo_dispositivo = table.Column<int>(type: "integer", nullable: false),
                    codigo_identificador = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    serial = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    serial_normalizado = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    marca = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    modelo = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    color = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    descripcion = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    activo = table.Column<bool>(type: "boolean", nullable: false),
                    creado_en = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    id_organizacion = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_dispositivo", x => x.id_dispositivo);
                });

            migrationBuilder.CreateTable(
                name: "invitacion_envio",
                schema: "public",
                columns: table => new
                {
                    id_envio = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    id_autorizacion = table.Column<int>(type: "integer", nullable: false),
                    id_token = table.Column<int>(type: "integer", nullable: false),
                    id_usuario_envia = table.Column<int>(type: "integer", nullable: false),
                    canal = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    destino = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    enviado_en = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    id_organizacion = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_invitacion_envio", x => x.id_envio);
                });

            migrationBuilder.CreateTable(
                name: "membresia_organizacion",
                schema: "public",
                columns: table => new
                {
                    id_membresia = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    id_usuario = table.Column<int>(type: "integer", nullable: false),
                    id_persona = table.Column<int>(type: "integer", nullable: true),
                    estado = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    creado_en = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    id_organizacion = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_membresia_organizacion", x => x.id_membresia);
                });

            migrationBuilder.CreateTable(
                name: "membresia_punto_acceso",
                schema: "public",
                columns: table => new
                {
                    id_organizacion = table.Column<int>(type: "integer", nullable: false),
                    id_membresia = table.Column<int>(type: "integer", nullable: false),
                    id_zona = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_membresia_punto_acceso", x => new { x.id_organizacion, x.id_membresia, x.id_zona });
                });

            migrationBuilder.CreateTable(
                name: "membresia_rol",
                schema: "public",
                columns: table => new
                {
                    id_organizacion = table.Column<int>(type: "integer", nullable: false),
                    id_membresia = table.Column<int>(type: "integer", nullable: false),
                    id_rol = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_membresia_rol", x => new { x.id_organizacion, x.id_membresia, x.id_rol });
                });

            migrationBuilder.CreateTable(
                name: "membresia_sede",
                schema: "public",
                columns: table => new
                {
                    id_organizacion = table.Column<int>(type: "integer", nullable: false),
                    id_membresia = table.Column<int>(type: "integer", nullable: false),
                    id_sede = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_membresia_sede", x => new { x.id_organizacion, x.id_membresia, x.id_sede });
                });

            migrationBuilder.CreateTable(
                name: "modulo",
                schema: "public",
                columns: table => new
                {
                    id_modulo = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    codigo = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    nombre = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    activo = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_modulo", x => x.id_modulo);
                });

            migrationBuilder.CreateTable(
                name: "movimiento_dispositivo",
                schema: "public",
                columns: table => new
                {
                    id_movimiento_dispositivo = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    id_dispositivo = table.Column<int>(type: "integer", nullable: false),
                    id_sede = table.Column<int>(type: "integer", nullable: false),
                    id_zona_punto_acceso = table.Column<int>(type: "integer", nullable: false),
                    id_persona_responsable = table.Column<int>(type: "integer", nullable: false),
                    id_visita = table.Column<int>(type: "integer", nullable: false),
                    id_usuario_registra = table.Column<int>(type: "integer", nullable: false),
                    tipo_movimiento = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    ocurrido_en = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    observacion = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    id_organizacion = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_movimiento_dispositivo", x => x.id_movimiento_dispositivo);
                });

            migrationBuilder.CreateTable(
                name: "movimiento_vehiculo",
                schema: "public",
                columns: table => new
                {
                    id_movimiento_vehiculo = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    id_vehiculo = table.Column<int>(type: "integer", nullable: false),
                    id_movimiento = table.Column<long>(type: "bigint", nullable: false),
                    id_visita = table.Column<int>(type: "integer", nullable: false),
                    tipo_movimiento = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    ocurrido_en = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    id_organizacion = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_movimiento_vehiculo", x => x.id_movimiento_vehiculo);
                });

            migrationBuilder.CreateTable(
                name: "notificacion",
                schema: "public",
                columns: table => new
                {
                    id_notificacion = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    id_plantilla = table.Column<int>(type: "integer", nullable: true),
                    evento = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    titulo = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    mensaje = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    creado_en = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    id_organizacion = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_notificacion", x => x.id_notificacion);
                });

            migrationBuilder.CreateTable(
                name: "notificacion_destinatario",
                schema: "public",
                columns: table => new
                {
                    id_destinatario = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    id_notificacion = table.Column<int>(type: "integer", nullable: false),
                    id_usuario = table.Column<int>(type: "integer", nullable: true),
                    id_persona = table.Column<int>(type: "integer", nullable: true),
                    canal = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    direccion = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    estado = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    intentos = table.Column<int>(type: "integer", nullable: false),
                    ultimo_error = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    enviado_en = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    leido_en = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    id_organizacion = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_notificacion_destinatario", x => x.id_destinatario);
                });

            migrationBuilder.CreateTable(
                name: "organizacion",
                schema: "public",
                columns: table => new
                {
                    id_organizacion = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    id_tipo_organizacion = table.Column<int>(type: "integer", nullable: false),
                    id_plan = table.Column<int>(type: "integer", nullable: false),
                    id_archivo_logo = table.Column<Guid>(type: "uuid", nullable: true),
                    nombre = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    identificacion = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    direccion = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    telefono = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    correo = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    estado = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    creado_en = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_organizacion", x => x.id_organizacion);
                });

            migrationBuilder.CreateTable(
                name: "organizacion_modulo",
                schema: "public",
                columns: table => new
                {
                    id_organizacion = table.Column<int>(type: "integer", nullable: false),
                    id_modulo = table.Column<int>(type: "integer", nullable: false),
                    habilitado = table.Column<bool>(type: "boolean", nullable: false),
                    cambiado_en = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    id_usuario_cambia = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_organizacion_modulo", x => new { x.id_organizacion, x.id_modulo });
                });

            migrationBuilder.CreateTable(
                name: "outbox",
                schema: "public",
                columns: table => new
                {
                    id_mensaje = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    tipo = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    carga = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    estado = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    intentos = table.Column<int>(type: "integer", nullable: false),
                    disponible_en = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    procesado_en = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    id_organizacion = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_outbox", x => x.id_mensaje);
                });

            migrationBuilder.CreateTable(
                name: "parametro_organizacion",
                schema: "public",
                columns: table => new
                {
                    id_organizacion = table.Column<int>(type: "integer", nullable: false),
                    clave = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    valor = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    actualizado_en = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_parametro_organizacion", x => new { x.id_organizacion, x.clave });
                });

            migrationBuilder.CreateTable(
                name: "permanencia",
                schema: "public",
                columns: table => new
                {
                    id_permanencia = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    id_persona = table.Column<int>(type: "integer", nullable: false),
                    id_sede = table.Column<int>(type: "integer", nullable: false),
                    id_visita = table.Column<int>(type: "integer", nullable: false),
                    id_movimiento_entrada = table.Column<long>(type: "bigint", nullable: false),
                    id_movimiento_salida = table.Column<long>(type: "bigint", nullable: true),
                    estado = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    abierta_en = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    cerrada_en = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    id_organizacion = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_permanencia", x => x.id_permanencia);
                });

            migrationBuilder.CreateTable(
                name: "permiso",
                schema: "public",
                columns: table => new
                {
                    id_permiso = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    codigo = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    descripcion = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    activo = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_permiso", x => x.id_permiso);
                });

            migrationBuilder.CreateTable(
                name: "persona_telefono",
                schema: "public",
                columns: table => new
                {
                    id_telefono = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    id_persona = table.Column<int>(type: "integer", nullable: false),
                    numero = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    tipo = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    principal = table.Column<bool>(type: "boolean", nullable: false),
                    activo = table.Column<bool>(type: "boolean", nullable: false),
                    id_organizacion = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_persona_telefono", x => x.id_telefono);
                });

            migrationBuilder.CreateTable(
                name: "persona_vehiculo",
                schema: "public",
                columns: table => new
                {
                    id_organizacion = table.Column<int>(type: "integer", nullable: false),
                    id_persona = table.Column<int>(type: "integer", nullable: false),
                    id_vehiculo = table.Column<int>(type: "integer", nullable: false),
                    relacion = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    activo = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_persona_vehiculo", x => new { x.id_organizacion, x.id_persona, x.id_vehiculo });
                });

            migrationBuilder.CreateTable(
                name: "plan",
                schema: "public",
                columns: table => new
                {
                    id_plan = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    nombre = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    descripcion = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    activo = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_plan", x => x.id_plan);
                });

            migrationBuilder.CreateTable(
                name: "plan_modulo",
                schema: "public",
                columns: table => new
                {
                    id_plan = table.Column<int>(type: "integer", nullable: false),
                    id_modulo = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_plan_modulo", x => new { x.id_plan, x.id_modulo });
                });

            migrationBuilder.CreateTable(
                name: "plantilla_notificacion",
                schema: "public",
                columns: table => new
                {
                    id_plantilla = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    evento = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    canal = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    asunto = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    cuerpo = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    activo = table.Column<bool>(type: "boolean", nullable: false),
                    id_organizacion = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_plantilla_notificacion", x => x.id_plantilla);
                });

            migrationBuilder.CreateTable(
                name: "plantilla_rol",
                schema: "public",
                columns: table => new
                {
                    id_plantilla_rol = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    nombre = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_plantilla_rol", x => x.id_plantilla_rol);
                });

            migrationBuilder.CreateTable(
                name: "plantilla_rol_permiso",
                schema: "public",
                columns: table => new
                {
                    id_plantilla_rol = table.Column<int>(type: "integer", nullable: false),
                    id_permiso = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_plantilla_rol_permiso", x => new { x.id_plantilla_rol, x.id_permiso });
                });

            migrationBuilder.CreateTable(
                name: "restriccion_acceso",
                schema: "public",
                columns: table => new
                {
                    id_restriccion = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    id_persona = table.Column<int>(type: "integer", nullable: false),
                    id_sede = table.Column<int>(type: "integer", nullable: true),
                    id_zona = table.Column<int>(type: "integer", nullable: true),
                    tipo_restriccion = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    motivo = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    fecha_inicio = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    fecha_fin = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    estado = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    id_usuario_registra = table.Column<int>(type: "integer", nullable: false),
                    creado_en = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    id_usuario_levanta = table.Column<int>(type: "integer", nullable: true),
                    levantada_en = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    id_organizacion = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_restriccion_acceso", x => x.id_restriccion);
                });

            migrationBuilder.CreateTable(
                name: "rol",
                schema: "public",
                columns: table => new
                {
                    id_rol = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    id_plantilla_rol = table.Column<int>(type: "integer", nullable: true),
                    nombre = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    descripcion = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    activo = table.Column<bool>(type: "boolean", nullable: false),
                    id_organizacion = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_rol", x => x.id_rol);
                });

            migrationBuilder.CreateTable(
                name: "rol_permiso",
                schema: "public",
                columns: table => new
                {
                    id_organizacion = table.Column<int>(type: "integer", nullable: false),
                    id_rol = table.Column<int>(type: "integer", nullable: false),
                    id_permiso = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_rol_permiso", x => new { x.id_organizacion, x.id_rol, x.id_permiso });
                });

            migrationBuilder.CreateTable(
                name: "solicitud_idempotente",
                schema: "public",
                columns: table => new
                {
                    id_organizacion = table.Column<int>(type: "integer", nullable: false),
                    clave_idempotencia = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    comando = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    hash_solicitud = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    codigo_resultado = table.Column<int>(type: "integer", nullable: false),
                    respuesta = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    creado_en = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_solicitud_idempotente", x => new { x.id_organizacion, x.clave_idempotencia });
                });

            migrationBuilder.CreateTable(
                name: "tipo_dispositivo",
                schema: "public",
                columns: table => new
                {
                    id_tipo_dispositivo = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    nombre = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    descripcion = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    activo = table.Column<bool>(type: "boolean", nullable: false),
                    id_organizacion = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tipo_dispositivo", x => x.id_tipo_dispositivo);
                });

            migrationBuilder.CreateTable(
                name: "tipo_documento",
                schema: "public",
                columns: table => new
                {
                    id_tipo_documento = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    codigo = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    nombre = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    pais_emisor = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    activo = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tipo_documento", x => x.id_tipo_documento);
                });

            migrationBuilder.CreateTable(
                name: "tipo_organizacion",
                schema: "public",
                columns: table => new
                {
                    id_tipo_organizacion = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    nombre = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    descripcion = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    activo = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tipo_organizacion", x => x.id_tipo_organizacion);
                });

            migrationBuilder.CreateTable(
                name: "tipo_persona",
                schema: "public",
                columns: table => new
                {
                    id_tipo_persona = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    nombre = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    descripcion = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    activo = table.Column<bool>(type: "boolean", nullable: false),
                    id_organizacion = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tipo_persona", x => x.id_tipo_persona);
                    table.UniqueConstraint("AK_tipo_persona_id_organizacion_id_tipo_persona", x => new { x.id_organizacion, x.id_tipo_persona });
                });

            migrationBuilder.CreateTable(
                name: "token_qr",
                schema: "public",
                columns: table => new
                {
                    id_token = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    id_autorizacion = table.Column<int>(type: "integer", nullable: false),
                    hash_sha256 = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    estado = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    emitido_en = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    expira_en = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    revocado_en = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    id_organizacion = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_token_qr", x => x.id_token);
                });

            migrationBuilder.CreateTable(
                name: "usuario",
                schema: "public",
                columns: table => new
                {
                    id_usuario = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    correo = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    correo_normalizado = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    password_hash = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    sello_seguridad = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    intentos_fallidos = table.Column<int>(type: "integer", nullable: false),
                    bloqueado_hasta = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    activo = table.Column<bool>(type: "boolean", nullable: false),
                    ultimo_acceso = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    creado_en = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_usuario", x => x.id_usuario);
                });

            migrationBuilder.CreateTable(
                name: "vehiculo",
                schema: "public",
                columns: table => new
                {
                    id_vehiculo = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    placa = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    placa_normalizada = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    tipo = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    marca = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    modelo = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    color = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    activo = table.Column<bool>(type: "boolean", nullable: false),
                    id_organizacion = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_vehiculo", x => x.id_vehiculo);
                });

            migrationBuilder.CreateTable(
                name: "sede",
                schema: "public",
                columns: table => new
                {
                    id_sede = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    id_ciudad = table.Column<int>(type: "integer", nullable: false),
                    nombre = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    direccion = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    telefono = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    correo = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    zona_horaria = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    id_membresia_responsable = table.Column<int>(type: "integer", nullable: true),
                    activo = table.Column<bool>(type: "boolean", nullable: false),
                    id_organizacion = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sede", x => x.id_sede);
                    table.UniqueConstraint("AK_sede_id_organizacion_id_sede", x => new { x.id_organizacion, x.id_sede });
                    table.ForeignKey(
                        name: "FK_sede_ciudad_id_ciudad",
                        column: x => x.id_ciudad,
                        principalSchema: "public",
                        principalTable: "ciudad",
                        principalColumn: "id_ciudad",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "persona",
                schema: "public",
                columns: table => new
                {
                    id_persona = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    id_tipo_persona = table.Column<int>(type: "integer", nullable: false),
                    id_tipo_documento = table.Column<int>(type: "integer", nullable: false),
                    numero_documento = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    numero_documento_normalizado = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    nombres = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    apellidos = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    correo = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    empresa_procedencia = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    id_archivo_foto = table.Column<Guid>(type: "uuid", nullable: true),
                    observacion = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    activo = table.Column<bool>(type: "boolean", nullable: false),
                    creado_en = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    id_usuario_crea = table.Column<int>(type: "integer", nullable: false),
                    id_organizacion = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_persona", x => x.id_persona);
                    table.UniqueConstraint("AK_persona_id_organizacion_id_persona", x => new { x.id_organizacion, x.id_persona });
                    table.ForeignKey(
                        name: "FK_persona_tipo_documento_id_tipo_documento",
                        column: x => x.id_tipo_documento,
                        principalSchema: "public",
                        principalTable: "tipo_documento",
                        principalColumn: "id_tipo_documento",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_persona_tipo_persona_id_organizacion_id_tipo_persona",
                        columns: x => new { x.id_organizacion, x.id_tipo_persona },
                        principalSchema: "public",
                        principalTable: "tipo_persona",
                        principalColumns: new[] { "id_organizacion", "id_tipo_persona" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "area",
                schema: "public",
                columns: table => new
                {
                    id_area = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    id_sede = table.Column<int>(type: "integer", nullable: false),
                    id_area_padre = table.Column<int>(type: "integer", nullable: true),
                    nombre = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    descripcion = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    activo = table.Column<bool>(type: "boolean", nullable: false),
                    id_organizacion = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_area", x => x.id_area);
                    table.UniqueConstraint("AK_area_id_organizacion_id_area", x => new { x.id_organizacion, x.id_area });
                    table.ForeignKey(
                        name: "FK_area_sede_id_organizacion_id_sede",
                        columns: x => new { x.id_organizacion, x.id_sede },
                        principalSchema: "public",
                        principalTable: "sede",
                        principalColumns: new[] { "id_organizacion", "id_sede" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "zona",
                schema: "public",
                columns: table => new
                {
                    id_zona = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    id_sede = table.Column<int>(type: "integer", nullable: false),
                    id_zona_padre = table.Column<int>(type: "integer", nullable: true),
                    nombre = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    descripcion = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    es_punto_acceso = table.Column<bool>(type: "boolean", nullable: false),
                    modo_acceso = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    activo = table.Column<bool>(type: "boolean", nullable: false),
                    id_organizacion = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_zona", x => x.id_zona);
                    table.UniqueConstraint("AK_zona_id_organizacion_id_zona", x => new { x.id_organizacion, x.id_zona });
                    table.CheckConstraint("ck_zona_modo_acceso", "es_punto_acceso OR modo_acceso IS NULL OR modo_acceso IN ('Entrada','Salida','Ambos')");
                    table.ForeignKey(
                        name: "FK_zona_sede_id_organizacion_id_sede",
                        columns: x => new { x.id_organizacion, x.id_sede },
                        principalSchema: "public",
                        principalTable: "sede",
                        principalColumns: new[] { "id_organizacion", "id_sede" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "autorizacion",
                schema: "public",
                columns: table => new
                {
                    id_autorizacion = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    id_persona = table.Column<int>(type: "integer", nullable: false),
                    id_sede = table.Column<int>(type: "integer", nullable: false),
                    id_area = table.Column<int>(type: "integer", nullable: false),
                    id_membresia_responsable = table.Column<int>(type: "integer", nullable: false),
                    id_usuario_crea = table.Column<int>(type: "integer", nullable: false),
                    modalidad = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    fecha_inicio = table.Column<DateOnly>(type: "date", nullable: false),
                    fecha_fin = table.Column<DateOnly>(type: "date", nullable: true),
                    cupo_ingresos = table.Column<int>(type: "integer", nullable: true),
                    ingresos_consumidos = table.Column<int>(type: "integer", nullable: false),
                    motivo = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    observacion = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    estado = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    version = table.Column<long>(type: "bigint", nullable: false),
                    creado_en = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    id_organizacion = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_autorizacion", x => x.id_autorizacion);
                    table.ForeignKey(
                        name: "FK_autorizacion_area_id_organizacion_id_area",
                        columns: x => new { x.id_organizacion, x.id_area },
                        principalSchema: "public",
                        principalTable: "area",
                        principalColumns: new[] { "id_organizacion", "id_area" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_autorizacion_persona_id_organizacion_id_persona",
                        columns: x => new { x.id_organizacion, x.id_persona },
                        principalSchema: "public",
                        principalTable: "persona",
                        principalColumns: new[] { "id_organizacion", "id_persona" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_autorizacion_sede_id_organizacion_id_sede",
                        columns: x => new { x.id_organizacion, x.id_sede },
                        principalSchema: "public",
                        principalTable: "sede",
                        principalColumns: new[] { "id_organizacion", "id_sede" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "visita",
                schema: "public",
                columns: table => new
                {
                    id_visita = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    id_persona = table.Column<int>(type: "integer", nullable: false),
                    id_autorizacion = table.Column<int>(type: "integer", nullable: true),
                    id_sede = table.Column<int>(type: "integer", nullable: false),
                    id_area = table.Column<int>(type: "integer", nullable: false),
                    id_membresia_responsable = table.Column<int>(type: "integer", nullable: false),
                    id_usuario_registra = table.Column<int>(type: "integer", nullable: false),
                    politica_aplicada = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    motivo = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    observacion = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    estado = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    version = table.Column<long>(type: "bigint", nullable: false),
                    creado_en = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    id_organizacion = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_visita", x => x.id_visita);
                    table.ForeignKey(
                        name: "FK_visita_area_id_organizacion_id_area",
                        columns: x => new { x.id_organizacion, x.id_area },
                        principalSchema: "public",
                        principalTable: "area",
                        principalColumns: new[] { "id_organizacion", "id_area" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_visita_persona_id_organizacion_id_persona",
                        columns: x => new { x.id_organizacion, x.id_persona },
                        principalSchema: "public",
                        principalTable: "persona",
                        principalColumns: new[] { "id_organizacion", "id_persona" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_visita_sede_id_organizacion_id_sede",
                        columns: x => new { x.id_organizacion, x.id_sede },
                        principalSchema: "public",
                        principalTable: "sede",
                        principalColumns: new[] { "id_organizacion", "id_sede" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "movimiento_acceso",
                schema: "public",
                columns: table => new
                {
                    id_movimiento = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    id_persona = table.Column<int>(type: "integer", nullable: false),
                    id_sede = table.Column<int>(type: "integer", nullable: false),
                    id_zona_punto_acceso = table.Column<int>(type: "integer", nullable: false),
                    id_visita = table.Column<int>(type: "integer", nullable: true),
                    id_autorizacion = table.Column<int>(type: "integer", nullable: true),
                    id_permanencia = table.Column<int>(type: "integer", nullable: true),
                    id_usuario_registra = table.Column<int>(type: "integer", nullable: false),
                    tipo_movimiento = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    resultado = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    motivo_denegacion = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    medio = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    id_movimiento_corrige = table.Column<long>(type: "bigint", nullable: true),
                    ocurrido_en = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    observacion = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    id_organizacion = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_movimiento_acceso", x => x.id_movimiento);
                    table.ForeignKey(
                        name: "FK_movimiento_acceso_persona_id_organizacion_id_persona",
                        columns: x => new { x.id_organizacion, x.id_persona },
                        principalSchema: "public",
                        principalTable: "persona",
                        principalColumns: new[] { "id_organizacion", "id_persona" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_movimiento_acceso_zona_id_organizacion_id_zona_punto_acceso",
                        columns: x => new { x.id_organizacion, x.id_zona_punto_acceso },
                        principalSchema: "public",
                        principalTable: "zona",
                        principalColumns: new[] { "id_organizacion", "id_zona" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_alerta_id_organizacion_clave_deduplicacion",
                schema: "public",
                table: "alerta",
                columns: new[] { "id_organizacion", "clave_deduplicacion" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_area_id_organizacion_id_area",
                schema: "public",
                table: "area",
                columns: new[] { "id_organizacion", "id_area" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_area_id_organizacion_id_sede",
                schema: "public",
                table: "area",
                columns: new[] { "id_organizacion", "id_sede" });

            migrationBuilder.CreateIndex(
                name: "IX_autorizacion_id_organizacion_id_area",
                schema: "public",
                table: "autorizacion",
                columns: new[] { "id_organizacion", "id_area" });

            migrationBuilder.CreateIndex(
                name: "IX_autorizacion_id_organizacion_id_autorizacion",
                schema: "public",
                table: "autorizacion",
                columns: new[] { "id_organizacion", "id_autorizacion" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_autorizacion_id_organizacion_id_persona",
                schema: "public",
                table: "autorizacion",
                columns: new[] { "id_organizacion", "id_persona" });

            migrationBuilder.CreateIndex(
                name: "IX_autorizacion_id_organizacion_id_sede",
                schema: "public",
                table: "autorizacion",
                columns: new[] { "id_organizacion", "id_sede" });

            migrationBuilder.CreateIndex(
                name: "IX_movimiento_acceso_id_organizacion_id_persona",
                schema: "public",
                table: "movimiento_acceso",
                columns: new[] { "id_organizacion", "id_persona" });

            migrationBuilder.CreateIndex(
                name: "IX_movimiento_acceso_id_organizacion_id_zona_punto_acceso",
                schema: "public",
                table: "movimiento_acceso",
                columns: new[] { "id_organizacion", "id_zona_punto_acceso" });

            migrationBuilder.CreateIndex(
                name: "IX_permiso_codigo",
                schema: "public",
                table: "permiso",
                column: "codigo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_persona_id_organizacion_id_persona",
                schema: "public",
                table: "persona",
                columns: new[] { "id_organizacion", "id_persona" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_persona_id_organizacion_id_tipo_documento_numero_documento_~",
                schema: "public",
                table: "persona",
                columns: new[] { "id_organizacion", "id_tipo_documento", "numero_documento_normalizado" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_persona_id_organizacion_id_tipo_persona",
                schema: "public",
                table: "persona",
                columns: new[] { "id_organizacion", "id_tipo_persona" });

            migrationBuilder.CreateIndex(
                name: "IX_persona_id_tipo_documento",
                schema: "public",
                table: "persona",
                column: "id_tipo_documento");

            migrationBuilder.CreateIndex(
                name: "IX_sede_id_ciudad",
                schema: "public",
                table: "sede",
                column: "id_ciudad");

            migrationBuilder.CreateIndex(
                name: "IX_sede_id_organizacion_id_sede",
                schema: "public",
                table: "sede",
                columns: new[] { "id_organizacion", "id_sede" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_tipo_persona_id_organizacion_id_tipo_persona",
                schema: "public",
                table: "tipo_persona",
                columns: new[] { "id_organizacion", "id_tipo_persona" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_token_qr_hash_sha256",
                schema: "public",
                table: "token_qr",
                column: "hash_sha256",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_usuario_correo_normalizado",
                schema: "public",
                table: "usuario",
                column: "correo_normalizado",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_vehiculo_id_organizacion_placa_normalizada",
                schema: "public",
                table: "vehiculo",
                columns: new[] { "id_organizacion", "placa_normalizada" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_visita_id_organizacion_id_area",
                schema: "public",
                table: "visita",
                columns: new[] { "id_organizacion", "id_area" });

            migrationBuilder.CreateIndex(
                name: "IX_visita_id_organizacion_id_persona",
                schema: "public",
                table: "visita",
                columns: new[] { "id_organizacion", "id_persona" });

            migrationBuilder.CreateIndex(
                name: "IX_visita_id_organizacion_id_sede",
                schema: "public",
                table: "visita",
                columns: new[] { "id_organizacion", "id_sede" });

            migrationBuilder.CreateIndex(
                name: "IX_visita_id_organizacion_id_visita",
                schema: "public",
                table: "visita",
                columns: new[] { "id_organizacion", "id_visita" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_zona_id_organizacion_id_sede",
                schema: "public",
                table: "zona",
                columns: new[] { "id_organizacion", "id_sede" });

            migrationBuilder.CreateIndex(
                name: "IX_zona_id_organizacion_id_zona",
                schema: "public",
                table: "zona",
                columns: new[] { "id_organizacion", "id_zona" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "alerta",
                schema: "public");

            migrationBuilder.DropTable(
                name: "alerta_transicion",
                schema: "public");

            migrationBuilder.DropTable(
                name: "archivo",
                schema: "public");

            migrationBuilder.DropTable(
                name: "area_responsable",
                schema: "public");

            migrationBuilder.DropTable(
                name: "auditoria",
                schema: "public");

            migrationBuilder.DropTable(
                name: "autorizacion",
                schema: "public");

            migrationBuilder.DropTable(
                name: "autorizacion_dispositivo",
                schema: "public");

            migrationBuilder.DropTable(
                name: "autorizacion_horario",
                schema: "public");

            migrationBuilder.DropTable(
                name: "autorizacion_transicion",
                schema: "public");

            migrationBuilder.DropTable(
                name: "autorizacion_vehiculo",
                schema: "public");

            migrationBuilder.DropTable(
                name: "autorizacion_zona",
                schema: "public");

            migrationBuilder.DropTable(
                name: "dispositivo",
                schema: "public");

            migrationBuilder.DropTable(
                name: "invitacion_envio",
                schema: "public");

            migrationBuilder.DropTable(
                name: "membresia_organizacion",
                schema: "public");

            migrationBuilder.DropTable(
                name: "membresia_punto_acceso",
                schema: "public");

            migrationBuilder.DropTable(
                name: "membresia_rol",
                schema: "public");

            migrationBuilder.DropTable(
                name: "membresia_sede",
                schema: "public");

            migrationBuilder.DropTable(
                name: "modulo",
                schema: "public");

            migrationBuilder.DropTable(
                name: "movimiento_acceso",
                schema: "public");

            migrationBuilder.DropTable(
                name: "movimiento_dispositivo",
                schema: "public");

            migrationBuilder.DropTable(
                name: "movimiento_vehiculo",
                schema: "public");

            migrationBuilder.DropTable(
                name: "notificacion",
                schema: "public");

            migrationBuilder.DropTable(
                name: "notificacion_destinatario",
                schema: "public");

            migrationBuilder.DropTable(
                name: "organizacion",
                schema: "public");

            migrationBuilder.DropTable(
                name: "organizacion_modulo",
                schema: "public");

            migrationBuilder.DropTable(
                name: "outbox",
                schema: "public");

            migrationBuilder.DropTable(
                name: "parametro_organizacion",
                schema: "public");

            migrationBuilder.DropTable(
                name: "permanencia",
                schema: "public");

            migrationBuilder.DropTable(
                name: "permiso",
                schema: "public");

            migrationBuilder.DropTable(
                name: "persona_telefono",
                schema: "public");

            migrationBuilder.DropTable(
                name: "persona_vehiculo",
                schema: "public");

            migrationBuilder.DropTable(
                name: "plan",
                schema: "public");

            migrationBuilder.DropTable(
                name: "plan_modulo",
                schema: "public");

            migrationBuilder.DropTable(
                name: "plantilla_notificacion",
                schema: "public");

            migrationBuilder.DropTable(
                name: "plantilla_rol",
                schema: "public");

            migrationBuilder.DropTable(
                name: "plantilla_rol_permiso",
                schema: "public");

            migrationBuilder.DropTable(
                name: "restriccion_acceso",
                schema: "public");

            migrationBuilder.DropTable(
                name: "rol",
                schema: "public");

            migrationBuilder.DropTable(
                name: "rol_permiso",
                schema: "public");

            migrationBuilder.DropTable(
                name: "solicitud_idempotente",
                schema: "public");

            migrationBuilder.DropTable(
                name: "tipo_dispositivo",
                schema: "public");

            migrationBuilder.DropTable(
                name: "tipo_organizacion",
                schema: "public");

            migrationBuilder.DropTable(
                name: "token_qr",
                schema: "public");

            migrationBuilder.DropTable(
                name: "usuario",
                schema: "public");

            migrationBuilder.DropTable(
                name: "vehiculo",
                schema: "public");

            migrationBuilder.DropTable(
                name: "visita",
                schema: "public");

            migrationBuilder.DropTable(
                name: "zona",
                schema: "public");

            migrationBuilder.DropTable(
                name: "area",
                schema: "public");

            migrationBuilder.DropTable(
                name: "persona",
                schema: "public");

            migrationBuilder.DropTable(
                name: "sede",
                schema: "public");

            migrationBuilder.DropTable(
                name: "tipo_documento",
                schema: "public");

            migrationBuilder.DropTable(
                name: "tipo_persona",
                schema: "public");

            migrationBuilder.DropTable(
                name: "ciudad",
                schema: "public");
        }
    }
}
