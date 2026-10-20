erDiagram

    %% =====================================================
    %% Modelo ER — versión 2 (2026-10-17)
    %% Ver wiki/consultas/Revisión del modelo ER db.md
    %% Convenciones:
    %%  - Toda entidad de cliente lleva id_organizacion NOT NULL y FKs
    %%    compuestas (id_organizacion, id) (ADR-004). Excepciones cerradas:
    %%    plano SaaS, catálogos globales y búsqueda por hash de TOKEN_QR.
    %%  - Instantes en timestamptz (UTC); horarios en hora local + zona
    %%    horaria de la sede (ADR-008).
    %%  - "activo" = inactivación lógica; "estado" = ciclo de vida (ADR-009).
    %%  - La vigencia de una autorización se calcula, no se persiste.
    %%  - Punto de acceso = ZONA con es_punto_acceso (ADR-019, propuesta).
    %%  - Nombres de columnas (id_organizacion vs organization_id): pendiente
    %%    fijar convención antes de EF Core.
    %% =====================================================


    %% =====================================================
    %% PLANO SAAS (sin id_organizacion, salvo vínculos)
    %% =====================================================

    TIPO_ORGANIZACION {
        int id_tipo_organizacion PK
        string nombre
        string descripcion
        boolean activo
    }

    PLAN {
        int id_plan PK
        string nombre
        string descripcion
        boolean activo
    }

    MODULO {
        int id_modulo PK
        string codigo "personas, vehiculos, dispositivos, correo..."
        string nombre
        boolean activo
    }

    PLAN_MODULO {
        int id_plan FK
        int id_modulo FK
    }

    ORGANIZACION {
        int id_organizacion PK
        int id_tipo_organizacion FK
        int id_plan FK
        uuid id_archivo_logo FK "nullable"
        string nombre
        string identificacion
        string direccion
        string telefono
        string correo
        string estado "Activa | Inactiva (efectos D-16)"
        timestamptz creado_en
    }

    ORGANIZACION_MODULO {
        int id_organizacion FK
        int id_modulo FK
        boolean habilitado
        timestamptz cambiado_en
        int id_usuario_cambia FK
    }

    PARAMETRO_ORGANIZACION {
        int id_organizacion FK
        string clave "umbral_vencimiento, permanencia_prolongada... (D-19)"
        string valor
        timestamptz actualizado_en
    }

    CIUDAD {
        int id_ciudad PK
        string nombre
        string departamento
        string pais
        boolean activo
    }


    %% =====================================================
    %% ESTRUCTURA FÍSICA
    %% =====================================================

    SEDE {
        int id_sede PK
        int id_organizacion FK
        int id_ciudad FK
        string nombre
        string direccion
        string telefono
        string correo
        string zona_horaria "IANA, obligatoria (ADR-008)"
        int id_membresia_responsable FK "nullable"
        boolean activo
    }

    AREA {
        int id_area PK
        int id_organizacion FK
        int id_sede FK
        int id_area_padre FK "nullable, jerarquía por definir (D-11)"
        string nombre
        string descripcion
        boolean activo
    }

    AREA_RESPONSABLE {
        int id_organizacion FK
        int id_area FK
        int id_membresia FK
        boolean es_autorizador
        boolean activo
    }

    ZONA {
        int id_zona PK
        int id_organizacion FK
        int id_sede FK
        int id_zona_padre FK "nullable"
        string nombre
        string descripcion
        boolean es_punto_acceso
        string modo_acceso "Entrada | Salida | Ambos; CHECK solo si es_punto_acceso"
        boolean activo
    }


    %% =====================================================
    %% IDENTIDAD, MEMBRESÍAS, ROLES Y PERMISOS (D-03)
    %% =====================================================

    USUARIO {
        int id_usuario PK
        string correo
        string correo_normalizado UK
        string password_hash
        string sello_seguridad
        int intentos_fallidos
        timestamptz bloqueado_hasta
        boolean activo
        timestamptz ultimo_acceso
        timestamptz creado_en
    }

    MEMBRESIA_ORGANIZACION {
        int id_membresia PK
        int id_organizacion FK
        int id_usuario FK
        int id_persona FK "nullable, vínculo PersonaUsuario"
        string estado "Invitada | Activa | Inactiva"
        timestamptz creado_en
    }

    PERMISO {
        int id_permiso PK
        string codigo UK "catálogo global"
        string descripcion
        boolean activo
    }

    PLANTILLA_ROL {
        int id_plantilla_rol PK
        string nombre "Operador, Autorizador, Auditor..."
    }

    PLANTILLA_ROL_PERMISO {
        int id_plantilla_rol FK
        int id_permiso FK
    }

    ROL {
        int id_rol PK
        int id_organizacion FK
        int id_plantilla_rol FK "nullable"
        string nombre
        string descripcion
        boolean activo
    }

    ROL_PERMISO {
        int id_organizacion FK
        int id_rol FK
        int id_permiso FK
    }

    MEMBRESIA_ROL {
        int id_organizacion FK
        int id_membresia FK
        int id_rol FK
    }

    MEMBRESIA_SEDE {
        int id_organizacion FK
        int id_membresia FK
        int id_sede FK
    }

    MEMBRESIA_PUNTO_ACCESO {
        int id_organizacion FK
        int id_membresia FK
        int id_zona FK "zona con es_punto_acceso"
    }


    %% =====================================================
    %% ARCHIVOS (ADR-013)
    %% =====================================================

    ARCHIVO {
        uuid id_archivo PK
        int id_organizacion FK "nullable solo para plano SaaS"
        string clave_almacenamiento "prefijo por organización"
        string tipo_contenido
        bigint tamano_bytes
        string hash_sha256
        int id_usuario_carga FK
        timestamptz creado_en
    }


    %% =====================================================
    %% PERSONAS
    %% =====================================================

    TIPO_DOCUMENTO {
        int id_tipo_documento PK
        string codigo
        string nombre
        string pais_emisor "D-09"
        boolean activo
    }

    TIPO_PERSONA {
        int id_tipo_persona PK
        int id_organizacion FK
        string nombre
        string descripcion
        boolean activo
    }

    PERSONA {
        int id_persona PK
        int id_organizacion FK
        int id_tipo_persona FK
        int id_tipo_documento FK
        string numero_documento
        string numero_documento_normalizado "UK(id_organizacion, id_tipo_documento, numero_documento_normalizado)"
        string nombres
        string apellidos
        string correo
        string empresa_procedencia
        uuid id_archivo_foto FK "nullable"
        string observacion
        boolean activo
        timestamptz creado_en
        int id_usuario_crea FK
    }

    PERSONA_TELEFONO {
        int id_telefono PK
        int id_organizacion FK
        int id_persona FK
        string numero
        string tipo
        boolean principal
        boolean activo
    }

    RESTRICCION_ACCESO {
        int id_restriccion PK
        int id_organizacion FK
        int id_persona FK
        int id_sede FK "nullable = toda la organización"
        int id_zona FK "nullable"
        string tipo_restriccion
        string motivo
        timestamptz fecha_inicio
        timestamptz fecha_fin "nullable"
        string estado "Activa | Levantada"
        int id_usuario_registra FK
        timestamptz creado_en
        int id_usuario_levanta FK "nullable"
        timestamptz levantada_en
    }


    %% =====================================================
    %% VEHÍCULOS (módulo opcional; D-10, D-17)
    %% =====================================================

    VEHICULO {
        int id_vehiculo PK
        int id_organizacion FK
        string placa
        string placa_normalizada "UK por organización (D-17)"
        string tipo
        string marca
        string modelo
        string color
        boolean activo
    }

    PERSONA_VEHICULO {
        int id_organizacion FK
        int id_persona FK
        int id_vehiculo FK
        string relacion "Propietario | Responsable"
        boolean activo
    }


    %% =====================================================
    %% DISPOSITIVOS (módulo opcional; D-10, D-17)
    %% =====================================================

    TIPO_DISPOSITIVO {
        int id_tipo_dispositivo PK
        int id_organizacion FK
        string nombre
        string descripcion
        boolean activo
    }

    DISPOSITIVO {
        int id_dispositivo PK
        int id_organizacion FK
        int id_persona_responsable FK
        int id_tipo_dispositivo FK
        string codigo_identificador
        string serial
        string serial_normalizado "unicidad por definir (D-17)"
        string marca
        string modelo
        string color
        string descripcion
        boolean activo
        timestamptz creado_en
    }


    %% =====================================================
    %% AUTORIZACIONES (ADR-007, ADR-009, ADR-010)
    %% =====================================================

    AUTORIZACION {
        int id_autorizacion PK
        int id_organizacion FK
        int id_persona FK
        int id_sede FK "una sede por autorización (PRD 6.2); confirmar"
        int id_area FK "destino"
        int id_membresia_responsable FK
        int id_usuario_crea FK
        string modalidad "Unica | Diaria | Temporal | Recurrente | Permanente"
        date fecha_inicio
        date fecha_fin "nullable solo Permanente (D-18)"
        int cupo_ingresos "nullable = sin límite; D-05"
        int ingresos_consumidos
        string motivo
        string observacion
        string estado "Pendiente | Aprobada | Rechazada | Cancelada | Revocada | Vencida | Agotada"
        bigint version "concurrencia optimista"
        timestamptz creado_en
    }

    AUTORIZACION_TRANSICION {
        bigint id_transicion PK
        int id_organizacion FK
        int id_autorizacion FK
        string estado_anterior
        string estado_nuevo "Aprobada, Rechazada, Revocada..."
        int id_usuario_actor FK "nullable si proceso del sistema"
        string observacion
        timestamptz ocurrido_en
    }

    AUTORIZACION_HORARIO {
        int id_autorizacion_horario PK
        int id_organizacion FK
        int id_autorizacion FK
        int dia_semana "1-7 ISO"
        time hora_inicio "local de la sede"
        time hora_fin "hora_fin > hora_inicio hasta D-06"
    }

    AUTORIZACION_ZONA {
        int id_organizacion FK
        int id_autorizacion FK
        int id_zona FK "debe pertenecer a la sede de la autorización"
    }

    AUTORIZACION_VEHICULO {
        int id_organizacion FK
        int id_autorizacion FK
        int id_vehiculo FK
    }

    AUTORIZACION_DISPOSITIVO {
        int id_organizacion FK
        int id_autorizacion FK
        int id_dispositivo FK
    }

    TOKEN_QR {
        int id_token PK
        int id_organizacion FK
        int id_autorizacion FK
        string hash_sha256 UK "único global; nunca el token en claro"
        string estado "Activo | Rotado | Revocado"
        timestamptz emitido_en
        timestamptz expira_en "nullable; política pendiente"
        timestamptz revocado_en
    }

    INVITACION_ENVIO {
        int id_envio PK
        int id_organizacion FK
        int id_autorizacion FK "la invitación es una autorización preaprobada"
        int id_token FK
        int id_usuario_envia FK
        string canal "Correo"
        string destino
        timestamptz enviado_en
    }


    %% =====================================================
    %% OPERACIÓN: VISITA → PERMANENCIA → MOVIMIENTO
    %% =====================================================

    VISITA {
        int id_visita PK
        int id_organizacion FK
        int id_persona FK
        int id_autorizacion FK "nullable según política"
        int id_sede FK
        int id_area FK
        int id_membresia_responsable FK
        int id_usuario_registra FK
        string politica_aplicada "nullable; ingreso sin autorización (PRD 6.3)"
        string motivo
        string observacion
        string estado "Pendiente | Autorizada | Ingreso | Finalizada | Rechazada | Cancelada | Vencida"
        bigint version
        timestamptz creado_en
    }

    PERMANENCIA {
        int id_permanencia PK
        int id_organizacion FK
        int id_persona FK
        int id_sede FK
        int id_visita FK
        bigint id_movimiento_entrada FK
        bigint id_movimiento_salida FK "nullable"
        string estado "Abierta | Cerrada; UK parcial (org, persona, sede) WHERE Abierta (D-07)"
        timestamptz abierta_en
        timestamptz cerrada_en
    }

    MOVIMIENTO_ACCESO {
        bigint id_movimiento PK
        int id_organizacion FK
        int id_persona FK
        int id_sede FK
        int id_zona_punto_acceso FK "ZONA con es_punto_acceso"
        int id_visita FK "nullable en intentos denegados"
        int id_autorizacion FK "nullable"
        int id_permanencia FK "nullable en intentos denegados"
        int id_usuario_registra FK
        string tipo_movimiento "Entrada | Salida"
        string resultado "Confirmado | Denegado"
        string motivo_denegacion
        string medio "Documento | QR"
        bigint id_movimiento_corrige FK "nullable; regularización D-08"
        timestamptz ocurrido_en
        string observacion
    }

    MOVIMIENTO_VEHICULO {
        bigint id_movimiento_vehiculo PK
        int id_organizacion FK
        int id_vehiculo FK
        bigint id_movimiento FK "movimiento de la persona responsable"
        int id_visita FK
        string tipo_movimiento "Entrada | Salida"
        timestamptz ocurrido_en
    }

    MOVIMIENTO_DISPOSITIVO {
        bigint id_movimiento_dispositivo PK
        int id_organizacion FK
        int id_dispositivo FK
        int id_sede FK
        int id_zona_punto_acceso FK
        int id_persona_responsable FK
        int id_visita FK
        int id_usuario_registra FK
        string tipo_movimiento "Entrada | Salida; no se cierra al salir la persona"
        timestamptz ocurrido_en
        string observacion
    }

    SOLICITUD_IDEMPOTENTE {
        int id_organizacion FK
        string clave_idempotencia "PK compuesta (org, clave)"
        string comando "RegistrarEntrada, RegistrarSalida..."
        string hash_solicitud
        int codigo_resultado
        text respuesta
        timestamptz creado_en
    }


    %% =====================================================
    %% ALERTAS (D-19)
    %% =====================================================

    ALERTA {
        int id_alerta PK
        int id_organizacion FK
        int id_sede FK
        string tipo_alerta
        int id_persona FK "nullable"
        int id_visita FK "nullable"
        int id_autorizacion FK "nullable"
        bigint id_movimiento FK "nullable"
        string clave_deduplicacion "UK por organización"
        string descripcion
        string estado "Nueva | Revisada | Cerrada"
        timestamptz generada_en
    }

    ALERTA_TRANSICION {
        bigint id_transicion PK
        int id_organizacion FK
        int id_alerta FK
        string estado_anterior
        string estado_nuevo
        int id_usuario_actor FK
        string observacion
        timestamptz ocurrido_en
    }


    %% =====================================================
    %% NOTIFICACIONES Y OUTBOX (ADR-012, D-12)
    %% =====================================================

    PLANTILLA_NOTIFICACION {
        int id_plantilla PK
        int id_organizacion FK
        string evento
        string canal
        string asunto
        text cuerpo
        boolean activo
    }

    NOTIFICACION {
        int id_notificacion PK
        int id_organizacion FK
        int id_plantilla FK "nullable"
        string evento
        string titulo
        string mensaje
        timestamptz creado_en
    }

    NOTIFICACION_DESTINATARIO {
        int id_destinatario PK
        int id_organizacion FK
        int id_notificacion FK
        int id_usuario FK "nullable"
        int id_persona FK "nullable; visitante sin usuario"
        string canal "Aplicacion | Correo"
        string direccion "correo destino"
        string estado "Pendiente | Enviada | Fallida | Leida"
        int intentos
        string ultimo_error
        timestamptz enviado_en
        timestamptz leido_en
    }

    OUTBOX {
        bigint id_mensaje PK
        int id_organizacion FK
        string tipo
        text carga
        string estado "Pendiente | Procesado | Fallido"
        int intentos
        timestamptz disponible_en
        timestamptz procesado_en
    }


    %% =====================================================
    %% AUDITORÍA (ADR-014, append-only)
    %% =====================================================

    AUDITORIA {
        bigint id_auditoria PK
        int id_organizacion FK "nullable para plano SaaS"
        string tipo_actor "Usuario | Sistema | Soporte"
        int id_usuario FK "nullable"
        string accion
        string entidad
        string id_registro
        string resultado "Exito | Denegado | Error"
        text valor_anterior
        text valor_nuevo
        string direccion_ip
        string id_correlacion
        timestamptz ocurrido_en
    }


    %% =====================================================
    %% RELACIONES - SAAS Y ORGANIZACIÓN
    %% =====================================================

    TIPO_ORGANIZACION ||--o{ ORGANIZACION : clasifica
    PLAN ||--o{ ORGANIZACION : asignado
    PLAN ||--o{ PLAN_MODULO : incluye
    MODULO ||--o{ PLAN_MODULO : incluido
    ORGANIZACION ||--o{ ORGANIZACION_MODULO : habilita
    MODULO ||--o{ ORGANIZACION_MODULO : habilitado
    ORGANIZACION ||--o{ PARAMETRO_ORGANIZACION : configura
    ORGANIZACION ||--o{ ARCHIVO : almacena

    ORGANIZACION ||--o{ SEDE : posee
    CIUDAD ||--o{ SEDE : contiene
    SEDE ||--o{ AREA : contiene
    AREA |o--o{ AREA : contiene
    AREA ||--o{ AREA_RESPONSABLE : asigna
    SEDE ||--o{ ZONA : contiene
    ZONA |o--o{ ZONA : contiene


    %% =====================================================
    %% RELACIONES - IDENTIDAD
    %% =====================================================

    USUARIO ||--o{ MEMBRESIA_ORGANIZACION : pertenece
    ORGANIZACION ||--o{ MEMBRESIA_ORGANIZACION : agrupa
    PERSONA |o--o| MEMBRESIA_ORGANIZACION : vinculada
    ORGANIZACION ||--o{ ROL : configura
    PLANTILLA_ROL |o--o{ ROL : origina
    PLANTILLA_ROL ||--o{ PLANTILLA_ROL_PERMISO : contiene
    PERMISO ||--o{ PLANTILLA_ROL_PERMISO : incluido
    ROL ||--o{ ROL_PERMISO : contiene
    PERMISO ||--o{ ROL_PERMISO : asignado
    MEMBRESIA_ORGANIZACION ||--o{ MEMBRESIA_ROL : posee
    ROL ||--o{ MEMBRESIA_ROL : asignado
    MEMBRESIA_ORGANIZACION ||--o{ MEMBRESIA_SEDE : alcance
    SEDE ||--o{ MEMBRESIA_SEDE : permite
    MEMBRESIA_ORGANIZACION ||--o{ MEMBRESIA_PUNTO_ACCESO : opera
    ZONA ||--o{ MEMBRESIA_PUNTO_ACCESO : operada
    MEMBRESIA_ORGANIZACION ||--o{ AREA_RESPONSABLE : responsable


    %% =====================================================
    %% RELACIONES - PERSONAS, VEHÍCULOS Y DISPOSITIVOS
    %% =====================================================

    ORGANIZACION ||--o{ TIPO_PERSONA : configura
    TIPO_PERSONA ||--o{ PERSONA : clasifica
    TIPO_DOCUMENTO ||--o{ PERSONA : identifica
    ORGANIZACION ||--o{ PERSONA : registra
    ARCHIVO |o--o{ PERSONA : foto
    PERSONA ||--o{ PERSONA_TELEFONO : posee
    PERSONA ||--o{ RESTRICCION_ACCESO : posee

    ORGANIZACION ||--o{ VEHICULO : registra
    PERSONA ||--o{ PERSONA_VEHICULO : asociada
    VEHICULO ||--o{ PERSONA_VEHICULO : asociado

    ORGANIZACION ||--o{ TIPO_DISPOSITIVO : configura
    TIPO_DISPOSITIVO ||--o{ DISPOSITIVO : clasifica
    PERSONA ||--o{ DISPOSITIVO : responsable


    %% =====================================================
    %% RELACIONES - AUTORIZACIONES
    %% =====================================================

    PERSONA ||--o{ AUTORIZACION : recibe
    SEDE ||--o{ AUTORIZACION : habilita
    AREA ||--o{ AUTORIZACION : destino
    AUTORIZACION ||--o{ AUTORIZACION_TRANSICION : historial
    AUTORIZACION ||--o{ AUTORIZACION_HORARIO : define
    AUTORIZACION ||--o{ AUTORIZACION_ZONA : habilita
    ZONA ||--o{ AUTORIZACION_ZONA : permitida
    AUTORIZACION ||--o{ AUTORIZACION_VEHICULO : incluye
    VEHICULO ||--o{ AUTORIZACION_VEHICULO : incluido
    AUTORIZACION ||--o{ AUTORIZACION_DISPOSITIVO : incluye
    DISPOSITIVO ||--o{ AUTORIZACION_DISPOSITIVO : incluido
    AUTORIZACION ||--o{ TOKEN_QR : identifica
    AUTORIZACION ||--o{ INVITACION_ENVIO : notifica
    TOKEN_QR ||--o{ INVITACION_ENVIO : contiene


    %% =====================================================
    %% RELACIONES - OPERACIÓN
    %% =====================================================

    PERSONA ||--o{ VISITA : realiza
    AUTORIZACION |o--o{ VISITA : respalda
    SEDE ||--o{ VISITA : ocurre
    AREA ||--o{ VISITA : destino

    VISITA ||--o{ PERMANENCIA : episodio
    PERSONA ||--o{ PERMANENCIA : permanece
    SEDE ||--o{ PERMANENCIA : en

    PERSONA ||--o{ MOVIMIENTO_ACCESO : realiza
    ZONA ||--o{ MOVIMIENTO_ACCESO : punto_acceso
    VISITA |o--o{ MOVIMIENTO_ACCESO : genera
    PERMANENCIA |o--o{ MOVIMIENTO_ACCESO : abre_cierra
    USUARIO ||--o{ MOVIMIENTO_ACCESO : registra
    MOVIMIENTO_ACCESO |o--o{ MOVIMIENTO_ACCESO : corrige

    VEHICULO ||--o{ MOVIMIENTO_VEHICULO : utilizado
    MOVIMIENTO_ACCESO ||--o{ MOVIMIENTO_VEHICULO : acompana
    DISPOSITIVO ||--o{ MOVIMIENTO_DISPOSITIVO : registra
    ZONA ||--o{ MOVIMIENTO_DISPOSITIVO : punto_acceso
    VISITA |o--o{ MOVIMIENTO_DISPOSITIVO : asociada

    ORGANIZACION ||--o{ SOLICITUD_IDEMPOTENTE : registra


    %% =====================================================
    %% RELACIONES - ALERTAS, NOTIFICACIONES, AUDITORÍA
    %% =====================================================

    ORGANIZACION ||--o{ ALERTA : genera
    SEDE ||--o{ ALERTA : ocurre
    PERSONA |o--o{ ALERTA : relacionada
    VISITA |o--o{ ALERTA : relacionada
    AUTORIZACION |o--o{ ALERTA : relacionada
    MOVIMIENTO_ACCESO |o--o{ ALERTA : origina
    ALERTA ||--o{ ALERTA_TRANSICION : historial

    ORGANIZACION ||--o{ PLANTILLA_NOTIFICACION : configura
    PLANTILLA_NOTIFICACION |o--o{ NOTIFICACION : usa
    NOTIFICACION ||--o{ NOTIFICACION_DESTINATARIO : envia
    USUARIO |o--o{ NOTIFICACION_DESTINATARIO : recibe
    PERSONA |o--o{ NOTIFICACION_DESTINATARIO : recibe
    ORGANIZACION ||--o{ OUTBOX : encola

    ORGANIZACION |o--o{ AUDITORIA : registra
    USUARIO |o--o{ AUDITORIA : ejecuta