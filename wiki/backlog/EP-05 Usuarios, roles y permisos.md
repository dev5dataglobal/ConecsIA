---
tipo: epica
estado: propuesta
fase: F1
requisitos: [RF-06, RN-10, CA-01, CA-02, CA-05, CA-07]
adr: [ADR-005, ADR-006, ADR-014]
decisiones: [D-03, D-04]
actualizado: 2026-10-17
tags: [conecsia, backlog, epica]
---

# EP-05 Usuarios, roles y permisos

> [!note] Fase: [[PLAN-DE-TRABAJO#F1]] · Requisitos: RF-06, RN-10 · ADR: [[ADR-005 Contexto de organización y autorización por permisos]], [[ADR-006 Identidad y sesiones]] (Propuesta)

## Objetivo de la épica
Administrar [[Usuarios, roles y permisos|usuarios, roles configurables y alcance territorial]] de modo que el acceso efectivo sea **permiso + organización + sedes/destinos/puntos asignados**, nunca el nombre del rol.

## Alcance / fuera de alcance
- **Dentro:** invitación y administración de usuarios/membresías, catálogo de permisos finos, roles plantilla y personalizados, asignación de alcance.
- **Fuera:** autenticación (HU-006), SSO/MFA (D-03), reglas de decisión de autorizaciones (D-04).

## Historias
| ID | Historia | Tamaño | Estado | Bloqueada por |
|---|---|---|---|---|
| HU-016 | Invitar y administrar usuarios | M | Lista para refinar | D-03 y HU-006 resueltos (2026-10-19/20); envío de la invitación: D-12 |
| HU-017 | Roles configurables y catálogo de permisos | L | Lista para refinar | — |
| HU-018 | Alcance por sede, destino y punto de acceso | M | Lista para refinar | — |

---

## HU-016 Invitar y administrar usuarios

**Como** Administrador de organización **quiero** invitar usuarios, asignarles roles e inactivarlos **para** controlar quién opera en mi organización sin registro público.

| Campo | Valor |
|---|---|
| Épica / Fase | EP-05 / F1 |
| Requisitos | RF-06, CA-05, PRD §5.2.8 |
| Tamaño relativo | M (propuesto) |
| Estado | Lista para refinar (aceptación de invitación depende de D-03) |

### Objetivo
Alta por invitación administrativa, gestión de membresías y su inactivación con revocación de sesiones, independiente de [[Persona]] (CA-05).

### Actores
Administrador de organización; Administrador de sede (si tiene permiso de invitar en su alcance); usuario invitado.

### Dependencias
- Historias: HU-006 (bloqueada por D-03), HU-007, HU-017, HU-008; correo (HU-042) o canal alterno para la invitación en desarrollo (captura local HU-003).
- ADR: [[ADR-006 Identidad y sesiones]] (Propuesta), [[ADR-005 Contexto de organización y autorización por permisos]] (Propuesta).
- Decisiones: **D-03** (invitación, multi-membresía), D-12 (correo de invitación).

### Contrato funcional
- **Entradas:** correo, nombre, roles[], alcance inicial (HU-018), vínculo opcional a Persona.
- **Reglas (servidor):** sin registro público; token de invitación de un solo uso, expiración configurable, almacenado como hash; si el correo ya tiene identidad, el comportamiento (nueva membresía vs rechazo) depende de D-03 — hasta decidir: **rechazar** con 409; un administrador no puede asignar permisos que él no posee (Propuesta, evita escalamiento); inactivar membresía revoca sesiones y conserva autoría.
- **Efectos persistidos:** `user`, `membership` (estado, roles, fechas, actor), `invitation` (hash, expiración, estado).
- **Errores:** 409 invitación duplicada/correo existente; 410 invitación expirada o usada; 403 escalamiento de privilegios.
- **Auditoría:** invitación, aceptación, cambios de roles (anterior/nuevo), inactivación/reactivación.
- **Endpoints indicativos:** `POST /api/v1/usuarios/invitaciones`, `POST /api/v1/invitaciones/{token}/aceptar`, `GET /api/v1/usuarios`, `PUT /api/v1/usuarios/{id}/roles`, `POST /api/v1/usuarios/{id}/inactivar`.

### Criterios de aceptación
- [ ] **Dado** una persona visitante y un usuario autenticado **cuando** se administran **entonces** inactivar uno no afecta al otro (CA-05).
- [ ] **Dado** una invitación ya aceptada **cuando** se reutiliza el token **entonces** se rechaza.
- [ ] **Dado** un usuario inactivado **cuando** consulto sus decisiones previas **entonces** conservan su autoría (PRD §5.2.5).
- [ ] **Dado** un administrador de A **cuando** lista usuarios **entonces** no ve usuarios de B (CA-01).

### Tareas backend
- [ ] Casos de uso de invitación/aceptación/inactivación; hash de token; revocación de sesiones.
- [ ] Prevención de escalamiento de privilegios.

### Tareas frontend
- [ ] Lista de usuarios, formulario de invitación, edición de roles, pantalla de aceptación de invitación.

### Pruebas
- [ ] Unitarias: expiración y uso único del token.
- [ ] Integración (PostgreSQL real): aceptación concurrente del mismo token ⇒ una sola membresía.
- [ ] Aislamiento / permisos negativos: invitar en otra organización; asignar permiso no poseído.
- [ ] E2E / UI: invitar, aceptar (correo capturado localmente), inactivar.

### Definition of Done
Cumple la [[PLAN-DE-TRABAJO#Definition of Done general|DoD general]] y además:
- [ ] Token de invitación nunca en logs ni en URL de terceros.

### Riesgos y decisiones pendientes
> [!todo] D-03 — multi-membresía e invitación. D-12 — proveedor y remitente de correo.

---

## HU-017 Roles configurables y catálogo de permisos

**Como** Administrador de organización **quiero** crear roles a partir de permisos finos y partir de perfiles plantilla **para** adaptar responsabilidades sin depender de nombres de rol.

| Campo | Valor |
|---|---|
| Épica / Fase | EP-05 / F1 |
| Requisitos | RF-06, RN-10, CA-07 (permiso de decidir), PRD §5.1–5.2 |
| Tamaño relativo | L (propuesto) |
| Estado | Lista para refinar |

### Objetivo
Catálogo cerrado de permisos (consultar, crear, modificar, inactivar, decidir, revocar, registrar entrada/salida, exportar, administrar por recurso) y roles por organización; perfiles iniciales como plantilla copiable: Administrador de organización, Administrador de sede, Operador de acceso, Autorizador, Auditor (Superadministrador SaaS pertenece al plano SaaS).

### Actores
Administrador de organización; sistema (evaluación de políticas).

### Dependencias
- Historias: HU-007, HU-010 (plantilla).
- ADR: [[ADR-005 Contexto de organización y autorización por permisos]] (Propuesta), [[ADR-014 Auditoría append-only]] (Propuesta).
- Decisiones: D-04 (qué permisos de decisión/revocación existen por tipo de autorización).

### Contrato funcional
- **Entradas:** nombre de rol, descripción, permisos[].
- **Reglas (servidor):** permisos solo del catálogo; el código **nunca** evalúa `role.Name`; el operador no recibe `autorizaciones.decidir` por defecto (PRD §6.2); al menos un administrador activo con `administrar` debe permanecer (no se puede quitar el último — Propuesta); roles plantilla editables como copia; cambios de rol invalidan caché de permisos.
- **Efectos persistidos:** `role`, `role_permission` por organización; catálogo `permission` global y versionado.
- **Errores:** 400 permiso desconocido; 409 nombre duplicado; 422 quitar último administrador; 403 escalamiento.
- **Auditoría:** alta/cambio/inactivación de rol con permisos anterior/nuevo.
- **Endpoints indicativos:** `GET /api/v1/permisos`, `GET/POST /api/v1/roles`, `PUT /api/v1/roles/{id}`, `POST /api/v1/roles/{id}/inactivar`.

### Criterios de aceptación
- [ ] **Dado** un rol llamado "Administrador" sin permisos **cuando** su usuario invoca un endpoint administrativo **entonces** se rechaza (RN-10).
- [ ] **Dado** un operador sin `autorizaciones.decidir` **cuando** intenta aprobar por API **entonces** recibe 403 (CA-07).
- [ ] **Dado** un cambio de permisos de un rol **cuando** sus usuarios hacen la siguiente petición **entonces** rige el nuevo conjunto.
- [ ] **Dado** roles de A **cuando** un administrador de B lista roles **entonces** no los ve (CA-01).

### Tareas backend
- [ ] Catálogo de permisos como constantes + semilla; políticas por permiso.
- [ ] CRUD de roles, invariante de último administrador, invalidación de caché.
- [ ] Prueba que recorre endpoints y exige permiso declarado.

### Tareas frontend
- [ ] Editor de roles con permisos agrupados por recurso; ocultar acciones sin permiso (solo UX).

### Pruebas
- [ ] Unitarias: evaluación de permisos e invariantes.
- [ ] Integración (PostgreSQL real): roles y caché.
- [ ] Aislamiento / permisos negativos: matriz endpoint × rol plantilla con casos negativos.
- [ ] E2E / UI: crear rol personalizado y verificar acceso.

### Definition of Done
Cumple la [[PLAN-DE-TRABAJO#Definition of Done general|DoD general]] y además:
- [ ] Matriz de permisos documentada en wiki y verificada por prueba automatizada.

### Riesgos y decisiones pendientes
> [!todo] D-04 — permisos de decisión, revocación y cancelación por tipo de autorización.

---

## HU-018 Alcance por sede, destino y punto de acceso

**Como** Administrador de organización **quiero** limitar a cada usuario a sedes, destinos y puntos de acceso asignados **para** que solo opere donde le corresponde.

| Campo | Valor |
|---|---|
| Épica / Fase | EP-05 / F1 |
| Requisitos | RF-06, CA-02, RN-10 |
| Tamaño relativo | M (propuesto) |
| Estado | Lista para refinar |

### Objetivo
Asignación de alcance territorial a la membresía y evaluador de alcance aplicado a consultas (filtrado) y comandos (rechazo).

### Actores
Administrador de organización; Administrador de sede (dentro de su sede); todos los usuarios (sujeto).

### Dependencias
- Historias: HU-007, HU-013, HU-014, HU-015, HU-017.
- ADR: [[ADR-005 Contexto de organización y autorización por permisos]] (Propuesta).
- Decisiones: D-04 (destinos de cada autorizador).

### Contrato funcional
- **Entradas:** membresía, sedes[], destinos[], puntos de acceso[], o marca "toda la organización".
- **Reglas (servidor):** alcance vacío ⇒ ningún acceso territorial (por defecto denegar); alcance de destino/punto implica pertenencia a una sede asignada; listas y búsquedas se filtran por alcance; comandos fuera de alcance ⇒ 403; un administrador de sede solo asigna dentro de su propio alcance.
- **Efectos persistidos:** `membership_scope` (tipo, objetivo, vigencia).
- **Errores:** 403 fuera de alcance; 404 objetivo de otra organización; 422 destino fuera de sede asignada.
- **Auditoría:** cambios de alcance (anterior/nuevo).
- **Endpoints indicativos:** `GET/PUT /api/v1/usuarios/{id}/alcance`.

### Criterios de aceptación
- [ ] **Dado** un operador asignado solo a S1 **cuando** registra o consulta en S2 por API **entonces** el backend lo rechaza (CA-02).
- [ ] **Dado** un operador con alcance a un punto de acceso **cuando** intenta operar en otro punto de la misma sede **entonces** se rechaza.
- [ ] **Dado** un administrador de sede S1 **cuando** intenta asignar alcance sobre S2 **entonces** se rechaza.

### Tareas backend
- [ ] Modelo de alcance; evaluador `IScopeEvaluator`; especificaciones de filtrado reutilizables en repositorios.

### Tareas frontend
- [ ] Editor de alcance (árbol sede → destinos/puntos).

### Pruebas
- [ ] Unitarias: evaluador con combinaciones de alcance.
- [ ] Integración (PostgreSQL real): filtrado de listas por alcance.
- [ ] Aislamiento / permisos negativos: sede no asignada, punto no asignado, objetivo de otra organización.
- [ ] E2E / UI: asignar alcance y comprobar visibilidad.

### Definition of Done
Cumple la [[PLAN-DE-TRABAJO#Definition of Done general|DoD general]] y además:
- [ ] Evaluador de alcance usado por todos los casos de uso con dimensión territorial (verificado por revisión y prueba).

### Riesgos y decisiones pendientes
> [!todo] D-04 — relación autorizador ↔ destinos puede requerir alcance específico de decisión distinto al de operación.
