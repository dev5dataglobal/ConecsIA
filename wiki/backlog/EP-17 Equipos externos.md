---
tipo: epica
estado: propuesta
fase: F5
requisitos: [RF-17, CA-24, CA-25, CA-01, CA-20]
adr: [ADR-004, ADR-005, ADR-007, ADR-011, ADR-014, ADR-019, ADR-020]
decisiones: [D-02, D-08, D-10, D-16]
actualizado: 2026-10-17
tags: [conecsia, backlog, epica, equipos]
---

# EP-17 Equipos externos

> [!note] Fase: [[PLAN-DE-TRABAJO#F5]] · Requisitos: RF-17, [[Criterios de aceptación|CA-24, CA-25]] · ADR: [[ADR-007 Concurrencia e idempotencia en accesos]], [[ADR-019 Zona única con atributo de punto de acceso]]

## Objetivo de la épica
Registrar dispositivos y equipos externos de las [[Persona|personas]], sus entradas y salidas, y saber qué equipos siguen dentro (**Base**, RF-17).

## Alcance / fuera de alcance
- **Dentro (Base):** propietario/responsable, tipo parametrizable, código/serial, marca, modelo, color, descripción, estado; múltiples dispositivos por persona y por autorización; movimientos con sede, zona como punto de acceso, responsable, visita y operador; consulta de equipos dentro.
- **Dentro (Propuesta):** advertir al operador cuando una persona sale con equipos pendientes y registrar la novedad con permiso y motivo.
- **Fuera / Pendiente (D-10):** bloqueo de salida, salida con otro responsable, transferencia de custodia. **La salida de la persona NO cierra los equipos.**
- **Módulo deshabilitado:** sin nuevos registros ni movimientos; histórico conservado.

## Historias
| ID | Historia | Tamaño | Estado | Bloqueada por |
|---|---|---|---|---|
| HU-049 | Registrar dispositivos externos | M | Lista para refinar | D-02 (prioridad) |
| HU-050 | Movimientos de dispositivos y equipos dentro | M | Lista para refinar (núcleo Base) | D-10 para custodia |

---

## HU-049 Registrar dispositivos externos

**Como** operador de acceso o administrador de sede **quiero** registrar los dispositivos de una persona y asociarlos a su autorización **para** controlar los equipos que ingresan.

| Campo | Valor |
|---|---|
| Épica / Fase | EP-17 / F5 |
| Requisitos | RF-17, CA-24, CA-01, CA-03, CA-20 |
| Tamaño relativo | M (propuesto, sin estimación aprobada) |
| Estado | Lista para refinar |

### Objetivo
Catálogo de dispositivos por organización con tipo parametrizable, vínculo a responsable y N:M a [[Autorización|autorizaciones]].

### Actores
Operador de acceso, Administrador de organización/sede (catálogo de tipos), Autorizador, Auditor.

### Dependencias
- Historias: HU-005, HU-007, HU-008, HU-011, HU-019, HU-023.
- ADR: [[ADR-004 Tenancy con base compartida y organization_id]] (Propuesta), [[ADR-005 Contexto de organización y autorización por permisos]] (Propuesta), [[ADR-014 Auditoría append-only]] (Propuesta), [[ADR-020 Contratos API y formato de errores]] (Propuesta).
- Decisiones: D-02, D-16, D-10 (cambio de responsable).

### Contrato funcional
- **Entradas:** tipoDispositivoId, codigo/serial?, marca, modelo, color, descripcion, responsablePersonaId, estado.
- **Reglas (servidor):** módulo `equipos` habilitado; permiso `equipos.gestionar`; tipo activo de la misma organización; responsable de la misma organización; unicidad Propuesta `(organization_id, tipo, serial_normalizado)` solo cuando hay serial; inactivación lógica.
- **Efectos persistidos:** `dispositivo`, `autorizacion_dispositivo`, `tipo_dispositivo`.
- **Errores / denegaciones:** 403 módulo/permiso; 404 otra organización; 409 serial duplicado; 422 validación o tipo inactivo.
- **Auditoría:** crear, modificar (anterior/nuevo), inactivar, asociar/desasociar.
- **Endpoints propuestos:** `POST/GET/PATCH /api/v1/dispositivos`, `GET/POST /api/v1/tipos-dispositivo`, `PUT /api/v1/autorizaciones/{id}/dispositivos` (indicativo).

### Criterios de aceptación
- [ ] **Dado** una persona **cuando** registro tres dispositivos y los asocio a su autorización **entonces** los tres quedan vinculados (CA-24).
- [ ] **Dado** un tipo inactivado **cuando** creo un dispositivo con él **entonces** se rechaza y los existentes conservan la referencia (CA-03).
- [ ] **Dado** un usuario de A **cuando** usa el ID de un dispositivo de B **entonces** recibe 404 (CA-01).
- [ ] **Dado** el módulo deshabilitado **cuando** creo un dispositivo **entonces** se rechaza y el histórico sigue consultable con permiso.
- [ ] **Dado** cualquier cambio **entonces** queda auditado (CA-20).

### Tareas backend
- [ ] Entidades, migración, filtros de tenant e índice único parcial por serial.
- [ ] Casos de uso CRUD/inactivación/asociación con verificación de módulo y permiso.
- [ ] Auditoría y ProblemDetails.

### Tareas frontend
- [ ] Catálogo de tipos; alta rápida de dispositivos desde la ficha de persona y en recepción; asociación en la autorización.

### Pruebas
- [ ] Unitarias: validaciones y normalización de serial.
- [ ] Integración (PostgreSQL real): unicidad concurrente e inactivación.
- [ ] Aislamiento / permisos negativos: A vs B, sin permiso, módulo deshabilitado.
- [ ] E2E / UI: alta de varios dispositivos en móvil.

### Definition of Done
Cumple la [[PLAN-DE-TRABAJO#Definition of Done general|DoD general]] y además:
- [ ] Regla de unicidad de serial validada por producto.
- [ ] Permisos del módulo incluidos en la matriz.

### Riesgos y decisiones pendientes
> [!todo] D-10 — Cambio de responsable del dispositivo.

> [!warning] Riesgo: seriales capturados manualmente con errores; no exigir unicidad cuando no hay serial.

---

## HU-050 Movimientos de dispositivos y equipos dentro

**Como** operador de acceso **quiero** registrar la entrada y salida de cada equipo **para** saber qué equipos siguen dentro de la sede.

| Campo | Valor |
|---|---|
| Épica / Fase | EP-17 / F5 |
| Requisitos | RF-17, CA-24, CA-25, CA-12, CA-01 |
| Tamaño relativo | M (propuesto, sin estimación aprobada) |
| Estado | Lista para refinar (núcleo Base); custodia Bloqueada por D-10 |

### Objetivo
Movimientos idempotentes por dispositivo, independientes de la [[Permanencia]] de la persona, y consulta de equipos dentro.

### Actores
Operador de acceso, Administrador de sede, Auditor.

### Dependencias
- Historias: HU-049, HU-032, HU-033, HU-034, HU-037, HU-018.
- ADR: [[ADR-007 Concurrencia e idempotencia en accesos]] (Propuesta), [[ADR-019 Zona única con atributo de punto de acceso]] (Propuesta), [[ADR-011 Tiempo real con SignalR]] (Propuesta), [[ADR-008 Modelo de tiempo y zonas horarias]] (Propuesta).
- Decisiones: **D-10** (custodia, bloqueo, transferencia), D-08.

### Contrato funcional
- **Entradas:** dispositivoIds[], visitaId, tipo, puntoAccesoId, responsablePersonaId, clave de idempotencia; para novedad: motivo.
- **Reglas (servidor):** módulo habilitado; permiso `equipos.movimientos.registrar` y alcance a sede/punto; dispositivo activo y asociado a la persona o autorización; sin doble entrada abierta por dispositivo; instante del servidor.
- **Efectos persistidos:** `movimiento_dispositivo` (dispositivo, instante, tipo, sede, zona/punto, responsable, visita, operador); novedad Propuesta `novedad_equipo` (motivo, actor) sin cerrar el movimiento.
- **No hace:** no cierra equipos al salir la persona; no bloquea la salida de la persona; no transfiere custodia (D-10).
- **Errores / denegaciones:** 403; 404 otra organización; 409 equipo ya dentro / sin entrada abierta; 422 no asociado.
- **Auditoría:** entrada, salida, novedad y denegaciones.
- **Endpoints propuestos:** `POST /api/v1/dispositivos/movimientos`, `GET /api/v1/dispositivos/dentro?sedeId=`, `GET /api/v1/dispositivos/{id}/movimientos`, `POST /api/v1/visitas/{id}/novedades-equipo` (indicativo).

### Criterios de aceptación
- [ ] **Dado** una persona con dos equipos dentro **cuando** sale **entonces** su permanencia se cierra, los equipos siguen "dentro" y el operador ve la advertencia (CA-24; advertencia Propuesta).
- [ ] **Dado** un movimiento registrado **entonces** identifica sede, zona/punto, responsable, visita y operador (CA-25).
- [ ] **Dado** catálogos inactivados **cuando** consulto el histórico con permiso **entonces** sigue accesible (CA-25, CA-15).
- [ ] **Dado** un reenvío con la misma clave **entonces** no hay duplicados (CA-12).
- [ ] **Dado** otra organización o sede sin alcance **entonces** 404/403 (CA-01, CA-02).

### Tareas backend
- [ ] Entidad y migración con índice parcial "entrada abierta por dispositivo".
- [ ] Caso de uso por lote en una transacción con idempotencia.
- [ ] Consulta de equipos dentro y aviso en la respuesta de salida de persona (HU-034).
- [ ] Novedad con permiso `equipos.novedad.registrar` y motivo obligatorio (Propuesta).

### Tareas frontend
- [ ] Selección de equipos en recepción, vista "equipos dentro" por sede, advertencia al registrar salida de persona, historial por equipo.

### Pruebas
- [ ] Unitarias: transiciones y validación de lote.
- [ ] Integración (PostgreSQL real): concurrencia y reintentos; la salida de persona no toca equipos.
- [ ] Aislamiento / permisos negativos: A vs B, sin alcance, sin permiso de novedad, módulo deshabilitado.
- [ ] E2E / UI: salida con equipos pendientes en tablet.

### Definition of Done
Cumple la [[PLAN-DE-TRABAJO#Definition of Done general|DoD general]] y además:
- [ ] Prueba automatizada que garantiza que la salida de persona no cierra equipos.
- [ ] "Equipos dentro" coincide con los movimientos abiertos.

### Riesgos y decisiones pendientes
> [!todo] D-10 — Bloqueo, salida con otro responsable y transferencia de custodia: no implementar sin política aprobada.

> [!todo] D-08 — Regularización de salidas de equipo omitidas.
