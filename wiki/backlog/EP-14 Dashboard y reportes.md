---
tipo: epica
estado: propuesta
fase: F4
requisitos: [RF-23, RF-24, CA-18, CA-19]
adr: [ADR-004, ADR-005, ADR-008, ADR-011, ADR-012, ADR-013, ADR-014, ADR-016]
decisiones: [D-12, D-13, D-14, D-15]
actualizado: 2026-10-17
tags: [conecsia, backlog, epica]
---

# EP-14 Dashboard y reportes

> [!note] Fase: [[PLAN-DE-TRABAJO#F4]] · Requisitos: RF-23, RF-24 · ADR: [[ADR-016 Reportes y exportaciones Excel y PDF]] (Propuesta)

## Objetivo de la épica
Ofrecer un dashboard operativo que respete sedes y permisos y reportes Excel/PDF del núcleo con filtros, corte temporal y zona horaria, sin fuga entre organizaciones.

## Alcance / fuera de alcance
- **Dentro (Base):** indicadores de PRD §10.2 y reportes de §10.3 del núcleo (sin vehículos/dispositivos).
- **Dentro (Propuesta):** generación síncrona en streaming para pequeños y en segundo plano para grandes; archivo privado.
- **Fuera:** reportes de vehículos y dispositivos (EP-16/EP-17), indicador "personas sin salida" hasta su definición, límites de tamaño (D-15).

## Historias
| ID | Historia | Tamaño | Estado | Bloqueada por |
|---|---|---|---|---|
| HU-044 | Dashboard operativo | M | Lista para refinar | D-12 (próximo vencimiento, parcial) |
| HU-045 | Reportes Excel y PDF del núcleo | L | Lista para refinar | D-13, D-15 (parcial) |

---

## HU-044 Dashboard operativo

**Como** administrador de sede **quiero** un tablero con los indicadores operativos de mis sedes **para** entender la operación del día.

| Campo | Valor |
|---|---|
| Épica / Fase | EP-14 / F4 |
| Requisitos | RF-23, CA-18 |
| Tamaño relativo | M (propuesto, sin estimación aprobada) |
| Estado | Lista para refinar |

### Objetivo
Indicadores: personas dentro, entradas/salidas del día, visitas pendientes/autorizadas/rechazadas, autorizaciones próximas a vencer, visitas por tipo/sede, franjas de mayor ingreso (PRD §10.2).

### Actores
Administrador de sede; Administrador de organización; Operador (vista reducida según permisos).

### Dependencias
- Historias: HU-033, HU-034, HU-037, HU-040.
- ADR: [[ADR-008 Modelo de tiempo y zonas horarias]], [[ADR-011 Tiempo real con SignalR]], [[ADR-005 Contexto de organización y autorización por permisos]] (Propuesta).
- Decisiones: D-12 (umbral próximo vencimiento), D-15 (frescura), D-06 ("día" por zona horaria de sede).

### Contrato funcional
- **Entradas:** sedes (dentro de asignadas), fecha de corte.
- **Reglas (servidor):** permiso `dashboard.ver`; "día" calculado en la zona horaria de la sede; personas dentro ≠ entradas del día; visita rechazada no cuenta como entrada; "personas sin salida" no se muestra hasta definirse; próximo vencimiento solo si hay umbral configurado.
- **Efectos:** ninguno; consultas agregadas.
- **Errores:** 403 sede no asignada.
- **Auditoría:** no aplica (lectura).
- **Endpoints propuestos:** `GET /api/v1/dashboard?sedeIds=&fecha=`.

### Criterios de aceptación
- [ ] **Dado** un usuario con sede A **cuando** abre el dashboard **entonces** no ve datos de sede B ni de otra organización (CA-18).
- [ ] **Dado** una persona que entró ayer y sigue dentro **cuando** se consulta hoy **entonces** cuenta en personas dentro y no en entradas del día (CA-18).
- [ ] **Dado** una visita rechazada **cuando** se calculan entradas **entonces** no se cuenta.

### Tareas backend
- [ ] Consultas agregadas con índices y cálculo de día local.

### Tareas frontend
- [ ] Tarjetas e indicadores con refresco por eventos SignalR y refetch.

### Pruebas
- [ ] Unitarias: límites del día local.
- [ ] Integración (PostgreSQL real): conteos con datos semilla.
- [ ] Concurrencia: no aplica; consistencia tras entradas paralelas.
- [ ] Aislamiento / permisos negativos: sedes no asignadas en la consulta ⇒ 403.
- [ ] E2E: dashboard en móvil y escritorio.

### Definition of Done
Cumple la [[PLAN-DE-TRABAJO#Definition of Done general|DoD general]] y además:
- [ ] Definición de cada indicador documentada en la página del módulo.

### Riesgos y decisiones pendientes
> [!todo] D-12 — umbral de próximo vencimiento · D-15 — frescura · definición de "personas sin salida".

---

## HU-045 Reportes Excel y PDF del núcleo

**Como** administrador o auditor **quiero** exportar reportes en Excel y PDF con filtros **para** analizar y archivar la operación.

| Campo | Valor |
|---|---|
| Épica / Fase | EP-14 / F4 |
| Requisitos | RF-24, CA-19, RF-25 |
| Tamaño relativo | L (propuesto, sin estimación aprobada) |
| Estado | Lista para refinar |

### Objetivo
Reportes del núcleo: historial de visitas, entradas y salidas, autorizaciones, autorizaciones vencidas, personas dentro (corte explícito), permanencia, visitas por sede/destino sin doble conteo, auditoría.

### Actores
Administrador de sede/organización; Auditor.

### Dependencias
- Historias: HU-038, HU-044, HU-046.
- ADR: [[ADR-016 Reportes y exportaciones Excel y PDF]], [[ADR-013 Almacenamiento privado de archivos]], [[ADR-012 Outbox transaccional y procesos de fondo]], [[ADR-014 Auditoría append-only]] (Propuesta).
- Decisiones: D-13 (almacenamiento, formatos), D-15 (límites síncronos), D-14 (retención de archivos).

### Contrato funcional
- **Entradas:** tipo de reporte, formato (xlsx/pdf), filtros fecha/sede/persona/tipo/estado/destino.
- **Reglas (servidor):** permiso por tipo de reporte + sedes asignadas; filtros aplicados en servidor; archivo registra filtros, corte temporal y zona horaria; por encima de un umbral **parametrizable** (D-15) ⇒ trabajo en segundo plano; descarga verifica organización **y** solicitante.
- **Efectos:** `exportacion(organization_id, solicitante, filtros, estado, ruta_privada)`.
- **Errores:** `EXPORTACION_NO_ENCONTRADA` (404 también para otra organización), `LIMITE_EXCEDIDO` (422).
- **Auditoría:** cada exportación (solicitud y descarga).
- **Endpoints propuestos:** `POST /api/v1/reportes/{tipo}` · `GET /api/v1/exportaciones/{id}` · `GET /api/v1/exportaciones/{id}/archivo`.

### Criterios de aceptación
- [ ] **Dado** filtros de fecha y sede **cuando** se exporta **entonces** Excel y PDF reflejan filtros, ámbito y corte temporal (CA-19).
- [ ] **Dado** una exportación de la organización A **cuando** un usuario de B pide el archivo **entonces** recibe 404 (CA-19).
- [ ] **Dado** una exportación **cuando** se descarga **entonces** queda auditada (CA-20).

### Tareas backend
- [ ] Generadores Excel/PDF (librería con licencia verificada en F0), job en segundo plano, almacenamiento privado con URL no adivinable.

### Tareas frontend
- [ ] Formulario de filtros, estado de exportación y descarga.

### Pruebas
- [ ] Unitarias: aplicación de filtros y metadatos del archivo.
- [ ] Integración (PostgreSQL real): contenido coincide con la consulta.
- [ ] Concurrencia/idempotencia: doble solicitud no genera trabajos duplicados si se usa `Idempotency-Key`.
- [ ] Aislamiento / permisos negativos: descarga cruzada entre organizaciones y entre usuarios.
- [ ] E2E: exportar y abrir archivo.

### Definition of Done
Cumple la [[PLAN-DE-TRABAJO#Definition of Done general|DoD general]] y además:
- [ ] Cada reporte del núcleo con prueba de contenido y de aislamiento.

### Riesgos y decisiones pendientes
> [!todo] D-13 — almacenamiento y formatos · D-15 — límites · D-14 — retención de archivos exportados.
