---
tipo: epica
estado: propuesta
fase: F1
requisitos: [RF-07, RF-08, RN-01, RN-08, CA-01, CA-03, CA-04, CA-05, CA-15]
adr: [ADR-004, ADR-013, ADR-014, ADR-018]
decisiones: [D-09, D-13, D-14]
actualizado: 2026-10-17
tags: [conecsia, backlog, epica]
---

# EP-06 Personas

> [!note] Fase: [[PLAN-DE-TRABAJO#F1]] · Requisitos: RF-07, RF-08, RN-08 · ADR: [[ADR-004 Tenancy con base compartida y organization_id]], [[ADR-013 Almacenamiento privado de archivos]] (Propuesta)

## Objetivo de la épica
Registro reutilizable de [[Persona|personas]] por organización, búsqueda por documento sin duplicados (también bajo concurrencia), catálogos de tipos, fotografía privada e inactivación que preserva el histórico.

## Alcance / fuera de alcance
- **Dentro:** alta/búsqueda/edición de persona, tipos de persona y documento, fotografía opcional, inactivación.
- **Fuera:** retención/eliminación de datos personales (D-14), fusión de duplicados (no definida), documentación de contratistas (D-11), compartir personas entre organizaciones (prohibido).

> [!warning] La **clave de identificación documental** no está definida (D-09). La Propuesta del PRD es `(organization_id, tipo_documento, documento_normalizado)` con normalización solo de espacios externos y **sin** eliminar ceros iniciales ni caracteres significativos; el país emisor está pendiente.

## Historias
| ID | Historia | Tamaño | Estado | Bloqueada por |
|---|---|---|---|---|
| HU-019 | Registrar y buscar persona por documento sin duplicados | L | Lista para refinar (clave final bloqueada) | D-09 |
| HU-020 | Tipos de persona y tipos de documento | S | Lista para refinar | Países emisores: D-09 |
| HU-021 | Fotografía de persona en almacenamiento privado | M | Lista para refinar (parcial) | D-13, D-14 |
| HU-022 | Inactivar persona preservando histórico | S | Lista para refinar | — |

---

## HU-019 Registrar y buscar persona por documento sin duplicados

**Como** Operador de acceso **quiero** buscar una persona por documento y, si no existe, registrarla **para** reutilizar su registro entre sedes de mi organización sin duplicarla.

| Campo | Valor |
|---|---|
| Épica / Fase | EP-06 / F1 |
| Requisitos | RF-07, RN-01, CA-04, CA-05, CA-01 |
| Tamaño relativo | L (propuesto) |
| Estado | Lista para refinar — clave definitiva **bloqueada por D-09** |

### Objetivo
Alta, búsqueda y edición de [[Persona]] con clave única por organización garantizada por índice único en PostgreSQL.

### Actores
Operador de acceso; Administrador de organización/sede; Autorizador (búsqueda para autorizar).

### Dependencias
- Historias: HU-005, HU-007, HU-008, HU-018, HU-020.
- ADR: [[ADR-004 Tenancy con base compartida y organization_id]] (Propuesta), [[ADR-018 Observabilidad sin datos sensibles]] (Propuesta), [[ADR-020 Contratos API y formato de errores]] (Propuesta).
- Decisiones: **D-09** (identificación documental, país emisor, personas sin documento estándar), D-14 (privacidad).

### Contrato funcional
- **Entradas:** tipo y número de documento, nombres, apellidos, teléfono, correo, empresa de procedencia, tipo de persona, observaciones.
- **Reglas (servidor):** normalización Propuesta: recortar espacios externos y mayúsculas para alfanuméricos; **no** quitar ceros ni caracteres sin regla; índice único Propuesto `(organization_id, document_type_id, document_normalized)`; alta concurrente resuelta con `INSERT … ON CONFLICT` y devolución de la existente; personas sin documento estándar ⇒ **rechazadas** hasta D-09; búsqueda exacta por documento + búsqueda por nombre paginada; nunca busca en otras organizaciones.
- **Efectos persistidos:** `person` con `organization_id`, estado `Activa`, versión, actor y fechas.
- **Errores:** 400 validación; 409 documento existente en edición; 404 persona de otra organización; 403 sin permiso `personas.crear`.
- **Auditoría:** alta y modificación (anterior/nuevo minimizado); búsquedas no se auditan individualmente salvo decisión D-14.
- **Logs:** documento enmascarado (ADR-018).
- **Endpoints indicativos:** `GET /api/v1/persons/lookup?documentTypeId=&number=`, `GET /api/v1/persons?q=`, `POST /api/v1/persons`, `PUT /api/v1/persons/{id}`.

### Criterios de aceptación
- [ ] **Dado** una persona registrada **cuando** busco su tipo y documento **entonces** recupero el registro existente (CA-04).
- [ ] **Dado** dos altas concurrentes con la misma clave **cuando** se procesan **entonces** existe una sola persona y ambas respuestas la referencian (CA-04).
- [ ] **Dado** el mismo documento en Org A y Org B **cuando** cada una lo busca **entonces** obtiene solo su propio registro, sin compartir datos (CA-01).
- [ ] **Dado** una persona registrada **cuando** se le crea o inactiva un usuario **entonces** la persona no cambia (CA-05).

### Tareas backend
- [ ] Agregado `Person`, normalizador de documento aislado y versionado (para cambiar con D-09).
- [ ] Índice único e `ON CONFLICT`; concurrencia optimista en edición.

### Tareas frontend
- [ ] Flujo "buscar primero" por documento; formulario de alta prellenado; edición con validaciones visibles.

### Pruebas
- [ ] Unitarias: normalizador (espacios, ceros iniciales preservados).
- [ ] Integración (PostgreSQL real): altas paralelas (N hilos) ⇒ una fila.
- [ ] Aislamiento / permisos negativos: lookup cruzado, edición cruzada ⇒ 404; operador sin `personas.crear`.
- [ ] E2E / UI: buscar, no encontrar, registrar, volver a buscar.

### Definition of Done
Cumple la [[PLAN-DE-TRABAJO#Definition of Done general|DoD general]] y además:
- [ ] D-09 resuelta y clave/normalización confirmadas antes de usar datos reales.

### Riesgos y decisiones pendientes
> [!todo] D-09 — país emisor, normalización, personas sin documento estándar. Pendiente: fusión de duplicados preexistentes (no definida, no implementar).

---

## HU-020 Tipos de persona y tipos de documento

**Como** Administrador de organización **quiero** configurar e inactivar tipos de persona y tipos de documento **para** clasificar personas sin perder históricos.

| Campo | Valor |
|---|---|
| Épica / Fase | EP-06 / F1 |
| Requisitos | RF-08, RN-08, CA-03 |
| Tamaño relativo | S (propuesto) |
| Estado | Lista para refinar |

### Objetivo
Catálogos por organización: tipos de persona (visitante, proveedor, contratista, cliente, técnico, domiciliario, residente, socio, familiar, otros) y tipos de documento, sembrados por plantilla (HU-010).

### Actores
Administrador de organización.

### Dependencias
- Historias: HU-007, HU-010.
- ADR: [[ADR-004 Tenancy con base compartida y organization_id]] (Propuesta).
- Decisiones: D-09 (países emisores / formato por tipo de documento).

### Contrato funcional
- **Entradas:** nombre, código, descripción, estado; para documento: patrón de validación opcional (Propuesta).
- **Reglas (servidor):** código único por organización; inactivar impide nuevo uso pero conserva referencias; tipo de persona ≠ frecuencia de visita (no duplicar categorías, PRD §6.1); no se borran tipos referenciados.
- **Efectos persistidos:** `person_type`, `document_type` por organización con `origen`.
- **Errores:** 409 código duplicado; 422 usar tipo inactivo en alta.
- **Auditoría:** alta, cambio, inactivación.
- **Endpoints indicativos:** `GET/POST /api/v1/person-types`, `GET/POST /api/v1/document-types`, `POST …/{id}/deactivate`.

### Criterios de aceptación
- [ ] **Dado** un tipo de persona inactivado **cuando** se consulta una persona/historial que lo usa **entonces** conserva la referencia (CA-03, CA-15).
- [ ] **Dado** un tipo inactivo **cuando** se intenta registrar una persona nueva con él **entonces** se rechaza.

### Tareas backend
- [ ] Entidades de catálogo genéricas con estado y origen.

### Tareas frontend
- [ ] Administración de catálogos (lista, alta, inactivar).

### Pruebas
- [ ] Unitarias: reglas de inactivación.
- [ ] Integración (PostgreSQL real): unicidad por organización.
- [ ] Aislamiento / permisos negativos: catálogos de B invisibles desde A; usuario sin permiso de configuración.
- [ ] E2E / UI: crear e inactivar tipo.

### Definition of Done
Cumple la [[PLAN-DE-TRABAJO#Definition of Done general|DoD general]] y además:
- [ ] Catálogos sembrados por plantilla y editables (CA-26 no afectado).

### Riesgos y decisiones pendientes
> [!todo] D-09 — validación por tipo de documento y país emisor.

---

## HU-021 Fotografía de persona en almacenamiento privado

**Como** Operador de acceso **quiero** adjuntar opcionalmente una fotografía a la persona **para** apoyar la verificación visual de identidad.

| Campo | Valor |
|---|---|
| Épica / Fase | EP-06 / F1 |
| Requisitos | RF-07, RN-01, CA-01 (descarga de archivos) |
| Tamaño relativo | M (propuesto) |
| Estado | Lista para refinar (parcial: proveedor D-13; tratamiento y retención D-14) |

### Objetivo
Subida y descarga de fotografía vía [[ADR-013 Almacenamiento privado de archivos]]: binario en almacenamiento privado con prefijo de organización, metadatos en PostgreSQL, acceso solo por API con verificación.

### Actores
Operador de acceso; Administrador; cualquier usuario con permiso de consulta de personas.

### Dependencias
- Historias: HU-019, HU-003 (emulador de objetos).
- ADR: [[ADR-013 Almacenamiento privado de archivos]] (Propuesta), [[ADR-018 Observabilidad sin datos sensibles]] (Propuesta).
- Decisiones: **D-13** (infraestructura, formatos, tamaños), **D-14** (tratamiento, retención y eliminación de fotografías — no operar con fotos reales hasta decidir).

### Contrato funcional
- **Entradas:** archivo de imagen (multipart) o captura de cámara.
- **Reglas (servidor):** formatos y tamaño máximo **configurables** (valores definitivos en D-13); validar tipo real por firma, no por extensión; quitar metadatos EXIF (Propuesta); clave de objeto con prefijo `org/{organization_id}/persons/{id}/`; descarga solo por API tras verificar organización y permiso, o URL firmada de vida corta; reemplazar foto conserva la anterior hasta definir retención (D-14).
- **Efectos persistidos:** `file_object` (organización, dueño, tipo, tamaño, hash, fecha, actor) y referencia en `person`.
- **Errores:** 413 tamaño excedido; 415 formato no permitido; 404 persona/archivo de otra organización.
- **Auditoría:** subida, reemplazo; descarga (Propuesta).
- **Endpoints indicativos:** `PUT /api/v1/persons/{id}/photo`, `GET /api/v1/persons/{id}/photo`.

### Criterios de aceptación
- [ ] **Dado** un usuario de A **cuando** solicita la foto de una persona de B conociendo su ID o clave **entonces** recibe 404 (CA-01).
- [ ] **Dado** un archivo con extensión de imagen pero contenido distinto **cuando** se sube **entonces** se rechaza con 415.
- [ ] **Dado** una URL firmada **cuando** expira **entonces** ya no permite descargar.

### Tareas backend
- [ ] Implementación `IFileStorage` (emulador local, proveedor real tras D-13); validación de firma; limpieza EXIF.

### Tareas frontend
- [ ] Captura por cámara/subida con vista previa; mostrar foto vía endpoint autenticado.

### Pruebas
- [ ] Unitarias: validación de formato y tamaño.
- [ ] Integración (PostgreSQL real + emulador de objetos): subida, metadatos y descarga.
- [ ] Aislamiento / permisos negativos: descarga cruzada; usuario sin permiso de consulta.
- [ ] E2E / UI: capturar y visualizar foto en tablet/móvil.

### Definition of Done
Cumple la [[PLAN-DE-TRABAJO#Definition of Done general|DoD general]] y además:
- [ ] No hay URLs públicas permanentes ni rutas de objeto expuestas al cliente.

### Riesgos y decisiones pendientes
> [!todo] D-13 — proveedor, formatos y tamaños. D-14 — finalidad, aviso, retención y eliminación de fotografías.

---

## HU-022 Inactivar persona preservando histórico

**Como** Administrador de organización **quiero** inactivar una persona **para** impedir nuevos ingresos sin perder su historial.

| Campo | Valor |
|---|---|
| Épica / Fase | EP-06 / F1 |
| Requisitos | RN-08, CA-15, CA-09 (persona inactiva denegada) |
| Tamaño relativo | S (propuesto) |
| Estado | Lista para refinar |

### Objetivo
Transición `Activa ↔ Inactiva` de [[Persona]] con motivo, sin borrado.

### Actores
Administrador de organización/sede con permiso `personas.inactivar`.

### Dependencias
- Historias: HU-019, HU-008. Consumida por HU-027/HU-032 (denegación con persona inactiva, CA-09).
- ADR: [[ADR-009 Ciclos de estado separados y vigencia calculada]] (Propuesta), [[ADR-014 Auditoría append-only]] (Propuesta).
- Decisiones: D-14 (eliminación definitiva — fuera de alcance; no se implementa borrado).

### Contrato funcional
- **Entradas:** persona, motivo (obligatorio), versión.
- **Reglas (servidor):** inactivar **no** borra ni anonimiza; no revoca autorizaciones automáticamente (la vigencia calculada la deniega en la entrada; revocar es una acción separada, HU-026); con permanencia abierta: se permite inactivar y la salida sigue siendo registrable (Propuesta, coherente con CA-13) — confirmar en refinamiento; reactivar requiere el mismo permiso.
- **Efectos persistidos:** estado, motivo, actor, instante.
- **Errores:** 409 concurrencia; 422 transición inválida; 404 otra organización.
- **Auditoría:** inactivación/reactivación con motivo.
- **Endpoints indicativos:** `POST /api/v1/persons/{id}/deactivate`, `POST /api/v1/persons/{id}/reactivate`.

### Criterios de aceptación
- [ ] **Dado** una persona inactivada **cuando** se consulta su historial **entonces** conserva autorizaciones, autorizadores y movimientos (CA-15).
- [ ] **Dado** una persona inactiva **cuando** se intenta su entrada **entonces** se deniega con causa y sin abrir permanencia (CA-09, verificado en EP-10).
- [ ] **Dado** un usuario sin `personas.inactivar` **cuando** invoca la API **entonces** recibe 403.

### Tareas backend
- [ ] Casos de uso de inactivación/reactivación con motivo y auditoría.

### Tareas frontend
- [ ] Acción con confirmación y motivo; indicador visible de persona inactiva en búsquedas.

### Pruebas
- [ ] Unitarias: transiciones.
- [ ] Integración (PostgreSQL real): historial intacto tras inactivar.
- [ ] Aislamiento / permisos negativos: inactivar persona de B; usuario sin permiso.
- [ ] E2E / UI: inactivar y ver estado en búsqueda.

### Definition of Done
Cumple la [[PLAN-DE-TRABAJO#Definition of Done general|DoD general]] y además:
- [ ] Ningún borrado físico de personas en el código.

### Riesgos y decisiones pendientes
> [!todo] D-14 — eliminación/anonimización por retención. Pendiente: confirmar efecto sobre permanencia abierta.
