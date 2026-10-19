---
tipo: epica
estado: propuesta
fase: F1
requisitos: [RF-03, RF-04, RF-05, RN-08, CA-02, CA-03, CA-15]
adr: [ADR-004, ADR-005, ADR-008, ADR-019, ADR-014]
decisiones: [D-01, D-04, D-06]
actualizado: 2026-10-17
tags: [conecsia, backlog, epica]
---

# EP-04 Estructura física

> [!note] Fase: [[PLAN-DE-TRABAJO#F1]] · Requisitos: RF-03, RF-04, RF-05 · ADR: [[ADR-008 Modelo de tiempo y zonas horarias]], [[ADR-019 Zona única con atributo de punto de acceso]] (Propuesta)

## Objetivo de la épica
Modelar la jerarquía física Organización → [[Sede]] → [[Área o destino]] · [[Zona y punto de acceso|Zona / punto de acceso]], configurable e inactivable conservando referencias históricas.

## Alcance / fuera de alcance
- **Dentro:** sedes con zona horaria IANA, áreas/destinos con responsables y autorizadores, zonas con atributo de punto de acceso (Propuesta ADR-019).
- **Fuera:** calendario/festivos y horarios nocturnos (D-06, EP-07), reglas de decisión de autorizaciones (D-04, HU-025), dispositivos lectores (D-13).

## Historias
| ID | Historia | Tamaño | Estado | Bloqueada por |
|---|---|---|---|---|
| HU-013 | Gestionar sedes con zona horaria | M | **En revisión** (verificada localmente 2026-10-17) | Autenticación provisional (D-03); ADR-008 por aprobar |
| HU-014 | Gestionar áreas/destinos y responsables/autorizadores | M | Lista para refinar | Alcance de autorizadores: D-04 |
| HU-015 | Gestionar zonas y puntos de acceso | M | Lista para refinar | ADR-019 por aprobar |

---

## HU-013 Gestionar sedes con zona horaria

**Como** Administrador de organización **quiero** crear, editar e inactivar sedes con su zona horaria **para** operar varias instalaciones y evaluar horarios en hora local.

| Campo | Valor |
|---|---|
| Épica / Fase | EP-04 / F1 |
| Requisitos | RF-03, RN-08, CA-02, CA-03 |
| Tamaño relativo | M (propuesto) |
| Estado | **En revisión** — implementada y verificada localmente el 2026-10-17 ([[Sedes]]); falta CI en GitHub, revisión de código y del PO |

> [!note] Implementación 2026-10-17 — primera vertical
> Detalle y limitaciones en [[Sedes]]; evidencia en [[Estado de implementación]]. Desviaciones: autenticación por esquema de desarrollo ([[ADR-022 Autenticación de desarrollo provisional]]) porque HU-006 está bloqueada por D-03; organizaciones y usuarios por semillas porque HU-009/HU-016 no existen.

### Objetivo
Entidad [[Sede]] por organización con nombre, dirección, zona horaria IANA obligatoria, estado y datos de contacto.

### Actores
Administrador de organización (crea/inactiva); Administrador de sede (edita la suya, si tiene permiso).

### Dependencias
- Historias: HU-005, HU-007, HU-008, HU-009.
- ADR: [[ADR-008 Modelo de tiempo y zonas horarias]] (Propuesta), [[ADR-004 Tenancy con base compartida y organization_id]] (Propuesta).
- Decisiones: D-06 (zona horaria por sede, festivos — esta historia solo almacena la zona), D-01 (término "Sede").

### Contrato funcional
- **Entradas:** nombre, dirección, ciudad/país, zona horaria IANA (p. ej. `America/Bogota`), estado.
- **Reglas (servidor):** nombre único por organización (normalizado); zona horaria validada contra base IANA; cambiar la zona horaria con autorizaciones recurrentes existentes **no** recalcula silenciosamente — se rechaza o advierte hasta definir en D-06 (por defecto: rechazar si existen autorizaciones vigentes); inactivar no borra, impide nuevas autorizaciones/movimientos.
- **Efectos persistidos:** `site` con `organization_id`, `time_zone`, estado, versión.
- **Errores:** 409 nombre duplicado/concurrencia; 422 inactivar sede con permanencias abiertas (Propuesta: rechazar hasta definir regla); 404 sede de otra organización; 403 fuera de alcance.
- **Auditoría:** alta, cambios (anterior/nuevo), inactivación.
- **Endpoints indicativos:** `GET/POST /api/v1/sedes`, `PUT /api/v1/sedes/{id}`, `POST /api/v1/sedes/{id}/inactivar`.

### Criterios de aceptación
- [x] **Dado** una organización con varias sedes **cuando** un operador asignado a S1 lista sedes **entonces** solo ve S1, y el backend rechaza operar en S2 (CA-02). *Verificado 2026-10-17: integración + Docker (403 `FUERA_DE_ALCANCE`).*
- [ ] **Dado** una sede inactivada **cuando** se consulta un historial que la referencia **entonces** la referencia se conserva (CA-03, CA-15). *Parcial: la sede inactiva sigue consultable y filtrable; aún no existen historiales (Accesos) que la referencien.*
- [x] **Dado** una zona horaria inválida **cuando** se guarda **entonces** se rechaza con 400. *Verificado 2026-10-17.*

### Tareas backend
- [x] Agregado `Sede`, validación IANA (con ICU/tzdata; NodaTime sigue propuesto), concurrencia optimista (`xmin`).
- [x] Filtrado por alcance de sedes asignadas. *Provisional: el alcance se guarda en la membresía hasta HU-018.*

### Tareas frontend
- [x] Lista paginada, formulario con selector de zona horaria, inactivación con confirmación.

### Pruebas
- [x] Unitarias: validación de zona horaria y transiciones de estado.
- [x] Integración (PostgreSQL real): unicidad por organización, también con altas concurrentes.
- [x] Aislamiento / permisos negativos: sede de B desde A ⇒ 404; operador sin permiso de administración ⇒ 403.
- [ ] E2E / UI: alta, edición e inactivación. *Verificado manualmente en Docker con el navegador el 2026-10-17; falta automatizar con Playwright.*

### Definition of Done
Cumple la [[PLAN-DE-TRABAJO#Definition of Done general|DoD general]] y además:
- [ ] Instantes en `timestamptz`; zona horaria persistida y expuesta en contratos.

### Riesgos y decisiones pendientes
> [!todo] D-06 — efecto de cambiar zona horaria y festivos. Pendiente: regla de inactivar sede con personas dentro.

---

## HU-014 Gestionar áreas/destinos y responsables/autorizadores

**Como** Administrador de organización o de sede **quiero** gestionar áreas/destinos con sus responsables y autorizadores **para** que las autorizaciones se dirijan a la unidad receptora correcta.

| Campo | Valor |
|---|---|
| Épica / Fase | EP-04 / F1 |
| Requisitos | RF-04, RN-08, CA-03, CA-15 |
| Tamaño relativo | M (propuesto) |
| Estado | Lista para refinar |

### Objetivo
Entidad [[Área o destino]] por sede, con asignación de responsables y autorizadores (usuarios de la organización), inactivable.

### Actores
Administrador de organización; Administrador de sede; Autorizador (consumidor).

### Dependencias
- Historias: HU-013, HU-016 (usuarios).
- ADR: [[ADR-019 Zona única con atributo de punto de acceso]] (Propuesta — AreaDestino separada de Zona), [[ADR-005 Contexto de organización y autorización por permisos]] (Propuesta).
- Decisiones: **D-04** (quién decide/revoca y destinos de cada autorizador).

### Contrato funcional
- **Entradas:** sede, nombre, tipo (opcional), responsables[], autorizadores[], estado.
- **Reglas (servidor):** sede y usuarios de la misma organización (FK compuesta); nombre único por sede; ser designado autorizador **no** otorga por sí permiso de decidir — se requiere permiso `autorizaciones.decidir` + alcance (RN-10); qué autorizaciones requieren decisión se define en D-04, no aquí.
- **Efectos persistidos:** `area_destination`, `area_responsible`, `area_authorizer` con vigencia (alta/baja) para conservar historial.
- **Errores:** 409 duplicado; 404 sede/usuario de otra organización; 422 asignar usuario inactivo.
- **Auditoría:** alta, cambios, asignación/retiro de responsables y autorizadores.
- **Endpoints indicativos:** `GET/POST /api/v1/sedes/{sedeId}/areas`, `PUT /api/v1/areas/{id}`, `PUT /api/v1/areas/{id}/autorizadores`.

### Criterios de aceptación
- [ ] **Dado** un área con responsables **cuando** se inactiva **entonces** autorizaciones e historiales previos conservan área y responsables (CA-03, CA-15).
- [ ] **Dado** un usuario de B **cuando** se intenta asignar como autorizador en un área de A **entonces** se rechaza (CA-01).
- [ ] **Dado** un autorizador retirado **cuando** se consulta el historial **entonces** permanece la autoría de sus decisiones previas.

### Tareas backend
- [ ] Agregado `AreaDestination` con asignaciones con vigencia; validaciones de pertenencia.

### Tareas frontend
- [ ] Gestión de áreas por sede; selector de usuarios filtrado por organización y estado.

### Pruebas
- [ ] Unitarias: reglas de asignación.
- [ ] Integración (PostgreSQL real): FKs compuestas y unicidad por sede.
- [ ] Aislamiento / permisos negativos: asignación cruzada; administrador de sede sobre otra sede ⇒ 403.
- [ ] E2E / UI: crear área, asignar y retirar autorizador.

### Definition of Done
Cumple la [[PLAN-DE-TRABAJO#Definition of Done general|DoD general]] y además:
- [ ] Historial de asignaciones consultable con vigencias.

### Riesgos y decisiones pendientes
> [!todo] D-04 — cuándo se exige autorización y destinos de cada autorizador; puede requerir ajustar el modelo de asignación.

---

## HU-015 Gestionar zonas y puntos de acceso

**Como** Administrador de sede **quiero** definir zonas restringibles y cuáles funcionan como puntos de acceso (entrada, salida o ambos) **para** registrar movimientos solo en lugares válidos.

| Campo | Valor |
|---|---|
| Épica / Fase | EP-04 / F1 |
| Requisitos | RF-05, RN-08, CA-03, CA-09 (preparación) |
| Tamaño relativo | M (propuesto) |
| Estado | Lista para refinar (ADR-019 por aprobar) |

### Objetivo
Entidad `Zona` por sede con `es_punto_acceso` y modo `Entrada | Salida | Ambos` ([[Zona y punto de acceso]]), según [[ADR-019 Zona única con atributo de punto de acceso]] (**Propuesta**; el CLAUDE.md exige verificarla antes del modelo físico).

### Actores
Administrador de organización; Administrador de sede.

### Dependencias
- Historias: HU-013. Habilita HU-018 (alcance por punto) y EP-10.
- ADR: [[ADR-019 Zona única con atributo de punto de acceso]] (Propuesta), [[ADR-004 Tenancy con base compartida y organization_id]] (Propuesta).
- Decisiones: D-13 (lectores/dispositivos asociados a punto — fuera de alcance).

### Contrato funcional
- **Entradas:** sede, nombre, descripción, `es_punto_acceso`, modo (requerido si es punto), estado.
- **Reglas (servidor):** nombre único por sede; modo solo si `es_punto_acceso`; restricción en BD que impide movimientos en zonas que no son punto de acceso (se materializa en EP-10); desmarcar punto de acceso o inactivar **no** altera movimientos históricos.
- **Efectos persistidos:** `zone` con `organization_id`, `site_id`, `is_access_point`, `access_mode`, estado.
- **Errores:** 400 modo inconsistente; 409 duplicado; 404 sede de otra organización.
- **Auditoría:** alta, cambios (anterior/nuevo), inactivación.
- **Endpoints indicativos:** `GET/POST /api/v1/sedes/{sedeId}/zonas`, `PUT /api/v1/zonas/{id}`, `POST /api/v1/zonas/{id}/inactivar`.

### Criterios de aceptación
- [ ] **Dado** una zona marcada como punto de acceso de entrada **cuando** se consulta para operar **entonces** solo se ofrece para entradas.
- [ ] **Dado** una zona inactivada **cuando** se consultan movimientos históricos **entonces** se conserva la referencia (CA-03).
- [ ] **Dado** un modo sin `es_punto_acceso` **cuando** se guarda **entonces** se rechaza con 400.

### Tareas backend
- [ ] Agregado `Zone`, `CHECK` de consistencia modo/punto, unicidad por sede.

### Tareas frontend
- [ ] Gestión de zonas por sede con conmutador "punto de acceso" y modo.

### Pruebas
- [ ] Unitarias: invariantes modo/punto.
- [ ] Integración (PostgreSQL real): `CHECK` y unicidad.
- [ ] Aislamiento / permisos negativos: zona de otra sede/organización; administrador sin alcance.
- [ ] E2E / UI: crear zona y punto de acceso.

### Definition of Done
Cumple la [[PLAN-DE-TRABAJO#Definition of Done general|DoD general]] y además:
- [ ] ADR-019 aprobado o ajustado antes de fijar la migración.

### Riesgos y decisiones pendientes
> [!todo] ADR-019 en Propuesta — confirmar modelo único Zona/punto. Pendiente: si un punto de acceso puede pertenecer a varias zonas (no contemplado).
