---
tipo: epica
estado: propuesta
fase: F3
requisitos: [RF-11, RF-18, RF-19, RF-20, RN-07, RN-08, CA-07, CA-14, CA-15]
adr: [ADR-004, ADR-005, ADR-008, ADR-011, ADR-012, ADR-015]
decisiones: [D-04, D-07, D-14, D-15]
actualizado: 2026-10-17
tags: [conecsia, backlog, epica]
---

# EP-11 Presencia e historial

> [!note] Fase: [[PLAN-DE-TRABAJO#F3]] · Requisitos: RF-18, RF-19, RF-11 · ADR: [[ADR-011 Tiempo real con SignalR]] (Propuesta)

## Objetivo de la épica
Mostrar quién permanece dentro, con actualización en tiempo real basada en estado persistido, ofrecer el historial completo por [[Persona]] y dar al autorizador una bandeja de solicitudes pendientes.

## Alcance / fuera de alcance
- **Dentro (Base):** personas dentro (RF-19), historial por persona (RF-18), bandeja de decisiones (RF-11, §10.1).
- **Dentro (Propuesta):** SignalR con grupos por organización/sede y recuperación desde API al reconectar.
- **Fuera:** reportes exportables (EP-14), alertas de permanencia prolongada (EP-12), aprobación multinivel (no se presupone, PRD §6.7).

## Historias
| ID | Historia | Tamaño | Estado | Bloqueada por |
|---|---|---|---|---|
| HU-037 | Personas dentro en tiempo real | L | Lista para refinar | D-15 (frescura) |
| HU-038 | Historial por persona | M | Lista para refinar | D-14 (retención) |
| HU-039 | Bandeja del autorizador | S | Lista para refinar | D-04 (parcial) |

---

## HU-037 Personas dentro en tiempo real

**Como** operador de acceso o administrador de sede **quiero** ver en tiempo real quién permanece dentro **para** controlar la ocupación y detectar salidas pendientes.

| Campo | Valor |
|---|---|
| Épica / Fase | EP-11 / F3 |
| Requisitos | RF-19, RN-07, CA-14 |
| Tamaño relativo | L (propuesto, sin estimación aprobada) |
| Estado | Lista para refinar |

### Objetivo
Vista paginada de [[Permanencia|permanencias]] abiertas que coincide con la base de datos; eventos SignalR solo como señal de cambio publicada tras el commit.

### Actores
Operador de acceso; Administrador de sede; Administrador de organización.

### Dependencias
- Historias: HU-033, HU-034, HU-007.
- ADR: [[ADR-011 Tiempo real con SignalR]], [[ADR-012 Outbox transaccional y procesos de fondo]], [[ADR-005 Contexto de organización y autorización por permisos]] (Propuesta).
- Decisiones: D-15 (frescura máxima), D-07 (presencia entre sedes).

### Contrato funcional
- **Entradas:** `sedeId` (opcional, dentro de las asignadas), filtros por destino/zona, paginación.
- **Reglas (servidor):** permiso `presencia.consultar`; solo sedes asignadas; suscripción SignalR a grupos `org:{id}:sede:{id}` validada con permisos vigentes al conectar y al cambiar membresía; eventos con identificadores mínimos (sin datos personales).
- **Efectos:** ninguno de escritura; outbox publica `PresenciaCambiada` después del commit.
- **Errores:** 403 `SEDE_NO_ASIGNADA`; suscripción rechazada si pierde permiso.
- **Auditoría:** no requerida para lectura (Pendiente confirmar con D-14 accesos a datos personales).
- **Endpoints propuestos:** `GET /api/v1/presencia?sedeId=` · hub `/hubs/operacion`.

### Criterios de aceptación
- [ ] **Dado** permanencias abiertas en BD **cuando** se consulta la vista **entonces** coincide exactamente con ellas (CA-14).
- [ ] **Dado** una desconexión durante la cual hubo entradas **cuando** el cliente reconecta **entonces** recarga desde la API y muestra el estado correcto (CA-14).
- [ ] **Dado** un operador de la sede A **cuando** ocurre una entrada en la sede B u otra organización **entonces** no recibe el evento (CA-01).
- [ ] **Dado** una transacción de entrada revertida **cuando** termina **entonces** no se emite evento.

### Tareas backend
- [ ] Consulta de presencia con índice por `(organization_id, sede_id, estado)`.
- [ ] Hub SignalR con autorización por grupo y revalidación de permisos.
- [ ] Publicador outbox → SignalR.

### Tareas frontend
- [ ] Lista de personas dentro con tiempo transcurrido, indicador de conexión y "última actualización"; refetch al reconectar.

### Pruebas
- [ ] Unitarias: construcción de nombres de grupo y filtros.
- [ ] Integración (PostgreSQL real): consistencia vista ↔ permanencias abiertas.
- [ ] Concurrencia: entradas/salidas paralelas no dejan la vista inconsistente tras refetch.
- [ ] Aislamiento / permisos negativos: suscripción a grupo de otra organización rechazada.
- [ ] E2E: dos navegadores, entrada en uno aparece en otro; corte de red y reconexión.

### Definition of Done
Cumple la [[PLAN-DE-TRABAJO#Definition of Done general|DoD general]] y además:
- [ ] Prueba automatizada de reconexión sin dependencia de eventos perdidos.

### Riesgos y decisiones pendientes
> [!todo] D-15 — presupuesto de frescura y volumen de conexiones simultáneas.

> [!todo] D-07 — cómo se muestra una persona con presencia en varias sedes.

---

## HU-038 Historial por persona

**Como** administrador de sede o auditor **quiero** consultar el historial completo de una persona **para** conocer visitas, autorizaciones, movimientos y responsables.

| Campo | Valor |
|---|---|
| Épica / Fase | EP-11 / F3 |
| Requisitos | RF-18, RF-20, RN-08, CA-15 |
| Tamaño relativo | M (propuesto, sin estimación aprobada) |
| Estado | Lista para refinar |

### Objetivo
Línea de tiempo por [[Persona]]: [[Visita|visitas]], [[Autorización|autorizaciones]], [[Movimiento|movimientos]], duración, sedes, destinos, motivos, responsables, autorizadores y observaciones (PRD §10.1.4), preservada aunque se inactiven catálogos.

### Actores
Administrador de sede; Administrador de organización; Auditor.

### Dependencias
- Historias: HU-032, HU-034, historias de personas (F1) y autorizaciones (F2).
- ADR: [[ADR-004 Tenancy con base compartida y organization_id]], [[ADR-008 Modelo de tiempo y zonas horarias]] (Propuesta).
- Decisiones: D-14 (retención/acceso a datos personales).

### Contrato funcional
- **Entradas:** `personaId`, rango de fechas, sede, tipo; paginación.
- **Reglas (servidor):** permiso `personas.historial.consultar`; solo registros de la organización y sedes asignadas; catálogos inactivos se muestran con su nombre histórico; fechas en zona horaria de la sede.
- **Efectos:** ninguno; consulta de solo lectura.
- **Errores:** 404 persona de otra organización; 403 sin permiso.
- **Auditoría:** acceso a historial (Propuesta, pendiente D-14).
- **Endpoints propuestos:** `GET /api/v1/personas/{id}/historial`.

### Criterios de aceptación
- [ ] **Dado** una sede o destino inactivado **cuando** se consulta el historial **entonces** se conservan autorizadores, responsables, duración y movimientos (CA-15).
- [ ] **Dado** una persona con documento igual en otra organización **cuando** se consulta **entonces** no se mezcla su historial (CA-01).
- [ ] **Dado** un usuario sin permiso **cuando** llama la API **entonces** recibe 403.

### Tareas backend
- [ ] Consulta agregada paginada con proyección de solo lectura.

### Tareas frontend
- [ ] Vista de línea de tiempo con filtros y estado de cada visita/permanencia.

### Pruebas
- [ ] Unitarias: cálculo de duración y zona horaria.
- [ ] Integración (PostgreSQL real): inactivar catálogo y verificar historial intacto.
- [ ] Concurrencia: no aplica (lectura); verificar consistencia con salida en curso.
- [ ] Aislamiento / permisos negativos: misma persona por documento en dos tenants.
- [ ] E2E: abrir historial desde búsqueda.

### Definition of Done
Cumple la [[PLAN-DE-TRABAJO#Definition of Done general|DoD general]] y además:
- [ ] CA-15 verificado con catálogos inactivados.

### Riesgos y decisiones pendientes
> [!todo] D-14 — retención y acceso a datos personales; no se implementa purga.

---

## HU-039 Bandeja del autorizador

**Como** autorizador **quiero** una bandeja con las solicitudes pendientes de mis destinos **para** aprobarlas o rechazarlas a tiempo.

| Campo | Valor |
|---|---|
| Épica / Fase | EP-11 / F3 |
| Requisitos | RF-11, PRD §10.1, CA-07 |
| Tamaño relativo | S (propuesto, sin estimación aprobada) |
| Estado | Lista para refinar (ámbito del autorizador depende de D-04) |

### Objetivo
Listado de solicitudes/visitas pendientes dentro del ámbito del autorizador, reutilizando el comando de decisión existente de F2.

### Actores
Autorizador; Administrador de sede.

### Dependencias
- Historias: historia de decisión de autorización (F2), HU-037 (canal SignalR).
- ADR: [[ADR-005 Contexto de organización y autorización por permisos]], [[ADR-009 Ciclos de estado separados y vigencia calculada]], [[ADR-011 Tiempo real con SignalR]] (Propuesta).
- Decisiones: **D-04** (quién decide y sobre qué destinos).

### Contrato funcional
- **Entradas:** filtros por sede/destino, estado Pendiente, paginación.
- **Reglas (servidor):** permiso `autorizaciones.decidir` + destinos asignados; vencidas no aparecen como decidibles; la decisión se valida en servidor (no por ocultar botones).
- **Efectos:** ninguno propio; la decisión usa el endpoint de F2.
- **Errores:** 403 sin permiso; `SOLICITUD_YA_DECIDIDA` (409) al decidir en paralelo.
- **Auditoría:** la de la decisión (F2).
- **Endpoints propuestos:** `GET /api/v1/autorizaciones/pendientes`.

### Criterios de aceptación
- [ ] **Dado** un usuario sin permiso de aprobar **cuando** llama la API de decisión **entonces** se rechaza (CA-07).
- [ ] **Dado** solicitudes de destinos no asignados **cuando** el autorizador abre la bandeja **entonces** no las ve.
- [ ] **Dado** una nueva solicitud **cuando** se confirma **entonces** la bandeja se actualiza tras el commit.

### Tareas backend
- [ ] Consulta de pendientes por ámbito del autorizador; evento `PendientesCambiados` vía outbox.

### Tareas frontend
- [ ] Bandeja con contador, acciones aprobar/rechazar con observación.

### Pruebas
- [ ] Unitarias: filtro de ámbito.
- [ ] Integración (PostgreSQL real): pendientes por destino asignado.
- [ ] Concurrencia: dos autorizadores deciden la misma solicitud ⇒ una sola decisión.
- [ ] Aislamiento / permisos negativos: pendientes de otra organización invisibles.
- [ ] E2E: solicitud creada en recepción aparece en la bandeja y se aprueba.

### Definition of Done
Cumple la [[PLAN-DE-TRABAJO#Definition of Done general|DoD general]] y además:
- [ ] Ámbito de autorizador documentado según estado de D-04.

### Riesgos y decisiones pendientes
> [!todo] D-04 — destinos de cada autorizador y quién revoca/cancela; no se presupone aprobación múltiple.
