---
tipo: modulo
estado: verificado
fuentes: [PRD v0.1]
relacionados: [HU-013, HU-005, HU-007, HU-008, RF-03, RN-01, RN-08, RN-10, CA-01, CA-02, CA-03, ADR-004, ADR-005, ADR-014, ADR-020, ADR-022, D-03, D-06, D-20]
actualizado: 2026-10-26
tags: [conecsia, modulo, f1, vertical]
---

# Sedes

Primera vertical funcional (2026-10-17): [[EP-04 Estructura física#HU-013 Gestionar sedes con zona horaria|HU-013]] de [[EP-04 Estructura física]] de punta a punta, con lo mínimo de [[EP-02 Tenancy, identidad y auditoría base]] que necesita para ser segura: aislamiento (HU-005), contexto por membresía (HU-007) y auditoría append-only (HU-008). Concepto: [[Sede]].

> [!warning] Autenticación provisional
> No hay inicio de sesión real: HU-006 sigue **bloqueada por D-03**. La identidad llega con un esquema **solo de desarrollo** ([[ADR-022 Autenticación de desarrollo provisional]], Propuesta) que la API rechaza fuera de `Development`/`Testing`.

## Qué hace (Implementado y Verificado)

| Operación | Endpoint | Permiso | Alcance | Errores |
|---|---|---|---|---|
| Listar (paginado, filtro de estado) | `GET /api/v1/sedes?estado=&pagina=&tamano=` | `sedes.consultar` | Solo sedes asignadas si el alcance no es total | 400 paginación inválida (tamaño 1–100) |
| Consultar | `GET /api/v1/sedes/{id}` | `sedes.consultar` | Sede asignada | 404 otra organización · 403 `FUERA_DE_ALCANCE` |
| Crear | `POST /api/v1/sedes` | `sedes.crear` | Alcance total | 400 campos · 409 `SEDE_NOMBRE_DUPLICADO` |
| Modificar | `PUT /api/v1/sedes/{id}` (con `version`) | `sedes.modificar` | Sede asignada | 409 `CONFLICTO_CONCURRENCIA` / duplicado |
| Inactivar | `POST /api/v1/sedes/{id}/inactivar` | `sedes.inactivar` | Sede asignada | 422 `SEDE_YA_INACTIVA` |
| Contexto propio | `GET /api/v1/me` | autenticado | — | 401 / 403 |
| Usuarios sintéticos (solo desarrollo) | `GET /api/v1/desarrollo/usuarios` | anónimo, solo con ADR-022 habilitado | — | 404 si está deshabilitado |

Respuestas de error en Problem Details con `traceId` y `code` ([[ADR-020 Contratos API y formato de errores]]).

## Reglas aplicadas en el servidor
- **Organización del contexto, nunca del cliente** (ADR-005): un `organizacionId` en el cuerpo se ignora. `X-Organizacion-Id` solo selecciona entre membresías propias; una ajena da **403 `ORGANIZACION_NO_PERMITIDA` y queda auditada** con resultado `Denegado`.
- **Permisos por clave, nunca por nombre de rol**: una membresía con el texto "Administrador" sin claves recibe 403.
- **Aislamiento** (ADR-004, CA-01): filtro global de EF Core por `organizacion_id` en toda entidad `IEntidadDeOrganizacion`; sin contexto no devuelve filas; el guardado rechaza escrituras de otra organización (respuesta 404). PostgreSQL: FK a `organizacion` y unicidad `(organizacion_id, nombre_normalizado)`. Clave alterna `(organizacion_id, id)` lista para FKs compuestas.
- **Alcance por sede** (CA-02): primero existencia (404 si es de otra organización), luego alcance (403).
- **Zona horaria IANA obligatoria** (RN-08): se rechazan identificadores de Windows (`SA Pacific Standard Time`) y desfases (`-05:00`). Validación con la base ICU/tzdata del sistema; NodaTime ([[ADR-008 Modelo de tiempo y zonas horarias]]) sigue en Propuesta.
- **Nombre único por organización**, normalizado (espacios y mayúsculas; conserva tildes). Verificación previa + índice único: altas concurrentes del mismo nombre crean una sola sede.
- **Concurrencia optimista** con `xmin` de PostgreSQL: editar con una versión leída antes de otro cambio da 409.
- **Inactivar no borra** (CA-03): la sede sigue consultable y filtrable como `Inactiva`.
- **Auditoría en la misma transacción** (ADR-014): `sede.crear`, `sede.modificar` (anterior/nuevo), `sede.inactivar` (estado anterior/nuevo) con organización, actor, instante, objeto, resultado y `traceId`. El rol `conecsia_app` no puede `UPDATE`, `DELETE` ni `TRUNCATE` sobre `auditoria_evento`.
- **Cuerpo malformado = 400**, nunca 500, y sin exponer nombres de tipos internos.

## Pendiente o provisional
> [!todo] Pendiente
> - Cambiar la zona horaria con autorizaciones vigentes: hoy no hay autorizaciones; la regla depende de **D-06**.
> - Inactivar una sede con personas dentro: no existe aún el módulo de Accesos; propuesta de HU-013 = rechazar. **Pendiente** de decisión.
> - Permisos y alcance se guardan directamente en `membresia` (`text[]`, `uuid[]`). **Provisional** hasta HU-017 (roles) y HU-018 (alcance administrable).
> - Organizaciones y usuarios solo existen por semillas: HU-009 y HU-016 no están implementadas.
> - Denegaciones por permiso (403) se registran en log pero **no** en la auditoría; solo se audita la selección de organización ajena.
> - Reactivar una sede: no está en el alcance de HU-013.

## Diseño (Implementado)
- `SharedKernel`: `IContextoSolicitud`, `Permisos`, `AccesoDenegadoException` (403), `ConflictoException` (409), `IEntidadDeOrganizacion` y `[SinFiltroDeOrganizacion]` (exclusión con justificación obligatoria), `IRegistroAuditoria`, `IUnidadDeTrabajo`, `IConsultaOrganizaciones`.
- `Infrastructure`: `ContextoSolicitud` (por petición), filtro global y verificación de guardado en `ConecsIADbContext`, `UnidadDeTrabajo`.
- Módulos: Organizaciones (`Organizacion` + consulta), Identidad (`Usuario`, `Membresia`, `ResolvedorContexto`, `MeController`, `DesarrolloController`), Estructura (`Sede`, `ServicioSedes`, validadores, `RepositorioSedes`, `SedesController`), Auditoria (`EventoAuditoria`, `RegistroAuditoria`).
- API: `AutenticacionDesarrollo`, `MiddlewareContextoOrganizacion`, filtro global `AuthorizeFilter` (todo controlador exige autenticación salvo `[AllowAnonymous]`), mapeo 403/409 en `ManejadorExcepciones`, errores de enlace como 400 en `FiltroValidacion`.
- Migración `PrimeraVerticalSedes`: tablas `organizacion`, `usuario`, `membresia`, `sede`, `auditoria_evento`; FKs entre módulos añadidas a mano; `REVOKE` de auditoría.
- Frontend: `/ingresar` (selector de usuario sintético), `/sedes` (lista, filtro, paginación, formulario en diálogo con selector de zona IANA, edición con versión, inactivación con confirmación), cabecera con organización y usuario, cierre de sesión ante 401. Los botones dependen de los permisos de `/me` solo por usabilidad.

## Extension Sedes y Areas (2026-10-26)
- Sedes: consulta, alta, edicion e inactivacion; ciudad desde catalogo y zona horaria derivada del catalogo.
- Areas: pertenencia obligatoria a sede e indicador de area comun condicionado a zona empresarial.
- Migracion `20261026150556_SedesAreasYCatalogoCiudad` para zona horaria, relaciones de zona empresarial y unicidad por organizacion/sede.
- Verificado: compilacion backend, build y typecheck frontend.

## Interfaz
Estilo según [[Sistema visual]] (2026-10-17): migas, tarjetas de resumen (total, activas, inactivas; respetan el alcance del usuario), tarjeta de filtros y tabla con estados en píldora.

## Verificación
Ver [[Estado de implementación#Verificación de la primera vertical (2026-10-17)]].

## UI y API de Sedes y áreas — 2026-10-26

- **Implementado:** la sección «Sedes y áreas» de [[Administración]] (`SedesAreasSeccion.tsx`, `apiSedes.ts`) usa los mismos componentes que Zonas empresariales: `Marco`, tarjetas `ad-zona`, buscador, filtros por estado y sede, `Dialogo`, `Confirmar` y avisos. Tiene un selector de vista Sedes / Áreas con contadores de activas.
- **Implementado:** `GET /api/sedes/ciudades` devuelve las ciudades activas con el módulo `sedes` (consultar). Antes el formulario llamaba a `/api/Ciudad`, que exige el módulo `configuracion`, y por eso no aparecían ciudades.
- **Implementado:** `POST /api/sedes|areas/{id}/activar|inactivar` con la acción **Editar**; cada cambio de estado se audita. Las áreas ahora también auditan crear y modificar, y no se reactivan si su sede está inactiva.
- **Verificado:** `SedesAreasTests` (4 pruebas): catálogo sin `configuracion`, zona horaria tomada de la ciudad, aislamiento entre organizaciones, nombre único, permisos de editar y auditoría. `tsc -b` ok.
- **Pendiente:** verlo en el navegador con el API reiniciado. Si la tabla `ciudad` está vacía, el formulario lo indica; el catálogo hay que cargarlo aparte (no hay CRUD de ciudades en la UI).

## Catálogo de ciudades DIVIPOLA — 2026-10-26

- **Implementado:** `ciudad.codigo_dane` (char(5), único cuando no es nulo). La migración `20261026215936_CatalogoCiudadesDivipola` carga los 1.122 registros de DIVIPOLA del DANE (1.103 municipios, 18 áreas no municipalizadas y 1 isla), con corte 2026-10-26. Nombres en formato título, país `Colombia`, zona horaria `America/Bogota`.
- La fuente queda versionada en `backend/Infrastructure/Data/Seeds/divipola-municipios-20261026.csv`. La carga es idempotente (`ON CONFLICT (codigo_dane)`), así que un corte nuevo se aplica con otra migración del mismo tipo.
- El `Down` solo borra las ciudades del catálogo que ninguna sede usa.
- **Verificado:** migración aplicada a la base local; prueba `El_catalogo_DIVIPOLA_queda_cargado_sin_codigos_repetidos`; 61/61 pruebas.
- **Implementado 2026-10-26:** el selector de ciudad tiene búsqueda (`Selector` con `buscable`).
- **Pendiente:** Las ciudades que se crearon a mano antes (sin `codigo_dane`) no se fusionan con las del catálogo.
