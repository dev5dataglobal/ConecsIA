---
tipo: epica
estado: propuesta
fase: F6
requisitos: [RF-26, RF-02, CA-26, CA-03]
adr: [ADR-004, ADR-005, ADR-013, ADR-014]
decisiones: [D-01, D-11, D-13, D-14]
actualizado: 2026-10-17
tags: [conecsia, backlog, epica, verticales]
---

# EP-18 Adaptación sectorial

> [!note] Fase: [[PLAN-DE-TRABAJO#F6]] · Requisitos: RF-26, [[Criterios de aceptación|CA-26]] · ADR: [[ADR-004 Tenancy con base compartida y organization_id]], [[ADR-014 Auditoría append-only]]

## Objetivo de la épica
Completar las plantillas por tipo de [[Organización]] (empresa, conjunto residencial, club, universidad, coworking, parque industrial) sobre el núcleo común, y especificar las capacidades sectoriales aún no definidas (**Base**, RF-26).

## Alcance / fuera de alcance
- **Dentro (Base):** plantillas que precargan catálogos y vocabulario ajustables; aplicar/cambiar plantilla sin sobrescribir personalizaciones, permisos ni históricos.
- **Dentro (spike):** especificación de documentación de contratistas, jerarquías residenciales y límites de invitados (HU-052).
- **Fuera:** cualquier regla de acceso basada en documentación de contratistas o límites de invitados hasta resolver D-11.

## Historias
| ID | Historia | Tamaño | Estado | Bloqueada por |
|---|---|---|---|---|
| HU-051 | Plantillas sectoriales completas | M | Lista para refinar | D-01 (vocabulario) |
| HU-052 | Documentación de contratistas y límites de invitados | Spike | **Bloqueada** | D-11 |

---

## HU-051 Plantillas sectoriales completas

**Como** superadministrador SaaS **quiero** plantillas completas por vertical **para** que una organización nueva arranque con catálogos y vocabulario adecuados, ajustables sin perder lo personalizado.

| Campo | Valor |
|---|---|
| Épica / Fase | EP-18 / F6 |
| Requisitos | RF-26, RF-02, CA-26, CA-03 |
| Tamaño relativo | M (propuesto, sin estimación aprobada) |
| Estado | Lista para refinar |

### Objetivo
Evolucionar la plantilla básica de HU-010 a plantillas versionadas por vertical, aplicables de forma no destructiva.

### Actores
Superadministrador SaaS (mantiene plantillas), Administrador de organización (aplica y ajusta).

### Dependencias
- Historias: HU-010, HU-012, HU-011, HU-017, HU-020.
- ADR: [[ADR-004 Tenancy con base compartida y organization_id]] (Propuesta), [[ADR-005 Contexto de organización y autorización por permisos]] (Propuesta), [[ADR-014 Auditoría append-only]] (Propuesta).
- Decisiones: D-01 (etiquetas por vertical), D-16 (módulos sugeridos por plan).

### Contrato funcional
- **Entradas:** plantillaId + versión; organizationId solo del contexto/plano SaaS con permiso.
- **Reglas (servidor):** plantillas globales de solo lectura para tenants; aplicar = merge: crea elementos faltantes y marca su origen (`origen_plantilla`, versión); **nunca** modifica elementos personalizados, roles ni permisos existentes, ni borra catálogos en uso; operación idempotente; vista previa (dry-run) de cambios.
- **Efectos persistidos:** catálogos (tipos de persona, documento, destino, zona, dispositivo/vehículo si el módulo está habilitado), etiquetas de vocabulario, registro `aplicacion_plantilla`.
- **Errores / denegaciones:** 403 sin permiso `organizacion.plantilla.aplicar`; 404 plantilla u organización ajena; 409 aplicación concurrente.
- **Auditoría:** aplicación con resumen de elementos creados/omitidos y versión.
- **Endpoints propuestos:** `GET /api/v1/plantillas-sectoriales`, `POST /api/v1/organizacion/plantilla/vista-previa`, `POST /api/v1/organizacion/plantilla/aplicar` (indicativo).

### Criterios de aceptación
- [ ] **Dado** una organización con catálogos personalizados **cuando** cambio de plantilla **entonces** no se modifican permisos existentes ni se eliminan personalizaciones o históricos (CA-26).
- [ ] **Dado** una plantilla aplicada **cuando** el administrador edita o inactiva un catálogo **entonces** el cambio persiste y conserva referencias (CA-26, CA-03).
- [ ] **Dado** la misma aplicación repetida **entonces** no duplica elementos.
- [ ] **Dado** un administrador de A **cuando** aplica plantilla sobre B **entonces** recibe 404 (CA-01).

### Tareas backend
- [ ] Modelo de plantilla versionada (semilla/JSON en repositorio) y servicio de merge no destructivo con dry-run.
- [ ] Marcado de origen y bloqueo por organización durante la aplicación.
- [ ] Auditoría del resumen.

### Tareas frontend
- [ ] Selector de plantilla, vista previa de diferencias y confirmación.

### Pruebas
- [ ] Unitarias: merge (crear/omitir/no tocar personalizados).
- [ ] Integración (PostgreSQL real): aplicación concurrente, idempotencia, históricos intactos.
- [ ] Aislamiento / permisos negativos: otra organización, usuario sin permiso.
- [ ] E2E / UI: alta de organización residencial con plantilla y ajuste posterior.

### Definition of Done
Cumple la [[PLAN-DE-TRABAJO#Definition of Done general|DoD general]] y además:
- [ ] Contenido de cada plantilla validado por producto (D-01) y documentado en la wiki.

### Riesgos y decisiones pendientes
> [!todo] D-01 — Vocabulario por vertical sin confirmar.

> [!todo] D-11 — Jerarquías residenciales: no se modelan en la plantilla hasta su definición.

---

## HU-052 Documentación de contratistas y límites de invitados

**Como** responsable de producto **quiero** un spike que especifique documentación de contratistas, jerarquías residenciales y límites de invitados **para** decidir si y cómo afectan el acceso.

| Campo | Valor |
|---|---|
| Épica / Fase | EP-18 / F6 |
| Requisitos | RF-26, PRD §8.3 |
| Tamaño relativo | Spike con time-box (propuesto) |
| Estado | **Bloqueada** por D-11 |

### Objetivo
Producir la especificación y la propuesta de modelo; **no** implementar reglas de acceso. El PRD no define tipos de documento, vigencia, revisión ni efecto en el acceso (**Pendiente**).

### Actores
Responsable de producto, arquitecto, administrador de organización piloto (empresa y residencial).

### Dependencias
- Historias: HU-021 (archivos privados), HU-024, HU-027, HU-032.
- ADR: [[ADR-013 Almacenamiento privado de archivos]] (Propuesta), [[ADR-009 Ciclos de estado separados y vigencia calculada]] (Propuesta).
- Decisiones: **D-11**, D-13 (formatos/tamaños), D-14 (retención de documentos).

### Contrato funcional
- **Entradas del spike:** preguntas a resolver: tipos de documento exigibles, vigencia y alertas, quién revisa/aprueba, efecto en la validación de entrada (¿bloquea, advierte?), límites de invitados por unidad/periodo, jerarquía residente-unidad.
- **Reglas:** ninguna en producción; cualquier efecto en el acceso requiere D-11 aprobada y ADR.
- **Salidas:** documento en `wiki/consultas/`, propuesta de modelo, borrador de HU derivadas con CA, actualización de D-11.
- **Errores / auditoría / endpoints:** no aplica (sin código productivo).

### Criterios de aceptación
- [ ] **Dado** el spike **cuando** termina el time-box **entonces** existen preguntas respondidas o escaladas, propuesta de modelo e impacto en HU-032/HU-027.
- [ ] **Dado** D-11 sin aprobar **entonces** no se integra en el backlog ninguna regla que bloquee accesos.

### Tareas backend
- [ ] Prototipo descartable opcional del modelo de documentos sobre ADR-013 (fuera de la rama principal).

### Tareas frontend
- [ ] No aplica (posibles bocetos de baja fidelidad).

### Pruebas
- [ ] No aplica para código productivo; revisión de la especificación con producto y piloto.

### Definition of Done
Cumple la [[PLAN-DE-TRABAJO#Definition of Done general|DoD general]] en lo aplicable y además:
- [ ] D-11 actualizada con opciones y recomendación; HU derivadas redactadas.

### Riesgos y decisiones pendientes
> [!todo] D-11 — Documentación de contratistas, jerarquías residenciales y límites de invitados.

> [!todo] D-14 — Retención y eliminación de documentos de contratistas (datos personales).
