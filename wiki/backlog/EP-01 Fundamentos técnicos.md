---
tipo: epica
estado: propuesta
fase: F0
requisitos: [RNF-mantenibilidad, RNF-observabilidad]
adr: [ADR-001, ADR-002, ADR-003, ADR-015, ADR-017, ADR-018, ADR-020]
decisiones: [D-13, D-14]
actualizado: 2026-10-17
tags: [conecsia, backlog, epica]
---

# EP-01 Fundamentos técnicos

> [!note] Fase: [[PLAN-DE-TRABAJO#F0]] · Requisitos: no funcionales (PRD §12) · ADR: [[ADR-001 Monolito modular por capacidades]], [[ADR-002 ASP.NET Core Web API en CSharp]], [[ADR-003 EF Core con Npgsql y migraciones versionadas]], [[ADR-015 Frontend SPA con React, TypeScript y Tailwind]], [[ADR-017 Entorno local y despliegue]], [[ADR-018 Observabilidad sin datos sensibles]], [[ADR-020 Contratos API y formato de errores]] — todos en estado **Propuesta**.

> [!warning] Todos los ADR de esta épica están en **Propuesta**. Las historias pueden refinarse, pero no deben cerrarse como "implementadas" sin aprobar el ADR correspondiente (registrar en [[Registro de ADR]]).

## Objetivo de la épica
Disponer de una base técnica reproducible (backend modular, frontend, entorno local, CI, errores y observabilidad) sobre la cual construir las capacidades de negocio sin deuda estructural. **Base:** stack .NET + React/TS + Tailwind + PostgreSQL confirmado. **Propuesta:** arquitectura modular y herramientas concretas.

## Alcance / fuera de alcance
- **Dentro:** solución .NET por módulos, pruebas automatizadas con PostgreSQL real, pipeline CI, SPA base, Docker Compose, semillas sintéticas, Problem Details, logs/trazas con correlación.
- **Fuera:** infraestructura productiva y proveedor cloud (D-13), respaldos (EP-19), autenticación real (HU-006), cualquier regla de negocio.

## Historias
| ID | Historia | Tamaño | Estado | Bloqueada por |
|---|---|---|---|---|
| HU-001 | Solución backend modular y pipeline CI | L | Lista para refinar | ADR-001/002/003 por aprobar |
| HU-002 | Proyecto frontend base | M | Lista para refinar | ADR-015 por aprobar |
| HU-003 | Entorno local con Docker Compose y semillas sintéticas | M | Lista para refinar | ADR-017 por aprobar |
| HU-004 | Observabilidad y formato de errores base | M | Lista para refinar | ADR-018/020 por aprobar |

---

## HU-001 Solución backend modular y pipeline CI

**Como** equipo de desarrollo **quiero** una solución backend organizada por módulos con capas y un pipeline de integración continua **para** construir capacidades aisladas y verificadas desde el primer commit.

| Campo | Valor |
|---|---|
| Épica / Fase | EP-01 / F0 |
| Requisitos | PRD §11 (arquitectura propuesta), §12.7 mantenibilidad |
| Tamaño relativo | L (propuesto, sin estimación aprobada) |
| Estado | Lista para refinar |

### Objetivo
Crear el esqueleto del monolito modular (Dominio / Aplicación / Infraestructura / API) con migraciones EF Core y CI que compile, pruebe y valide reglas de dependencia.

### Actores
Equipo de desarrollo; pipeline CI.

### Dependencias
- Historias: ninguna (raíz de la ruta crítica).
- ADR: [[ADR-001 Monolito modular por capacidades]] (Propuesta), [[ADR-002 ASP.NET Core Web API en CSharp]] (Propuesta), [[ADR-003 EF Core con Npgsql y migraciones versionadas]] (Propuesta).
- Decisiones: ninguna de negocio; versión LTS de .NET a verificar en F0.

### Contrato funcional
- **Entradas:** código fuente; variables de entorno de conexión.
- **Reglas (servidor):** un módulo no referencia la infraestructura de otro; los casos de uso dependen de interfaces (`IClock`, `IFileStorage`, `IEmailSender`); convención `snake_case`; migraciones versionadas, **no** aplicadas automáticamente al arrancar en producción.
- **Efectos persistidos:** migración inicial vacía/tabla de historial de migraciones.
- **Errores:** fallo de build, prueba o regla de arquitectura ⇒ pipeline rojo.
- **Auditoría:** no aplica (infraestructura de auditoría en HU-008).
- **Endpoints indicativos:** `GET /api/v1/health` (liveness), `GET /api/v1/health/ready` (incluye PostgreSQL).

### Criterios de aceptación
- [ ] **Dado** un clon limpio **cuando** se ejecuta el build y las pruebas **entonces** compila y las pruebas de integración corren contra PostgreSQL real (Testcontainers o equivalente).
- [ ] **Dado** un módulo que referencia la infraestructura de otro **cuando** corre el pipeline **entonces** la prueba de arquitectura falla.
- [ ] **Dado** una migración nueva **cuando** se genera el script idempotente **entonces** se aplica sin errores sobre una base vacía y sobre la versión anterior.

### Tareas backend
- [ ] Crear solución y proyectos por módulo (SaaS, Identidad, Estructura, Personas, Autorizaciones, Accesos, Auditoría, Compartido).
- [ ] Configurar EF Core + Npgsql, `snake_case`, `DbContext` por módulo o compartido (decidir en refinamiento).
- [ ] Endpoints de salud; abstracción `IClock`.
- [ ] Pruebas de arquitectura (NetArchTest/ArchUnitNET).
- [ ] Pipeline CI: restore, build, análisis estático, pruebas unitarias e integración, generación de script de migración.

### Tareas frontend
- No aplica (HU-002).

### Pruebas
- [ ] Unitarias: ejemplo de dominio y reglas de arquitectura.
- [ ] Integración (PostgreSQL real): migraciones y `health/ready`.
- [ ] Aislamiento / permisos negativos: no aplica todavía; se deja el arnés de pruebas multi-organización preparado para HU-005.
- [ ] E2E / UI: no aplica.

### Definition of Done
Cumple la [[PLAN-DE-TRABAJO#Definition of Done general|DoD general]] y además:
- [ ] Pipeline verde en rama principal y obligatorio para merge.
- [ ] ADR-001/002/003 aprobados o con desviaciones registradas.

### Riesgos y decisiones pendientes
> [!todo] Riesgo — granularidad excesiva de módulos en F0; mantener el mínimo y crecer por capacidad.

---

## HU-002 Proyecto frontend base

**Como** equipo de desarrollo **quiero** una SPA React + TypeScript + Tailwind con cliente API tipado **para** construir pantallas consistentes y responsivas que no dupliquen lógica de negocio.

| Campo | Valor |
|---|---|
| Épica / Fase | EP-01 / F0 |
| Requisitos | PRD §9 (experiencia), CA-21 (responsivo, preparación) |
| Tamaño relativo | M (propuesto) |
| Estado | Lista para refinar |

### Objetivo
Proyecto frontend en modo estricto, organizado por funcionalidades, con layout responsivo, manejo uniforme de Problem Details y cliente generado desde OpenAPI.

### Actores
Equipo de desarrollo; usuarios finales (indirecto).

### Dependencias
- Historias: HU-001 (OpenAPI disponible), HU-004 (formato de errores).
- ADR: [[ADR-015 Frontend SPA con React, TypeScript y Tailwind]] (Propuesta), [[ADR-020 Contratos API y formato de errores]] (Propuesta).
- Decisiones: D-01 (terminología visible) — se usan claves de texto, no literales sectoriales.

### Contrato funcional
- **Entradas:** especificación OpenAPI del backend.
- **Reglas:** el frontend **no** decide permisos, vigencias ni cupos; oculta controles solo como ayuda de UX, la validez la determina el servidor. Textos vía claves i18n para soportar vocabulario configurable (HU-012).
- **Efectos persistidos:** ninguno en servidor; preferencias locales no sensibles.
- **Errores:** muestra Problem Details (`title`, `detail`, errores por campo, `traceId`) sin exponer trazas internas.
- **Auditoría:** no aplica.
- **Endpoints:** consume `GET /api/v1/health`.

### Criterios de aceptación
- [ ] **Dado** el proyecto **cuando** se ejecuta lint, type-check y pruebas en CI **entonces** pasan en modo estricto.
- [ ] **Dado** una respuesta 400 con errores por campo **cuando** se muestra un formulario **entonces** cada error aparece junto a su campo.
- [ ] **Dado** un viewport de móvil, tablet y escritorio **cuando** se abre el layout base **entonces** no hay desbordamiento horizontal (preparación de CA-21).

### Tareas backend
- [ ] Publicar OpenAPI estable en build (soporte a generación del cliente).

### Tareas frontend
- [ ] Inicializar proyecto (Vite propuesto), TS estricto, Tailwind, ESLint/Prettier.
- [ ] Estructura por funcionalidades; enrutador; layout responsivo; TanStack Query (propuesto).
- [ ] Generación de cliente tipado desde OpenAPI en CI.
- [ ] Componente de errores Problem Details e i18n con claves.

### Pruebas
- [ ] Unitarias: componentes base y mapeo de errores.
- [ ] Integración: cliente generado contra API local (`health`).
- [ ] Aislamiento / permisos negativos: no aplica.
- [ ] E2E / UI: humo (Playwright propuesto) en tres tamaños de viewport.

### Definition of Done
Cumple la [[PLAN-DE-TRABAJO#Definition of Done general|DoD general]] y además:
- [ ] Cliente API regenerable sin cambios manuales; build de contenedor reproducible.

### Riesgos y decisiones pendientes
> [!todo] D-01 — terminología definitiva; mientras tanto, solo claves de texto.

---

## HU-003 Entorno local con Docker Compose y semillas sintéticas

**Como** desarrollador **quiero** levantar todo el entorno local con un comando y datos sintéticos de varias organizaciones **para** desarrollar y probar aislamiento sin datos personales reales.

| Campo | Valor |
|---|---|
| Épica / Fase | EP-01 / F0 |
| Requisitos | PRD §12.5 (sin datos reales), RN-01 (pruebas multi-tenant) |
| Tamaño relativo | M (propuesto) |
| Estado | Lista para refinar |

### Objetivo
Docker Compose con PostgreSQL (volumen persistente), captura de correo y almacenamiento de objetos emulado; semillas sintéticas con al menos dos organizaciones.

### Actores
Desarrollador; QA.

### Dependencias
- Historias: HU-001, HU-002.
- ADR: [[ADR-017 Entorno local y despliegue]] (Propuesta), [[ADR-013 Almacenamiento privado de archivos]] (Propuesta, solo emulador).
- Decisiones: D-13 (infraestructura definitiva), D-14 (prohíbe datos reales hasta definir privacidad).

### Contrato funcional
- **Entradas:** `docker compose up`; variables de entorno / user-secrets.
- **Reglas:** secretos fuera del repositorio; semillas **solo** sintéticas y marcadas como tales; semillas idempotentes (re-ejecutar no duplica); comando de semillas deshabilitado fuera de entorno `Development`.
- **Efectos persistidos:** organizaciones "Org A" y "Org B" de prueba, sedes, usuarios de prueba (cuando exista HU-006/HU-016).
- **Errores:** ejecución en entorno no local ⇒ el comando se rechaza.
- **Auditoría:** acciones de semilla registradas con actor de sistema `seed` (cuando exista HU-008).
- **Endpoints:** no aplica.

### Criterios de aceptación
- [ ] **Dado** una máquina con Docker **cuando** ejecuto el compose **entonces** API, SPA, PostgreSQL, captura de correo y almacenamiento emulado quedan disponibles.
- [ ] **Dado** semillas ya cargadas **cuando** se ejecutan de nuevo **entonces** no se duplican registros.
- [ ] **Dado** el entorno `Production` **cuando** se invoca el sembrado **entonces** se rechaza.

### Tareas backend
- [ ] `docker-compose.yml`, Dockerfile de API, configuración por variables de entorno.
- [ ] Comando de semillas idempotente con dos organizaciones sintéticas.

### Tareas frontend
- [ ] Dockerfile de SPA y configuración de URL de API por entorno.

### Pruebas
- [ ] Unitarias: generador de datos sintéticos.
- [ ] Integración (PostgreSQL real): semillas idempotentes.
- [ ] Aislamiento / permisos negativos: las semillas dejan datos en Org A y Org B para pruebas cruzadas.
- [ ] E2E / UI: humo sobre el entorno compuesto.

### Definition of Done
Cumple la [[PLAN-DE-TRABAJO#Definition of Done general|DoD general]] y además:
- [ ] README técnico de arranque (documentación técnica, no de negocio).
- [ ] Ningún secreto en el repositorio (verificado por escáner en CI).

### Riesgos y decisiones pendientes
> [!todo] D-13 — proveedor de almacenamiento y correo reales. D-14 — no usar datos reales hasta su definición.

---

## HU-004 Observabilidad y formato de errores base

**Como** equipo de operación **quiero** logs estructurados con correlación, métricas/trazas y errores uniformes **para** diagnosticar fallos sin exponer datos sensibles.

| Campo | Valor |
|---|---|
| Épica / Fase | EP-01 / F0 |
| Requisitos | PRD §12.5 (sin secretos en logs), §12.7 observabilidad |
| Tamaño relativo | M (propuesto) |
| Estado | Lista para refinar |

### Objetivo
Middleware de errores Problem Details (RFC 9457), identificador de correlación, OpenTelemetry y lista de exclusión/enmascarado de datos sensibles.

### Actores
Equipo de operación; desarrolladores; clientes API.

### Dependencias
- Historias: HU-001.
- ADR: [[ADR-018 Observabilidad sin datos sensibles]] (Propuesta), [[ADR-020 Contratos API y formato de errores]] (Propuesta).
- Decisiones: exportador de telemetría a definir con D-13.

### Contrato funcional
- **Entradas:** cualquier petición HTTP; header de correlación opcional.
- **Reglas:** 400 validación, 401/403 autenticación/permiso, **404 para recursos de otra organización** (no revelar existencia), 409 conflicto/concurrencia, 422 regla de negocio; denegaciones de negocio con código estable (p. ej. `AUTORIZACION_NO_VIGENTE`); excepciones no controladas ⇒ 500 genérico con `traceId`.
- **Efectos persistidos:** ninguno.
- **Errores:** nunca se incluyen stack traces, SQL ni datos personales en la respuesta.
- **Auditoría:** no aplica; los logs **no** sustituyen la auditoría (HU-008).
- **Enmascarado:** contraseñas, tokens (sesión, QR), números de documento completos, correo y teléfono.
- **Endpoints:** transversal.

### Criterios de aceptación
- [ ] **Dado** un error de validación **cuando** la API responde **entonces** devuelve Problem Details 400 con errores por campo y `traceId`.
- [ ] **Dado** una petición con contraseña o token **cuando** se registra en log **entonces** el valor aparece enmascarado.
- [ ] **Dado** una excepción no controlada **cuando** ocurre **entonces** la respuesta es 500 sin detalles internos y el log contiene el `traceId`.

### Tareas backend
- [ ] Middleware de excepciones y mapeo a Problem Details; catálogo de códigos de motivo.
- [ ] Logging estructurado con correlación; OpenTelemetry (exportador configurable).
- [ ] Destructurador/enmascarador de campos sensibles.

### Tareas frontend
- [ ] Mostrar `traceId` en errores 500 para soporte.

### Pruebas
- [ ] Unitarias: mapeo excepción → código HTTP; enmascarado.
- [ ] Integración (PostgreSQL real): violación de unicidad ⇒ 409.
- [ ] Aislamiento / permisos negativos: plantilla de prueba que verifica 404 (no 403) al acceder a ID de otra organización, reutilizable desde HU-005.
- [ ] E2E / UI: error visible en formulario de ejemplo.

### Definition of Done
Cumple la [[PLAN-DE-TRABAJO#Definition of Done general|DoD general]] y además:
- [ ] Prueba automatizada que falla si un log contiene patrones de token/contraseña.

### Riesgos y decisiones pendientes
> [!todo] D-13 — destino de telemetría. Riesgo: el código 422 vs 409 para reglas de negocio debe fijarse en ADR-020 antes de generar el cliente.
