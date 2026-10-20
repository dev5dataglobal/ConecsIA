---
tipo: epica
estado: propuesta
fase: F4
requisitos: [RF-25, CA-20]
adr: [ADR-004, ADR-005, ADR-014, ADR-016, ADR-018]
decisiones: [D-14]
actualizado: 2026-10-17
tags: [conecsia, backlog, epica]
---

# EP-15 Auditoría consultable

> [!note] Fase: [[PLAN-DE-TRABAJO#F4]] · Requisitos: RF-25 · ADR: [[ADR-014 Auditoría append-only]] (Propuesta)

## Objetivo de la épica
Permitir al auditor consultar, filtrar y exportar la auditoría de su organización sin poder alterarla.

## Alcance / fuera de alcance
- **Dentro (Base):** consulta con filtros y detalle permitido (PRD §10.1.7, §12.6).
- **Fuera:** escritura de auditoría (transversal desde F0, HU de EP-02); retención/purga (D-14); auditoría transversal multi-organización de soporte (no definida).

## Historias
| ID | Historia | Tamaño | Estado | Bloqueada por |
|---|---|---|---|---|
| HU-046 | Consulta de auditoría para auditor | M | Lista para refinar | D-14 (parcial) |

---

## HU-046 Consulta de auditoría para auditor

**Como** auditor **quiero** consultar las acciones críticas de mi organización con filtros **para** verificar quién hizo qué, cuándo y con qué resultado.

| Campo | Valor |
|---|---|
| Épica / Fase | EP-15 / F4 |
| Requisitos | RF-25, CA-20 |
| Tamaño relativo | M (propuesto, sin estimación aprobada) |
| Estado | Lista para refinar |

### Objetivo
Consulta paginada de solo lectura sobre la tabla append-only, con detalle de valores anterior/nuevo sin exponer secretos ni datos personales innecesarios.

### Actores
Auditor; Administrador de organización (si su rol tiene el permiso).

### Dependencias
- Historias: historia de auditoría base (EP-02), HU-032..HU-035, HU-045 (exportación).
- ADR: [[ADR-014 Auditoría append-only]], [[ADR-005 Contexto de organización y autorización por permisos]], [[ADR-018 Observabilidad sin datos sensibles]] (Propuesta).
- Decisiones: D-14 (retención y acceso a datos personales).

### Contrato funcional
- **Entradas:** rango de fechas, actor, acción, tipo/ID de objeto, resultado, sede; paginación por cursor.
- **Reglas (servidor):** permiso `auditoria.consultar` (no por nombre de rol); filtro obligatorio por organización del contexto; rol de BD de la aplicación sin `UPDATE`/`DELETE` sobre la tabla; campos sensibles enmascarados.
- **Efectos:** ninguno; la propia consulta/exportación de auditoría se audita (Propuesta PRD).
- **Errores:** 403 sin permiso; 404 evento de otra organización; no existen endpoints de modificación (405).
- **Endpoints propuestos:** `GET /api/v1/auditoria` · `GET /api/v1/auditoria/{id}` · exportación vía HU-045.

### Criterios de aceptación
- [ ] **Dado** una entrada, salida, decisión o cambio de permisos **cuando** el auditor filtra **entonces** ve actor, organización, instante, acción y objeto (CA-20).
- [ ] **Dado** cualquier usuario ordinario **cuando** intenta modificar o borrar un evento (API o SQL con el rol de la app) **entonces** falla (CA-20).
- [ ] **Dado** un auditor de la organización A **cuando** consulta **entonces** no ve eventos de B (CA-01).

### Tareas backend
- [ ] Endpoints de lectura con índices por `(organization_id, instante)`, enmascarado de campos.
- [ ] Verificación de permisos de BD en migración/despliegue.

### Tareas frontend
- [ ] Vista de auditoría con filtros, detalle anterior/nuevo y enlace a exportar.

### Pruebas
- [ ] Unitarias: enmascarado y filtros.
- [ ] Integración (PostgreSQL real): `UPDATE`/`DELETE` con rol de app rechazados.
- [ ] Concurrencia: no aplica (lectura); paginación estable con inserciones simultáneas.
- [ ] Aislamiento / permisos negativos: auditor de otra organización; operador sin permiso ⇒ 403.
- [ ] E2E: filtrar y abrir detalle.

### Definition of Done
Cumple la [[PLAN-DE-TRABAJO#Definition of Done general|DoD general]] y además:
- [ ] CA-20 verificado incluyendo el intento SQL con el rol de la aplicación.

### Riesgos y decisiones pendientes
> [!todo] D-14 — retención de auditoría y acceso a datos personales; no se implementa purga.
