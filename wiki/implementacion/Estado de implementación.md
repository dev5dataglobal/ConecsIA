---
tipo: implementacion
estado: implementado
actualizado: 2026-10-17
tags: [conecsia]
---

# Estado de implementación

> [!note] Al 2026-10-17 existen las **fundaciones técnicas (F0, EP-01)** y la **primera vertical funcional: gestión de sedes** (HU-013 con la base de HU-005, HU-007 y HU-008). La autenticación es provisional ([[ADR-022 Autenticación de desarrollo provisional]]). La carpeta aún no es un repositorio git ni está en GitHub.

## Módulos
| Módulo | Estado | Archivos | Pruebas | CA verificados |
|---|---|---|---|---|
| [[Plataforma base]] (HU-001 a HU-004) | **Implementado y verificado localmente** (falta CI en GitHub) | `backend/`, `frontend/`, `database/`, `docker-compose.yml`, `.github/workflows/ci.yml` | Ver abajo | Ninguno del PRD; F0 no tiene CA de negocio |
| [[Sedes]] (HU-013; base de HU-005, 007, 008) | **Implementado y verificado localmente** (falta CI, revisión de código y del PO) | Estructura, Identidad, Organizaciones, Auditoria; `ConecsIA.Api/Seguridad`; migración `PrimeraVerticalSedes`; `frontend/src/features/{sedes,sesion}` | Ver abajo | CA-01, CA-02 verificados; CA-03 parcial |
| Personas, Autorizaciones, Accesos | Solo frontera de módulo (`IModulo`) | `backend/src/Modules/*` | Arquitectura | — |

## Verificación del 2026-10-17

| Verificación | Resultado |
|---|---|
| `dotnet build` (.NET 10.0.402, advertencias como errores) | **Correcto** |
| `dotnet format --verify-no-changes` | **Correcto** |
| Pruebas de arquitectura | **51/51 correctas**. Prueba de mutación: tres violaciones introducidas a propósito (referencia entre módulos, dominio con EF Core, `DateTime.UtcNow` en un módulo) fueron detectadas y luego revertidas |
| Pruebas unitarias backend | **25/25 correctas** |
| Pruebas de integración (Testcontainers + PostgreSQL 18) | **13/13 correctas**: salud, errores 400/404/422/500 con traceId, migraciones aplicadas, script idempotente aplicado dos veces, rol app sin DDL y con DML |
| `docker compose up --build` | **Correcto**: postgres (puerto de host 5434 en esta máquina), mailpit, api y web en ejecución. API y SPA (vía proxy) responden `ready` = Healthy, rutas SPA 200, Mailpit 200. Migración aplicada por `conecsia_migrator`; tabla de historial propiedad de ese rol |
| Migración `Inicial` | Generada (vacía) y aplicada en el contenedor y en las pruebas |
| Arranque manual de la API sin base de datos | `health` = Healthy; `ready` = Unhealthy (esperado sin PostgreSQL); `sistema/version` lista 7 módulos; cabecera `X-Trace-Id` presente; logs JSON redactados |
| Frontend: lint, formato, tipos | **Correcto** |
| Frontend: pruebas (Vitest) | **14/14 correctas** |
| Frontend: build de producción | **Correcto** |
| Tipos generados desde OpenAPI | Generados (`src/shared/api/schema.d.ts`) |

## Verificación de la primera vertical (2026-10-17)

TDD: las pruebas se escribieron primero y fallaron (backend sin compilar por tipos inexistentes; frontend con el módulo `sesionDesarrollo` inexistente) antes de implementar.

| Verificación | Resultado |
|---|---|
| `dotnet build -c Release` (advertencias como errores) | **Correcto**, 0 advertencias |
| `dotnet format --verify-no-changes` | **Correcto** |
| Pruebas de arquitectura | **51/51** |
| Pruebas unitarias backend | **52/52** (dominio `Sede`, `ResolvedorContexto`, guarda de entorno de ADR-022, previas) |
| Pruebas de integración (PostgreSQL 18 real, Testcontainers) | **48/48**: 401/403/404/409/422/400, CA-01 (listar, leer, modificar, inactivar entre organizaciones), CA-02 (alcance por sede), rol llamado "Administrador" sin claves ⇒ 403, organización ajena ⇒ 403 auditado, organización del cuerpo ignorada, altas concurrentes ⇒ una sola sede, versión obsoleta ⇒ 409, auditoría con anterior/nuevo, `UPDATE`/`DELETE`/`TRUNCATE` de auditoría denegados al rol app, FK y unicidad en PostgreSQL, filtro global, escritura cruzada rechazada, convención de filtro por entidad, cuerpo malformado ⇒ 400 |
| Pruebas de mutación | Quitar el filtro global ⇒ 8 pruebas fallan; quitar la verificación de alcance ⇒ falla la prueba de CA-02. Ambas revertidas |
| Frontend: lint, formato, tipos, build | **Correcto** |
| Frontend: Vitest | **22/22** (8 nuevas: listado, identidad enviada, acciones ocultas sin permiso, alta con errores de campo del servidor, edición con versión y 409, inactivación con confirmación, 403 con traceId, sesión de desarrollo y 401) |
| `docker compose up --build` + flujo por nginx | **19/19 pasos correctos**: 401 sin identidad, 400 malformado, 201 alta, 409 duplicado normalizado, 400 zona no IANA, 404 desde otra organización (leer e inactivar), 403 organización ajena, operador solo ve su sede y recibe 403 fuera de alcance y al crear, 200/409 edición con versión, 200/422 inactivación, sede inactiva consultable, 3 eventos de auditoría con actor y organización, `DELETE` de auditoría denegado al rol app, rutas SPA 200 |
| UI en navegador (Docker) | Selector de usuario, alta, 409 visible con referencia, inactivación con confirmación, operador sin acciones y solo con su sede; móvil 375 px sin scroll horizontal de página |
| OpenAPI | Exportado (6 rutas) y tipos del frontend regenerados |

**Defectos encontrados y corregidos durante la verificación:**
- La build Release de Docker fallaba con CA1861 en la migración generada: el `.editorconfig` raíz no entra en el contexto `backend/`. Se añadió `.editorconfig` en `Migraciones/`.
- Un cuerpo JSON inválido llegaba como `null` al caso de uso ⇒ **500**. Ahora `FiltroValidacion` devuelve 400 con mensaje genérico (sin nombres de tipos internos). Prueba añadida.
- nginx reenviaba con HTTP/1.0 y Kestrel rechazaba `POST /inactivar` sin cuerpo (400 vacío). Ahora `proxy_http_version 1.1`.
- La política de autorización de respaldo convertía rutas inexistentes en 401; se reemplazó por un `AuthorizeFilter` global en MVC (las rutas desconocidas siguen en 404).
- Docker Desktop volvió a caer al reiniciarse (sockets obsoletos en `%LOCALAPPDATA%\Docker\run\`); lo reparó el usuario.

## HU-006 Autenticación con cookie — Fase 2 del goal (2026-10-19)
Implementado y verificado contra PostgreSQL real. Suite: 177/177 (56 unitarias, 59 de arquitectura, 62 de integración).

| Pieza | Archivo |
|---|---|
| Credencial, sello de seguridad, bloqueo 5 / 15 min | `Modules.Identidad/Domain/Usuario.cs`, migración `CredencialUsuario` |
| Caso de uso de inicio y cierre de sesión, auditoría `sesion.iniciar` / `sesion.bloquear` / `sesion.cerrar` | `Modules.Identidad/Application/ServicioIngreso.cs` |
| `POST /api/v1/auth/login` y `/logout` | `Modules.Identidad/Api/AutenticacionController.cs` |
| Hash con `PasswordHasher` de Identity | `Modules.Identidad/Api/HasherContrasenas.cs` |
| Política aprobada (valores), `ISesionUsuario`, errores 401/423 | `SharedKernel/Seguridad/Sesion.cs`, `Api/Errores/ManejadorExcepciones.cs` |
| Cookie `conecsia.sesion` (HttpOnly, Secure, SameSite=Strict; 30 min deslizante; 8 h absolutas; validación del sello en cada petición) | `Api/Seguridad/AutenticacionCookie.cs` |
| Esquema compuesto: cabecera de desarrollo solo si está habilitada; si no, la cookie | `Api/Composicion/ExtensionesServicios.cs` |
| Anti-CSRF (`X-CSRF-TOKEN`, `GET /api/v1/auth/csrf`) para escrituras con cookie de sesión | `Api/Seguridad/ProteccionCsrf.cs` |
| Pruebas | `UnitTests/Identidad/UsuarioCredencialTests.cs`, `IntegrationTests/SesionTests.cs` |

> [!warning] Limitaciones conocidas
> - ~~Cierre de sesión sin estado en el servidor~~ — **resuelto 2026-10-19 (Fase 3):** tabla `sesion_usuario`; cerrar o revocar una sesión invalida cualquier copia de la cookie. Endpoints `GET /api/v1/auth/sesiones` y `DELETE /api/v1/auth/sesiones/{id}` (solo sesiones propias; una ajena da 404). Las filas se conservan según D-14.
> - La expiración deslizante de ASP.NET renueva la cookie al pasar la mitad del plazo: el cierre efectivo por inactividad ocurre entre 15 y 30 min.
> - Los límites de 8 h y 30 min no tienen prueba automatizada (requieren reloj controlable en la cookie).
> - Un bloqueo revela que la cuenta existe (423 solo para cuentas reales). Lo mitigará el límite por IP, **detenido por D-21**.
> - Restablecimiento de contraseña detenido por D-12 (canal de correo) y MFA por la decisión de no implementarlo ahora.
> - El inicio de sesión no exige token CSRF (no hay cookie de sesión que abusar). Riesgo residual: forzar el ingreso con la cuenta de otro.
> - No existe todavía un flujo para definir la contraseña inicial (HU-016, invitación) ni semillas con contraseña: la SPA sigue con el modo de desarrollo hasta la Fase 4.
> - `openapi/ConecsIA.Api.json` y `frontend/src/shared/api/schema.d.ts` no se regeneraron.

## HU-006 Frontend — Fase 4 del goal (2026-10-20)
- **Implementado y verificado (pruebas de componente):** `IngresarPage` con formulario real; `apiSesion.ts` (ingreso y cierre); `http.ts` envía `X-CSRF-TOKEN` en las escrituras con cookie, reintenta una vez ante `CSRF_INVALIDO` y emite un evento global ante 401; `RequiereSesion` vuelve a `/ingresar` cuando lo recibe. El selector de usuarios sintéticos solo aparece si la API lo ofrece (ADR-022).
- La SPA guarda solo una marca «sesión iniciada» en `sessionStorage`; la cookie es HttpOnly y la valida el servidor.
- Pruebas: `frontend/src/features/sesion/SesionReal.test.tsx` (6). Suite: 28/28; typecheck, lint, prettier y build en verde.
- **Pendiente:** pantallas de restablecimiento (D-12), E2E con navegador real (no hay usuarios sembrados con contraseña ni Playwright) y regenerar `schema.d.ts`.

## HU-006 E2E con el stack real (2026-10-20)
- **Verificado** a mano en el navegador contra Docker Compose: error genérico, ingreso, escritura con token anti-CSRF (201), cookie inaccesible desde JS, cierre de sesión que revoca (`/me` → 401).
- **Defecto encontrado y corregido:** con `SecurePolicy=Always`, `GET /api/v1/auth/csrf` daba 500 sobre el HTTP local detrás de nginx. Ahora la política es `SameAsRequest` solo en Development y `Always` en los demás entornos (`AutenticacionCookie.PoliticaSecure`, con prueba unitaria).
- **Semillas:** `SemillasContrasenas` asigna a los usuarios sintéticos la contraseña de `SEMILLAS_CONTRASENA_USUARIOS` (.env, no versionado; mínimo 12 caracteres).

> [!warning] Hallazgos para producción (EP-19)
> - Fuera de Development el TLS termina en el proxy: la API necesita `X-Forwarded-Proto` de proxies de confianza (ligado a D-21) o la cookie Secure y el antiforgery fallarán.
> - Las claves de Data Protection no se persisten en el contenedor (aviso «No XML encryptor configured»): al reiniciar la API se invalidan todas las cookies de sesión y CSRF. Falta definir el almacenamiento de claves (ADR-013 / D-13).

## Punto de sustitución de la autenticación (inventario 2026-10-17, goal HU-006)
Inventario para reemplazar [[ADR-022 Autenticación de desarrollo provisional]] por la opción que fije D-03, sin tocar contexto, permisos ni auditoría.

| Pieza | Archivo | Al llegar HU-006 |
|---|---|---|
| Esquema `Desarrollo` (cabecera `X-Usuario-Desarrollo` → claim `NameIdentifier`) | `backend/src/ConecsIA.Api/Seguridad/AutenticacionDesarrollo.cs` | Se conserva solo para Development/Testing o se retira |
| Registro del esquema (`AddAuthentication`) y validación de entorno | `backend/src/ConecsIA.Api/Composicion/ExtensionesServicios.cs` (`AgregarSeguridad`) | **Único punto de cambio en el backend**: registrar el esquema real |
| `AuthorizeFilter` global, `UseAuthentication`/`UseAuthorization` | `ExtensionesServicios.AgregarApi`, `Program.cs` | Sin cambios |
| Contexto de organización (usuario activo → 401; sin membresía → 403) | `Seguridad/MiddlewareContextoOrganizacion.cs`, `Modules.Identidad/Application/ResolvedorContexto.cs` | Sin cambios: solo necesita el claim `NameIdentifier` con el id del usuario |
| `GET /api/v1/me` y `GET /api/v1/desarrollo/usuarios` | `Modules.Identidad/Api/IdentidadControllers.cs` | `/me` sin cambios; el listado de desarrollo queda solo con el esquema habilitado |
| SPA: usuario en `sessionStorage`, cabecera, 401 → cierre | `frontend/src/features/sesion/*`, `frontend/src/shared/api/http.ts`, `app/layout/AppShell.tsx` | Se reemplaza por inicio de sesión real (cookie o token, según D-03) |
| Cliente de pruebas `Como(usuario)` | `backend/tests/ConecsIA.IntegrationTests/Infraestructura/Escenario.cs` | Puede mantenerse con el esquema de pruebas |

**Verificado:** `ArchitectureTests/AutenticacionTests.cs` (solo la composición conoce el esquema; los módulos no dependen de `Microsoft.AspNetCore.Authentication` ni de la API) e `IntegrationTests/ContratoAutenticacionTests.cs` (sin membresía → 403; membresía o usuario inactivados → la siguiente petición se rechaza). Suite completa: 162/162 en verde.

## Deuda y limitaciones conocidas
- **Primera vertical:** autenticación provisional (ADR-022, D-03); permisos y alcance guardados en `membresia` hasta HU-017/HU-018; organizaciones y usuarios solo por semillas (HU-009, HU-016); 403 por permiso no auditados; sin E2E automatizado (Playwright); el frontend usa tipos escritos a mano para sedes en vez de `schema.d.ts`; FKs entre módulos creadas a mano en la migración (EF no las conoce); D-06 y la regla de inactivar con personas dentro siguen pendientes; el `.editorconfig` raíz no aplica a la build Docker (solo se replicó la regla de migraciones).
- **CI sin ejecutar:** repositorio git inicializado el 2026-10-17 (sin commit inicial), aún no está en GitHub. El workflow existe pero no se ha corrido.
- Defecto corregido en integración: el filtro de validación perdía `errors` en el 400; ahora usa la `ProblemDetailsFactory` de MVC.
- En esta máquina el puerto 5432 está ocupado; el compose admite `POSTGRES_HOST_PORT` en `.env`.
- [[ADR-021 DbContext compartido y ensamblado de migraciones]] está implementado y **aprobado 2026-10-19**; la condición 1 se verifica con `PersistenciaPorModuloTests` desde HU-005 (2026-10-19, `loop/core`).
- El enmascarado de logs también oculta las IPv4: es preferible a filtrar documentos con puntos.
- Falta E2E (Playwright), almacenamiento de objetos emulado (ADR-013 en Propuesta) y semillas con datos (depende de HU-009).
- El contrato OpenAPI se exporta a mano desde la API en ejecución; falta automatizarlo en CI.
- `openapi-typescript` declara compatibilidad con TypeScript 5; se ejecuta con `npx` fuera de las dependencias del proyecto, que usa TypeScript 6.

## HU-005 cerrada en `loop/core` (2026-10-19)
- **Implementado:** auditoría `Denegado` de las escrituras entre organizaciones (`AuditoriaEscrituraEntreOrganizaciones`, invocada por `ManejadorExcepciones`; responde 404 sin datos ajenos). `EscrituraEntreOrganizacionesException.Entidad` expone solo el tipo.
- **Pruebas:** `PersistenciaPorModuloTests` (regla de ADR-021 condición 1, con prueba de control), `SuiteAislamientoTests` (suite parametrizada Org A/B reutilizable; registrar cada recurso nuevo), convenciones de índices únicos y FK con organización, y escritura cruzada auditada. `dotnet test`: 196/196.
- **Limitaciones:** aún no hay FK compuestas porque no existen entidades hijas (la convención fallará cuando aparezcan). El spike RLS es documental (ADR-004) y la adopción queda en D-22. Sin migración y sin cambios de frontend.

## HU-007 cerrada en `loop/core` (2026-10-19)
- **Backend:** `GET /api/v1/me/membresias` con `[SinOrganizacionResuelta]` (solo exige usuario activo). Denegaciones de permiso o alcance auditadas como `seguridad.acceso_denegado` con código, método y plantilla de ruta, sin IDs (`AuditoriaDenegaciones`, que reemplaza a `AuditoriaEscrituraEntreOrganizaciones`). `ContextoSolicitud.ComoProceso(organizacion, permisos)` para procesos de fondo.
- **Frontend:** con varias membresías se muestra `ElegirOrganizacion`; la selección viaja en `X-Organizacion-Id` (sessionStorage) y se cambia desde la cabecera. Una selección rechazada (`ORGANIZACION_NO_PERMITIDA`) se olvida.
- **Limitaciones:** no hay caché de permisos (se leen en cada petición; rendimiento por evaluar en D-15). No hay selector de sede. El OpenAPI se regeneró con la API local en Development.

## HU-008 cerrada en `loop/core` (2026-10-19)
- `MinimizadorValoresAuditoria` enmascara las propiedades con nombre de secreto (contraseña, hash, token, sello, clave, cookie), también anidadas. `RegistroAuditoria` lo usa.
- Verificado: una operación rechazada (409) no deja evento de éxito, y si la auditoría no se puede escribir la operación completa falla (500, sin cambio).
- **Pendiente:** la IP del cliente no se registra (requiere migración y definir su tratamiento como dato personal, D-14). La lectura de auditoría aislada por organización es HU-046.
