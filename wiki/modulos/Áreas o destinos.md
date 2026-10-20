---
tipo: modulo
estado: implementado
fuentes: [PRD v0.1]
relacionados: [HU-014, HU-018, RF-04, RN-10, CA-01, CA-03, CA-15, ADR-004, ADR-019, D-04]
actualizado: 2026-10-19
tags: [conecsia, modulo, ep-04]
---

# Áreas o destinos

[[EP-04 Estructura física#HU-014 Gestionar áreas/destinos y responsables/autorizadores|HU-014]], implementada el 2026-10-19 en `loop/core` (sin fusionar). Concepto: [[Área o destino]].

## Qué existe (Implementado)
- **Tabla `area`:** `organizacion_id`, `sede_id`, nombre único por sede (normalizado), tipo opcional, estado, fechas y `xmin`. Tiene una **FK compuesta** `(organizacion_id, sede_id)` → `sede(organizacion_id, id)`: PostgreSQL rechaza un área apuntando a una sede de otra organización (verificado).
- **Tabla `area_asignacion`:** responsable o autorizador con vigencia (`desde_en`, `asignada_por`, `hasta_en`, `retirada_por`). Tiene FK compuesta al área y FK a `usuario`. Reemplazar los vigentes retira con fecha y nunca borra.
- **API:** `GET/POST /api/v1/sedes/{sedeId}/areas`, `GET/PUT /api/v1/areas/{id}`, `POST /api/v1/areas/{id}/inactivar`, `GET /api/v1/areas/{id}/asignaciones` (historial) y `PUT /api/v1/areas/{id}/responsables|autorizadores`.
- **Reglas:** permisos `areas.*` y alcance de la sede (HU-018). Una sede, área o usuario de otra organización responde 404; un usuario inactivo, 422 (contrato `IConsultaUsuarios`); el nombre repetido en la sede, 409; una versión obsoleta, 409.
- **RN-10:** ser autorizador **no** concede permisos (verificado). Decidir exige `autorizaciones.decidir` y el alcance (EP-07, D-04).
- **Auditoría:** `area.crear`, `area.modificar`, `area.inactivar`, `area.responsables` y `area.autorizadores` (agregados y retirados).
- **SPA:** `/sedes/:sedeId/areas`, enlazada desde el nombre de cada sede.
- **Plantilla:** Administrador de sede recibe los permisos `areas.*` (propuesta).

## Pendiente
- ⏸ D-04: destinos de cada autorizador y alcance por destino (HU-018).
- La relación con Zona y punto de acceso depende de ADR-019 (Propuesta) y HU-015 (⏸).
