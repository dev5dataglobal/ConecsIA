---
tipo: epica
estado: propuesta
fase: F4
requisitos: [RF-21, CA-09, CA-16]
adr: [ADR-004, ADR-005, ADR-008, ADR-011, ADR-012, ADR-014]
decisiones: [D-06, D-08, D-12, D-15]
actualizado: 2026-10-17
tags: [conecsia, backlog, epica]
---

# EP-12 Alertas

> [!note] Fase: [[PLAN-DE-TRABAJO#F4]] · Requisitos: RF-21 · ADR: [[ADR-012 Outbox transaccional y procesos de fondo]] (Propuesta)

## Objetivo de la épica
Generar alertas operativas con motivo, entidad vinculada y seguimiento, deduplicadas, y gestionar su ciclo Nueva → Revisada → Cerrada sin efectos colaterales sobre autorizaciones o permanencias.

## Alcance / fuera de alcance
- **Dentro (Base, PRD §11.1):** próximo vencimiento/vencida, intento sin autorización, fuera de horario, sede/zona no autorizada, permanencia prolongada, persona sin salida, visita pendiente, autorización revocada, reutilización no permitida de QR.
- **Dentro (Propuesta):** deduplicación por evento y política; umbrales parametrizables por organización.
- **Fuera:** valores de umbral (D-12), definición de "persona sin salida" (pendiente, PRD §10.2), notificación por correo de alertas (EP-13 según D-12).

## Historias
| ID | Historia | Tamaño | Estado | Bloqueada por |
|---|---|---|---|---|
| HU-040 | Generar alertas operativas | L | Lista para refinar (parcial) | D-12 (umbrales) |
| HU-041 | Gestionar ciclo de alerta Nueva → Revisada → Cerrada | S | Lista para refinar | — |

---

## HU-040 Generar alertas operativas

**Como** administrador de sede **quiero** que el sistema genere alertas con su motivo **para** atender novedades operativas.

| Campo | Valor |
|---|---|
| Épica / Fase | EP-12 / F4 |
| Requisitos | RF-21, CA-16 |
| Tamaño relativo | L (propuesto, sin estimación aprobada) |
| Estado | Lista para refinar; alertas por umbral **bloqueadas por D-12** |

### Objetivo
Crear alertas a partir de (a) denegaciones de entrada (síncronas, vía outbox) y (b) procesos programados (vencimientos, permanencias), con deduplicación.

### Actores
Sistema (generador); Administrador de sede y de organización (destinatarios).

### Dependencias
- Historias: HU-032, HU-033, HU-031 (reutilización de QR), HU-037.
- ADR: [[ADR-012 Outbox transaccional y procesos de fondo]], [[ADR-008 Modelo de tiempo y zonas horarias]], [[ADR-011 Tiempo real con SignalR]], [[ADR-014 Auditoría append-only]] (Propuesta).
- Decisiones: **D-12** (umbrales), D-06, D-08 (persona sin salida), D-15.

### Contrato funcional
- **Entradas:** eventos de dominio (`EntradaDenegada{motivo}`, `AutorizacionRevocada`, `QrReutilizado`) y tareas programadas.
- **Reglas (servidor):** cada alerta lleva `organization_id`, `sede_id`, tipo, entidad vinculada, instante, estado Nueva; clave de deduplicación `(organization_id, tipo, entidad_id, ventana)` única; tareas programadas con bloqueo distribuido (advisory lock) para no ejecutarse dos veces; umbrales leídos de configuración por organización: **sin valor por defecto** — si no está configurado, la alerta de umbral no se genera (D-12). "Persona sin salida" no se activa hasta definirse (PRD §10.2).
- **Efectos:** fila `alerta` + outbox (`AlertaCreada` para SignalR tras commit).
- **Errores:** duplicado ⇒ se ignora (idempotente).
- **Auditoría:** creación por actor sistema.
- **Endpoints propuestos:** `GET /api/v1/alertas?sedeId=&estado=&tipo=` · `PUT /api/v1/organizacion/parametros-alertas` (admin org).

### Criterios de aceptación
- [ ] **Dado** una entrada denegada fuera de horario **cuando** se registra **entonces** se genera una alerta con motivo `FUERA_DE_HORARIO` (CA-16, CA-09).
- [ ] **Dado** un QR de autorización única ya consumido **cuando** se vuelve a escanear **entonces** se genera alerta de reutilización (CA-16).
- [ ] **Dado** el mismo evento repetido **cuando** se procesa dos veces **entonces** existe una sola alerta.
- [ ] **Dado** una organización sin umbral configurado **cuando** corre la tarea **entonces** no genera alertas de umbral.

### Tareas backend
- [ ] Entidad `alerta`, índice único de deduplicación, manejadores de eventos.
- [ ] `BackgroundService` programado con advisory lock por organización.
- [ ] Parámetros de alertas por organización con auditoría de cambios.

### Tareas frontend
- [ ] Listado de alertas por sede con tipo y motivo; formulario de parámetros (admin).

### Pruebas
- [ ] Unitarias: generadores por tipo y clave de deduplicación.
- [ ] Integración (PostgreSQL real): alerta creada solo si la transacción origen confirma.
- [ ] Concurrencia/idempotencia: dos instancias del job ⇒ sin duplicados.
- [ ] Aislamiento / permisos negativos: alertas de otra organización invisibles; parámetros solo admin.
- [ ] E2E: denegación → alerta visible en tiempo real.

### Definition of Done
Cumple la [[PLAN-DE-TRABAJO#Definition of Done general|DoD general]] y además:
- [ ] Cada uno de los 9 escenarios de RF-21 marcado como implementado o bloqueado con su D-xx.

### Riesgos y decisiones pendientes
> [!todo] D-12 — umbrales de próximo vencimiento y permanencia prolongada.

> [!todo] D-08 / PRD §10.2 — definición de "persona sin salida".

---

## HU-041 Gestionar ciclo de alerta Nueva → Revisada → Cerrada

**Como** administrador de sede **quiero** marcar alertas como revisadas y cerradas con seguimiento **para** dejar constancia de su atención.

| Campo | Valor |
|---|---|
| Épica / Fase | EP-12 / F4 |
| Requisitos | RF-21, CA-16 |
| Tamaño relativo | S (propuesto, sin estimación aprobada) |
| Estado | Lista para refinar |

### Objetivo
Transiciones validadas en servidor que conservan usuario, fecha y observación; cerrar una alerta no modifica autorizaciones ni permanencias.

### Actores
Administrador de sede; Administrador de organización; Auditor (lectura).

### Dependencias
- Historias: HU-040.
- ADR: [[ADR-009 Ciclos de estado separados y vigencia calculada]], [[ADR-014 Auditoría append-only]], [[ADR-020 Contratos API y formato de errores]] (Propuesta).
- Decisiones: ninguna bloqueante.

### Contrato funcional
- **Entradas:** `alertaId`, transición (`revisar`/`cerrar`), observación, versión (concurrencia optimista).
- **Reglas (servidor):** permiso `alertas.gestionar` + sede asignada; solo Nueva → Revisada → Cerrada; Cerrada es terminal.
- **Efectos:** estado + registro `alerta_seguimiento(actor, instante, transicion, observacion)`.
- **Errores:** `TRANSICION_INVALIDA` (409), `CONFLICTO_VERSION` (409), 404 otra organización.
- **Auditoría:** cada transición.
- **Endpoints propuestos:** `POST /api/v1/alertas/{id}/revisar` · `POST /api/v1/alertas/{id}/cerrar`.

### Criterios de aceptación
- [ ] **Dado** una alerta Nueva **cuando** se revisa y luego se cierra **entonces** se conservan usuario y fecha de cada transición (CA-16).
- [ ] **Dado** una alerta de permanencia **cuando** se cierra **entonces** la permanencia sigue abierta (PRD §11.1).
- [ ] **Dado** una alerta Cerrada **cuando** se intenta revisar **entonces** se rechaza `TRANSICION_INVALIDA`.

### Tareas backend
- [ ] Máquina de estados y tabla de seguimiento.

### Tareas frontend
- [ ] Acciones revisar/cerrar con observación y línea de seguimiento.

### Pruebas
- [ ] Unitarias: transiciones válidas/ inválidas.
- [ ] Integración (PostgreSQL real): seguimiento persistido.
- [ ] Concurrencia: dos usuarios cierran a la vez ⇒ uno recibe conflicto.
- [ ] Aislamiento / permisos negativos: operador sin permiso ⇒ 403.
- [ ] E2E: revisar y cerrar desde el listado.

### Definition of Done
Cumple la [[PLAN-DE-TRABAJO#Definition of Done general|DoD general]] y además:
- [ ] CA-16 verificado para el ciclo completo.

### Riesgos y decisiones pendientes
> [!note] Sin decisiones bloqueantes. Reapertura de alertas no está definida y no se implementa.
