---
tipo: epica
estado: propuesta
fase: F0
requisitos: [RN-01, RN-10, RF-06, RF-25, CA-01, CA-02, CA-20]
adr: [ADR-004, ADR-005, ADR-006, ADR-014, ADR-018, ADR-020]
decisiones: [D-03, D-14]
actualizado: 2026-10-17
tags: [conecsia, backlog, epica]
---

# EP-02 Tenancy, identidad y auditoría base

> [!note] Fase: [[PLAN-DE-TRABAJO#F0]] · Requisitos: RN-01, RN-10, RF-06, RF-25 · ADR: [[ADR-004 Tenancy con base compartida y organization_id]], [[ADR-005 Contexto de organización y autorización por permisos]], [[ADR-006 Identidad y sesiones]], [[ADR-014 Auditoría append-only]] — todos **Propuesta**.

## Objetivo de la épica
Garantizar desde la base que ninguna operación cruza la frontera de [[Organización]] ([[Multi-tenancy y aislamiento]]), que el contexto se deriva de la membresía verificada y que toda operación crítica queda auditada de forma inalterable ([[Seguridad y auditoría]]).

## Alcance / fuera de alcance
- **Dentro:** `organization_id` obligatorio, FKs compuestas, filtro global, contexto de organización/sede, autenticación (cuando D-03 se cierre), auditoría append-only en la misma transacción.
- **Fuera:** consulta de auditoría para auditor (HU-046), roles configurables completos (HU-017), SSO/MFA (D-03), acceso de soporte del superadministrador (Propuesta PRD §5.2.6, sin historia en este alcance).

## Historias
| ID | Historia | Tamaño | Estado | Bloqueada por |
|---|---|---|---|---|
| HU-005 | Aislamiento por organización en persistencia | L | **Implementada** 2026-10-19 en `loop/core` (sin fusionar) | RLS: D-22 |
| HU-006 | Autenticación y sesión de usuario | L | **Goal cerrado** 2026-10-21; el restablecimiento va a loop_integraciones y MFA, D-21 y ASVS a loop_quality | — |
| HU-007 | Contexto de organización y sede derivado de membresía | M | **En curso** (base implementada con HU-013) | Selección multi-membresía y caché dependen de D-03; autenticación provisional (ADR-022) |
| HU-008 | Registro de auditoría append-only | M | **En curso** (base implementada con HU-013) | Retención (D-14) |

---

> [!note] Avance 2026-10-17 (primera vertical, [[Sedes]])
> - **HU-005:** `IEntidadDeOrganizacion`, filtro global, rechazo de escrituras cruzadas al guardar, FK a `organizacion`, unicidad por organización y prueba de convención con exclusiones justificadas (`[SinFiltroDeOrganizacion]`). Falta: FKs compuestas efectivas (no hay todavía entidades hijas), spike RLS, suite parametrizada reutilizable.
> - **HU-007:** `ResolvedorContexto` + middleware; `X-Organizacion-Id` solo selecciona; 403 auditado ante organización ajena; `GET /api/v1/me`. Falta: autenticación real (D-03), caché de permisos, contexto explícito para procesos de fondo.
> - **HU-008:** `auditoria_evento` append-only (`REVOKE UPDATE, DELETE, TRUNCATE`) en la misma transacción. Falta: auditar denegaciones de permiso y procesos de fondo.

## HU-005 Aislamiento por organización en persistencia

**Como** responsable de seguridad de la plataforma **quiero** que toda entidad de cliente esté atada a su organización en el esquema y en las consultas **para** que sea imposible leer o relacionar datos entre organizaciones (RN-01).

| Campo | Valor |
|---|---|
| Épica / Fase | EP-02 / F0 |
| Requisitos | RN-01, CA-01 |
| Tamaño relativo | L (propuesto) |
| Estado | Lista para refinar |

### Objetivo
Implementar el patrón de tenancy de [[ADR-004 Tenancy con base compartida y organization_id]]: `organization_id NOT NULL`, FKs compuestas `(organization_id, id)`, índices únicos con `organization_id`, filtro global EF Core alimentado solo desde el contexto verificado.

### Actores
Sistema (todas las operaciones); desarrolladores (convención obligatoria).

### Dependencias
- Historias: HU-001, HU-003.
- ADR: [[ADR-004 Tenancy con base compartida y organization_id]] (Propuesta), [[ADR-005 Contexto de organización y autorización por permisos]] (Propuesta), [[ADR-020 Contratos API y formato de errores]] (Propuesta, 404 cross-tenant).
- Decisiones: evaluación de RLS como segunda defensa (spike dentro de la historia, no bloqueante).

### Contrato funcional
- **Entradas:** contexto de organización resuelto por HU-007 (`ITenantContext`); nunca un `organizationId` del cuerpo/query sin verificación.
- **Reglas (servidor):** entidad base `TenantEntity`; el `SaveChanges` rechaza escrituras cuyo `organization_id` difiera del contexto; ignorar el filtro global (`IgnoreQueryFilters`) solo en código marcado y revisado (p. ej. plano SaaS); procesos de fondo deben recibir `organization_id` explícito.
- **Efectos persistidos:** convención aplicada en todas las migraciones posteriores.
- **Errores:** recurso de otra organización ⇒ **404** (no 403); intento de escritura cruzada ⇒ excepción y 404/400 según caso; FK compuesta impide asociaciones cruzadas a nivel de base.
- **Auditoría:** intentos de escritura cruzada detectados se registran como resultado `Denegado` (cuando exista HU-008).
- **Endpoints:** transversal; endpoint de prueba interno solo en `Development`.

### Criterios de aceptación
- [x] **Dado** un usuario de Org A que conoce el ID de un recurso de Org B **cuando** intenta listarlo, leerlo o modificarlo **entonces** recibe 404 y no se revela su existencia (CA-01). — *`SuiteAislamientoTests` (Sede).*
- [ ] **Dado** un intento de relacionar una entidad de A con otra de B **cuando** se persiste **entonces** la FK compuesta lo rechaza en PostgreSQL (CA-01). — *No aplica aún: no hay relaciones entre entidades de organización. La prueba de convención `Toda_relacion_entre_entidades_de_organizacion_usa_una_clave_compuesta...` hará fallar la suite cuando aparezca una sin `organizacion_id`.*
- [x] **Dado** un nuevo `DbSet` de cliente sin filtro de organización **cuando** corre la suite **entonces** la prueba de convención falla.

### Tareas backend
- [x] `TenantEntity`, interceptor de `SaveChanges`, filtro global de consulta. (`IEntidadDeOrganizacion`, `ConecsIADbContext`)
- [x] Convención de FKs compuestas e índices únicos con `organization_id`. (pruebas de convención sobre el modelo)
- [x] Prueba de convención que recorre el modelo EF y exige filtro + `organization_id`.
- [x] Spike RLS con pool de conexiones y tareas de fondo; resultado documentado en ADR-004. (documental; la adopción queda en D-22)

### Tareas frontend
- No aplica (la SPA nunca envía `organizationId` como fuente de verdad).

### Pruebas
- [x] Unitarias: interceptor rechaza `organization_id` distinto.
- [x] Integración (PostgreSQL real): FK compuesta y unicidad por organización. (FK a `organizacion` y unicidad; la FK compuesta entre hijas aún no existe)
- [x] Aislamiento / permisos negativos: suite parametrizada Org A vs Org B (listar, leer, modificar, inactivar) reutilizable por todas las historias.
- [ ] E2E / UI: no aplica.

### Definition of Done
Cumple la [[PLAN-DE-TRABAJO#Definition of Done general|DoD general]] y además:
- [x] Suite de aislamiento obligatoria en CI; ningún `IgnoreQueryFilters` sin justificación revisada.

### Riesgos y decisiones pendientes
> [!todo] Riesgo — filtros de tenant incompletos (PRD, riesgos). RLS: adoptar o descartar con evidencia del spike.

---

## HU-006 Autenticación y sesión de usuario

**Como** usuario invitado **quiero** autenticarme de forma segura y mantener una sesión protegida **para** operar solo dentro de las organizaciones donde tengo membresía.

| Campo | Valor |
|---|---|
| Épica / Fase | EP-02 / F0 |
| Requisitos | PRD §5.2 (reglas 1, 7, 8), §12.5 |
| Tamaño relativo | L (propuesto) |
| Estado | **En curso** — D-03 aprobada el 2026-10-19 ([[ADR-006 Identidad y sesiones]], opción A) |

> [!success] Desbloqueada el 2026-10-19: opción A, multi-membresía, 8 h / 30 min, bloqueo 5 / 15 min, contraseñas de 12+ caracteres, MFA en Fase 3. Texto original: D-03 debía decidir proveedor de identidad (opción A: ASP.NET Core Identity + cookie; opción B: OIDC externo), múltiples membresías, invitación, SSO y MFA. [[ADR-006 Identidad y sesiones]] **no puede aprobarse** sin cerrar D-03. Puede avanzarse un stub de autenticación solo para desarrollo local.

### Objetivo
Autenticación de [[Usuarios, roles y permisos|usuarios]] separada de [[Persona]], con derivación segura de contraseñas (si opción A), límite de intentos, restablecimiento seguro y revocación de sesiones al retirar acceso.

### Actores
Usuario de organización; Superadministrador SaaS; sistema.

### Dependencias
- Historias: HU-001, HU-004, HU-005. Habilita HU-007, HU-016.
- ADR: [[ADR-006 Identidad y sesiones]] (Propuesta), [[ADR-018 Observabilidad sin datos sensibles]] (Propuesta).
- Decisiones: **D-03** (bloqueante), D-14 (retención de registros de sesión).

### Contrato funcional
- **Entradas:** credenciales o flujo OIDC (según D-03); token de invitación (HU-016).
- **Reglas (servidor):** sin registro público; usuario inactivo o sin membresía activa no obtiene sesión útil; límite de intentos con bloqueo temporal (umbral configurable, valor a definir); cookie `HttpOnly`/`Secure`/`SameSite` + anti-CSRF si opción A; inactivar usuario o membresía invalida sesiones vigentes.
- **Efectos persistidos:** usuario, hash de credencial (nunca en claro), sellos de seguridad, sesiones/refresh según proveedor.
- **Errores:** 401 credenciales inválidas (mensaje genérico, sin revelar si el usuario existe); 429/423 por intentos excedidos.
- **Auditoría:** inicio/cierre de sesión, fallos, bloqueos, restablecimiento y revocación; nunca contraseñas ni tokens.
- **Endpoints indicativos:** `POST /api/v1/auth/login`, `POST /api/v1/auth/logout`, `POST /api/v1/auth/password/reset-request`, `POST /api/v1/auth/password/reset`, `GET /api/v1/auth/me`.

### Criterios de aceptación
- [ ] **Dado** credenciales válidas de usuario activo **cuando** inicia sesión **entonces** obtiene una sesión protegida y `GET /me` devuelve sus membresías activas.
- [ ] **Dado** un usuario cuya membresía se inactiva **cuando** usa su sesión **entonces** la siguiente petición protegida es rechazada (PRD §5.2.8).
- [ ] **Dado** varios intentos fallidos **cuando** se supera el umbral **entonces** se bloquea temporalmente y se audita.
- [ ] **Dado** cualquier flujo de autenticación **cuando** se revisan logs y auditoría **entonces** no contienen contraseñas ni tokens.

### Tareas backend
- [ ] Implementar opción elegida en D-03; política de contraseñas y bloqueo.
- [ ] Revocación de sesiones al inactivar usuario/membresía (sello de seguridad).
- [ ] Flujo de restablecimiento con token de un solo uso y expiración corta.

### Tareas frontend
- [ ] Pantallas de inicio de sesión, restablecimiento y cierre; manejo de 401 global.

### Pruebas
- [ ] Unitarias: política de bloqueo y expiración de tokens.
- [ ] Integración (PostgreSQL real): ciclo completo login → revocación.
- [ ] Aislamiento / permisos negativos: sesión de usuario de A no accede a recursos de B; usuario inactivo rechazado.
- [ ] E2E / UI: login, error genérico, cierre de sesión.

### Definition of Done
Cumple la [[PLAN-DE-TRABAJO#Definition of Done general|DoD general]] y además:
- [ ] D-03 cerrada y ADR-006 aprobado.
- [ ] Revisión de seguridad (OWASP ASVS nivel acordado) del flujo.

### Riesgos y decisiones pendientes
> [!todo] D-03 — proveedor, multi-membresía, SSO, MFA, duración de sesión. D-14 — retención de eventos de sesión.

---

## HU-007 Contexto de organización y sede derivado de membresía

**Como** usuario autenticado **quiero** que el servidor determine mi organización y sedes a partir de mi membresía **para** que nadie pueda operar en otra organización manipulando identificadores.

| Campo | Valor |
|---|---|
| Épica / Fase | EP-02 / F0 |
| Requisitos | RN-01, RN-10, RF-06, PRD §5.2 (reglas 1–3), CA-01, CA-02 |
| Tamaño relativo | M (propuesto) |
| Estado | Lista para refinar (selección multi-membresía depende de D-03) |

### Objetivo
Resolver por petición `ITenantContext` (organización, membresía, permisos efectivos, sedes/destinos/puntos asignados) y exponer políticas de autorización por permiso + alcance.

### Actores
Todo usuario autenticado; procesos de fondo (contexto explícito).

### Dependencias
- Historias: HU-005, HU-006 (o stub local). Habilita HU-017, HU-018.
- ADR: [[ADR-005 Contexto de organización y autorización por permisos]] (Propuesta).
- Decisiones: D-03 (si una identidad puede tener varias membresías).

### Contrato funcional
- **Entradas:** sesión autenticada; cabecera opcional de **selección** de organización/sede (p. ej. `X-Organization-Id`) que solo elige entre membresías verificadas.
- **Reglas (servidor):** organización activa + membresía vigente + permiso + alcance territorial en cada operación; nunca se otorga acceso por el nombre del rol; organización seleccionada no perteneciente al usuario ⇒ rechazo; organización inactiva ⇒ efectos según D-16 (hasta decidir: sin operaciones nuevas).
- **Efectos persistidos:** ninguno (lectura de membresías); caché de permisos con clave que incluye organización y versión de membresía.
- **Errores:** 403 sin permiso dentro de la propia organización; 404 si se apunta a recurso de otra organización; 403 si la selección de organización no corresponde a una membresía.
- **Auditoría:** intentos de selección de organización no permitida y denegaciones de permiso (Propuesta PRD §12.6).
- **Endpoints indicativos:** `GET /api/v1/me/memberships`, `POST /api/v1/me/context` (si D-03 permite multi-membresía).

### Criterios de aceptación
- [ ] **Dado** un usuario con membresía solo en A **cuando** envía `X-Organization-Id` de B **entonces** se rechaza y se audita (CA-01).
- [ ] **Dado** un operador asignado a la sede S1 **cuando** opera sobre S2 **entonces** el backend lo rechaza aunque la UI no lo impida (CA-02).
- [ ] **Dado** un cambio de permisos de la membresía **cuando** se hace la siguiente petición **entonces** se aplican los permisos nuevos (invalidación de caché).

### Tareas backend
- [ ] Middleware de resolución de contexto; `ITenantContext` inyectable.
- [ ] Políticas ASP.NET Core basadas en permiso (`RequirePermission("personas.crear")`) + evaluador de alcance.
- [ ] Contexto explícito para procesos de fondo.

### Tareas frontend
- [ ] Selector de organización/sede (solo si D-03 permite varias membresías); el selector **no** es control de seguridad.

### Pruebas
- [ ] Unitarias: evaluador permiso + alcance.
- [ ] Integración (PostgreSQL real): resolución con membresías activas/inactivas.
- [ ] Aislamiento / permisos negativos: cabecera manipulada, sede no asignada, membresía inactiva, rol con nombre "Administrador" sin permisos.
- [ ] E2E / UI: cambio de contexto (si aplica).

### Definition of Done
Cumple la [[PLAN-DE-TRABAJO#Definition of Done general|DoD general]] y además:
- [ ] Ningún endpoint de cliente sin política de permiso (prueba que recorre endpoints).

### Riesgos y decisiones pendientes
> [!todo] D-03 — multi-membresía. D-16 — efectos operativos de organización inactiva.

---

## HU-008 Registro de auditoría append-only

**Como** auditor **quiero** que toda operación crítica quede registrada de forma inalterable **para** reconstruir quién hizo qué, cuándo y con qué resultado (RF-25).

| Campo | Valor |
|---|---|
| Épica / Fase | EP-02 / F0 |
| Requisitos | RF-25, CA-20, PRD §12.6 |
| Tamaño relativo | M (propuesto) |
| Estado | Lista para refinar |

### Objetivo
Servicio de auditoría escrito en la misma transacción que la operación, tabla solo-inserción sin `UPDATE`/`DELETE` para el rol de aplicación.

### Actores
Sistema; cualquier actor que ejecute operaciones críticas; auditor (consulta en HU-046).

### Dependencias
- Historias: HU-005, HU-007.
- ADR: [[ADR-014 Auditoría append-only]] (Propuesta), [[ADR-018 Observabilidad sin datos sensibles]] (Propuesta).
- Decisiones: D-14 (retención de auditoría — **no** se fija plazo; no se purga hasta decidir).

### Contrato funcional
- **Entradas:** `IAuditLog.Record(accion, objeto, resultado, anterior?, nuevo?)` desde casos de uso.
- **Reglas (servidor):** registra organización, actor (usuario o sistema), instante (`timestamptz`), acción, tipo/ID de objeto, resultado, valores anterior/nuevo cuando aplique, IP si disponible, `traceId`; misma transacción (si la operación hace rollback, no queda auditoría de éxito); valores minimizados (sin secretos ni datos personales completos innecesarios).
- **Efectos persistidos:** tabla `audit_event` con `organization_id` (nullable solo para eventos del plano SaaS, marcados).
- **Errores:** fallo al escribir auditoría ⇒ falla la operación completa.
- **Auditoría:** cobertura mínima: creación, modificación, inactivación, decisión, rechazo, revocación, entrada, salida, cambios de permisos/configuración; Propuesta: denegaciones, exportaciones, acceso de soporte.
- **Endpoints:** no hay endpoints de escritura; ninguno de modificación o borrado.

### Criterios de aceptación
- [ ] **Dado** una operación crítica confirmada **cuando** se consulta la tabla **entonces** existe el evento con actor, organización, instante, acción y objeto (CA-20).
- [ ] **Dado** el rol de base de datos de la aplicación **cuando** intenta `UPDATE` o `DELETE` sobre auditoría **entonces** PostgreSQL lo rechaza (CA-20).
- [ ] **Dado** una operación que falla y hace rollback **cuando** se revisa auditoría **entonces** no hay evento de éxito.

### Tareas backend
- [ ] Tabla, índices (organización, instante, objeto) y `GRANT` solo `INSERT, SELECT` al rol de aplicación en migración.
- [ ] Servicio `IAuditLog` transaccional y serialización minimizada de valores.
- [ ] Prueba de cobertura: casos de uso críticos deben auditar.

### Tareas frontend
- No aplica (consulta en HU-046).

### Pruebas
- [ ] Unitarias: minimización/enmascarado de valores.
- [ ] Integración (PostgreSQL real): permisos `GRANT` y atomicidad con rollback.
- [ ] Aislamiento / permisos negativos: eventos de A no visibles en contexto de B.
- [ ] E2E / UI: no aplica.

### Definition of Done
Cumple la [[PLAN-DE-TRABAJO#Definition of Done general|DoD general]] y además:
- [ ] Separación de roles de BD (migraciones vs aplicación) documentada y verificada.

### Riesgos y decisiones pendientes
> [!todo] D-14 — retención y volumen de auditoría; particionado por fecha a evaluar con D-15.
