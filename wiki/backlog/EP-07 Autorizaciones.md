---
tipo: epica
estado: propuesta
fase: F2
requisitos: [RF-09, RF-10, RF-20, RN-03, RN-04, RN-05, RN-06, RN-09, CA-06, CA-07, CA-09, CA-10, CA-11]
adr: [ADR-005, ADR-007, ADR-008, ADR-009, ADR-014, ADR-020]
decisiones: [D-04, D-05, D-06, D-07]
actualizado: 2026-10-17
tags: [conecsia, backlog, epica]
---

# EP-07 Autorizaciones

> [!note] Fase: [[PLAN-DE-TRABAJO#F2]] · Requisitos: RF-09, RF-10, RF-20 · ADR: [[ADR-009 Ciclos de estado separados y vigencia calculada]], [[ADR-008 Modelo de tiempo y zonas horarias]], [[ADR-007 Concurrencia e idempotencia en accesos]] (todos Propuesta)

## Objetivo de la épica
Permitir crear, solicitar, decidir, cancelar, revocar y evaluar [[Autorización|autorizaciones]] por organización, con trazabilidad completa de creador y autorizador, y un evaluador único de vigencia en servidor que aplica el principio **aprobada ≠ vigente** (Base: PRD §6.2, §6.7).

## Alcance / fuera de alcance
- **Dentro:** modelo de autorización y sus estados (Propuesta, [[ADR-009 Ciclos de estado separados y vigencia calculada|ADR-009]]); modalidades única/diaria/temporal/recurrente/permanente; calendario en zona horaria de la [[Sede]]; cupo de ingresos separado de la modalidad; solicitud y decisión; cancelación/revocación/vencimiento; evaluador de vigencia reutilizado por [[EP-10 Entradas y salidas]] y [[EP-09 QR]].
- **Fuera:** confirmación de entrada y consumo atómico del cupo (HU-032/HU-033), visitas ([[EP-08 Visitas]]), QR (EP-09), aprobación multinivel (no se presupone, D-04), notificaciones por correo (EP-13).

## Historias
| ID | Historia | Tamaño | Estado | Bloqueada por |
|---|---|---|---|---|
| HU-023 | Crear autorización directa por usuario facultado | L | Lista para refinar | D-04 (parcial: destinos por autorizador) |
| HU-024 | Modalidades, calendario y cupo de ingresos | L | Bloqueada parcial | D-05, D-06 |
| HU-025 | Solicitar y decidir autorizaciones | M | Bloqueada parcial | D-04 |
| HU-026 | Cancelar, revocar y vencer autorizaciones | M | Bloqueada parcial | D-04 |
| HU-027 | Evaluador de vigencia de autorización | M | Lista para refinar | D-05, D-06 (casos afectados deniegan) |

---

## HU-023 Crear autorización directa por usuario facultado

**Como** autorizador o administrador de sede **quiero** emitir una autorización para una persona hacia una sede/destino **para** que quede aprobada y atribuida a mí sin un paso adicional de decisión.

| Campo | Valor |
|---|---|
| Épica / Fase | EP-07 / F2 |
| Requisitos | RF-09, RF-20, RN-04, CA-06 |
| Tamaño relativo | L (propuesto, sin estimación aprobada) |
| Estado | Lista para refinar |

### Objetivo
Persistir una autorización completa (persona, organización, sede, [[Área o destino|destino]], [[Zona y punto de acceso|zonas]], modalidad, condiciones, motivo, responsable, creador, autorizador, decisión e instante). Base: PRD §6.2, la emisión directa por usuario facultado puede quedar aprobada.

### Actores
Autorizador, Administrador de sede, Administrador de organización ([[Usuarios, roles y permisos]]). El Operador de acceso **no** obtiene este permiso por registrar personas (Base).

### Dependencias
- Historias: HU-005, HU-007, HU-008, HU-013, HU-014, HU-015, HU-017, HU-018, HU-019, HU-024.
- ADR: [[ADR-004 Tenancy con base compartida y organization_id]], [[ADR-005 Contexto de organización y autorización por permisos]], [[ADR-009 Ciclos de estado separados y vigencia calculada]], [[ADR-014 Auditoría append-only]], [[ADR-020 Contratos API y formato de errores]] (todos Propuesta).
- Decisiones: D-04 (qué destinos autoriza cada usuario; cuándo se exige autorización).

### Contrato funcional
- **Entradas:** `personaId`, `sedeId`, `destinoId`, `zonaIds[]` opcional, `modalidad` + condiciones (HU-024), `motivo`, `responsableId`, `observaciones`, cabecera `Idempotency-Key`.
- **Reglas (servidor):** organización derivada del contexto verificado, nunca del cuerpo; permiso `autorizaciones.emitir` + alcance sobre sede **y** destino (HU-018); persona activa de la misma organización; sede/destino/zonas activos y zonas pertenecientes a la sede; autorizador = actor autenticado (no se acepta otro en el cuerpo); condiciones válidas según HU-024.
- **Efectos persistidos:** `autorizacion` en **Aprobada** con `creadoPor`, `autorizadoPor`, `decididoEn` (UTC) y condiciones; transición inicial registrada.
- **Errores:** 403 sin permiso/alcance; 404 si persona/sede/destino es de otra organización (sin revelar existencia); 422 condiciones inválidas o catálogo inactivo; 409 misma clave de idempotencia con cuerpo distinto.
- **Auditoría:** `autorizacion.creada` (organización, actor, instante, objeto, resultado, valores nuevos); denegaciones 403/404 auditadas.
- **Endpoints propuestos:** `POST /api/v1/autorizaciones`, `GET /api/v1/autorizaciones/{id}`, `GET /api/v1/autorizaciones?personaId=&sedeId=&estado=` (indicativos).

### Criterios de aceptación
- [ ] **Dado** un autorizador con alcance sobre la sede y destino **cuando** crea una autorización **entonces** queda Aprobada y conserva persona, destino, modalidad, condiciones, creador y decisión atribuible (CA-06).
- [ ] **Dado** un operador sin `autorizaciones.emitir` **cuando** llama a la API directamente **entonces** recibe 403 y no se persiste nada (CA-07, RN-10).
- [ ] **Dado** un `destinoId` de otra organización **cuando** se envía **entonces** responde 404 y se audita el intento (CA-01).
- [ ] **Dado** una autorización Aprobada con inicio mañana **cuando** se evalúa hoy **entonces** no es vigente (HU-027, RN-05).

### Tareas backend
- [ ] Entidades `Autorizacion`, `AutorizacionZona`, `AutorizacionTransicion`; migración con `organization_id`, FK compuestas por organización e índice `(organization_id, persona_id, estado)`.
- [ ] Comando `CrearAutorizacionDirecta` con validador, verificación de alcance y auditoría en la misma transacción.
- [ ] Idempotencia del comando ([[ADR-007 Concurrencia e idempotencia en accesos|ADR-007]], Propuesta).

### Tareas frontend
- [ ] Formulario: persona buscada por documento, sede/destino/zonas filtrados por alcance, modalidad (componente de HU-024).
- [ ] Detalle con creador, autorizador, instante y condiciones; errores 403/422 según ADR-020.

### Pruebas
- [ ] Unitarias: validador de condiciones, invariante "zonas ⊂ sede".
- [ ] Integración (PostgreSQL real): persistencia, FK por organización, auditoría en la misma transacción, idempotencia.
- [ ] Aislamiento / permisos negativos: org A no crea ni lee autorizaciones de org B; operador sin permiso; autorizador fuera de su destino.
- [ ] E2E / UI: crear autorización y verla en el detalle.

### Definition of Done
Cumple la [[PLAN-DE-TRABAJO#Definition of Done general|DoD general]] y además:
- [ ] CA-06 verificado por prueba de integración que lee la fila persistida y su auditoría.
- [ ] Ningún campo de autorizador u organización se acepta desde el cliente.

### Riesgos y decisiones pendientes
> [!todo] D-04 — Matriz de qué usuarios/destinos pueden autorizar y si alguna política exige decisión aun a usuarios facultados. Hasta decidir: emisión directa solo con permiso explícito + alcance sobre el destino.

---

## HU-024 Modalidades, calendario y cupo de ingresos

**Como** usuario facultado **quiero** definir modalidad, calendario y cupo de una autorización **para** que el servidor sepa exactamente cuándo y cuántas veces habilita ingresos.

| Campo | Valor |
|---|---|
| Épica / Fase | EP-07 / F2 |
| Requisitos | RF-10, RN-05, RN-09, CA-10, CA-11 |
| Tamaño relativo | L (propuesto, sin estimación aprobada) |
| Estado | Bloqueada parcial (D-05, D-06) |

### Objetivo
Modelar las cinco modalidades (Base) separando **modalidad** de **cupo de ingresos** (Propuesta PRD §6.2) y expresando ventanas en hora local de la sede (Propuesta [[ADR-008 Modelo de tiempo y zonas horarias|ADR-008]]).

### Actores
Autorizador, Administrador de sede/organización.

### Dependencias
- Historias: HU-013 (zona horaria de sede), HU-023.
- ADR: [[ADR-008 Modelo de tiempo y zonas horarias]], [[ADR-009 Ciclos de estado separados y vigencia calculada]] (Propuesta).
- Decisiones: D-05 (reingresos, cupos por modalidad, modificación de aprobadas), D-06 (nocturnos, límites inclusivos/exclusivos, festivos).

### Contrato funcional
- **Entradas:** `modalidad` ∈ {Unica, Diaria, Temporal, Recurrente, Permanente}; `fechaInicio`, `fechaFin` (fechas locales de la sede); `diasSemana[]`; `ventanas[]` (`horaInicio`, `horaFin` locales); `cupoIngresos`.
- **Reglas (servidor):** Única ⇒ cupo = 1 (Base). Diaria ⇒ un solo día; Temporal ⇒ rango de fechas; Recurrente ⇒ días + ventanas obligatorios, múltiples accesos (Base RN-09). Permanente ⇒ sin `fechaFin` **solo si la política de la organización lo permite** (bandera, por defecto deshabilitada). Diaria/Temporal: mientras D-05 esté pendiente, cupo **1** y no se infieren reingresos (Base PRD §6.2). Ventanas que crucen medianoche **se rechazan** hasta D-06; festivos no se modelan hasta D-06. Modificar condiciones de una Aprobada **no se habilita** hasta D-05 (revocar y emitir otra).
- **Efectos persistidos:** condiciones normalizadas; `ingresosConsumidos = 0`.
- **Errores:** 422 con código por regla (`ventana_cruza_medianoche_no_soportada`, `permanente_no_permitida`, `cupo_invalido`, `modificacion_no_habilitada`).
- **Auditoría:** dentro de la creación (HU-023/HU-025); intentos de modificación rechazados se auditan.
- **Endpoints propuestos:** validación embebida en `POST /api/v1/autorizaciones` y `POST /api/v1/solicitudes-autorizacion`; `GET /api/v1/politicas/autorizacion` (lectura de la política de la organización actual).

### Criterios de aceptación
- [ ] **Dado** una autorización Única **cuando** se crea **entonces** su cupo es 1 y no puede aumentarse (base para CA-10 en HU-033).
- [ ] **Dado** una Recurrente lun–mié 08:00–12:00 **cuando** se evalúa en semanas distintas **entonces** ambas ventanas son válidas y no se cierra tras una salida (CA-11).
- [ ] **Dado** una ventana 22:00–06:00 **cuando** se envía **entonces** se rechaza con código explícito mientras D-06 esté pendiente.
- [ ] **Dado** una Diaria sin decisión D-05 **cuando** ya registró un ingreso **entonces** un segundo ingreso no se habilita.

### Tareas backend
- [ ] Value objects `Calendario`, `Ventana`, `Cupo` con zona horaria IANA de la sede (librería a confirmar en ADR-008).
- [ ] Política `permitePermanente` por organización; columnas `cupo_ingresos`, `ingresos_consumidos` con CHECK.
- [ ] Rechazo explícito de modificación de condiciones de Aprobadas.

### Tareas frontend
- [ ] Editor de modalidad con campos dinámicos y vista previa en hora local de la sede.
- [ ] Mensajes "no soportado aún" para casos pendientes de D-05/D-06.

### Pruebas
- [ ] Unitarias: tabla de casos por modalidad; conversión local↔UTC, incluidos cambios de horario si la zona los tiene.
- [ ] Integración (PostgreSQL real): persistencia de condiciones y CHECK (cupo = 1 en Única).
- [ ] Aislamiento / permisos negativos: la política `permitePermanente` de org A no afecta a org B.
- [ ] E2E / UI: crear recurrente y ver sus ventanas.

### Definition of Done
Cumple la [[PLAN-DE-TRABAJO#Definition of Done general|DoD general]] y además:
- [ ] Cada comportamiento dependiente de D-05/D-06 documentado como "rechazado hasta decidir" en [[Autorización]].

### Riesgos y decisiones pendientes
> [!todo] D-05 — Reingresos y cupos de Diaria/Temporal; modificación de autorizaciones aprobadas.

> [!todo] D-06 — Horarios nocturnos, límites inclusivos/exclusivos, festivos. Sugerencia técnica (inicio inclusivo, fin exclusivo) **no aprobada**.

---

## HU-025 Solicitar y decidir autorizaciones

**Como** solicitante **quiero** registrar una solicitud que quede Pendiente **y como** autorizador **quiero** aprobarla o rechazarla **para** que el ingreso solo se habilite tras una decisión válida.

| Campo | Valor |
|---|---|
| Épica / Fase | EP-07 / F2 |
| Requisitos | RF-09, RF-20, RN-03, RN-10, CA-06, CA-07 |
| Tamaño relativo | M (propuesto, sin estimación aprobada) |
| Estado | Bloqueada parcial (D-04) |

### Objetivo
Flujo Pendiente → Aprobada | Rechazada con decisión atribuible a un autorizador facultado (Base PRD §6.2, §6.7). Sin aprobación multinivel (no se presupone).

### Actores
Operador de acceso o responsable (solicita), Autorizador del destino (decide), Administrador de sede.

### Dependencias
- Historias: HU-023, HU-024, HU-014 (autorizadores del destino), HU-018.
- ADR: [[ADR-009 Ciclos de estado separados y vigencia calculada]], [[ADR-005 Contexto de organización y autorización por permisos]], [[ADR-007 Concurrencia e idempotencia en accesos]] (Propuesta).
- Decisiones: D-04 (quién decide y sobre qué destinos); D-12 (notificación, fuera de esta historia).

### Contrato funcional
- **Entradas:** solicitud con los datos de HU-023 sin decisión; decisión `Aprobar | Rechazar`, `observaciones`, `version`.
- **Reglas (servidor):** solicitar requiere `autorizaciones.solicitar`; decidir requiere `autorizaciones.decidir` + alcance sobre el destino (nunca por nombre de rol); solo desde Pendiente; una solicitud cuya `fechaFin` ya pasó no puede aprobarse.
- **Efectos persistidos:** estado, `autorizadoPor`, `decididoEn`, observaciones, transición. Nunca se fabrica una aprobación a nombre del responsable.
- **Errores:** 403 sin permiso/alcance; 409 ya decidida o versión obsoleta; 422 vencida.
- **Auditoría:** `autorizacion.solicitada|aprobada|rechazada`, intentos 403 y 409.
- **Endpoints propuestos:** `POST /api/v1/solicitudes-autorizacion`, `POST /api/v1/autorizaciones/{id}/aprobar`, `POST /api/v1/autorizaciones/{id}/rechazar`, `GET /api/v1/autorizaciones/pendientes`.

### Criterios de aceptación
- [ ] **Dado** una solicitud Pendiente **cuando** un usuario sin permiso de decisión llama a aprobar por API **entonces** 403 y sigue Pendiente (CA-07).
- [ ] **Dado** una solicitud Pendiente **cuando** se evalúa vigencia **entonces** NoVigente `no_aprobada` (CA-07, CA-09).
- [ ] **Dado** dos autorizadores que deciden a la vez **cuando** ambos envían **entonces** persiste una sola decisión y la otra recibe 409.
- [ ] **Dado** una aprobación **entonces** conserva autorizador, instante y observaciones (CA-06).

### Tareas backend
- [ ] Comandos `SolicitarAutorizacion`, `AprobarAutorizacion`, `RechazarAutorizacion` sobre la máquina de estados del dominio.
- [ ] Concurrencia optimista (`xmin` o columna `version`); consulta de pendientes filtrada por alcance.

### Tareas frontend
- [ ] Lista de pendientes y acciones aprobar/rechazar con confirmación (bandeja completa en HU-039).
- [ ] Acciones visibles solo con permiso, sin depender de ello para la seguridad.

### Pruebas
- [ ] Unitarias: transiciones válidas e inválidas.
- [ ] Integración (PostgreSQL real): concurrencia optimista con dos transacciones reales.
- [ ] Aislamiento / permisos negativos: autorizador de org A no ve ni decide pendientes de org B; autorizador de otro destino ⇒ 403.
- [ ] Concurrencia: aprobar y rechazar simultáneos.
- [ ] E2E / UI: solicitar, aprobar y ver estado.

### Definition of Done
Cumple la [[PLAN-DE-TRABAJO#Definition of Done general|DoD general]] y además:
- [ ] CA-07 (parte API) verificado con prueba negativa automatizada.

### Riesgos y decisiones pendientes
> [!todo] D-04 — Quién decide por destino, si el rechazo exige observación, vencimiento de solicitudes no decididas, cuándo se exige solicitud. Hasta decidir: solo permiso explícito + alcance; sin multinivel; observación opcional.

---

## HU-026 Cancelar, revocar y vencer autorizaciones

**Como** usuario facultado **quiero** cancelar o revocar una autorización **y como** sistema **quiero** marcar las vencidas **para** que los estados terminales no habiliten nuevos ingresos.

| Campo | Valor |
|---|---|
| Épica / Fase | EP-07 / F2 |
| Requisitos | RN-06, RF-20, CA-09, CA-13 |
| Tamaño relativo | M (propuesto, sin estimación aprobada) |
| Estado | Bloqueada parcial (D-04) |

### Objetivo
Transiciones Pendiente → Cancelada/Vencida y Aprobada → Revocada/Cancelada/Vencida (Propuesta ADR-009), conservando historial y sin afectar [[Permanencia|permanencias]] abiertas (Propuesta PRD §6.6).

### Actores
Autorizador, Administrador de sede/organización, proceso de fondo de vencimiento.

### Dependencias
- Historias: HU-023, HU-025, HU-027; HU-030 (invalidar QR); HU-034 (salida tras revocación).
- ADR: [[ADR-009 Ciclos de estado separados y vigencia calculada]], [[ADR-012 Outbox transaccional y procesos de fondo]], [[ADR-014 Auditoría append-only]] (Propuesta).
- Decisiones: D-04 (quién cancela/revoca), D-05.

### Contrato funcional
- **Entradas:** `motivo` (obligatorio), `version`.
- **Reglas (servidor):** permisos `autorizaciones.cancelar` / `autorizaciones.revocar` + alcance; solo desde estados no terminales; *Agotada* solo la produce el consumo de cupo (HU-033); el job marca *Vencida* por organización con su propio contexto de tenant, pero el control de acceso **siempre recalcula** (ADR-009); revocar no cierra permanencias abiertas.
- **Efectos persistidos:** estado, actor, instante, motivo, transición; invalidación del token QR en la misma transacción (HU-030).
- **Errores:** 403; 404 otra organización; 409 estado terminal o versión obsoleta.
- **Auditoría:** `autorizacion.cancelada|revocada|vencida` con valor anterior/nuevo; el job audita con actor "sistema".
- **Endpoints propuestos:** `POST /api/v1/autorizaciones/{id}/cancelar`, `POST /api/v1/autorizaciones/{id}/revocar`.

### Criterios de aceptación
- [ ] **Dado** una autorización revocada **cuando** se intenta un nuevo ingreso **entonces** se deniega con causa `revocada` (CA-09, RN-06).
- [ ] **Dado** una persona dentro cuya autorización se revoca **cuando** registra salida **entonces** la salida se permite (CA-13, regla Propuesta).
- [ ] **Dado** una Aprobada con `fechaFin` pasada que el job aún no marcó **cuando** se evalúa **entonces** NoVigente.
- [ ] **Dado** una Revocada **cuando** se intenta revocar de nuevo **entonces** 409.

### Tareas backend
- [ ] Comandos cancelar/revocar; job idempotente de vencimiento por lotes y por organización.
- [ ] Evento de dominio `AutorizacionInvalidada` que invalida el QR en la misma transacción.

### Tareas frontend
- [ ] Acciones cancelar/revocar con motivo obligatorio y confirmación; historial de transiciones en el detalle.

### Pruebas
- [ ] Unitarias: transiciones desde cada estado.
- [ ] Integración (PostgreSQL real): job respeta tenant; QR invalidado en la misma transacción.
- [ ] Aislamiento / permisos negativos: revocar autorización de otra org ⇒ 404; sin permiso ⇒ 403.
- [ ] Concurrencia: revocación simultánea a una confirmación de entrada (coordinar con HU-033).
- [ ] E2E / UI: revocar y ver estado e historial.

### Definition of Done
Cumple la [[PLAN-DE-TRABAJO#Definition of Done general|DoD general]] y además:
- [ ] RN-06 verificado: ningún estado terminal produce "vigente".

### Riesgos y decisiones pendientes
> [!todo] D-04 — Matriz de quién cancela vs. revoca y diferencia operativa entre ambas. Hasta decidir: solo permiso explícito + alcance.

> [!warning] "La revocación no impide la salida" es **Propuesta** (PRD §6.6), pendiente de aprobación.

---

## HU-027 Evaluador de vigencia de autorización

**Como** servicio de entrada/QR **quiero** una evaluación única y determinista de vigencia **para** decidir si una autorización habilita un ingreso en un instante, lugar y punto concretos.

| Campo | Valor |
|---|---|
| Épica / Fase | EP-07 / F2 |
| Requisitos | RN-03, RN-05, RN-06, RN-09, CA-09, CA-11 |
| Tamaño relativo | M (propuesto, sin estimación aprobada) |
| Estado | Lista para refinar |

### Objetivo
Componente de dominio puro `EvaluadorVigencia(autorizacion, instanteUtc, sede, punto/zona, persona)` → `Vigente | NoVigente(causas[])`. **Aprobada ≠ vigente** (Base). No persiste estado ni consume cupo.

### Actores
Servicios internos (HU-031, HU-032, HU-033); indirectamente el Operador de acceso.

### Dependencias
- Historias: HU-024, HU-026, HU-013, HU-015, HU-022.
- ADR: [[ADR-009 Ciclos de estado separados y vigencia calculada]], [[ADR-008 Modelo de tiempo y zonas horarias]] (Propuesta).
- Decisiones: D-05, D-06 (casos no definidos ⇒ NoVigente `regla_no_definida`); D-07 no aplica aquí (presencia en HU-033).

### Contrato funcional
- **Entradas:** autorización y condiciones, instante del servidor (nunca del cliente), sede y punto/zona del intento, estado de la persona.
- **Reglas:** estado = Aprobada; persona activa; sede coincide; zona del punto ∈ zonas permitidas si hay restricción; fecha local de la sede en rango; día permitido; hora dentro de alguna ventana; `ingresosConsumidos < cupo`. Evalúa **todas** las causas.
- **Salidas:** resultado + causas codificadas (`no_aprobada`, `revocada`, `cancelada`, `vencida`, `agotada`, `fuera_de_fecha`, `dia_no_permitido`, `fuera_de_horario`, `sede_incorrecta`, `zona_no_permitida`, `persona_inactiva`, `regla_no_definida`).
- **Errores:** no lanza excepciones; siempre resultado tipado.
- **Auditoría:** la realiza el consumidor (lectura QR/entrada).
- **Endpoints propuestos:** `POST /api/v1/autorizaciones/{id}/evaluar` (sin efectos; permiso `accesos.validar` + alcance de punto).

### Criterios de aceptación
- [ ] **Dado** una Aprobada para mañana **cuando** se evalúa hoy **entonces** NoVigente `fuera_de_fecha` (RN-05).
- [ ] **Dado** persona inactiva, sede/zona incorrecta o fuera de horario **cuando** se evalúa **entonces** NoVigente con cada causa (CA-09).
- [ ] **Dado** una recurrente **cuando** se evalúa en dos ventanas válidas distintas **entonces** ambas Vigente (CA-11).
- [ ] **Dado** cualquier evaluación **entonces** no cambia `ingresosConsumidos` ni el estado.

### Tareas backend
- [ ] Evaluador puro en Dominio con reloj inyectable.
- [ ] Endpoint de evaluación de solo lectura filtrado por organización.

### Tareas frontend
- [ ] Catálogo de mensajes legibles por causa (reutilizado en HU-031/HU-036).

### Pruebas
- [ ] Unitarias: matriz exhaustiva de causas; bordes de fecha/hora en la zona horaria de la sede.
- [ ] Integración (PostgreSQL real): carga y evaluación sin escrituras (verificar ausencia de cambios).
- [ ] Aislamiento / permisos negativos: autorización de otra org ⇒ 404; operador sin alcance sobre el punto ⇒ 403.
- [ ] E2E / UI: cubierto por HU-031.

### Definition of Done
Cumple la [[PLAN-DE-TRABAJO#Definition of Done general|DoD general]] y además:
- [ ] Cobertura de ramas del evaluador ≥ 95 % y tabla de causas documentada en [[Autorización]].

### Riesgos y decisiones pendientes
> [!todo] D-05 / D-06 — Reingresos, límites inclusivos y nocturnos: hasta decidir, deniegan con `regla_no_definida`.

> [!warning] Riesgo de divergencia si la entrada reimplementa reglas: HU-031/HU-032/HU-033 deben usar exclusivamente este evaluador.
