---
tipo: epica
estado: propuesta
fase: F5
requisitos: [RF-16, CA-23, CA-01, CA-20]
adr: [ADR-004, ADR-005, ADR-007, ADR-009, ADR-014, ADR-019, ADR-020]
decisiones: [D-02, D-10, D-16]
actualizado: 2026-10-17
tags: [conecsia, backlog, epica, vehiculos]
---

# EP-16 Vehículos

> [!note] Fase: [[PLAN-DE-TRABAJO#F5]] · Requisitos: RF-16, [[Criterios de aceptación|CA-23]] · ADR: [[ADR-004 Tenancy con base compartida y organization_id]], [[ADR-007 Concurrencia e idempotencia en accesos]], [[ADR-014 Auditoría append-only]]

## Objetivo de la épica
Registrar vehículos por [[Organización]], asociarlos a [[Persona|personas]] y [[Autorización|autorizaciones]], y registrar sus entradas y salidas vinculadas a una [[Visita]] y su responsable, solo cuando el módulo esté habilitado (**Base**, RF-16).

## Alcance / fuera de alcance
- **Dentro (Base):** placa, tipo, marca, modelo opcional, color, propietario/responsable; asociación a personas/autorizaciones; movimientos de entrada/salida vinculados a visita y responsable; consulta histórica con permiso.
- **Dentro (Propuesta):** placa normalizada y unicidad por organización (el PRD exige definirlas; la regla concreta se valida en refinamiento).
- **Fuera / Pendiente (D-10):** acompañantes, cambio de conductor, cupos de parqueadero y salida independiente del vehículo respecto de la persona. **La salida de la persona no cierra el vehículo.**
- **Fuera:** reconocimiento automático de placas (LPR) e integración con barreras.
- **Módulo deshabilitado:** no se crean nuevos registros ni movimientos; el histórico se conserva y se consulta con permiso.

## Historias
| ID | Historia | Tamaño | Estado | Bloqueada por |
|---|---|---|---|---|
| HU-047 | Registrar vehículos y asociarlos a personas y autorizaciones | M | Lista para refinar | D-02 (prioridad) |
| HU-048 | Movimientos vehiculares | M | Lista para refinar (núcleo Base) | D-10 para reglas extendidas |

---

## HU-047 Registrar vehículos y asociarlos a personas y autorizaciones

**Como** administrador de sede u operador de acceso **quiero** registrar un vehículo y asociarlo a su responsable y a autorizaciones **para** controlar qué vehículos pueden ingresar.

| Campo | Valor |
|---|---|
| Épica / Fase | EP-16 / F5 |
| Requisitos | RF-16, CA-23, CA-01, CA-20 |
| Tamaño relativo | M (propuesto, sin estimación aprobada) |
| Estado | Lista para refinar |

### Objetivo
Catálogo de vehículos por organización, reutilizable, con vínculo N:M a personas y a autorizaciones.

### Actores
Administrador de organización/sede, Operador de acceso, Autorizador (asocia vehículo a su autorización), Auditor (lectura). Ver [[Usuarios, roles y permisos]].

### Dependencias
- Historias: HU-005, HU-007, HU-008, HU-011 (habilitación de módulos), HU-019, HU-023.
- ADR: [[ADR-004 Tenancy con base compartida y organization_id]] (Propuesta), [[ADR-005 Contexto de organización y autorización por permisos]] (Propuesta), [[ADR-014 Auditoría append-only]] (Propuesta), [[ADR-020 Contratos API y formato de errores]] (Propuesta).
- Decisiones: D-02 (¿lo requiere el primer cliente?), D-16 (módulos por plan), D-10 (cambio de responsable).

### Contrato funcional
- **Entradas:** placa, tipoVehiculoId (catálogo), marca, modelo?, color, responsablePersonaId, observaciones?.
- **Reglas (servidor):** módulo `vehiculos` habilitado para la organización del contexto (nunca la enviada por el cliente); permiso `vehiculos.gestionar`; placa normalizada (Propuesta: mayúsculas, sin espacios ni guiones, conservando el valor original) con índice único `(organization_id, placa_normalizada)`; responsable y autorización de la misma organización; alta concurrente de la misma placa ⇒ una sola fila (violación de unicidad → 409).
- **Efectos persistidos:** `vehiculo`, `vehiculo_persona`, `autorizacion_vehiculo`; inactivación lógica, sin borrado.
- **Errores / denegaciones:** 403 módulo deshabilitado o sin permiso; 404 recurso de otra organización (no revela existencia); 409 placa duplicada; 422 validación.
- **Auditoría:** crear, modificar (anterior/nuevo), inactivar, asociar/desasociar; sin datos personales completos en el evento.
- **Endpoints propuestos:** `POST/GET/PATCH /api/v1/vehiculos`, `POST /api/v1/vehiculos/{id}/inactivar`, `PUT /api/v1/autorizaciones/{id}/vehiculos` (indicativo).

### Criterios de aceptación
- [ ] **Dado** el módulo habilitado **cuando** registro placa, responsable y autorización **entonces** quedan persistidos y vinculados (CA-23).
- [ ] **Dado** una placa "ABC123" existente **cuando** se registra "abc-123", incluso en dos altas concurrentes **entonces** no se duplica (CA-23, regla Propuesta).
- [ ] **Dado** un usuario de la organización A **cuando** consulta o asocia un vehículo de B por ID **entonces** recibe 404 (CA-01).
- [ ] **Dado** el módulo deshabilitado **cuando** intento crear o asociar **entonces** se rechaza y el histórico sigue consultable con permiso (CA-23).
- [ ] **Dado** cualquier alta o cambio **entonces** se audita actor, organización, instante, acción y objeto (CA-20).

### Tareas backend
- [ ] Entidades, configuración EF Core y migración con `organization_id`, filtros de tenant e índice único.
- [ ] Normalizador de placa (función pura) y catálogo `tipo_vehiculo` por organización.
- [ ] Casos de uso crear/editar/inactivar/asociar con verificación de módulo y permiso.
- [ ] Auditoría y mapeo de errores a ProblemDetails.

### Tareas frontend
- [ ] Listado con búsqueda por placa, formulario, asociación desde la ficha de persona y de autorización; ocultar el menú con el módulo deshabilitado (solo UX; decide el servidor).

### Pruebas
- [ ] Unitarias: normalización de placa, validaciones.
- [ ] Integración (PostgreSQL real): unicidad bajo concurrencia; la inactivación preserva vínculos.
- [ ] Aislamiento / permisos negativos: A no lee ni asocia vehículos de B; sin `vehiculos.gestionar` ⇒ 403; módulo deshabilitado ⇒ 403.
- [ ] E2E / UI: alta y asociación en escritorio y móvil.

### Definition of Done
Cumple la [[PLAN-DE-TRABAJO#Definition of Done general|DoD general]] y además:
- [ ] Regla de normalización y ámbito de unicidad aprobados y documentados en la wiki.
- [ ] Matriz de permisos del módulo actualizada.

### Riesgos y decisiones pendientes
> [!todo] D-10 — Cambio de responsable y vehículos compartidos: no se implementa regla propia.

> [!todo] D-02 / D-16 — Prioridad para el primer cliente y plan que incluye el módulo.

> [!warning] Riesgo: placas extranjeras o sin formato estándar pueden romper la normalización; conservar el valor original.

---

## HU-048 Movimientos vehiculares

**Como** operador de acceso **quiero** registrar la entrada y salida de un vehículo vinculado a una visita y su responsable **para** saber qué vehículos entraron, salieron y siguen dentro.

| Campo | Valor |
|---|---|
| Épica / Fase | EP-16 / F5 |
| Requisitos | RF-16, CA-23, CA-12, CA-20 |
| Tamaño relativo | M (propuesto, sin estimación aprobada) |
| Estado | Lista para refinar (núcleo Base); reglas extendidas Bloqueadas por D-10 |

### Objetivo
Registrar movimientos vehiculares idempotentes e independientes del [[Movimiento]] y la [[Permanencia]] de la persona.

### Actores
Operador de acceso, Administrador de sede, Auditor.

### Dependencias
- Historias: HU-047, HU-032, HU-033, HU-034, HU-018, HU-036.
- ADR: [[ADR-007 Concurrencia e idempotencia en accesos]] (Propuesta), [[ADR-009 Ciclos de estado separados y vigencia calculada]] (Propuesta), [[ADR-019 Zona única con atributo de punto de acceso]] (Propuesta), [[ADR-008 Modelo de tiempo y zonas horarias]] (Propuesta), [[ADR-011 Tiempo real con SignalR]] (Propuesta).
- Decisiones: **D-10** (acompañantes, cambio de conductor, parqueaderos, salida independiente), D-07, D-08.

### Contrato funcional
- **Entradas:** vehiculoId, visitaId, tipo (entrada/salida), puntoAccesoId ([[Zona y punto de acceso|zona con `esPuntoAcceso`]]), clave de idempotencia (header).
- **Reglas (servidor):** módulo habilitado; permiso `vehiculos.movimientos.registrar` y alcance sobre la sede/punto; vehículo activo y asociado a la persona o a la autorización de la visita; visita abierta para la entrada; sin segunda entrada abierta del mismo vehículo (índice parcial + transacción); instante del servidor (`timestamptz`).
- **Efectos persistidos:** `movimiento_vehiculo` (vehículo, visita, responsable, sede, punto, operador, instante, tipo); estado "dentro" derivado.
- **No hace:** no cierra el vehículo al salir la persona ni la persona al salir el vehículo; no controla cupos de parqueadero (D-10).
- **Errores / denegaciones:** 403 módulo/permiso/alcance; 404 otra organización; 409 vehículo ya dentro o sin entrada abierta; 422 vehículo no asociado.
- **Auditoría:** entrada, salida e intentos denegados (Propuesta) con causa.
- **Endpoints propuestos:** `POST /api/v1/vehiculos/{id}/entradas`, `POST /api/v1/vehiculos/{id}/salidas`, `GET /api/v1/vehiculos/dentro?sedeId=`, `GET /api/v1/vehiculos/{id}/movimientos` (indicativo).

### Criterios de aceptación
- [ ] **Dado** un vehículo asociado y una visita abierta **cuando** registro la entrada **entonces** el movimiento queda vinculado a visita, responsable, sede, punto y operador (CA-23).
- [ ] **Dado** la misma solicitud reenviada con igual clave **cuando** llega al servidor **entonces** no se duplica el movimiento (CA-12).
- [ ] **Dado** una persona que registra salida con su vehículo dentro **cuando** se confirma **entonces** el vehículo sigue dentro y el operador ve un aviso (Propuesta; CA-23, D-10).
- [ ] **Dado** el módulo deshabilitado **cuando** intento registrar un movimiento **entonces** se rechaza y el histórico permanece consultable (CA-23).
- [ ] **Dado** un operador de otra organización o sin alcance a la sede **cuando** registra **entonces** recibe 404/403 (CA-01, CA-02).

### Tareas backend
- [ ] Entidad, migración e índice parcial único "entrada abierta por vehículo".
- [ ] Casos de uso entrada/salida con transacción e idempotencia según ADR-007.
- [ ] Consulta "vehículos dentro" por sede con alcance; evento en tiempo real por organización/sede.
- [ ] Auditoría y ProblemDetails.

### Tareas frontend
- [ ] Sección vehículo en recepción (HU-036), listado de vehículos dentro, historial por vehículo y aviso de vehículo pendiente al registrar la salida de la persona.

### Pruebas
- [ ] Unitarias: transiciones entrada/salida.
- [ ] Integración (PostgreSQL real): doble envío y dos operadores concurrentes ⇒ un solo movimiento.
- [ ] Aislamiento / permisos negativos: otra organización, otra sede, sin permiso, módulo deshabilitado.
- [ ] E2E / UI: recepción con persona + vehículo en tablet.

### Definition of Done
Cumple la [[PLAN-DE-TRABAJO#Definition of Done general|DoD general]] y además:
- [ ] Prueba automatizada que verifica que la salida de la persona no altera movimientos vehiculares.
- [ ] Reglas de D-10 documentadas como pendientes sin lógica implícita en el código.

### Riesgos y decisiones pendientes
> [!todo] D-10 — Acompañantes, cambio de conductor, cupos de parqueadero y salida independiente: sin regla aprobada; no implementar.

> [!todo] D-08 — Regularización de salidas vehiculares omitidas.

> [!warning] Riesgo: si D-10 exige otro modelo (p. ej. conductor por movimiento) podría requerir migración adicional.
