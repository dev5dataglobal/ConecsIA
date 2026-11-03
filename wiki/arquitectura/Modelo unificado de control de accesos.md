---
tipo: arquitectura
estado: propuesta
fuentes: [PRD v0.1, solicitud del usuario 2026-11-03]
relacionados: [ADR-029, ADR-026, D-49, D-50, D-51, D-52, D-53, D-54, D-55, D-40, D-41, D-42]
actualizado: 2026-11-03
tags: [conecsia, arquitectura, control-accesos]
---

# Modelo unificado de control de accesos

Entregable 1 de la unificación de [[Visitas]] y [[Control de accesos]]. Decisión rectora: [[ADR-029 Registro de acceso unificado]]. Análisis previo: [[Unificación de Visitas y Control de accesos - análisis inicial]]. **Todo es Propuesta** hasta que el usuario confirme este entregable.

## Principios
1. **Un registro de acceso = `autorizacion`.** No se crea `AccessRecord`: se amplía la tabla que ya tiene vigencia, cupo, consumo atómico, incompatibles y transiciones.
2. **`visita` se conserva como episodio** (D-49 resuelta 2026-11-03): ya no se crea desde una pantalla propia; nace al registrar una entrada (o al preanunciar) y siempre apunta a un registro de acceso.
3. **Un solo módulo** `control_accesos`: un menú, `/api/accesos/**`, un formulario. `/visitas`, `/autorizaciones` y `/api/visitas/**` se retiran tras la migración.
4. **Configuración por organización, no por código:** categorías de acceso con terminología, flujo y campos obligatorios; plantillas por tipo de organización.
5. **Objetos con decisión propia** (ADR-026 se mantiene): `autorizacion_objeto` cuelga del registro de acceso.
6. **Intentos fallidos** = `movimiento_acceso` con `resultado = 'Denegado'` y motivos; nunca se abre permanencia.

## Cambios por tabla

| Tabla | Cambio | Estado |
|---|---|---|
| `plantilla_categoria_acceso` | **Nueva** (plataforma): categorías sugeridas por `tipo_organizacion` | Propuesta |
| `categoria_acceso` | **Nueva** (por organización): `codigo`, `nombre` (término visible: Visitante, Invitado, Contratista, Estudiante…), `flujo_aprobacion` (Automatico/Anfitrion/Seguridad, D-52), `modalidades_permitidas text[]`, `exige_anfitrion`, `exige_motivo`, `permite_vehiculos`, `permite_dispositivos`, `activo` | Propuesta |
| `horario_base` | **Nueva**: franjas por día (`id_sede` nulo = organización); varias franjas por día | Propuesta |
| `organizacion` | + `zona_horaria` (por defecto `America/Bogota`); `sede.zona_horaria` pasa a sobrescritura opcional (D-50) | Propuesta |
| `autorizacion` | + `id_categoria_acceso`, `usa_horario_base`, `origen` (Manual/Politica/Migracion), `id_visita_origen`; estados persistidos Pendiente, Aprobada, Rechazada, Revocada, Cancelada (**Vigente/Vencida/Consumida derivadas**); CHECK de motivo en Rechazada/Revocada/Cancelada | Propuesta |
| `autorizacion_horario` | Se elimina la unicidad por día ⇒ varias franjas; CHECK de no solapamiento por exclusión (`btree_gist`) | Propuesta |
| `visita` | `id_autorizacion` obligatorio para filas nuevas (CHECK con fecha de corte); `id_punto_acceso_entrada`; columnas de excepción y política quedan solo como histórico | Propuesta |
| `movimiento_acceso` | `id_autorizacion` obligatorio en filas nuevas; `id_visita`/`id_permanencia` nulos en intentos denegados; + `id_movimiento_objeto[]` no: se usa `movimiento_vehiculo`/`movimiento_dispositivo` con FK al movimiento de la persona | Propuesta |
| `autorizacion_objeto` | `id_autorizacion` obligatorio en filas nuevas; la decisión hereda vigencia del registro salvo que se acote | Propuesta |
| `parqueadero_cupo` | **Nueva, opcional** (módulo `parqueaderos`): cupo por sede (D-54) | Propuesta |
| `auditoria` | + `agente_usuario` (dispositivo); sigue inmutable por trigger | Propuesta |
| `parametro_organizacion` | Claves nuevas: `accesos.fuera_horario` = `Bloquear`/`Alertar` (D-51), `accesos.permite_permanente` (renombra D-42) | Propuesta |

## Diagrama ER

```mermaid
erDiagram
    TIPO_ORGANIZACION ||--o{ ORGANIZACION : clasifica
    TIPO_ORGANIZACION ||--o{ PLANTILLA_CATEGORIA_ACCESO : sugiere
    ORGANIZACION ||--o{ CATEGORIA_ACCESO : configura
    ORGANIZACION ||--o{ SEDE : tiene
    ORGANIZACION ||--o{ HORARIO_BASE : "horario por defecto"
    SEDE ||--o{ HORARIO_BASE : sobrescribe
    SEDE ||--o{ AREA : contiene
    SEDE ||--o{ PUNTO_ACCESO : contiene
    ORGANIZACION ||--o{ PERSONA : registra
    PERSONA ||--o{ PERSONA_VEHICULO : usa
    VEHICULO ||--o{ PERSONA_VEHICULO : ""
    PERSONA ||--o{ DISPOSITIVO : responsable

    CATEGORIA_ACCESO ||--o{ AUTORIZACION : tipifica
    PERSONA ||--o{ AUTORIZACION : "registro de acceso"
    SEDE ||--o{ AUTORIZACION : ""
    AREA ||--o{ AUTORIZACION : destino
    MEMBRESIA_ORGANIZACION ||--o{ AUTORIZACION : "anfitrión"
    USUARIO ||--o{ AUTORIZACION : "crea / decide"
    AUTORIZACION ||--o{ AUTORIZACION_HORARIO : "franjas propias"
    AUTORIZACION ||--o{ AUTORIZACION_TRANSICION : "historial estados"
    AUTORIZACION ||--o{ AUTORIZACION_OBJETO : "vehículos y dispositivos"
    VEHICULO ||--o{ AUTORIZACION_OBJETO : ""
    DISPOSITIVO ||--o{ AUTORIZACION_OBJETO : ""
    AUTORIZACION_OBJETO ||--o{ AUTORIZACION_OBJETO_TRANSICION : "historial decisión"

    AUTORIZACION ||--o{ VISITA : "episodios"
    VISITA ||--o| PERMANENCIA : abre
    AUTORIZACION ||--o{ MOVIMIENTO_ACCESO : "entradas, salidas, intentos"
    PUNTO_ACCESO ||--o{ MOVIMIENTO_ACCESO : registra
    PERMANENCIA ||--|{ MOVIMIENTO_ACCESO : "entrada / salida"
    MOVIMIENTO_ACCESO ||--o{ MOVIMIENTO_VEHICULO : acompaña
    MOVIMIENTO_ACCESO ||--o{ MOVIMIENTO_DISPOSITIVO : acompaña
    SEDE ||--o| PARQUEADERO_CUPO : "opcional"
    ORGANIZACION ||--o{ AUDITORIA : "append-only"

    CATEGORIA_ACCESO {
        int id_categoria_acceso PK
        int id_organizacion FK
        string codigo
        string nombre "término visible"
        string flujo_aprobacion "Automatico|Anfitrion|Seguridad"
        string[] modalidades_permitidas
        bool exige_anfitrion
        bool permite_vehiculos
        bool permite_dispositivos
        bool activo
    }
    HORARIO_BASE {
        int id_horario_base PK
        int id_organizacion FK
        int id_sede FK "nulo = organización"
        int dia_semana "ISO 1-7"
        time hora_inicio
        time hora_fin
    }
    AUTORIZACION {
        int id_autorizacion PK
        int id_organizacion FK
        int id_categoria_acceso FK
        int id_persona FK
        int id_sede FK
        int id_area FK
        int id_membresia_responsable FK
        string modalidad "Unica|Diaria|Temporal|Recurrente|Permanente"
        date fecha_inicio
        date fecha_fin
        bool usa_horario_base
        int cupo_ingresos
        int ingresos_consumidos
        string motivo
        string estado "Pendiente|Aprobada|Rechazada|Revocada|Cancelada"
        int id_usuario_crea FK
        int id_usuario_decide FK
        string motivo_decision
        string origen "Manual|Politica|Migracion"
        bigint version
    }
    AUTORIZACION_OBJETO {
        int id_autorizacion_objeto PK
        int id_autorizacion FK
        string tipo_objeto "Vehiculo|Dispositivo"
        string estado "Pendiente|Aprobada|Rechazada|Revocada"
        int id_usuario_decide FK
        timestamptz decidido_en
        string motivo "obligatorio al rechazar"
    }
    VISITA {
        int id_visita PK
        int id_autorizacion FK "obligatorio"
        string estado
    }
    MOVIMIENTO_ACCESO {
        bigint id_movimiento PK
        int id_autorizacion FK
        int id_punto_acceso FK
        int id_usuario_registra FK
        string tipo_movimiento "Entrada|Salida"
        string resultado "Confirmado|Denegado|Alerta"
        string motivo_denegacion
        timestamptz ocurrido_en
    }
```

## Estados

**Registro de acceso (persistido):** `Pendiente → Aprobada | Rechazada | Cancelada`; `Aprobada → Revocada | Cancelada`. Motivo obligatorio en Rechazada, Revocada y Cancelada. Flujo `Automatico` ⇒ nace Aprobada con `origen = Politica` y sin decisor humano.

**Vigencia (derivada en cada intento, hora local con zona sede ?? organización):** `NoIniciada`, `Vigente`, `FueraDeHorario`, `Vencida`, `Consumida`. Solo Aprobada + Vigente habilita la entrada. Se muestra en listados con el mismo cálculo del servidor.

**Objeto:** ciclo ADR-026 sin cambios. La entrada de la persona no exige que sus objetos estén aprobados; el objeto se registra en el movimiento solo si está Aprobado y vigente (D-53, Propuesta).

## Configuración por tipo de organización (Propuesta)

| Tipo | Categorías sugeridas (término visible) | Flujo por defecto |
|---|---|---|
| Empresa | Visitante, Contratista, Proveedor | Anfitrión |
| Conjunto residencial | Visitante, Domiciliario, Trabajador | Anfitrión (residente) |
| Club | Invitado, Socio, Proveedor | Seguridad |
| Universidad | Visitante, Estudiante externo, Contratista | Seguridad |
| Coworking | Invitado, Miembro, Proveedor | Automático |
| Parque industrial | Contratista, Transportador, Visitante | Seguridad |

Al crear la organización se copian las plantillas; la organización las edita sin tocar el núcleo.

## Endpoints previstos (`/api/accesos`)
`GET /` (filtros + paginación) · `GET /{id}` · `GET /opciones` · `POST /` (persona + destino + categoría + permanencia + horario + objetos) · `POST /{id}/aprobar|rechazar|revocar|cancelar` · `POST /{id}/objetos/{idObjeto}/aprobar|rechazar|revocar` · `POST /{id}/entrada`, `POST /permanencias/{id}/salida` (Idempotency-Key) · `GET /presencia` · `GET /historial` + `GET /historial/exportar?formato=csv`.

## Migración de datos (resumen; detalle en Fase 2)
1. Crear `categoria_acceso` por organización desde plantillas; categoría «Visitante» y «Acceso recurrente» para lo existente.
2. `autorizacion` existentes ⇒ categoría según modalidad (Recurrente/Permanente ⇒ «Acceso recurrente»; resto ⇒ «Visitante»), `origen = Manual`.
3. `visita` sin autorización ⇒ crear `autorizacion` Única con la sede, destino, responsable y fecha de la visita, `origen = Migracion`; estado: política aplicada ⇒ Aprobada sin decisor; Pendiente ⇒ Pendiente; Rechazada/Cancelada/Vencida ⇒ Rechazada/Cancelada con motivo «Migrado desde visita #id». Se enlaza la visita y su transición inicial.
4. `movimiento_acceso.id_autorizacion` ⇐ `visita.id_autorizacion` cuando falte.
5. `autorizacion_objeto` sin `id_autorizacion` se conservan como históricos (nulo permitido antes de la fecha de corte).
6. Bitácora `public.migracion_accesos_adr029` con cada fila creada o no trasladable; auditoría `Migrar` por registro. No se borra ninguna tabla ni columna.
7. Verificación: conteos antes/después de visitas, movimientos y permanencias iguales; toda visita con autorización.

## Fase 2: migración implementada (2026-11-03)

Migración `20261103163234_RegistroAccesoUnificado` (rama `feature/organizaciones`). **Implementado y verificado sobre copia desechable; no aplicada a `conecsia`.**

- **Esquema:** tablas `plantilla_categoria_acceso`, `categoria_acceso`, `horario_base`; `organizacion.zona_horaria` (por defecto `America/Bogota`); `autorizacion.id_categoria_acceso`, `usa_horario_base`, `origen` (CHECK), `id_visita_origen` (FK a visita); `auditoria.agente_usuario` + índices de historial; FK `movimiento_acceso → autorizacion`; `autorizacion_horario` admite varias franjas por día.
- **Datos:** 6 tipos de organización (se crean los que faltan), 19 plantillas, categorías copiadas a cada organización (+ VISITANTE y RECURRENTE garantizadas); autorizaciones existentes categorizadas por modalidad; cada visita sin autorización ⇒ autorización Única `origen = Migracion` con transición, vínculo en la visita y sus movimientos, auditoría `Migrar` y fila en `migracion_accesos_adr029`; `accesos.permite_permanente` copia la clave anterior.
- **Diferido a Fase 3 (con el código):** `id_categoria_acceso` NOT NULL, `visita.id_autorizacion` obligatorio, CHECK de motivo en Rechazada/Revocada/Cancelada, `parqueadero_cupo` (D-54), retiro de rutas de menú. La zona horaria de la sede se conserva como está (obligatoria); la de la organización se usará como valor por defecto al crear sedes (D-50).
- **Verificación:** copia de la base Docker: 0 visitas sin autorización, bitácora y auditoría correctas; Down → Up reversible (la auditoría inmutable se conserva); `dotnet test` **104/104**.
