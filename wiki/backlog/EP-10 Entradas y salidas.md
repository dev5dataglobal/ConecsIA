---
tipo: epica
estado: propuesta
fase: F3
requisitos: [RF-13, RF-14, RF-15, RN-02, RN-03, RN-05, RN-06, RN-07, CA-09, CA-10, CA-12, CA-13, CA-21]
adr: [ADR-004, ADR-005, ADR-007, ADR-008, ADR-009, ADR-011, ADR-012, ADR-014, ADR-015, ADR-019, ADR-020]
decisiones: [D-04, D-05, D-06, D-07, D-08, D-13, D-15]
actualizado: 2026-10-17
tags: [conecsia, backlog, epica]
---

# EP-10 Entradas y salidas

> [!note] Fase: [[PLAN-DE-TRABAJO#F3]] · Requisitos: RF-13, RF-14, RN-02, RN-07 · ADR: [[ADR-007 Concurrencia e idempotencia en accesos]] (Propuesta)

## Objetivo de la épica
Registrar en servidor entradas validadas y salidas que cierran la [[Permanencia]] correcta, de forma atómica, idempotente y auditada, con una pantalla de recepción usable en computador, tablet y móvil.

## Alcance / fuera de alcance
- **Dentro (Base):** validación completa de entrada (RF-13), registro de [[Movimiento]] de entrada/salida, apertura/cierre de permanencia, motivos de denegación, detección de inconsistencias, recepción responsiva (CA-21).
- **Dentro (Propuesta PRD):** consumo atómico + `Idempotency-Key`; una permanencia abierta por persona y sede (sujeta a D-07); salida permitida aunque la autorización se revoque después del ingreso.
- **Fuera:** regularización de omisiones (bloqueada por D-08), reingresos y cupos por modalidad (D-05), presencia simultánea entre sedes (D-07), vehículos/equipos (EP-16/EP-17), modo offline.

## Historias
| ID | Historia | Tamaño | Estado | Bloqueada por |
|---|---|---|---|---|
| HU-032 | Registrar entrada validada | L | Lista para refinar | D-05, D-06 (parcial) |
| HU-033 | Consumo atómico e idempotencia de la entrada | L | Lista para refinar | D-07 (restricción entre sedes) |
| HU-034 | Registrar salida y cerrar permanencia | M | Lista para refinar | — |
| HU-035 | Tratamiento de inconsistencias de entrada/salida | M | Bloqueada (regularización) | D-08 |
| HU-036 | Pantalla de recepción responsiva | L | Lista para refinar | D-13, D-15 (parcial) |

---

## HU-032 Registrar entrada validada

**Como** operador de acceso **quiero** confirmar la entrada de una persona solo cuando el servidor valida todas sus condiciones **para** que nadie ingrese sin cumplir política, vigencia y lugar.

| Campo | Valor |
|---|---|
| Épica / Fase | EP-10 / F3 |
| Requisitos | RF-13, RN-02, RN-03, RN-05, RN-06 |
| Tamaño relativo | L (propuesto, sin estimación aprobada) |
| Estado | Lista para refinar |

### Objetivo
Comando `RegistrarEntrada` que evalúa identidad, estado de [[Persona]], política, vigencia calculada de la [[Autorización]], [[Sede]], [[Área o destino]], [[Zona y punto de acceso|zonas]], horario y permisos del operador; devuelve éxito confirmado o motivo concreto (Base, PRD §6.5).

### Actores
Operador de acceso (principal); Administrador de sede; sistema (auditoría/outbox).

### Dependencias
- Historias: HU-005, HU-007, HU-030, HU-031 e historias de autorización/visita de F2.
- ADR: [[ADR-005 Contexto de organización y autorización por permisos]], [[ADR-008 Modelo de tiempo y zonas horarias]], [[ADR-009 Ciclos de estado separados y vigencia calculada]], [[ADR-019 Zona única con atributo de punto de acceso]], [[ADR-020 Contratos API y formato de errores]] (todos Propuesta).
- Decisiones: D-04 (cuándo se exige autorización), D-05 (reingresos/cupos), D-06 (zona horaria, nocturnos, límites).

### Contrato funcional
- **Entradas:** `personaId` o documento, `autorizacionId` o token QR, `sedeId`, `puntoAccesoId`, `destinoId`, zonas, observaciones. `organizationId` **solo** derivado del contexto de membresía, nunca del cuerpo.
- **Reglas (servidor):** permiso `accesos.entrada.registrar` + sede/punto asignados; persona activa e identificada (RN-02); autorización Aprobada **y** vigente: fecha, días, horario en zona horaria de la sede, sede, zonas y cupo (RN-05, RN-06); ingreso sin autorización solo si la política lo permite, registrando la política aplicada sin fabricar aprobación (PRD §6.3).
- **Efectos:** ver HU-033 (misma transacción): Movimiento de entrada, Permanencia Abierta, [[Visita]] → Ingresó, consumo de cupo, auditoría, outbox.
- **Denegaciones (ADR-020 aprobado: `200` con `resultado: "Denegada"` y código de motivo; `403` solo para `PUNTO_NO_ASIGNADO`):** `PERSONA_INACTIVA`, `PERSONA_NO_IDENTIFICADA`, `AUTORIZACION_REQUERIDA`, `AUTORIZACION_NO_APROBADA`, `AUTORIZACION_NO_VIGENTE`, `AUTORIZACION_REVOCADA`, `AUTORIZACION_VENCIDA`, `AUTORIZACION_AGOTADA`, `FUERA_DE_HORARIO`, `SEDE_NO_AUTORIZADA`, `ZONA_NO_AUTORIZADA`, `PUNTO_NO_ASIGNADO` (403), `PERMANENCIA_YA_ABIERTA`. Recurso de otra organización ⇒ 404.
- **Auditoría:** entrada confirmada y también intento **denegado** (Propuesta PRD §12.6) con motivo, actor, punto, instante y resultado.
- **Endpoints propuestos:** `POST /api/v1/accesos/entradas/validar` (sin efectos) · `POST /api/v1/accesos/entradas` (confirma).

### Criterios de aceptación
- [ ] **Dado** una persona inactiva **cuando** se confirma la entrada **entonces** se deniega con `PERSONA_INACTIVA` y no se abre permanencia (CA-09).
- [ ] **Dado** una autorización aprobada para mañana **cuando** se intenta entrar hoy **entonces** se deniega con `AUTORIZACION_NO_VIGENTE` (CA-09).
- [ ] **Dado** sede, zona u horario fuera de lo autorizado **cuando** se confirma **entonces** se deniega con el motivo correspondiente y se audita el intento (CA-09, CA-20).
- [ ] **Dado** una visita pendiente con política que exige aprobación **cuando** se intenta ingresar **entonces** se deniega (CA-07).
- [ ] **Dado** un operador sin el punto asignado **cuando** llama la API directamente **entonces** recibe 403 (CA-02).
- [ ] **Dado** condiciones válidas **cuando** se confirma **entonces** quedan persistidos persona, instante, sede, punto, autorización y operador.

### Tareas backend
- [ ] Servicio de dominio `EvaluadorDeIngreso` con reglas ordenadas y catálogo de códigos de motivo.
- [ ] Endpoints validar (lectura) y confirmar (comando) con mapeo a ProblemDetails.
- [ ] Reingreso sobre autorización ya usada: **rechazado por defecto y parametrizable** hasta D-05.

### Tareas frontend
- [ ] Panel de resultado de validación con motivo legible por código; botón confirmar explícito.

### Pruebas
- [ ] Unitarias: cada regla del evaluador; bordes de horario según ADR-008 (límites marcados pendientes por D-06).
- [ ] Integración (PostgreSQL real): una denegación no deja filas en movimiento/permanencia/cupo.
- [ ] Concurrencia/idempotencia: cubierta en HU-033.
- [ ] Aislamiento / permisos negativos: autorización o persona de otra organización ⇒ 404; operador sin permiso ⇒ 403; `organizationId` manipulado en el cuerpo se ignora.
- [ ] E2E / UI: buscar persona → validar → confirmar.

### Definition of Done
Cumple la [[PLAN-DE-TRABAJO#Definition of Done general|DoD general]] y además:
- [ ] Catálogo de códigos de motivo publicado en el contrato OpenAPI.
- [ ] CA-09 verificado con una prueba automatizada por cada causa.

### Riesgos y decisiones pendientes
> [!todo] D-05 — reingresos y cupos por modalidad: hasta decidir, el reingreso sobre autorización única consumida se rechaza (`AUTORIZACION_AGOTADA`).

> [!todo] D-06 — límites inclusivos/exclusivos, horarios nocturnos y festivos.

---

## HU-033 Consumo atómico e idempotencia de la entrada

**Como** operador de acceso **quiero** que dos confirmaciones simultáneas o repetidas produzcan una sola entrada **para** que una autorización única no se consuma dos veces.

| Campo | Valor |
|---|---|
| Épica / Fase | EP-10 / F3 |
| Requisitos | RF-13, RF-10, CA-10, CA-12 |
| Tamaño relativo | L (propuesto, sin estimación aprobada) |
| Estado | Lista para refinar (restricción entre sedes bloqueada por D-07) |

### Objetivo
Validar, consumir cupo, crear [[Movimiento]], abrir [[Permanencia]], actualizar [[Visita]], auditar y escribir outbox en **una sola transacción**, con clave de idempotencia por comando (Propuesta ADR-007).

### Actores
Operador de acceso; sistema.

### Dependencias
- Historias: HU-032, HU-005.
- ADR: [[ADR-007 Concurrencia e idempotencia en accesos]], [[ADR-012 Outbox transaccional y procesos de fondo]], [[ADR-014 Auditoría append-only]], [[ADR-003 EF Core con Npgsql y migraciones versionadas]] (Propuesta).
- Decisiones: D-07 (duplicados por sede y simultaneidad entre sedes), D-05.

### Contrato funcional
- **Entradas:** comando de HU-032 + cabecera `Idempotency-Key` obligatoria.
- **Reglas (servidor):** `SELECT … FOR UPDATE` sobre la autorización o `UPDATE … WHERE usos < cupo`; revocación y entrada serializadas sobre la misma fila; índice único parcial `(organization_id, persona_id, sede_id) WHERE estado = 'Abierta'` (**Propuesta PRD, sujeta a D-07**). Presencia simultánea en sedes distintas: **sin regla nueva** hasta D-07 (no se crea restricción ni se declara permitida).
- **Efectos:** tabla `idempotencia(organization_id, clave, operacion, hash_solicitud, resultado, creado_utc)`; misma clave + mismo hash ⇒ devuelve el resultado original; misma clave + hash distinto ⇒ 422.
- **Errores:** `IDEMPOTENCY_KEY_REQUERIDA` (400), `IDEMPOTENCY_KEY_CONFLICTO` (422), `AUTORIZACION_AGOTADA` (409), `PERMANENCIA_YA_ABIERTA` (409), `CONFLICTO_CONCURRENCIA` (409, reintentable).
- **Auditoría:** en la misma transacción; el perdedor de la carrera se audita como intento denegado.
- **Eventos:** SignalR y correo vía outbox, publicados **después del commit**; una falla de correo no revierte la entrada.
- **Endpoints propuestos:** `POST /api/v1/accesos/entradas` (con `Idempotency-Key`).

### Criterios de aceptación
- [ ] **Dado** una autorización única **cuando** dos operadores confirman a la vez **entonces** exactamente una entrada queda confirmada y la otra recibe `AUTORIZACION_AGOTADA` o `PERMANENCIA_YA_ABIERTA` (CA-10).
- [ ] **Dado** una entrada confirmada **cuando** se reenvía la misma solicitud con la misma clave **entonces** se devuelve el mismo resultado sin nuevo movimiento (CA-12).
- [ ] **Dado** una revocación concurrente con la entrada **cuando** ambas se ejecutan **entonces** o la entrada precede a la revocación o se deniega (CA-09).
- [ ] **Dado** un fallo en cualquier paso **cuando** la transacción aborta **entonces** no quedan movimiento, permanencia, cupo consumido, auditoría de éxito ni mensaje outbox.

### Tareas backend
- [ ] Migración: índice único parcial y tabla de idempotencia con unicidad `(organization_id, clave)`.
- [ ] Filtro de idempotencia para comandos de acceso (entrada y salida).
- [ ] Unidad de trabajo única para el caso de uso; violación de unicidad ⇒ `PERMANENCIA_YA_ABIERTA`.

### Tareas frontend
- [ ] Generar `Idempotency-Key` por intento de confirmación y reutilizarla en reintentos; bloquear doble clic.

### Pruebas
- [ ] Unitarias: hash de solicitud y reglas de reuso de clave.
- [ ] Integración (PostgreSQL real, Testcontainers): rollback completo ante excepción inyectada en cada paso.
- [ ] Concurrencia/idempotencia: N tareas paralelas sobre autorización única ⇒ 1 éxito; entrada vs revocación en paralelo.
- [ ] Aislamiento: la misma clave en dos organizaciones no colisiona ni filtra resultados.
- [ ] E2E: doble envío con red lenta muestra un único ingreso.

### Definition of Done
Cumple la [[PLAN-DE-TRABAJO#Definition of Done general|DoD general]] y además:
- [ ] Prueba de concurrencia estable en CI (repetida ≥ 100 veces sin falso resultado).
- [ ] Vida útil de claves de idempotencia parametrizada (valor Pendiente, D-14/D-15).

### Riesgos y decisiones pendientes
> [!todo] D-07 — confirmar índice por sede y la política entre sedes; hasta decidir no se agrega restricción entre sedes.

> [!warning] Bloqueos de fila en horas pico; dimensionar con D-15.

---

## HU-034 Registrar salida y cerrar permanencia

**Como** operador de acceso **quiero** registrar la salida de quien está dentro **para** cerrar su permanencia correcta y calcular la duración.

| Campo | Valor |
|---|---|
| Épica / Fase | EP-10 / F3 |
| Requisitos | RF-14, RN-07, RN-09, CA-11, CA-12, CA-13 |
| Tamaño relativo | M (propuesto, sin estimación aprobada) |
| Estado | Lista para refinar |

### Objetivo
Cerrar con actualización condicional la [[Permanencia]] abierta, registrar [[Movimiento]] de salida y finalizar la [[Visita]], sin cerrar una [[Autorización]] recurrente vigente.

### Actores
Operador de acceso; sistema.

### Dependencias
- Historias: HU-032, HU-033.
- ADR: [[ADR-007 Concurrencia e idempotencia en accesos]], [[ADR-009 Ciclos de estado separados y vigencia calculada]], [[ADR-011 Tiempo real con SignalR]], [[ADR-012 Outbox transaccional y procesos de fondo]] (Propuesta).
- Decisiones: D-07; D-10 (los equipos no se cierran con la salida de la persona).

### Contrato funcional
- **Entradas:** `permanenciaId` o `personaId` + `sedeId`, `puntoAccesoId`, observaciones, `Idempotency-Key`.
- **Reglas (servidor):** permiso `accesos.salida.registrar` + punto asignado; `UPDATE … SET estado = 'Cerrada' WHERE id = @id AND estado = 'Abierta'`; duración = salida − entrada en UTC (≥ 0). **Propuesta PRD §6.6:** revocación o vencimiento posterior al ingreso **no** impide la salida; se conserva el estado histórico del ingreso. Autorización recurrente sigue Aprobada (RN-09). Equipos asociados no se cierran.
- **Efectos:** Movimiento de salida, Permanencia Cerrada con duración, Visita → Finalizada, auditoría, outbox (evento de presencia tras commit).
- **Errores:** `PERMANENCIA_NO_ABIERTA` (409, deriva a HU-035), `PUNTO_NO_ASIGNADO` (403); reintento con la misma clave devuelve el resultado original.
- **Auditoría:** salida con actor, punto, instante y permanencia afectada.
- **Endpoints propuestos:** `POST /api/v1/accesos/salidas`.

### Criterios de aceptación
- [ ] **Dado** una permanencia abierta **cuando** se registra la salida **entonces** se cierra ese ingreso y la duración no es negativa (CA-13).
- [ ] **Dado** una autorización revocada después del ingreso **cuando** se registra la salida **entonces** se permite (CA-13, Propuesta).
- [ ] **Dado** una salida ya registrada **cuando** se reintenta **entonces** no se crea otra salida (CA-12).
- [ ] **Dado** una autorización recurrente **cuando** termina la visita **entonces** la autorización sigue habilitando visitas en ventanas válidas (CA-11).

### Tareas backend
- [ ] Caso de uso `RegistrarSalida` transaccional reutilizando el filtro de idempotencia de HU-033.
- [ ] Consulta de permanencias abiertas por persona/sede filtrada por organización.

### Tareas frontend
- [ ] Acción "Registrar salida" desde la búsqueda y desde personas dentro; confirmación solo tras respuesta del servidor.

### Pruebas
- [ ] Unitarias: cálculo de duración y transición de visita.
- [ ] Integración (PostgreSQL real): revocar autorización y luego registrar salida.
- [ ] Concurrencia/idempotencia: dos salidas simultáneas ⇒ una sola salida.
- [ ] Aislamiento / permisos negativos: permanencia de otra organización ⇒ 404; operador sin permiso ⇒ 403.
- [ ] E2E: entrada → salida → aparece en historial.

### Definition of Done
Cumple la [[PLAN-DE-TRABAJO#Definition of Done general|DoD general]] y además:
- [ ] CA-11, CA-12 y CA-13 verificados automáticamente.

### Riesgos y decisiones pendientes
> [!todo] La salida tras revocación es Propuesta del PRD; confirmarla al aprobar ADR-007/ADR-009.

---

## HU-035 Tratamiento de inconsistencias de entrada/salida

**Como** operador de acceso **quiero** que el sistema muestre explícitamente salidas sin entrada abierta y entradas sin salida **para** no alterar el historial en silencio.

| Campo | Valor |
|---|---|
| Épica / Fase | EP-10 / F3 |
| Requisitos | RN-07, RF-14 |
| Tamaño relativo | M (propuesto, sin estimación aprobada) |
| Estado | Detección: Lista para refinar · Regularización: **Bloqueada por D-08** |

### Objetivo
Detectar y registrar inconsistencias como registros propios, visibles y auditados. La corrección/regularización no se implementa hasta resolver D-08.

### Actores
Operador de acceso; Administrador de sede; Auditor (consulta).

### Dependencias
- Historias: HU-034, HU-040 (alerta de persona sin salida), HU-046.
- ADR: [[ADR-014 Auditoría append-only]], [[ADR-007 Concurrencia e idempotencia en accesos]] (Propuesta).
- Decisiones: **D-08** (regularización, permisos, evidencia), D-07.

### Contrato funcional
- **Entradas:** intento de salida sin permanencia abierta; consulta de inconsistencias por sede.
- **Reglas (servidor):** salida sin permanencia abierta ⇒ se rechaza y se crea `inconsistencia_acceso` (tipo `SALIDA_SIN_ENTRADA`, persona, sede, punto, operador, instante). Nunca se crea una entrada ficticia ni se modifica un movimiento. Regularización: endpoint **no disponible** hasta D-08.
- **Efectos:** registro de inconsistencia + auditoría; sin cambios en movimientos ni permanencias.
- **Errores:** `PERMANENCIA_NO_ABIERTA` (409), `REGULARIZACION_NO_DISPONIBLE` (501).
- **Auditoría:** creación de la inconsistencia con actor y resultado.
- **Endpoints propuestos:** `GET /api/v1/accesos/inconsistencias?sedeId=` · `POST /api/v1/accesos/regularizaciones` (Pendiente D-08).

### Criterios de aceptación
- [ ] **Dado** una persona sin permanencia abierta **cuando** se registra su salida **entonces** se muestra la inconsistencia y no se crea movimiento (RN-07, CA-13).
- [ ] **Dado** inconsistencias registradas **cuando** se consultan **entonces** solo aparecen las de la organización y sedes del usuario (CA-01).
- [ ] **Dado** cualquier usuario **cuando** intenta modificar un movimiento histórico **entonces** no existe operación que lo permita (CA-20).

### Tareas backend
- [ ] Entidad y tabla `inconsistencia_acceso` (solo inserción; sin estado de resolución hasta D-08).
- [ ] Consulta paginada por sede con filtro de organización.

### Tareas frontend
- [ ] Aviso explícito en recepción y lista de inconsistencias para administrador de sede.

### Pruebas
- [ ] Unitarias: clasificación de inconsistencias.
- [ ] Integración (PostgreSQL real): movimientos existentes intactos.
- [ ] Concurrencia/idempotencia: el mismo reintento no genera dos inconsistencias.
- [ ] Aislamiento / permisos negativos: inconsistencias de otra organización invisibles; operador sin permiso de consulta ⇒ 403.
- [ ] E2E: salida sin entrada muestra aviso.

### Definition of Done
Cumple la [[PLAN-DE-TRABAJO#Definition of Done general|DoD general]] y además:
- [ ] Ninguna ruta de código actualiza ni borra movimientos (revisión + prueba).

### Riesgos y decisiones pendientes
> [!todo] D-08 — regularización de entradas/salidas omitidas, permiso específico, motivo y evidencia. Bloquea la segunda mitad de la historia y el piloto operativo.

---

## HU-036 Pantalla de recepción responsiva

**Como** operador de acceso **quiero** una pantalla de recepción que funcione en computador, tablet y móvil **para** buscar, validar y registrar entradas y salidas con validaciones visibles.

| Campo | Valor |
|---|---|
| Épica / Fase | EP-10 / F3 |
| Requisitos | CA-21, PRD §9.2, RF-13, RF-14, RF-15 |
| Tamaño relativo | L (propuesto, sin estimación aprobada) |
| Estado | Lista para refinar |

### Objetivo
Interfaz operativa sobre la API de HU-031..HU-035 que no decide permisos ni validez por sí sola.

### Actores
Operador de acceso; Administrador de sede.

### Dependencias
- Historias: HU-002, HU-031, HU-032, HU-033, HU-034, HU-035, HU-037.
- ADR: [[ADR-015 Frontend SPA con React, TypeScript y Tailwind]], [[ADR-011 Tiempo real con SignalR]], [[ADR-020 Contratos API y formato de errores]] (Propuesta).
- Decisiones: D-13 (lectores QR), D-15 (frescura de pantalla), D-01 (terminología).

### Contrato funcional
- **Entradas:** búsqueda por documento/nombre, lectura de [[QR]] (cámara o lector), punto de acceso asignado.
- **Reglas:** la UI muestra solo lo que devuelve la API; ocultar controles no sustituye permisos; nunca indica "guardado" sin respuesta 2xx; estado de conexión visible (PRD §7.2).
- **Efectos:** ninguno propio; delega en endpoints de entrada/salida.
- **Errores:** traduce códigos de motivo a mensajes; distingue error de red (reintento con la misma `Idempotency-Key`) de denegación.
- **Auditoría:** la realiza el backend.
- **Endpoints consumidos:** `/accesos/entradas/validar`, `/accesos/entradas`, `/accesos/salidas`, `/qr/validar`, `/presencia`, `GET /api/v1/mis-puntos-acceso` (nuevo).

### Criterios de aceptación
- [ ] **Dado** un móvil de 375 px **cuando** se ejecuta buscar → validar → entrar → salir **entonces** el flujo se completa sin scroll horizontal y con validaciones visibles (CA-21).
- [ ] **Dado** pérdida de conexión **cuando** se intenta confirmar **entonces** la UI no marca la entrada como registrada (CA-14, §7.2).
- [ ] **Dado** una denegación **cuando** llega el código **entonces** se muestra el motivo concreto (CA-09).

### Tareas backend
- [ ] Endpoint de puntos de acceso asignados al operador actual.

### Tareas frontend
- [ ] Vistas de recepción, validación, confirmación y salida rápida; lector QR por cámara; navegación por teclado.
- [ ] Indicador de conexión SignalR y última actualización.

### Pruebas
- [ ] Unitarias: mapeo código → mensaje; manejo de reintentos.
- [ ] Integración: pruebas de contrato contra la API real.
- [ ] Permisos negativos: usuario sin permiso no accede a recepción y la API responde 403.
- [ ] E2E (Playwright): computador, tablet y móvil.

### Definition of Done
Cumple la [[PLAN-DE-TRABAJO#Definition of Done general|DoD general]] y además:
- [ ] E2E en tres tamaños de viewport en CI.

### Riesgos y decisiones pendientes
> [!todo] D-13 — lectores QR compatibles · D-15 — frescura máxima de pantalla · D-01 — etiquetas sectoriales.
