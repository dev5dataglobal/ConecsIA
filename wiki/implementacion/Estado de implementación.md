---
tipo: implementacion
estado: implementado
actualizado: 2026-10-31
tags: [conecsia]
---

## Zona empresarial por sede y retiro de zonas de acceso (ADR-028) — 2026-10-31
- Rama `feature/organizaciones`, sin commit. Decisión: [[ADR-028 Zona empresarial por sede y retiro de zonas de acceso]]; módulo [[Puntos de acceso]].
- **Backend.** Retirados: entidades `Zona`, `AutorizacionZona`, `ZonaEmpresarialOrganizacion` (configuraciones, repositorios, interfaces, registro en `IUnitOfWork`/`UnitOfWork`, `DbSet`, mapeos), `AutorizacionZonaController`, `ZonasPuntosAccesoController` → `PuntosAccesoController` (solo `/api/puntos-acceso`), `PUT /api/autorizaciones-acceso/{id}/zonas`, parámetro `autorizaciones.exige_zonas`, `ReglasOrganizacion.AsignarZonaAsync/ZonaDeEmpresaAsync/EsSuperiorAsync`, `empresas-disponibles`.
  Cambiados: `PuntoAcceso` (sin `IdZona`, `+EsComunZonaEmpresarial`, FK a sede), `MovimientoAcceso` (sin `IdZona`, `+ZonaHistorica`), `RestriccionAcceso` (sin `IdZona`), `SedeController` (zona activa, bloqueo si hay elementos comunes, `GET /api/sedes/zonas-empresariales`), `AutorizacionController.ValidateScope` (sin organización administradora), `ZonaEmpresarialController` (catálogo de plataforma con sedes derivadas), `OrganizacionesAdministracionController` y DTO de organización (`ZonasEmpresariales` derivadas de sedes), `VisitasController`/`ReglasVisita.MotivosAutorizacion` (sin zonas). Nuevo `IPuntoAccesoRepository`.
- **Migración** `20261031145759_ZonasEmpresarialesPorSedeYRetiroZonasAcceso` con bitácora `public.migracion_zonas_adr028`; **no reversible**. **No aplicada** a `conecsia` ni a `conecsia_verificacion` (solo a bases temporales de prueba).
- **Frontend.** `PuntosAccesoSeccion` + `apiPuntosAcceso` (reemplazan `ZonasAccesoSeccion`/`apiZonasAcceso`), ruta `/administracion/puntos-acceso`; `ZonasSeccion` sin organización superior ni empresas; `OrganizacionesSeccion` sin selector de zona (columna «Zonas empresariales» según sedes); selector de zona empresarial en el formulario de sede; Control de accesos sin selector ni edición de zonas; Visitas sin zonas (muestra `zonaHistorica`).
- **Pruebas.** Backend 104/104 (`PuntosAccesoTests` 8 reemplaza `ZonasPuntosAccesoTests`; `MigracionZonasTests` 1 nueva con datos del esquema anterior; ajustadas `AdministracionTests`, `VisitasTests`, `AutorizacionesAccesoTests`). Queda el fallo de limpieza de clase ya conocido (`ContactoTests`, borrado de la base temporal), sin pruebas fallidas. Frontend 53/53 en serie; en paralelo hay fallos intermitentes por tiempo en módulos no tocados. `tsc -b` correcto. `oxlint --deny-warnings` reporta advertencias previas en archivos ajenos al cambio.
- **CA verificados por integración:** punto en sede de otra organización rechazado (API y FK); `/api/zonas` 404; autorización sin zonas válida en cualquier punto de su sede; movimiento con sede y punto; punto común solo con zona; sede con elementos comunes no cambia de zona; organización con sedes en dos zonas; otra organización no ve sedes ajenas de la zona; migración traslada lo inequívoco y deja pendientes.
- **Aplicada 2026-10-31 a `conecsia_verificacion`** (con `SolicitudesContacto`, que faltaba) tras respaldo `conecsia_verificacion_respaldo_20261031` (copia por TEMPLATE). Bitácora: 4 zonas retiradas, 3 puntos conservados en su sede, sin vínculos zona-organización ni pendientes. **No aplicada a `conecsia`.**
- **Verificado en navegador 2026-10-31** (admin.alfa, API 5192 + frontend 5174): Puntos de acceso lista los 3 puntos migrados; formulario de sede asigna «Zona Franca Verificación» (zona de prueba creada por SQL) y la tarjeta la muestra; el interruptor «Punto común» aparece solo con sede en zona; se creó «Portería del parque» común con trazabilidad. Sin errores de consola salvo 502/401 mientras la API arrancaba. Observación: el menú lateral muestra el módulo `sedes` como «Sedes y zonas · Pronto» (etiqueta y ruta de `app_modulos`, no tocadas).

## Tipo de permiso de acceso derivado y visitas extemporáneas — 2026-10-29
- Rama `feature/organizaciones`, sin commit. Backend: `Services/ReglasPermisoAcceso.cs` (nuevo), `PersonasController` (filtro `tipoPermiso`, `CompletarPermisoAsync`), `VisitasController.Registrar` (409 + auditoría), DTO `AccesoRecurrenteDto`. Sin migración.
- Frontend: `PermisoAcceso` en `PersonasPage.tsx` (tabla, filtro, detalle, nota en formulario); `VisitasPage.tsx` (Ingreso extemporáneo, bloqueo por sede).
- Pruebas: backend `Persona_con_acceso_recurrente_no_se_registra_como_visita_extemporanea` (nueva) y 2 pruebas de Visitas ajustadas para vincular la autorización recurrente; frontend 1 prueba nueva en Visitas y aserción en Personas. Sin verificación en navegador (la API en ejecución bloquea el binario).
- Pendiente: D-46 (excepción, sede vs. destino).

## Zonas y puntos de acceso (RF-05, HU-015) — 2026-10-28

- **Implementado** en la rama `feature/organizaciones` (sin commit): [[Zonas y puntos de acceso]], [[ADR-027 Punto de acceso como entidad dentro de la zona]].
- Backend: `PuntoAcceso` (entidad, configuración, FK compuestas), `Zona` con trazabilidad y nombre normalizado único por sede, `ZonasPuntosAccesoController` (`/api/zonas`, `/api/puntos-acceso`), `PUT /api/autorizaciones-acceso/{id}/zonas`, parámetro `autorizaciones.exige_zonas`, movimientos de visitas con `id_punto_acceso` + `id_zona`. Se retiró `ZonaController` genérico.
- Migración `20261029001444_ZonasYPuntosAcceso` (con migración de datos). **Aplicada 2026-10-29** a `conecsia` y `conecsia_verificacion`.
- Frontend: pestaña Administración › Zonas y puntos de acceso; selector de zonas (obligatorio según política) y «Editar zonas» en solicitudes pendientes de Control de accesos; punto con su zona en Visitas.
- Pruebas: backend 100/100 (incluye `ZonasPuntosAccesoTests`, 6); frontend 41/41. Hubo fallos de limpieza de clase (`Test Class Cleanup Failure`, NpgsqlException al borrar la base de pruebas) en SedesAreasTests y SesionTests, sin pruebas fallidas.
- CA verificados por integración: zonas en sede y puntos en zona; punto a zona de otra sede/organización rechazado (API y FK); autorización limitada a zonas; movimiento registra punto y zona; punto fuera de zona permitida ⇒ 409; inactivar impide nuevas configuraciones y conserva historial.
- **Verificado en navegador 2026-10-29** (administración de zonas y puntos; ver [[Zonas y puntos de acceso]]). Configuración `api-verificacion` añadida a `.claude/launch.json` (Release, :5192, base `conecsia_verificacion`).

## Mensajes controlados de autenticación — 2026-10-21

- Un correo inexistente o inactivo responde `401` con: “El usuario ingresado no está registrado”.
- Una contraseña incorrecta responde `401` con: “La contraseña ingresada es incorrecta”.
- Un usuario bloqueado recibe un mensaje temporal en español.
- El frontend ya no muestra `Unauthorized`, el mensaje genérico de sesión ni referencias `traceId` para errores de credenciales; las referencias quedan reservadas para errores del servidor.

# Estado de implementación

## Control de accesos: solicitudes de autorización y retiro del tipo de acceso — 2026-10-28
- Rama `feature/organizaciones`, sin commit. Backend: `AutorizacionesAccesoController` (`/api/autorizaciones-acceso`), `Services/ReglasAutorizacion.cs`, `DTOs/AutorizacionesAccesoDto.cs`; alcance por sede movido a `AdministracionControllerBase`; `ReglasVisita.MotivosAutorizacion` valida franjas. Retirados `Persona.TipoAcceso` y la excepción de Visitas.
- Migración `20261028223749_AutorizacionesAccesoYRetiroTipoAcceso` (columnas de decisión y horas, CHECK de modalidad/estado/fechas/horas, FKs a usuario y de horario/transición, ruta `/control-accesos`, DROP `persona.tipo_acceso`). Aplicada a `conecsia` el 2026-10-28 y API :5191 reiniciada con el binario nuevo.
- Frontend: `features/control-accesos/` (página, API, CSS), botón «Gestionar acceso» en Personas, Visitas sin excepción; `.ad-tabla-wrap` con `position: relative` (corrige desborde móvil por el `sr-only` de la columna de acciones).
- **Verificado:** `dotnet test` 94/94; `vitest` 38/38; `tsc -b` limpio; navegador sobre base desechable. Detalle en [[Control de accesos]].
- `scripts/seed-verificacion-visitas.sql`: autorizaciones Temporal con horas (exigidas por el nuevo CHECK).

## Tipo de permiso de acceso y visitas extemporáneas — 2026-10-27 (retirado el 2026-10-28)

- Rama `feature/organizaciones`, sin commit. Backend: `Persona.TipoAcceso`, `Visita.ExcepcionTipoAcceso/MotivoExcepcion`, `ReglasPersona.TiposAcceso`, `PersonasController` (filtro, validación, auditoría `CambiarTipoAcceso`), `VisitasController.Registrar` (409 recurrente, excepción auditada). Migración `20261027235631_PersonaTipoAccesoYExcepcionVisita` aplicada a `conecsia` el 2026-10-27.
- Frontend: `PersonasPage.tsx` (selector, columna, filtro, detalle, `PermisoAcceso`), `VisitasPage.tsx` (paso Visitante con bloqueo y excepción), CSS en `personas.css` y `visitas.css`.
- Pruebas: backend 87/87 (nueva: conservación del tipo al editar, auditoría, 409/400, excepción auditada); frontend 35/35 (nueva: bloqueo y excepción en el paso Visitante); `tsc` limpio.
- Verificado en navegador el 2026-10-27 (base desechable). Decisiones: [[Decisiones pendientes|D-38, D-39]].

## Módulo de visitas — 2026-10-26

- **Implementado (backend):** `VisitasController` (`/api/visitas`: listado paginado con indicadores, detalle con evaluación de ingreso, opciones, autorizaciones vinculables, registrar, vincular autorización, cancelar, entrada, salida, presencia), reglas puras en `Services/ReglasVisita.cs`, contratos en `DTOs/VisitasDto.cs`; `Auditar` acepta resultado (`Denegado`). Detalle: [[Visitas]].
- **Retirado:** scaffolds `VisitaController`, `PermanenciaController`, `MovimientoAccesoController`, `SolicitudIdempotenteController`.
- **Migración:** `20261027015608_VisitasCicloOperativo` (columnas, CHECK, FK compuestas, índices únicos de permanencia, corrección del único `zona(org, sede)`, ruta `/visitas`). Aplicada a `conecsia` el 2026-10-26 (índices únicos, CHECK y ruta `/visitas` confirmados). La API en :5191 sigue con el binario anterior hasta reiniciarla.
- **Implementado (frontend):** `src/features/visitas/` (listado, nueva visita por pasos, detalle con entrada/salida/cancelar/vincular, personas dentro), ruta `/visitas`; `PersonaFormulario` exportado para reutilizarlo; `ProblemDetails.motivos`.
- **Verificado:** `dotnet test` 86/86 (13 nuevas en `VisitasTests`: estados, política, vigencia/día/horario/zona, ingreso único, idempotencia, 6 entradas simultáneas ⇒ 1 permanencia, salida sin entrada/duplicada, aislamiento entre organizaciones, alcance por sede, permisos separados, búsqueda/paginación); `vitest` 34/34 (3 nuevas); `tsc -b` y `oxlint` limpios. Navegador (API :5192 + frontend :5174 sobre `conecsia_verificacion`, eliminada al terminar): búsqueda por documento, registro autorizado y pendiente, motivos visibles, entrada, personas dentro, salida con duración, móvil 375 px sin desborde.
- **Pendiente:** EP-07 (decidir autorizaciones), HU-014/HU-015 (responsables y puntos de acceso sin pantalla), QR, SignalR, D-04..D-08, D-34..D-37.

## Foto de personas, dispositivos y autorizaciones de vehículos y dispositivos — 2026-10-26

- **Corregido (frontend):** foto invisible en detalle y edición. Causa: `useUrlObjeto` revocaba en `StrictMode` la URL creada en `useMemo`. Ahora la crea y revoca el mismo efecto; la descarga usa `apiBlob` con renovación de sesión; «Descartar selección» ya no elimina la foto guardada. Detalle en [[Personas#Corrección: foto no visible en detalle y edición (2026-10-26)]].
- **Implementado (backend):** `DispositivosController` (tipos y dispositivos por persona), `AutorizacionesObjetoController` (`/api/autorizaciones-objeto`: listar/filtrar, detalle con historial, solicitar, aprobar, rechazar, revocar), `Services/ReglasDispositivo.cs`, `DTOs/DispositivosAutorizacionesObjetoDto.cs`; entidades `AutorizacionObjeto`, `AutorizacionObjetoTransicion`; `Dispositivo.IdUsuarioCrea`; `PersonaDetalleDto.RegistradoPor`. Diseño: [[ADR-026 Autorización de ingreso de vehículos y dispositivos]].
- **Retirado:** scaffolds `DispositivoController`, `TipoDispositivoController`, `AutorizacionDispositivoController`, `AutorizacionVehiculoController` y DTOs `DispositivoDto`/`TipoDispositivoDto` antiguos.
- **Migración:** `20261026224617_DispositivosYAutorizacionesObjeto` (tablas nuevas, FKs compuestas, CHECKs, triggers append-only en `auditoria` y transiciones, FK `persona.id_usuario_crea`, ruta `/autorizaciones`). Aplicada a `conecsia` el 2026-10-26. Detalle en [[Autorizaciones de vehículos y dispositivos]] y `db.md`.
- **Implementado (frontend):** sección Dispositivos y «Registrado por» en el detalle de persona; `src/features/autorizaciones/` (ruta `/autorizaciones`).
- **Verificado:** ver la entrada de [[log]] del 2026-10-26 (pruebas backend y frontend). Verificado en navegador sobre base desechable.
- **Pendiente:** D-31, D-32, D-33, D-17; movimientos HU-048/HU-050; paginación.

## Módulo de Personas — 2026-10-26

- **Implementado (backend):** `PersonasController` (`/api/personas`: listado con búsqueda/filtros, búsqueda por documento, detalle, alta, edición, estado, foto, teléfonos, vehículos) y `CatalogosPersonaController` (tipos de persona por organización; tipos de documento globales con escritura solo de plataforma), sobre `AdministracionControllerBase`. Reglas en `Services/ReglasPersona.cs`; contratos en `DTOs/PersonasAdministracionDto.cs`. Al crear una organización se siembran sus tipos de persona.
- **Retirado:** controladores scaffold `PersonaController`, `PersonaTelefonoController`, `PersonaVehiculoController`, `TipoPersonaController` y `TipoDocumentoController` (aceptaban la entidad completa del cliente y permitían `DELETE` físico). Las pruebas que usaban `/api/Persona` como endpoint de muestra ahora usan `/api/personas`.
- **Migración:** `20261026180826_PersonasRelacionesYCatalogos` — 7 FKs nuevas, índice único erróneo `persona(org, tipo_persona)` corregido, `codigo_pais` y únicos parciales en teléfonos, `patron_validacion` y único `codigo` en tipos de documento, siembra (CC, TI, CE, PA, PPT, NIT; 6 tipos de persona por organización) y ruta `/personas`. Aplicada en `conecsia`. Detalle en [[Personas]] y `db.md`.
- **Implementado (frontend):** `src/features/personas/` (listado, formulario con foto y «buscar primero», detalle con teléfonos y vehículos, catálogo de tipos), ruta `/personas`; `useUrlObjeto` movido a `administracion/utilidades.tsx`; `Cabecera`/`Marco` aceptan `migas`.
- **Verificado:** `dotnet test` 56/56 (21 nuevas en `PersonasTests`); `vitest` 20/20 (4 nuevas); `tsc -b` limpio; navegador con API de verificación sobre base desechable (eliminada al terminar).
- **Pendiente:** ver [[Personas#Pendiente]] (D-09, D-13, D-14, D-17, motivo de inactivación, FK `id_usuario_crea`, `DOC-001`, `VehiculoController` scaffold, paginación).

## Sesión renovable, recuperación de contraseña y ajustes de Perfiles — 2026-10-26

- **Implementado (backend):** [[ADR-025 Sesión renovable y recuperación de contraseña]].
  - `AuthController`: `login` (acepta `recordarme`), `renovar`, `cerrar-sesion`, `recuperar-contrasena`, `restablecer-contrasena`.
  - `UserService` emite el acceso con un método común (`EmitirAccesoAsync`) para login y renovación; los claims `permission` ya no incluyen perfiles inactivos.
  - Nuevos: `RecuperacionContrasenaService`, `IEnviadorCorreo` + `SmtpEnviadorCorreo` (`Correo`), `SesionOptions` (`Sesion`), `PoliticaContrasena` (también usada por `PUT /api/usuario/me/contrasena`), `TokensSeguros`, política de límite `recuperacion`.
  - Entidades `SesionRefresco` y `RecuperacionContrasena`; migración `20261026132034_SesionesRefrescoYRecuperacionContrasena` (solo crea 2 tablas).
- **Implementado (frontend):** `http.ts` renueva una vez ante `401` y reintenta; `main.tsx` restaura la sesión antes de montar las rutas; login con «Recordarme» activo y rutas `/recuperar-contrasena` y `/restablecer-contrasena` con el mismo diseño del pase; la caché de React Query se vacía al iniciar y cerrar sesión; `Selector` accesible; tarjetas y botones de Perfiles (ver [[Administración]]).
- **Causa del problema de permisos:** ver [[Administración]] § Diagnóstico 2026-10-26.
- **Verificado:**
  - `dotnet test`: 35/35 (11 nuevas en `SesionTests`: cookie de sesión vs persistente, rotación, cierre, cabecera obligatoria, pestañas simultáneas, módulos al iniciar sesión, cambio de perfil aplicado a la sesión activa y al renovar, 503 sin correo, misma respuesta exista o no la cuenta, un solo uso + revocación de sesiones, token desconocido).
  - `vitest`: 16/16 (nuevas: selector con teclado y mouse, «Recordarme» + renovación tras 401 sin tokens en almacenamiento, enlace de recuperación).
  - Navegador (API de verificación en 5192 contra `conecsia_verificacion`, migrada; frontend 5174): login con «Recordarme», cookie no visible para JS, recarga restaura la sesión, cerrar sesión revoca (renovar posterior → 401), selector con teclado/mouse, tarjetas en escritorio y 375px, recuperación → 503 con mensaje claro (correo sin configurar), enlace inválido → mensaje de error.
- **No verificado:** cerrar y reabrir el navegador real (la persistencia se comprobó por el atributo `Expires` de la cookie en pruebas de integración); envío SMTP real (no hay servidor configurado).
- **Pendiente:** configurar `Correo` y `Sesion:UrlFrontend` ([[Decisiones pendientes|D-30]]); cierre por inactividad ([[Decisiones pendientes|D-29]]).

## Administración completa, acciones por módulo y zonas empresariales — 2026-10-24

- **Implementado (backend):**
  - Acciones `puede_consultar`, `puede_anadir` y `puede_editar` en `modulos_permisos`; `[RequiereModulo]` deduce la acción del verbo HTTP o la recibe explícita.
  - Módulos `usuarios`, `perfiles`, `organizaciones` y `zonas_empresariales`; `administracion` quedó inactivo.
  - `ContextoOrganizacionMiddleware` (`X-Organizacion-Id`, solo plataforma). La sesión se valida por membresía y organización activas.
  - Login rechaza usuarios sin membresía activa (`UserInactive`).
  - Controladores nuevos `UsuariosAdministracionController`, `PerfilesAdministracionController`, `OrganizacionesAdministracionController` (con logo) y `ZonaEmpresarialController` reescrito. Se retiró `AdministracionController`.
  - `AlmacenamientoArchivos` (local y privado); `ReglasOrganizacion`.
  - El `DbContext` tiene `PermitirEscrituraEn`; `UnauthorizedAccessException` responde 403.
  - Detalle en [[ADR-024 Acciones por módulo y contexto de organización]] y [[Administración]].
- **Migración `20261024213105_AccionesModuloYZonasEmpresariales`:**
  - Agrega las tres columnas (las filas existentes quedan en `true`).
  - Siembra los 4 módulos y los copia a los perfiles que tenían `administracion`, que se inactiva.
  - Crea los índices únicos parciales de zona: administradora única por zona y zona única por empresa.
  - No borra datos. Se aplica con `dotnet ef database update --project backend/Infrastructure --startup-project backend/ApiConecsia` (la fábrica de diseño lee `appsettings*.json` del proyecto de inicio o `CONECSIA_CONNECTION_STRING`; ver entrada 2026-10-26 en [[log]]). Revertir: `dotnet ef database update 20261022235936_AddAppModulosYModulosPermisos`.
- **Aplicado 2026-10-24 en la base local `conecsia`.** El rol 1 conserva solo `dashboard` (no tenía `administracion`); el usuario 1 accede a todo como administrador de plataforma.
- **Implementado (frontend):**
  - Rutas `/administracion/{usuarios|perfiles|organizaciones|zonas-empresariales}` con `RutaModulo` por código; `/administracion` redirige a la primera permitida.
  - Secciones con búsqueda, filtros, formularios validados, confirmación de inactivación y avisos.
  - Matriz de permisos ampliada y responsiva. Selector de organización en la barra solo para plataforma (`sessionStorage` por usuario, caché reiniciada al cambiar).
  - `esAdministradorGeneral` usa el claim `platform_admin`.
- **Verificado:**
  - Backend xUnit **24/24**: 14 nuevas en `AdministracionTests` (crear y editar usuario con perfil, duplicados, inactivar usuario, organización y perfil, acciones separadas, matriz y anti-escalamiento, organizaciones, logo, zonas con empresas y exclusividad, visibilidad entre empresas, selector solo para plataforma y aislamiento por ruta y cuerpo) y 10 existentes ajustadas.
  - Frontend Vitest **12/12**; `tsc -b` sin errores; `oxlint` sin avisos nuevos (persisten avisos previos en `PerfilModal`, `IngresarPage` y `rutas.tsx`).
  - Navegador (API de verificación en el puerto 5192 contra la base desechable `conecsia_verificacion`, sembrada con `backend/scripts/seed-verificacion-administracion.sql`; frontend `frontend-verificacion` en el 5174):
    - Un administrador de empresa ve solo sus usuarios, crea un usuario con perfil, edita la matriz y ve solo su organización.
    - La plataforma crea una zona con 2 empresas y, al seleccionar otra organización, ve solo sus usuarios.
    - La matriz se revisó en 375×812 sin recortes. Sin errores de consola.
- `vite build` de producción sin errores.
- **No verificado en la interfaz:** la carga de logo (sí por prueba de API).
- **Pendiente:** [[Decisiones pendientes|D-24]]..[[Decisiones pendientes|D-28]]; la multi-membresía con selector para usuarios normales sigue en [[Decisiones pendientes|D-03]].

## Acceso a módulos por rol (app_modulos / modulos_permisos) — 2026-10-22

- **Implementado (backend):** entidades `AppModulo` y `ModuloPermiso` con configuraciones EF separadas; migración `20261022235936_AddAppModulosYModulosPermisos` (catálogo de 12 módulos + traslado desde `rol_permiso`); repositorios en `UnitOfWork`; `ModuloAccesoService` con caché invalidable; `ModuloPermisoService` (validaciones, anti-escalamiento, auditoría); endpoints `GET /api/app-modulos/mios`, `GET|POST /api/app-modulos`, `PUT /api/app-modulos/{id}`, `PATCH /api/app-modulos/{id}/estado`, `GET|PUT /api/roles/{idRol}/modulos`, `POST|DELETE /api/roles/{idRol}/modulos/{idModulo}`; política `[RequiereModulo]` en 55 controladores. Ver [[ADR-023 Acceso a módulos por rol en base de datos]].
- **Implementado (frontend):** menú construido desde `/api/app-modulos/mios`; `RutaModulo` muestra «Acceso denegado» si el servidor no concede el módulo; Administración → Perfiles → «Módulos» reemplaza las asignaciones del rol.
- **Corregido (errores previos):** `ConecsiaDbContext` capturaba la organización al construirse (durante `OnTokenValidated`, sin usuario) y el filtro quedaba en `-1` toda la petición; ahora se evalúa por consulta. Migración manual de zonas empresariales con argumentos posicionales erróneos (no aplicable en ninguna base).
- **Verificado:** proyecto nuevo `backend/ConecsIA.Tests` (xUnit + WebApplicationFactory + PostgreSQL temporal creado con las migraciones y eliminado al final): **10/10**. Cubre catálogo inicial, unicidad/FK en BD, denegación por defecto vía API directa, asignar/retirar con efecto inmediato y auditoría, reemplazo atómico, aislamiento entre organizaciones, anti-escalamiento, catálogo solo para el administrador general. Frontend Vitest **9/9**, `tsc`, `vite build`, `oxlint` sin errores.
- **Aplicado 2026-10-22 en la base local `conecsia`:** `20261022120000_AddBusinessZonesAndAuthorizationScopes` y `20261022235936_AddAppModulosYModulosPermisos` (respaldo previo con `pg_dump`). Resultado: 12 módulos, rol «Administrador plataforma» con `dashboard`; usuarios, roles y organizaciones intactos (1/1/1). El usuario 1 accede como administrador general; los demás necesitan módulos asignados.
- **Hallazgo:** en la base local `permiso` y `rol_permiso` están vacías; por eso el usuario 1 recibía 403 en Administración (sus endpoints exigen permisos que no existen). Con este cambio el módulo `administracion` queda cubierto por el administrador general, pero las pestañas siguen dependiendo de permisos `usuarios.*`/`roles.*` que no están sembrados.

## Módulo de Administración rediseñado — 2026-10-22

- **Implementado:** `features/administracion/AdministracionPage.tsx` y `administracion.css` con el lenguaje del dashboard (banda oscura con pestañas y conteos, tarjeta clara, tablas con avatar, etiquetas de perfil, estados de color, tarjetas de perfil, modal con `Escape`, «Cancelar» y doble envío bloqueado).
- Pestañas y botones de creación se muestran según los permisos que exige el backend (`AdministracionAuthorization`): `usuarios.consultar`/`usuarios.crear`, `saas.organizaciones.consultar` o `configuracion.consultar`/`saas.organizaciones.administrar`, `roles.consultar`/`roles.administrar` (también con prefijo `saas.`). Solo presentación: el servidor sigue autorizando.
- Estados: un 403 muestra «Tu perfil no tiene permiso…» distinto del error genérico; vacíos según búsqueda.
- **Verificado:** Vitest 6/6 (incluye `AdministracionPage.test.tsx`), `tsc`, `vite build`; revisión visual 1440×900 y 375×812 con datos de demostración en el navegador.
- **Observación:** el error que se ve con el usuario real en Usuarios es un 403 del backend (`Forbid()` en `GET /api/administracion/usuarios`); falta revisar los permisos/organización de ese usuario. Lógica de creación sin cambios (usuario se crea en la primera organización listada y sin perfiles).

## Shell autenticado y dashboard rediseñados — 2026-10-22

- **Implementado:** `frontend/src/app/shell/` (`AppShell.tsx`, `navegacion.ts`, `contextoShell.ts`, `appShell.css`) como ruta de layout protegida para `/dashboard` y `/administracion` (antes Administración no tenía menú). Iconos SVG compartidos en `shared/ui/Icono.tsx`; tokens `--ui-*` en `styles.css`.
- Menú: lateral oscuro con el lenguaje del login, grupos General/Operación/Gestión, ítem activo con `NavLink` (`aria-current`), colapsable en escritorio (preferencia en `localStorage`), panel deslizable con velo y `Escape` en <1024px. Se filtra por permiso del token (`tienePermiso`/`esAdministradorGeneral`); los módulos sin pantalla se muestran deshabilitados con «Pronto» (no navegan).
- Dashboard: banda oscura con saludo, organización, sede, hora de consulta, filtros (sede, Hoy/7 días) y accesos rápidos reales (Actualizar, Administración si hay permiso, Mi perfil). Indicadores, visitas por día, ingresos por tipo, personas dentro y actividad provienen solo de `GET /api/dashboard` (alcance filtrado en el backend por organización).
- Retirado: buscador, selectores de organización/sede de la barra, campana de notificaciones y tendencia «—» de las métricas (no tenían funcionalidad), y el contador de alertas del menú.
- **Verificado:** `tsc -b`, `vite build`, Vitest 4/4 (incluye `DashboardPage.test.tsx`: filtrado del menú por permiso, sección activa, indicadores y estados vacíos). Revisión visual a 1440×900, 820×1180 y 375×812 con datos de demostración inyectados solo en la pestaña del navegador (sin cuenta de prueba en el backend local).
- **No verificado:** con datos reales de la API y con un usuario sin `usuarios.consultar` contra el backend.

## Rediseño visual del login «Pase de acceso» — 2026-10-22

- **Implementado:** `frontend/src/features/sesion/IngresarPage.tsx` + `ingresar.css` (clases `lg-*`). Fondo oscuro con aurora y malla de puntos; el formulario es una credencial física (ranura, QR decorativo, perforado, pila de tarjetas detrás) con inclinación 3D sutil al mover el mouse; campos con etiqueta flotante; estado del pase (listo / validando / rechazado) con escaneo del QR y sacudida ante error.
- Movimiento según la skill de Emil Kowalski: solo `transform`/`opacity`, `cubic-bezier(0.23, 1, 0.32, 1)`, presión `scale(0.97)`, hover tras `(hover: hover) and (pointer: fine)`, `prefers-reduced-motion` quita desplazamientos y conserva fundidos.
- El feed de actividad del héroe usa `EJEMPLOS` ficticios y se rotula «Vista ilustrativa»; no consume la API. Se retiró el bloque muerto del selector de usuarios sintéticos.
- **Verificado:** `tsc -b` sin errores y `IngresarPage.test.tsx` 2/2. **Verificado en navegador (2026-10-22):** escritorio 1440×900, tablet vertical 768×1024 y horizontal 1024×768; estado de error (QR rojo, alerta, contraseña limpiada). La entrada se hace solo con CSS (`animation … both`) para que no quede invisible si `requestAnimationFrame` se demora. Las reglas viejas `.login-*` de `styles.css` quedan sin uso.

## Integración frontend del login — 2026-10-21

- El frontend usa `POST /api/auth/login` con `{ correo, password }` y mantiene el JWT solo en memoria.
- `apiFetch` centraliza `Authorization: Bearer`, limpieza de sesión ante `401` y conservación de sesión ante `403`.
- `RutaProtegida` redirige al ingreso cuando no existe un token en memoria.
- Se retiró del flujo real el contrato antiguo `/api/v1/auth`, CSRF, cookies y usuarios sintéticos.
- Variables: `VITE_API_BASE_URL` para despliegues separados y `API_PROXY_TARGET` para el proxy de Vite.
- En desarrollo local, `API_PROXY_TARGET` debe apuntar a `http://localhost:5191`, que es el perfil HTTP de `ApiConecsia`.
- Verificado: `npm run build` correcto. Vitest quedó pendiente porque el proceso trabajador no finaliza en este entorno.

## Administrador inicial — 2026-10-21

- **Creado en la base configurada:** organización `dataglobal`, usuario `desarrollador5@dataglobal.com`, membresía activa y rol `Administrador de organización`.
- El rol recibió los permisos activos ya existentes en el catálogo; no se inventaron códigos nuevos.
- La operación fue idempotente: una segunda ejecución encontró organización y usuario existentes y no duplicó registros.
- La contraseña no se guardó en el repositorio ni en la documentación; se almacenó únicamente como hash compatible con `PasswordHasher<Usuario>`.

> [!note] Arranque local 2026-10-21: copiar `backend/ApiConecsia/appsettings.Local.example.json` como `appsettings.Local.json` y completar los valores privados antes de ejecutar `dotnet run`.

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

## HU-006 Frontend — pantalla alineada a la referencia (2026-10-20)
- **Implementado:** `/ingresar` se sirve fuera del shell autenticado y conserva el contrato real de cookie/CSRF de `apiSesion.ts`.
- **Implementado:** composición responsive de dos paneles, validación HTML, estado de carga, prevención de doble envío, mostrar/ocultar contraseña y mensajes del servidor.
- **Retirado:** selector de usuarios sintéticos, credenciales de demostración y textos de prototipo de la pantalla de login.
- **Pendiente por alcance:** recuperación de contraseña, Microsoft y persistencia configurable de “Recordarme” siguen sin backend aprobado; los controles se presentan deshabilitados para no simular funcionalidad.
- **Verificado:** `npm.cmd run build`, `npm.cmd run lint` y `npm.cmd test -- --run` (26/26).

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
## Seguridad JWT y aislamiento — 2026-10-21

- **Implementado:** login JWT en `AuthController`/`UserService`, hashing existente de ASP.NET Core Identity, bloqueo tras cinco intentos fallidos durante 15 minutos y DTO sin `PasswordHash`.
- **Implementado:** validación obligatoria de issuer, audience, firma, expiración, claims de usuario, estado activo y `security_stamp`.
- **Implementado:** política global autenticada, endpoint anónimo explícito para login y políticas reutilizables de autenticación/permisos.
- **Implementado:** filtros globales de `ConecsiaDbContext` y validación de escrituras para entidades multi-organización.
- **Pendiente:** matriz definitiva de permisos por endpoint (D-04) y refresh token persistente revocable; no se añadió migración.
- **Configuración:** secretos y conexión fueron retirados de `appsettings.Development.json`; usar variables `JWT__Key`, `JWT__Issuer`, `JWT__Audience`, `JWT__DurationInMinutes` y `CONECSIA_CONNECTION_STRING`.


## Restauración tras commit 6b3127b + Sedes y áreas — 2026-10-26

> [!warning] Incidente
> El commit `6b3127b` (2026-10-26) borró 106 archivos y devolvió ~70 a versiones antiguas: autenticación (`AuthController`, `SesionRefresco`, `RecuperacionContrasena`), módulos/permisos, administración, dashboard, tests y migraciones. El login respondía con error porque `/api/auth/login` no existía.

- **Implementado (sin commit):** `git revert --no-commit 6b3127b` restaura el estado de `cf458da`; encima se reaplicaron los cambios de sedes/áreas con merge de 3 vías.
- `SedeController` (`api/sedes`) y `AreaController` (`api/areas`): versión nueva del usuario + `[RequiereModulo(CodigosModulo.Sedes)]`.
- `IOrganizationContext.UserId` (claim `sub`/`NameIdentifier`) para auditar al actor.
- Migración única `SedesAreasYCatalogoCiudad`: `ciudad.zona_horaria` (default `America/Bogota`), `organizacion.tiene_sedes`, índices únicos `sede(id_organizacion, nombre)` y `area(id_organizacion, id_sede, nombre)`. Reemplaza las dos migraciones locales, que volvían a crear `app_modulos`/`modulos_permisos`.
- Frontend: sección `SedesAreasSeccion` en [[Administración]] (código de módulo `sedes`), estilos acotados en `sedesAreas.css`.
- **Verificado:** `dotnet build` ok; `dotnet test` 35/35; `tsc -b` ok; `vitest` 16/16.
- **Implementado 2026-10-26:** migración `20261026171951_SedesAreasYCatalogoCiudad` aplicada a la base local.
- **Pendiente:** probar el login y las sedes en la app real. Para que se vea la sección, el rol necesita una fila en `modulos_permisos` para el módulo `sedes`.

### 2026-10-29 — Modales de detalle, historial de cancelación y decisión por objeto
- **Implementado:** `GET /api/visitas/{id}` devuelve `historial` (eventos `Registrar`, `VincularAutorizacion`, `Cancelar`, `ActualizarEstado` leídos de `auditoria`; sin cambio de esquema). La cancelación incluye usuario, instante y motivo; se muestra en «Movimientos e historial» de [[Visitas]] junto a los movimientos físicos. Un reintento de cancelación responde 409 y no duplica el evento (bloqueo `FOR UPDATE` + estado).
- **Implementado:** en [[Control de accesos]] el detalle lista vehículos y dispositivos activos de la persona; cada uno se aprueba o rechaza sobre su propia autorización de objeto ([[Autorizaciones de vehículos y dispositivos]]), con estados explícitos (Pendiente/Aprobado/Rechazado/Sin solicitud) y «Ver decisiones» con las transiciones. «Solicitar» crea la autorización de objeto asociada (`idAutorizacion`) con sede, área, fechas y horario del ingreso; si el ingreso no tiene fecha de fin no se solicita aquí.
- **Implementado:** modal de [[Personas]] por secciones (franja de identidad, datos personales, acceso y registro, información asociada) con botón «Dispositivos» en `--color-acento` (#D5F379). Franja de cabecera de los detalles en gris (`--color-superficie`); el verde queda como acento.
- **Verificado:** `tsc -b` ok; `vitest` 42/42; `dotnet test` Visitas + objetos 27/27. **No verificado** en navegador; la API en ejecución debe reiniciarse para servir `historial`.

## Docker Compose sobre el layout actual (2026-10-31, verificado 2026-11-03)

- **Implementado** (sin commit): `docker-compose.yml` con `postgres`, `migraciones`, `mailpit`, `api` y `web`; instrucciones en `DOCKER.md`. Las migraciones se aplican con un bundle EF Core en un servicio de un solo uso (la API no migra al iniciar); la cadena llega por `CONECSIA_CONNECTION_STRING`, porque `DesignTimeDbContextFactory` la exige antes de `--connection`.
- **Verificado 2026-11-03** (`docker compose build` + `up -d`): 3 imágenes construidas; 16 migraciones aplicadas; swagger 200; SPA `/` y `/login` 200; `/api/auth/login` por nginx 401 con cuenta inexistente; endpoint protegido sin token 401; Mailpit 200; una tabla marcador sobrevivió a `down`/`up` (luego borrada); segunda ejecución de migraciones: "No migrations were applied".
- Volumen nuevo `postgres-datos`: el antiguo `conecsia_postgres-data` (2026-10-17, esquema y roles de la integración anterior) se dejó intacto.
- **No verificado:** inicio de sesión real con usuario (no hay semillas automáticas), subida de archivos ni envío de correo.

> [!note] Docker Desktop en esta máquina
> El backend fallaba con `remove ...\Docker\run\dockerInference: El sistema no tiene acceso al archivo` (sockets obsoletos, ya visto antes). No se podían borrar; se renombró `%LOCALAPPDATA%\Docker\run` a `run.obsoleto-20261103` y Docker arrancó.
- **Datos 2026-11-03:** la base de Docker se pobló copiando la base local (2 usuarios, 2 organizaciones); procedimiento en `DOCKER.md`.
