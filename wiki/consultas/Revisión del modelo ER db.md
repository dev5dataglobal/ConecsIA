---
tipo: consulta
estado: propuesta
# db.md reescrito 2026-10-17 (v2) aplicando estos hallazgos
fuentes: [PRD v0.1, db.md]
relacionados: [ADR-004, ADR-007, ADR-008, ADR-009, ADR-010, ADR-013, ADR-014, ADR-019, D-03, D-05, D-07, D-09, D-17]
actualizado: 2026-10-17
tags: [conecsia, modelo-datos]
---

# Revisión del modelo ER db.md

> [!note] Revisión del diagrama Mermaid `db.md` (raíz del repo) contra [[PRD v0.1]], [[ADR-004 Tenancy con base compartida y organization_id]] (aprobado), [[ADR-014 Auditoría append-only]] (aprobado) y ADR propuestos. Todo lo listado es **Propuesta** de adaptación; luego aplicados en `db.md` v2.

> [!note] 2026-10-17: `db.md` reescrito (v2) aplicando A–J, con ZONA como punto de acceso (ADR-019, aún Propuesta). Las decisiones pendientes quedan anotadas en el propio diagrama.

## Hallazgos (qué adaptar y por qué)

### A. Errores del diagrama
1. `SEDE` está definida **dos veces** (l.35 y l.54, con atributos distintos) → dejar una sola.
2. `PUNTO_ACCESO` se usa en relaciones (l.397, 399, 488) pero **no está definida**, y contradice `ZONA.es_punto_acceso` (ADR-019 propone Zona única). Elegir uno: o se elimina `PUNTO_ACCESO` y `MOVIMIENTO_ACCESO.id_zona_punto_acceso` apunta a ZONA, o se define la entidad y se registra la alternativa en ADR-019.
3. Relación `SEDE ||--o{ ZONA` duplicada (l.72 y l.396).
4. `AUTORIZACION_DISPOSITIVO` y relaciones de dispositivo declaradas antes de definir `AUTORIZACION` (funciona en Mermaid, pero ordenar por legibilidad); faltan `ORGANIZACION ||--o{ DISPOSITIVO/TIPO_DISPOSITIVO`.

### B. Aislamiento multi-tenant (ADR-004 aprobado)
5. Falta `id_organizacion NOT NULL` en entidades de cliente: `SEDE` ya lo tiene, pero no `AREA`, `ZONA`, `PERSONA_TELEFONO`, `VEHICULO`, `USUARIO_ROL`, `USUARIO_SEDE`, `AUTORIZACION_HORARIO/SEDE/ZONA/DISPOSITIVO`, `MOVIMIENTO_ACCESO`, `NOTIFICACION_DESTINATARIO`.
6. FKs compuestas `(id_organizacion, id)` y únicos por organización (p. ej. `UNIQUE(id_organizacion, tipo_documento, numero_documento_normalizado)` en PERSONA — D-09 pendiente).
7. `CIUDAD`, `TIPO_ORGANIZACION`, `PERMISO`, `TIPO_AUTORIZACION` sin organización: válido solo si están en la **lista cerrada de excepciones** del ADR-004 (hoy solo plano SaaS y hash QR). `TIPO_AUTORIZACION` = modalidad fija (enum) → OK como catálogo global; confirmar. `ROL` por organización OK, pero plantillas de perfiles iniciales necesitan origen global.

### C. Plano SaaS faltante (PRD §12.3)
8. Faltan `PLAN`, `MODULO`, `ORGANIZACION_MODULO`, `CONFIGURACION/PARAMETRO` (umbrales de alertas, políticas). Vehículos/dispositivos dependen de habilitación por módulo.

### D. Identidad (ADR-005/006, D-03)
9. `USUARIO.id_persona` acopla usuario→persona; Persona es por organización pero el usuario puede tener varias membresías → agregar `MEMBRESIA_ORGANIZACION (usuario, organizacion, estado)` y colgar `USUARIO_ROL`/`USUARIO_SEDE` (alcance) de la membresía. Vínculo `PERSONA_USUARIO` opcional por organización.
10. Falta alcance a destinos (autorizador por `AREA`) y operador por punto de acceso: `MEMBRESIA_AREA`, `MEMBRESIA_PUNTO_ACCESO`.
11. `USUARIO` sin correo/normalizado, bloqueo, sello de seguridad; depende de D-03 (si ASP.NET Identity, sus tablas reemplazan parte).

### E. Estructura física
12. `SEDE` sin `zona_horaria` IANA (ADR-008) ni responsable; `AREA` sin responsables/autorizadores (`AREA_RESPONSABLE`) ni jerarquía opcional.
13. `ZONA.tipo_acceso` → enum `Entrada|Salida|Ambos`, con check: solo si `es_punto_acceso`.

### F. Personas
14. `tipo_documento` como string → catálogo `TIPO_DOCUMENTO` (+ país emisor, D-09); añadir `numero_documento_normalizado`, `empresa_procedencia`.
15. `fotografia string` → FK a `ARCHIVO` (metadatos privados, ADR-013); igual para logo de organización.
16. `VEHICULO.id_persona` (1 dueño) → `PERSONA_VEHICULO` N:M + `id_organizacion`, `placa_normalizada` (D-17).

### G. Autorizaciones (ADR-009, ADR-007, ADR-010)
17. Faltan: `id_area` destino, `id_persona_responsable`, `id_usuario_creador` (≠ autorizador), `cupo_ingresos` y `usos_consumidos` (separar modalidad de cupo, D-05), `row_version` (concurrencia optimista).
18. Decisión/revocación como historial: `AUTORIZACION_DECISION` (actor, decisión, instante, observación) o `AUTORIZACION_TRANSICION`; hoy solo `id_usuario_autoriza` + `estado`.
19. `estado` como enum cerrado: Pendiente, Aprobada, Rechazada, Cancelada, Revocada, Vencida, Agotada. **No persistir "vigente"**.
20. `fecha_inicio/fin date` + `hora_inicio/fin` duplicados con `AUTORIZACION_HORARIO` → dejar fechas en autorización y horarios solo en la tabla hija; `fecha_fin` nullable para Permanente (D-18). Horarios en hora local + TZ de sede; rechazar cruces de medianoche (D-06).
21. `AUTORIZACION_SEDE` N:M vs PRD "una sede por autorización" → confirmar; zonas deben pertenecer a la sede (FK compuesta).
22. Falta `TOKEN_QR` (hash SHA-256 único global, estado, emisión/rotación, expiración). Hoy el token está en `INVITACION.token_qr` en claro → mover y guardar solo hash.
23. Falta `AUTORIZACION_VEHICULO` (PRD §8.1).

### H. Invitación / Visita / Permanencia / Movimiento
24. `INVITACION` duplica autorización (sede, área, fechas, QR). Proponer: invitación = autorización preaprobada + envío (o tabla de envío ligada a autorización). Decidir.
25. Falta **`PERMANENCIA`** (Abierta/Cerrada, movimiento de entrada, movimiento de salida, instantes). Índice único parcial `(id_organizacion, id_persona, id_sede) WHERE estado='Abierta'` (D-07).
26. `VISITA`: estado enum (Pendiente…Vencida), `id_usuario_registra`, `politica_aplicada` (ingreso sin autorización, PRD §6.3).
27. `MOVIMIENTO_ACCESO`: añadir `id_organizacion`, `id_sede`, `id_autorizacion`, `id_permanencia`, `resultado`, `motivo_denegacion` (intentos denegados), `idempotency_key` única por org, `timestamptz`; `id_vehiculo` → `MOVIMIENTO_VEHICULO` separado. Correcciones como registros nuevos (`id_movimiento_corregido`, D-08), nunca update.
28. Falta `MOVIMIENTO_DISPOSITIVO` (dispositivo, sede, punto, responsable, visita, operador, tipo) para saber qué equipos siguen dentro (RF-17).

### I. Restricciones, alertas, notificaciones, soporte
29. `RESTRICCION_ACCESO`: alcance (sede/zona opcional), `id_usuario_levanta`/fecha.
30. `ALERTA`: falta `id_sede`, `id_movimiento`, `clave_deduplicacion`; estado Nueva/Revisada/Cerrada con `ALERTA_TRANSICION` (actor + fecha por paso).
31. `NOTIFICACION`: faltan `PLANTILLA` por organización, `OUTBOX` (ADR-012), intentos/error; destinatario puede ser persona/correo, no solo usuario.
32. `AUDITORIA`: añadir `resultado`, `tipo_actor` (usuario/sistema/soporte), `id_correlacion`; `id_organizacion` nullable para plano SaaS; append-only por privilegios (ADR-014).
33. Falta `IDEMPOTENCIA` (clave, comando, org, resultado) y `ARCHIVO`.

### J. Convenciones transversales
34. Tipos: `datetime` → `timestamptz`; `boolean estado` (activo/inactivo) vs `string estado` (ciclo) — renombrar a `activo` para no confundir; columnas de auditoría (`creado_por`, `creado_en`, `actualizado_en`) y `xmin`/row_version.
35. Nombres: ADR-004 usa `organization_id`; el diagrama usa `id_organizacion`. Fijar convención (snake_case español o inglés) antes de EF Core.

## Decisiones que el usuario debe resolver
ADR-019 (Zona vs PuntoAcceso), INVITACION vs AUTORIZACION, una o varias sedes por autorización, D-03 (membresías), D-05 cupos, D-07 presencia, D-09 documento, D-17 placa/serial.

