---
tipo: plan
estado: propuesta
fuentes: [PRD v0.1, wiki/backlog EP-01..EP-19, Registro de ADR, db.md, código en main 85be740]
relacionados: [goal_authentication.md, PLAN-DE-TRABAJO, tablero-scrum]
actualizado: 2026-10-21
tags: [conecsia, loops, plan]
---

# Plan de loops

> [!warning] Ejecución bloqueada
> `goal_authentication.md` **no está completo**. El núcleo de HU-006 está verificado, pero siguen abiertas cinco casillas, todas detenidas por decisiones de producto: MFA (D-03f), límite por IP (D-21), solicitud y confirmación de restablecimiento (D-12 + ADR-012), expiración del token de restablecimiento y revisión OWASP ASVS. Por eso **ningún loop está marcado como listo para ejecutar**. Cada `loop_*.md` repite esta condición en su sección «Condiciones de inicio».
>
> Para desbloquear, el responsable debe elegir una de dos opciones: a) cerrar esas decisiones y terminar el goal, o b) declarar formalmente que las casillas detenidas se difieren (por ejemplo, a `loop_integraciones` y `loop_quality`) y dar el goal por cerrado. La opción b es la que se propone aquí, porque ninguna historia de los loops depende de MFA, del límite por IP ni del restablecimiento (ver §2.3).

> [!note] Fuentes que no existen
> Se pidieron `AGENTS.md`, `wiki/vision-general.md` y `wiki/pendientes.md`, pero no existen en el repositorio. Se usaron sus equivalentes: `CLAUDE.md`, `wiki/overview.md` y `wiki/decisiones/Decisiones pendientes.md`. Las historias y dependencias salen de `wiki/backlog/EP-*.md`, y ninguna se inventó.

## 1. Estado de partida (2026-10-21)

| Estado | Historias |
|---|---|
| En revisión (implementadas y verificadas localmente, sin cerrar DoD) | HU-001, HU-002, HU-003, HU-004, HU-013 |
| Núcleo verificado; piezas restantes bloqueadas | HU-006 |
| En curso (base implementada con HU-013) | HU-005, HU-007, HU-008 |
| Sin empezar | HU-009 a HU-012, HU-014 a HU-056 |

**Dependen de la autenticación:** de forma directa HU-007 (selección multi-membresía), HU-016 (invitación y contraseña inicial) y HU-055 (endurecimiento). De forma indirecta, todas las historias que exigen sesión real, es decir, todas desde F1. Las semillas y ADR-022 solo sirven en Development.

**Código existente:** módulos `Organizaciones`, `Identidad`, `Estructura` y `Auditoria` con contenido; `Personas`, `Autorizaciones` y `Accesos` como esqueletos vacíos. Frontend con `features/inicio`, `sedes`, `sesion` y `sistema`. Migraciones `Inicial`, `PrimeraVerticalSedes`, `CredencialUsuario` y `SesionesUsuario`.

## 2. Matriz del backlog

Leyenda de módulos backend: Org = Organizaciones, Ide = Identidad, Est = Estructura, Per = Personas, Aut = Autorizaciones, Acc = Accesos, Aud = Auditoria. **Nuevo** = módulo que todavía no existe (nombre propuesto). Las tablas son las de `db.md`, versión 2.

| HU | Épica | Fase | Dependencias (backlog) | Backend | Frontend | Tablas / migraciones | Integración externa | Conflictos posibles | Loop |
|---|---|---|---|---|---|---|---|---|---|
| HU-001 | EP-01 | F0 | — | toda la solución, CI | — | — | GitHub Actions | `Directory.*.props`, CI | quality (cierre) |
| HU-002 | EP-01 | F0 | 001, 004 | — | `app/`, `shared/` | — | — | `rutas.tsx`, `AppShell` | quality (cierre) |
| HU-003 | EP-01 | F0 | 001, 002 | Api/Semillas | — | — | Docker, Mailpit | `docker-compose.yml` | quality (cierre) |
| HU-004 | EP-01 | F0 | 001 | Api/Errores, Observabilidad | `shared/api` | — | — | `ManejadorExcepciones` | quality (cierre) |
| HU-005 | EP-02 | F0 | 001, 003 | Infrastructure/Persistencia | — | filtros globales; spike RLS | — | `ConecsIADbContext` | core |
| HU-006 | EP-02 | F0 | 001, 004, 005 | Ide, Api/Seguridad | `features/sesion` | USUARIO, sesión | correo (reset) | — | goal_authentication |
| HU-007 | EP-02 | F0 | 005, 006 | Ide, Api/Seguridad | `features/sesion` (selector) | MEMBRESIA_ORGANIZACION | — | `MiddlewareContextoOrganizacion` | core |
| HU-008 | EP-02 | F0 | 005, 007 | Aud | — | AUDITORIA | — | `IRegistroAuditoria` (consumido por todos) | core |
| HU-009 | EP-03 | F1 | 005, 007, 008, 016 | Org | nuevo `features/organizaciones` | ORGANIZACION | — | — | core |
| HU-010 | EP-03 | F1 | 009, 020 | Org | `organizaciones` | TIPO_ORGANIZACION, PLANTILLA_ROL* | — | catálogos de HU-020 | core |
| HU-011 | EP-03 | F1 | 009, 007 | Org | `organizaciones` | PLAN, MODULO, PLAN_MODULO, ORGANIZACION_MODULO | — | consumido por EP-16/17 | core |
| HU-012 | EP-03 | F1 | 002, 010 | Org | `shared/i18n` | PARAMETRO_ORGANIZACION o vocabulario | — | `es.ts`, `i18n.tsx` | core |
| HU-013 | EP-04 | F1 | 005, 007, 008, 009 | Est | `features/sedes` | SEDE, CIUDAD | — | — | core (cierre DoD) |
| HU-014 | EP-04 | F1 | 013, 016 | Est | nuevo `features/areas` | AREA, AREA_RESPONSABLE | — | D-04 | core |
| HU-015 | EP-04 | F1 | 013 | Est | nuevo `features/zonas` | ZONA (ADR-019) | — | consumido por Acc | core |
| HU-016 | EP-05 | F1 | 006, 007, 017, 008; 042 | Ide | nuevo `features/usuarios` | USUARIO, MEMBRESIA_* | correo (D-12) | envío lo entrega integraciones | core |
| HU-017 | EP-05 | F1 | 007, 010 | Ide | `features/roles` | PERMISO, ROL, ROL_PERMISO, MEMBRESIA_ROL, PLANTILLA_ROL_PERMISO | — | `Permisos.cs` (SharedKernel) | core |
| HU-018 | EP-05 | F1 | 007, 013, 014, 015, 017 | Ide, Api/Seguridad | `features/usuarios` | MEMBRESIA_SEDE, MEMBRESIA_PUNTO_ACCESO | — | filtro de alcance usado por todos | core |
| HU-019 | EP-06 | F1 | 005, 007, 008, 018, 020 | Per | nuevo `features/personas` | PERSONA, PERSONA_TELEFONO, RESTRICCION_ACCESO | — | D-09 | personas |
| HU-020 | EP-06 | F1 | 007, 010 | Per | `personas` | TIPO_PERSONA, TIPO_DOCUMENTO | — | plantilla de HU-010 | personas |
| HU-021 | EP-06 | F1 | 019, 003 | Per + **nuevo Archivos** | `personas` (foto) | ARCHIVO | almacenamiento de objetos (D-13) | `docker-compose.yml` | integraciones |
| HU-022 | EP-06 | F1 | 019, 008 | Per | `personas` | PERSONA.activo | — | — | personas |
| HU-023 | EP-07 | F2 | 005, 007, 008, 013, 014, 015, 017, 018, 019, 024 | Aut | nuevo `features/autorizaciones` | AUTORIZACION, AUTORIZACION_ZONA, AUTORIZACION_TRANSICION | — | — | autorizaciones |
| HU-024 | EP-07 | F2 | 013, 023 | Aut | `autorizaciones` | AUTORIZACION_HORARIO | — | D-05, D-06, D-18 | autorizaciones |
| HU-025 | EP-07 | F2 | 023, 024, 014, 018 | Aut | `autorizaciones` | AUTORIZACION_TRANSICION | — | D-04 | autorizaciones |
| HU-026 | EP-07 | F2 | 023, 025, 027, 030, 034 | Aut | `autorizaciones` | AUTORIZACION_TRANSICION | — | salida tras revocación (034, Acc) | autorizaciones |
| HU-027 | EP-07 | F2 | 024, 026, 013, 015, 022 | Aut | — | (calculado, sin tabla) | — | consumido por Acc | autorizaciones |
| HU-028 | EP-08 | F2 | 019, 025, 027, 014, 018 | Aut | nuevo `features/visitas` | VISITA | — | VISITA la consume Acc | autorizaciones |
| HU-029 | EP-08 | F2 | 023, 024, 030, 019; 042/043 | Aut | `visitas` | INVITACION_ENVIO | correo (D-12) | envío lo entrega integraciones | autorizaciones |
| HU-030 | EP-09 | F2 | 023, 026, 029 | Aut | `autorizaciones` | TOKEN_QR | — | ADR-010 | autorizaciones |
| HU-031 | EP-09 | F2 | 027, 030, 018, 021 | Aut | nuevo `features/recepcion` (lectura) | — | lector QR (D-13) | `recepcion/` compartido con Acc | autorizaciones |
| HU-032 | EP-10 | F3 | 005, 007, 030, 031, F2 | Acc | `recepcion` | PERMANENCIA, MOVIMIENTO_ACCESO | — | — | accesos |
| HU-033 | EP-10 | F3 | 032, 005 | Acc | — | SOLICITUD_IDEMPOTENTE, restricciones | — | ADR-007, D-07 | accesos |
| HU-034 | EP-10 | F3 | 032, 033 | Acc | `recepcion` | PERMANENCIA | — | — | accesos |
| HU-035 | EP-10 | F3 | 034, 040, 046 | Acc | `recepcion` | MOVIMIENTO_ACCESO (corrección) | — | D-08; depende de alertas | accesos (parte bloqueada al final) |
| HU-036 | EP-10 | F3 | 002, 031–035, 037 | — | `recepcion` | — | lector QR (D-13) | — | accesos |
| HU-037 | EP-11 | F3 | 033, 034, 007 | Acc + SignalR | nuevo `features/presencia` | (consulta) | — | ADR-011 en Propuesta | accesos |
| HU-038 | EP-11 | F3 | 032, 034, F1, F2 | Acc | `presencia` | (consulta) | — | D-14 | accesos |
| HU-039 | EP-11 | F3 | F2 (decisión), 037 | Aut + SignalR | `autorizaciones` (bandeja) | (consulta) | — | toca Aut después de su loop | accesos |
| HU-040 | EP-12 | F4 | 032, 033, 031, 037 | **nuevo Alertas** | nuevo `features/alertas` | ALERTA, ALERTA_TRANSICION, PARAMETRO_ORGANIZACION | — | D-12, D-19 | reporting |
| HU-041 | EP-12 | F4 | 040 | Alertas | `alertas` | ALERTA_TRANSICION | — | — | reporting |
| HU-042 | EP-13 | F4 | 033, F2, 043 | **nuevo Notificaciones** + Infrastructure (outbox) | — | OUTBOX, NOTIFICACION, NOTIFICACION_DESTINATARIO | SMTP (D-12) | `UnidadDeTrabajo` (outbox transaccional) | integraciones |
| HU-043 | EP-13 | F4 | 042 | Notificaciones | nuevo `features/plantillas` | PLANTILLA_NOTIFICACION | SMTP | — | integraciones |
| HU-044 | EP-14 | F4 | 033, 034, 037, 040 | **nuevo Reportes** | nuevo `features/dashboard` | (consultas) | — | — | reporting |
| HU-045 | EP-14 | F4 | 038, 044, 046 | Reportes | `reportes` | (consultas) | librerías Excel/PDF (ADR-016) | — | reporting |
| HU-046 | EP-15 | F4 | EP-02, 032–035, 045 | Aud | nuevo `features/auditoria` | AUDITORIA (índices) | — | — | reporting |
| HU-047 | EP-16 | F5 | 005, 007, 008, 011, 019, 023 | **nuevo Vehiculos** | nuevo `features/vehiculos` | VEHICULO, PERSONA_VEHICULO, AUTORIZACION_VEHICULO | — | D-17; toca Aut | opcionales |
| HU-048 | EP-16 | F5 | 047, 032–034, 018, 036 | Vehiculos + Acc | `recepcion` | MOVIMIENTO_VEHICULO | — | D-10; toca Acc | opcionales |
| HU-049 | EP-17 | F5 | 005, 007, 008, 011, 019, 023 | **nuevo Dispositivos** | nuevo `features/dispositivos` | TIPO_DISPOSITIVO, DISPOSITIVO, AUTORIZACION_DISPOSITIVO | — | D-17; toca Aut | opcionales |
| HU-050 | EP-17 | F5 | 049, 032–034, 037, 018 | Dispositivos + Acc | `recepcion`, `presencia` | MOVIMIENTO_DISPOSITIVO | — | D-10; toca Acc | opcionales |
| HU-051 | EP-18 | F6 | 010, 012, 011, 017, 020 | Org (plantillas) | `organizaciones` | PLANTILLA_ROL*, semillas | — | D-01 | sectorial |
| HU-052 | EP-18 | F6 | 021, 024, 027, 032 | Per, Aut | varias | por definir | archivos | **Bloqueada D-11** | sectorial |
| HU-053 | EP-19 | F7 | 003, 021 | — (operación) | — | respaldos | almacenamiento | D-13, D-14 | quality |
| HU-054 | EP-19 | F7 | 019, 031–034, 037, 004 | — (pruebas) | — | — | herramienta de carga | D-15 | quality |
| HU-055 | EP-19 | F7 | 005–008, 017, 018, 021, 030, 045 | transversal | transversal | — | — | toca archivos de todos | quality |
| HU-056 | EP-19 | F7 | 053–055, MVP | — | — | — | — | D-02, D-15 | quality (último) |

### 2.1 Puntos de conflicto compartidos (todos los loops)

| Archivo / recurso | Por qué conflicta | Regla |
|---|---|---|
| `backend/src/ConecsIA.Migrations/Migraciones/*` y `ConecsIADbContextModelSnapshot.cs` | Hay un solo ensamblado de migraciones con un único snapshot (ADR-021). Dos migraciones generadas en paralelo **chocan** en el snapshot. | **Token de migración:** solo un loop a la vez genera una migración. Antes de generarla se hace rebase sobre `main`; la migración se fusiona en un PR propio y pequeño, y después pasa el turno. |
| `backend/src/ConecsIA.Api/Composicion/CatalogoModulos.cs`, `ConecsIA.Migrations/EnsambladosModulos.cs`, `backend/ConecsIA.slnx` | Hay que registrarlos al crear un módulo nuevo. | Cambio de una línea y en un commit aislado. Lo hace el loop dueño del módulo. |
| `backend/src/ConecsIA.SharedKernel/Seguridad/Permisos.cs` | Catálogo de permisos. | Pertenece a `loop_core`. Los demás añaden constantes al final, en un commit aislado. |
| `frontend/src/app/rutas.tsx`, `app/layout/AppShell.tsx`, `shared/i18n/es.ts` | Navegación y textos. | Solo se añaden entradas, en un commit aislado por loop. Se rebasa antes de abrir el PR. |
| `frontend/src/shared/api/schema.d.ts`, `backend/openapi/ConecsIA.Api.json` | Se generan a partir de la API. | No se editan a mano: se regeneran tras el rebase, justo antes del PR. |
| `docker-compose.yml`, `.env.example` | Servicios locales. | Pertenecen a `loop_integraciones` (almacenamiento) y `loop_quality`. |
| `wiki/log.md`, `wiki/tablero-scrum.md`, `wiki/implementacion/Estado de implementación.md`, `wiki/index.md` | Cada iteración los actualiza. | Solo se añade contenido. Si hay conflicto al rebasar, se conservan ambas entradas. |

### 2.2 Historias excluidas de los loops y motivo

| HU | Motivo |
|---|---|
| HU-006 (casillas restantes) | Pertenecen a `goal_authentication.md`. Si se difieren, el restablecimiento pasa a `loop_integraciones` y MFA, límite por IP y ASVS a `loop_quality`. |
| HU-052 | Bloqueada por D-11 (spike). Se incluye en `loop_sectorial` solo como tarea detenida. |
| HU-035 (regularización) | La parte de regularización está bloqueada por D-08. Solo se implementa la detección. |
| HU-047 a HU-050 | Pueden quedar fuera del MVP según D-02. El loop existe, pero su inicio requiere confirmar D-02. |
| HU-056 | Es un piloto con usuarios reales, no código. El loop solo prepara los materiales y la evidencia. |

### 2.3 Por qué la autenticación pendiente no bloquea las historias técnicas

- Ninguna historia depende de MFA ni del límite por IP.
- El restablecimiento y la invitación comparten el canal de correo (D-12). Las dos esperan a `loop_integraciones`.
- HU-007 (multi-membresía) ya tiene la decisión tomada en D-03b y se implementa en `loop_core`.

## 3. Loops

| Loop | Contexto | Historias | Backend propio | Frontend propio |
|---|---|---|---|---|
| [`loop_core.md`](loop_core.md) | Tenancy, organizaciones, estructura física, usuarios, roles y alcance | HU-005, 007, 008, 009, 010, 011, 012, 013 (cierre), 014, 015, 016, 017, 018 | `Modules.Organizaciones`, `Modules.Identidad`, `Modules.Estructura`, `Modules.Auditoria` (escritura), `Api/Seguridad`, `Infrastructure/Persistencia`, `SharedKernel/Seguridad` | `features/organizaciones`, `sedes`, `areas`, `zonas`, `usuarios`, `roles`, `sesion` (selector), `shared/i18n` (vocabulario) |
| [`loop_personas.md`](loop_personas.md) | Registro reutilizable de personas | HU-019, 020, 022 | `Modules.Personas` | `features/personas` |
| [`loop_autorizaciones.md`](loop_autorizaciones.md) | Flujo central: autorización, vigencia, visitas y QR | HU-023 a HU-031 | `Modules.Autorizaciones` | `features/autorizaciones`, `visitas`, `recepcion/lectura-qr` |
| [`loop_accesos.md`](loop_accesos.md) | Operación: entradas, salidas, presencia e historial | HU-032 a HU-039 | `Modules.Accesos` (+ hub SignalR) | `features/recepcion`, `presencia`, bandeja en `autorizaciones` (HU-039) |
| [`loop_integraciones.md`](loop_integraciones.md) | Outbox, correo, plantillas, archivos privados; entrega el envío de HU-016/029 y el restablecimiento | HU-042, 043, 021 (+ envío de 016/029 y restablecimiento de 006, si se difiere) | nuevos `Modules.Notificaciones` y `Modules.Archivos`; outbox en `Infrastructure` | `features/plantillas`, foto en `personas` (componente aislado) |
| [`loop_reporting.md`](loop_reporting.md) | Alertas, dashboard, reportes y auditoría consultable | HU-040, 041, 044, 045, 046 | nuevos `Modules.Alertas` y `Modules.Reportes`; lectura en `Modules.Auditoria` | `features/alertas`, `dashboard`, `reportes`, `auditoria` |
| [`loop_opcionales.md`](loop_opcionales.md) | Vehículos y equipos externos | HU-047 a HU-050 | nuevos `Modules.Vehiculos` y `Modules.Dispositivos` | `features/vehiculos`, `dispositivos` |
| [`loop_sectorial.md`](loop_sectorial.md) | Plantillas por vertical | HU-051 (HU-052 detenida) | semillas y plantillas en `Modules.Organizaciones` | ajustes en `organizaciones` |
| [`loop_quality.md`](loop_quality.md) | Cierre de EP-01, CI, carga, respaldo, endurecimiento y piloto | HU-001–004 (cierre DoD), 053, 054, 055, 056 | pruebas, CI, `Observabilidad`, configuración | pruebas E2E |

**¿Por qué no hay un `loop_frontend` independiente?** La DoD exige que cada historia entregue su interfaz integrada. Un loop solo de frontend tendría que esperar a los contratos de cada vertical y tocaría los mismos `features/`, así que cada loop es dueño de su backend y de su frontend. La única pantalla transversal grande, la de recepción (HU-036), queda en `loop_accesos`, que es el dueño de `features/recepcion`. `loop_autorizaciones` solo aporta el componente de lectura de QR (HU-031), en una subcarpeta.

**¿Por qué personas va aparte de core?** `Modules.Personas` es un módulo propio, con un bloqueo distinto (D-09) y sin archivos en común con core. Puede empezar cuando core fusione HU-010 y HU-018, sin esperar a que core termine HU-012 o HU-016.

## 4. Dependencias entre loops y orden recomendado

```
goal_authentication (cerrar o diferir)
        │
        ▼
   loop_core ──M1──► loop_personas ──M3──► loop_autorizaciones ──M4──► loop_accesos ──M5──► loop_reporting
      │  (HU-010,017,018)        │                                     │                   │
      │                          └──► loop_integraciones (HU-021 tras M3; HU-042/043 tras ADR-012)
      │                                                                └──► loop_opcionales (tras M5 y D-02)
      └──M2──► loop_sectorial (tras M2 + HU-020 de personas)
loop_quality: cierre de EP-01 en paralelo desde el inicio; HU-053..056 al final.
```

### Hitos de integración en `main`

| Hito | Contenido fusionado en `main` | Desbloquea |
|---|---|---|
| **M0** | Goal de autenticación cerrado o diferido formalmente; `main` publicado y CI en verde | `loop_core`, `loop_quality` (EP-01) |
| **M1** | core: HU-005, 007, 008 cerradas + HU-010 + HU-017 + HU-018 | `loop_personas` |
| **M2** | core completo (HU-009–016) | `loop_sectorial` (con HU-020); invitación de HU-016 lista para recibir el envío |
| **M3** | personas: HU-019, 020, 022 | `loop_autorizaciones`; HU-021 en `loop_integraciones` |
| **M4** | autorizaciones: HU-023–031 (las partes bloqueadas, aisladas) | `loop_accesos` |
| **M5** | accesos: HU-032–034, 036–039 | `loop_reporting`, `loop_opcionales`, HU-054 |
| **M6** | integraciones: outbox + correo | envío de HU-016, HU-029, restablecimiento y notificaciones de alertas |
| **M7** | reporting completo | HU-035 (alerta de persona sin salida, HU-040; auditoría, HU-046), HU-055 |
| **M8** | quality: HU-053–055 | HU-056 (piloto) |

### Qué puede correr en paralelo

| En paralelo | Condición |
|---|---|
| `loop_core` ‖ `loop_quality` (solo cierre de EP-01) | quality no toca módulos de dominio, solo CI, pruebas y observabilidad. |
| `loop_personas` ‖ resto de `loop_core` (HU-009, 011, 012, 014–016) | Respetando el token de migración. |
| `loop_integraciones` (outbox/correo) ‖ `loop_autorizaciones` o `loop_accesos` | Módulos nuevos y distintos. El outbox toca `Infrastructure/Persistencia/UnidadDeTrabajo.cs`: ese cambio se fusiona primero y en solitario. |
| `loop_sectorial` ‖ `loop_autorizaciones` | Sectorial solo toca plantillas y semillas de `Organizaciones`. |
| `loop_reporting` ‖ `loop_opcionales` | Módulos distintos. Opcionales toca `Accesos` (movimientos) y reporting solo lo lee. |

**Deben esperar a una fusión en `main`:** personas espera M1; autorizaciones espera M3; accesos espera M4; reporting y opcionales esperan M5; HU-035 completa espera M7; HU-056 espera M8.

**Nunca en paralelo:** dos loops que escriban en el mismo módulo, por ejemplo `loop_accesos` y `loop_opcionales` sobre `Modules.Accesos`. Las partes de opcionales que tocan `Accesos` (HU-048 y HU-050) empiezan cuando `loop_accesos` está cerrado.

## 5. Worktrees y ramas

| Loop | Worktree recomendado | Rama |
|---|---|---|
| core | `../ConecsIA-wt/core` | `loop/core` |
| personas | `../ConecsIA-wt/personas` | `loop/personas` |
| autorizaciones | `../ConecsIA-wt/autorizaciones` | `loop/autorizaciones` |
| accesos | `../ConecsIA-wt/accesos` | `loop/accesos` |
| integraciones | `../ConecsIA-wt/integraciones` | `loop/integraciones` |
| reporting | `../ConecsIA-wt/reporting` | `loop/reporting` |
| opcionales | `../ConecsIA-wt/opcionales` | `loop/opcionales` |
| sectorial | `../ConecsIA-wt/sectorial` | `loop/sectorial` |
| quality | `../ConecsIA-wt/quality` | `loop/quality` |

```bash
git worktree add ../ConecsIA-wt/core -b loop/core main
```

- Cada historia se integra en `main` con un PR propio desde la rama del loop. No hay que esperar al final del loop.
- **PostgreSQL por worktree:** en las pruebas de integración, cada worktree usa su propia base de datos (Testcontainers o un nombre de BD distinto) para no pisarse.
- Docker Compose local: un solo stack a la vez, o puertos distintos por worktree.

## 6. Condiciones comunes

**De inicio (todas):** M0 alcanzado; dependencias del loop fusionadas en `main`; ADR del loop aprobados, o la parte que dependa de ellos aislada y anotada; rama creada desde un `main` actualizado.

**De bloqueo (todas):** la tarea depende de una D-xx abierta que cambia el comportamiento, o de un ADR en Propuesta que cambia el modelo. También bloquea que otro loop tenga el token de migración y la tarea lo necesite, o que el CI de `main` esté en rojo. En cualquiera de esos casos se registra en la wiki, se marca la casilla con ⏸ y se pasa a la siguiente tarea independiente.

## 7. Orden de cierre de fases

| Fase | Se cierra cuando… | Loops |
|---|---|---|
| F0 | M0 + M1 (HU-005, 007, 008) + EP-01 cerrada por quality | goal_authentication, core, quality |
| F1 | M2 + M3 + HU-021 | core, personas, integraciones |
| F2 | M4 + envío de HU-029 | autorizaciones, integraciones |
| F3 | M5 + HU-035 (detección) | accesos |
| F4 (fin del MVP) | M6 + M7 | integraciones, reporting |
| F5 | loop_opcionales cerrado (si D-02 lo incluye) | opcionales |
| F6 | HU-051 (+ HU-052 si se resuelve D-11) | sectorial |
| F7 | M8 + HU-056 | quality |

## 8. Decisiones que condicionan los loops

| Decisión | Loop afectado | Efecto |
|---|---|---|
| Cierre o diferimiento del goal (MFA, D-21, reset, ASVS) | todos | M0 |
| D-20 idioma de rutas | todos desde personas | Debe resolverse **antes de M1**. Si se cambia después, hay que renombrar las rutas ya implementadas. |
| D-01, D-16 | core | Vocabulario e inactivación de organización. |
| D-04 | core (HU-014), autorizaciones | Quién autoriza y decide. |
| D-09 | personas | Clave única de persona. |
| D-05, D-06, D-18 | autorizaciones | Modalidades, calendario y permanente. |
| D-07, D-08 | accesos | Presencia entre sedes y regularización. |
| D-12, D-19 | integraciones, reporting | Correo y umbrales. |
| D-13, D-14 | integraciones, quality | Archivos y retención. |
| D-02, D-10, D-17 | opcionales | Alcance, custodia y unicidad. |
| D-11 | sectorial | HU-052. |
| D-15 | accesos (HU-037), quality | Frescura y carga. |
| ADR en Propuesta: 007, 008, 009, 010, 011, 012, 013, 015, 016, 019, 021 | según el loop | Aprobarlos antes de la historia que los implementa. |
