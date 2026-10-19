---
tipo: epica
estado: propuesta
fase: F1
requisitos: [RF-01, RF-02, RF-26, RN-08, RN-11, CA-01, CA-03, CA-26]
adr: [ADR-004, ADR-005, ADR-014]
decisiones: [D-01, D-16, D-11]
actualizado: 2026-10-17
tags: [conecsia, backlog, epica]
---

# EP-03 Organizaciones y plano SaaS

> [!note] Fase: [[PLAN-DE-TRABAJO#F1]] · Requisitos: RF-01, RF-02, RF-26 (básico) · ADR: [[ADR-004 Tenancy con base compartida y organization_id]], [[ADR-005 Contexto de organización y autorización por permisos]], [[ADR-014 Auditoría append-only]] (Propuesta)

## Objetivo de la épica
Permitir al Superadministrador SaaS dar de alta, configurar e inactivar [[Organización|organizaciones]], asignar tipo, plan y módulos, y que cada organización ajuste su vocabulario y catálogos básicos sin alterar el núcleo (RN-11).

## Alcance / fuera de alcance
- **Dentro:** CRUD/inactivación de organizaciones, tipos de organización con plantilla básica, planes y habilitación de módulos, vocabulario configurable.
- **Fuera:** cobro, facturación, pasarelas, límites comerciales y suspensión por impago (diferidos, PRD §5.3); plantillas sectoriales completas (HU-051); acceso de soporte a datos personales.

## Historias
| ID | Historia | Tamaño | Estado | Bloqueada por |
|---|---|---|---|---|
| HU-009 | Alta, edición e inactivación de organizaciones | M | Lista para refinar | Efectos de inactivación: D-16 |
| HU-010 | Tipos de organización y plantilla básica de catálogos | M | Lista para refinar | — |
| HU-011 | Planes y habilitación de módulos por organización | M | Lista para refinar (parcial) | D-16 |
| HU-012 | Vocabulario configurable por organización | S | Lista para refinar | Etiquetas definitivas: D-01 |

---

## HU-009 Alta, edición e inactivación de organizaciones

**Como** Superadministrador SaaS **quiero** crear, editar e inactivar organizaciones cliente **para** gestionar el ciclo de vida de cada tenant.

| Campo | Valor |
|---|---|
| Épica / Fase | EP-03 / F1 |
| Requisitos | RF-01, RN-01, RN-08, CA-01 |
| Tamaño relativo | M (propuesto) |
| Estado | Lista para refinar |

### Objetivo
Gestionar la entidad [[Organización]] (frontera de aislamiento) con información general, tipo, estado y primer administrador.

### Actores
Superadministrador SaaS; Administrador de organización (lectura/edición de su información general, si el permiso lo concede).

### Dependencias
- Historias: HU-005, HU-007, HU-008; HU-016 (invitar primer administrador).
- ADR: [[ADR-004 Tenancy con base compartida y organization_id]] (Propuesta), [[ADR-005 Contexto de organización y autorización por permisos]] (Propuesta), [[ADR-014 Auditoría append-only]] (Propuesta).
- Decisiones: **D-16** (efectos operativos de inactivar), D-01 (nombre comercial).

### Contrato funcional
- **Entradas:** nombre, identificación tributaria/legal (opcional, formato no definido), tipo (HU-010), zona horaria por defecto (Propuesta, ver [[ADR-008 Modelo de tiempo y zonas horarias]]), contacto administrativo, correo del primer administrador.
- **Reglas (servidor):** solo permiso de plano SaaS (`saas.organizaciones.*`); nombre único global normalizado (Propuesta); el superadministrador **no** obtiene acceso ordinario a datos personales del cliente (Propuesta PRD §5.2.6); inactivar no borra datos.
- **Efectos persistidos:** `organization` con estado `Activa | Inactiva`, fechas de alta/inactivación y actor.
- **Inactivación (Pendiente D-16):** hasta decidir, inactivar **bloquea nuevas operaciones** y sesiones de usuarios de esa organización; no se define acceso de consulta histórica ni reactivación automática.
- **Errores:** 409 nombre duplicado o versión concurrente; 403 sin permiso SaaS; 404 para usuarios de otra organización.
- **Auditoría:** alta, cambios (anterior/nuevo), inactivación/reactivación.
- **Endpoints indicativos:** `POST/GET /api/v1/saas/organizaciones`, `PUT /api/v1/saas/organizaciones/{id}`, `POST /api/v1/saas/organizaciones/{id}/inactivar`.

### Criterios de aceptación
- [ ] **Dado** un superadministrador **cuando** crea una organización **entonces** queda activa, auditada y con invitación al primer administrador.
- [ ] **Dado** un administrador de A **cuando** intenta leer o editar la organización B **entonces** recibe 404 (CA-01).
- [ ] **Dado** una organización inactivada **cuando** uno de sus usuarios intenta una operación nueva **entonces** se rechaza y los datos históricos permanecen (RN-08).

### Tareas backend
- [ ] Agregado `Organization`, casos de uso de alta/edición/inactivación con concurrencia optimista.
- [ ] Política de plano SaaS separada de permisos de organización.
- [ ] Integración con invitación (HU-016) y auditoría.

### Tareas frontend
- [ ] Consola SaaS: lista paginada, formulario, confirmación de inactivación con advertencia de efectos.

### Pruebas
- [ ] Unitarias: transiciones de estado.
- [ ] Integración (PostgreSQL real): unicidad y concurrencia optimista.
- [ ] Aislamiento / permisos negativos: administrador de organización sin permiso SaaS; usuario de organización inactiva.
- [ ] E2E / UI: alta e inactivación.

### Definition of Done
Cumple la [[PLAN-DE-TRABAJO#Definition of Done general|DoD general]] y además:
- [ ] Comportamiento de inactivación revisado contra D-16 cuando se resuelva.

### Riesgos y decisiones pendientes
> [!todo] D-16 — efectos de inactivar (consulta histórica, sesiones, procesos de fondo, reactivación). D-01 — nombre comercial.

---

## HU-010 Tipos de organización y plantilla básica de catálogos

**Como** Superadministrador SaaS **quiero** asignar un tipo de organización que precargue catálogos y vocabulario básicos **para** acelerar la configuración inicial sin alterar el núcleo.

| Campo | Valor |
|---|---|
| Épica / Fase | EP-03 / F1 |
| Requisitos | RF-02, RF-26 (básico), RN-11, CA-26, CA-03 |
| Tamaño relativo | M (propuesto) |
| Estado | Lista para refinar |

### Objetivo
Catálogo de tipos (empresa, conjunto residencial, club, universidad, coworking, parque industrial, otro) y plantilla básica versionada que copia catálogos a la organización.

### Actores
Superadministrador SaaS; Administrador de organización (ajusta catálogos).

### Dependencias
- Historias: HU-009; HU-020 (tipos de persona/documento como catálogos sembrables).
- ADR: [[ADR-004 Tenancy con base compartida y organization_id]] (Propuesta).
- Decisiones: D-01 (etiquetas), D-11 (contenido sectorial avanzado — fuera de alcance).

### Contrato funcional
- **Entradas:** tipo de organización; versión de plantilla.
- **Reglas (servidor):** aplicar plantilla **copia** catálogos a la organización (no referencia compartida mutable); re-aplicar o cambiar plantilla **no** sobrescribe elementos personalizados ni elimina históricos; solo agrega faltantes marcados como origen plantilla (Propuesta); no modifica roles/permisos existentes.
- **Efectos persistidos:** catálogos de la organización con `origen` (`plantilla:vX` / `personalizado`) y registro de aplicación de plantilla.
- **Errores:** 409 si hay aplicación de plantilla concurrente; 404 tipo inexistente.
- **Auditoría:** aplicación/cambio de plantilla con resumen de elementos agregados.
- **Endpoints indicativos:** `GET /api/v1/saas/tipos-organizacion`, `POST /api/v1/saas/organizaciones/{id}/aplicar-plantilla`.

### Criterios de aceptación
- [ ] **Dado** una organización con catálogos personalizados **cuando** se cambia su plantilla **entonces** no se modifican permisos ni se eliminan personalizaciones o históricos (CA-26).
- [ ] **Dado** un catálogo precargado **cuando** el administrador lo edita o inactiva **entonces** se conserva la referencia histórica (CA-03).
- [ ] **Dado** dos aplicaciones simultáneas de plantilla **cuando** se ejecutan **entonces** no se duplican elementos.

### Tareas backend
- [ ] Modelo de tipo de organización y plantilla versionada (datos semilla en migración).
- [ ] Servicio de aplicación idempotente (clave natural por elemento de plantilla).

### Tareas frontend
- [ ] Selector de tipo en alta de organización; vista de qué elementos vienen de plantilla.

### Pruebas
- [ ] Unitarias: fusión plantilla vs personalizado.
- [ ] Integración (PostgreSQL real): idempotencia y concurrencia de aplicación.
- [ ] Aislamiento / permisos negativos: aplicar plantilla a otra organización sin permiso SaaS.
- [ ] E2E / UI: alta con tipo y ajuste de un catálogo.

### Definition of Done
Cumple la [[PLAN-DE-TRABAJO#Definition of Done general|DoD general]] y además:
- [ ] CA-26 verificado con prueba automatizada.

### Riesgos y decisiones pendientes
> [!todo] D-11 — contenido sectorial completo (HU-051). D-01 — etiquetas por vertical.

---

## HU-011 Planes y habilitación de módulos por organización

**Como** Superadministrador SaaS **quiero** asociar un plan y habilitar módulos opcionales por organización **para** que cada cliente solo opere los módulos contratados.

| Campo | Valor |
|---|---|
| Épica / Fase | EP-03 / F1 |
| Requisitos | RF-01, PRD §5.3, CA-23 (efecto de deshabilitar) |
| Tamaño relativo | M (propuesto) |
| Estado | Lista para refinar (parcial; contenido de planes depende de D-16) |

### Objetivo
Registrar plan y banderas de módulo (vehículos, equipos, correo, etc.) con verificación en servidor en cada operación del módulo.

### Actores
Superadministrador SaaS; sistema (guardas de módulo).

### Dependencias
- Historias: HU-009, HU-007.
- ADR: [[ADR-005 Contexto de organización y autorización por permisos]] (Propuesta).
- Decisiones: **D-16** (plan SaaS y módulos), D-02 (vehículos/equipos en primer cliente).

### Contrato funcional
- **Entradas:** plan (catálogo), lista de módulos habilitados.
- **Reglas (servidor):** guarda `RequireModule("vehiculos")` evaluada en servidor; deshabilitar impide **nuevas** operaciones del módulo; consulta histórica a usuarios autorizados (Propuesta PRD §5.3); inactivar un módulo **nunca** borra datos; cobro y límites comerciales fuera de alcance.
- **Efectos persistidos:** `organization_module` con estado, fecha y actor; plan asignado.
- **Errores:** operación de módulo deshabilitado ⇒ 403 con código `MODULO_NO_HABILITADO`.
- **Auditoría:** cambio de plan y de habilitación (anterior/nuevo).
- **Endpoints indicativos:** `GET /api/v1/saas/planes`, `PUT /api/v1/saas/organizaciones/{id}/modulos`, `GET /api/v1/me/modulos`.

### Criterios de aceptación
- [ ] **Dado** un módulo deshabilitado **cuando** se invoca una operación nueva del módulo por API **entonces** se rechaza con `MODULO_NO_HABILITADO` (CA-23).
- [ ] **Dado** un módulo deshabilitado tras uso **cuando** un usuario con permiso consulta históricos **entonces** siguen disponibles (Propuesta).
- [ ] **Dado** un administrador de organización **cuando** intenta habilitarse un módulo **entonces** se rechaza (solo plano SaaS).

### Tareas backend
- [ ] Modelo de plan y módulos; guarda de módulo reutilizable; caché por organización con invalidación.

### Tareas frontend
- [ ] Consola SaaS de plan/módulos; la SPA oculta menús de módulos no habilitados (solo UX).

### Pruebas
- [ ] Unitarias: guarda de módulo.
- [ ] Integración (PostgreSQL real): habilitar/deshabilitar y efecto inmediato.
- [ ] Aislamiento / permisos negativos: habilitación en A no afecta B; administrador de organización sin permiso SaaS.
- [ ] E2E / UI: deshabilitar módulo y verificar rechazo.

### Definition of Done
Cumple la [[PLAN-DE-TRABAJO#Definition of Done general|DoD general]] y además:
- [ ] Catálogo de módulos documentado; contenido de planes revisado contra D-16.

### Riesgos y decisiones pendientes
> [!todo] D-16 — planes, módulos y efectos. D-02 — necesidad de vehículos/equipos en el primer cliente.

---

## HU-012 Vocabulario configurable por organización

**Como** Administrador de organización **quiero** ajustar las etiquetas visibles (p. ej. "Sede" → "Conjunto", "Área" → "Torre/Apartamento") **para** que la interfaz use la terminología de mi sector sin cambiar el núcleo.

| Campo | Valor |
|---|---|
| Épica / Fase | EP-03 / F1 |
| Requisitos | RN-11, RF-26 (básico), PRD §1 |
| Tamaño relativo | S (propuesto) |
| Estado | Lista para refinar (etiquetas definitivas: D-01) |

### Objetivo
Diccionario de términos por organización sobre claves fijas del núcleo, con valores por defecto de la plantilla (HU-010).

### Actores
Administrador de organización; todos los usuarios (consumo).

### Dependencias
- Historias: HU-002 (claves i18n), HU-010.
- ADR: [[ADR-015 Frontend SPA con React, TypeScript y Tailwind]] (Propuesta).
- Decisiones: **D-01** (términos internos y etiquetas por vertical).

### Contrato funcional
- **Entradas:** pares `clave → etiqueta` (singular/plural) para un conjunto cerrado de claves (organización, sede, área/destino, zona, punto de acceso, persona, visita, autorización).
- **Reglas (servidor):** solo claves del catálogo cerrado; longitud máxima y sanitización (sin HTML); no cambia nombres de API, permisos ni códigos de motivo.
- **Efectos persistidos:** `organization_vocabulary` con versión.
- **Errores:** 400 clave desconocida o valor inválido; 403 sin permiso `configuracion.modificar`.
- **Auditoría:** cambios con valores anterior/nuevo.
- **Endpoints indicativos:** `GET /api/v1/vocabulario`, `PUT /api/v1/vocabulario`.

### Criterios de aceptación
- [ ] **Dado** una etiqueta personalizada **cuando** un usuario de la organización abre la SPA **entonces** ve el término personalizado y la API conserva sus nombres de núcleo (RN-11).
- [ ] **Dado** un usuario de otra organización **cuando** consulta vocabulario **entonces** recibe el de su propia organización.

### Tareas backend
- [ ] Modelo, validación y endpoints; caché por organización.

### Tareas frontend
- [ ] Proveedor de vocabulario que resuelve claves i18n con la etiqueta de la organización; pantalla de edición.

### Pruebas
- [ ] Unitarias: validación de claves y sanitización.
- [ ] Integración (PostgreSQL real): persistencia y versión.
- [ ] Aislamiento / permisos negativos: modificar vocabulario de otra organización; usuario sin permiso.
- [ ] E2E / UI: etiqueta cambiada visible en menú y formularios.

### Definition of Done
Cumple la [[PLAN-DE-TRABAJO#Definition of Done general|DoD general]] y además:
- [ ] Catálogo de claves documentado; ningún literal sectorial fijo en la SPA.

### Riesgos y decisiones pendientes
> [!todo] D-01 — confirmar términos internos Sede/Área y etiquetas por vertical antes de consolidar UI y contratos.
