---
tipo: modulo
estado: implementado
fuentes: [PRD v0.1]
relacionados: [HU-019, HU-020, HU-021, HU-022, HU-047, HU-049, ADR-026, RF-07, RF-08, RN-01, RN-08, CA-01, CA-03, CA-04, CA-15, D-09, D-13, D-14, D-17, ADR-004, ADR-013, ADR-024]
actualizado: 2026-10-26
tags: [conecsia, modulo, f1, personas]
---

# Personas

Módulo de registro de [[Persona|personas]] por organización, con teléfonos (1:N), vehículos (N:M) y dispositivos (1:N, ver [[Autorizaciones de vehículos y dispositivos]]). Épica: [[EP-06 Personas]]; vehículos: [[EP-16 Vehículos]] (parcial). Implementado y verificado el 2026-10-26.

## Qué hace (Implementado y Verificado)

| Operación | Endpoint | Acción de módulo |
|---|---|---|
| Listar con búsqueda (nombre, documento, correo) y filtros (estado, tipo) | `GET /api/personas?buscar=&estado=Activo\|Inactivo&idTipoPersona=` | Personas · Consultar |
| Buscar por identificación (flujo «buscar primero») | `GET /api/personas/buscar-documento?idTipoDocumento=&numero=` → 200 / 404 | Personas · Consultar |
| Detalle con teléfonos, vehículos y **registrado por** (correo del creador) | `GET /api/personas/{id}` | Personas · Consultar |
| Catálogos del formulario | `GET /api/personas/catalogos` | Personas · Consultar |
| Crear / editar | `POST /api/personas` · `PUT /api/personas/{id}` | Añadir · Editar |
| Activar / inactivar | `PATCH /api/personas/{id}/estado` | Editar |
| Foto (cargar, ver, quitar) | `POST\|GET\|DELETE /api/personas/{id}/foto` | Editar / Consultar |
| Teléfonos | `POST /api/personas/{id}/telefonos` · `PUT …/{idTelefono}` · `PATCH …/{idTelefono}/estado` | Personas · Editar |
| Vehículos | `POST /api/personas/{id}/vehiculos` · `PUT …/{idVehiculo}` · `PATCH …/{idVehiculo}/estado` | Personas · Editar **y** Vehículos · Añadir/Editar |
| Tipos de persona (por organización) | `GET\|POST /api/personas/tipos-persona` · `PUT …/{id}` · `PATCH …/{id}/estado` | Consultar / Añadir / Editar |
| Tipos de documento (globales) | `GET\|POST /api/personas/tipos-documento` · `PUT …/{id}` · `PATCH …/{id}/estado` | Consultar; escritura **solo administrador de plataforma** |

No hay `DELETE` de personas, teléfonos ni vínculos con vehículos: todo se inactiva.

Código: `PersonasController`, `CatalogosPersonaController`, `Services/ReglasPersona.cs`, `DTOs/PersonasAdministracionDto.cs`; frontend `src/features/personas/` (ruta `/personas`).

## Reglas aplicadas en el servidor
- **Organización del contexto** ([[ADR-024 Acciones por módulo y contexto de organización]]): la persona se crea con `IOrganizationContext.OrganizationId` (token o, para la plataforma, la organización seleccionada y validada por `ContextoOrganizacionMiddleware`). Un `idOrganizacion` en el cuerpo se ignora. Lecturas y escrituras de otra organización ⇒ 404 por el filtro global de EF Core y la validación de escritura del `DbContext`.
- **Identificación única** `(id_organizacion, id_tipo_documento, numero_documento_normalizado)` con índice único: verificación previa (409) y, ante doble envío o alta concurrente, la violación de unicidad se traduce en 409. Verificado con 5 altas paralelas ⇒ 1 fila. El mismo número con otro tipo de documento, o en otra organización, es otro registro.
- **Normalización del documento** (Propuesta, D-09): recortar espacios externos y mayúsculas; no se quitan ceros ni caracteres.
- **Formato del documento** por `tipo_documento.patron_validacion` (regex con límite de tiempo; patrón genérico si falta). Semilla: CC `^[0-9]{3,10}$`, TI `^[0-9]{6,11}$`, CE `^[A-Z0-9]{3,12}$`, PA `^[A-Z0-9]{5,20}$`, PPT `^[0-9]{5,15}$`, NIT `^[0-9]{6,10}(-?[0-9])?$`.
- **Obligatorios**: tipo de persona, tipo y número de documento, nombres, apellidos y correo (con formato). Opcionales: foto, observación, empresa de procedencia.
- **Tipos inactivos** no se aceptan en altas; una persona que ya tenía el tipo lo conserva al editar.
- **Teléfonos**: número guardado solo con dígitos (`+57` aparte); `+57` exige móvil `3XXXXXXXXX` o fijo `60XXXXXXXX`; otros indicativos, longitud E.164. Sin duplicados activos por persona y un solo principal activo (índices únicos parciales); marcar principal desmarca el anterior en la misma transacción. Quitar = inactivar.
- **Vehículos** (ver abajo): placa sin espacios ni guiones y en mayúsculas, 4–8 letras y números con al menos uno de cada (provisional, D-17); tipo y relación de listas cerradas.
- **Registrado por**: `persona.id_usuario_crea` se toma del token al crear (FK a `usuario` desde 2026-10-26); `PersonaEntrada` no tiene ese campo, así que un `idUsuarioCrea` enviado en alta o edición se ignora (verificado por prueba).
- **Foto** con [[ADR-013 Almacenamiento privado de archivos]] (implementación local provisional): tipo real por firma (PNG/JPEG/WebP), máximo 2 MB, fila `archivo` con organización y hash; descarga solo por API autenticada. Reemplazar o quitar conserva el archivo anterior (D-14).
- **Auditoría** (`auditoria`, misma transacción en altas): `Crear`, `Modificar` (anterior/nuevo), `Activar`/`Inactivar`, `CargarFoto`/`QuitarFoto`, `AgregarTelefono`/`ModificarTelefono`/`InactivarTelefono`, `AsociarVehiculo`/`ModificarVehiculo`/`DesasociarVehiculo`; tipos de persona y de documento con su propia entidad.
- **Tipos de persona iniciales** por organización: Empleado, Visitante, Contratista, Proveedor, Residente, Cliente — sembrados por la migración para las organizaciones existentes y al crear una organización desde la plataforma.

## Vehículos: criterio de la relación
El modelo existente ya define `persona_vehiculo` con PK `(id_organizacion, id_persona, id_vehiculo)` y `relacion`: es **N:M**, no 1:N. Se conservó:
- La placa es única por organización (`vehiculo`). Asociar una placa que ya existe **vincula el mismo vehículo** sin sobrescribir sus datos; si no existe, se crea.
- Editar desde una persona modifica los datos del vehículo compartido (la interfaz avisa si hay otras personas asociadas) y la relación con esa persona.
- Desasociar inactiva solo el vínculo; el vehículo, sus otros vínculos y su historial (`autorizacion_vehiculo`, `movimiento_vehiculo`) se conservan. Volver a asociarlo reactiva el vínculo.
- Los endpoints exigen también el módulo **Vehículos** (módulo opcional, CLAUDE.md §4).

## Relaciones de base de datos (migración `20261026180826_PersonasRelacionesYCatalogos`)

| Relación | Antes | Ahora |
|---|---|---|
| persona → organizacion | sin FK | FK `id_organizacion` |
| persona → archivo (foto) | sin FK | FK `id_archivo_foto` |
| persona → tipo_persona / tipo_documento | FK (compuesta / simple) | sin cambio |
| persona `(org, tipo_persona)` | **índice único** (solo una persona por tipo) | índice no único — corregido |
| persona_telefono → persona | sin FK | FK compuesta `(id_organizacion, id_persona)` |
| persona_vehiculo → persona / vehiculo | sin FK | FKs compuestas; AK `vehiculo(id_organizacion, id_vehiculo)` |
| vehiculo → organizacion | sin FK | FK |
| tipo_persona → organizacion | sin FK | FK; único `(id_organizacion, nombre)` |
| tipo_documento | sin unicidad | único `codigo`; columna `patron_validacion` |
| persona_telefono | — | columna `codigo_pais` (default `+57`); únicos parciales de número activo y principal |

Todas las FKs con `ON DELETE RESTRICT`. La migración no altera ni borra datos: agrega columnas, índices y FKs, y siembra con `ON CONFLICT`/`NOT EXISTS`. Aplicada en la base de trabajo `conecsia` el 2026-10-26. Ya existían FKs desde `autorizacion`, `visita` y `movimiento_acceso` hacia `persona(id_organizacion, id_persona)`, por lo que una persona con historial tampoco puede borrarse físicamente en la base.

## Verificación (2026-10-26)
- `PersonasTests`: 21 pruebas de integración contra PostgreSQL real (organización del contexto, duplicidad y concurrencia, formatos, tipos ajenos/inactivos, aislamiento A/B, permisos por acción y módulo de vehículos, plataforma vs. tipos de documento, siembra en organización nueva, foto, teléfonos, vehículos N:M, FKs en `pg_constraint`). Suite completa: **56/56**.
- `vitest`: 4 pruebas nuevas en `PersonasPage.test.tsx`; total **20/20**; `tsc -b` y `oxlint` sin hallazgos nuevos.
- Navegador (API :5192 sobre base desechable, frontend :5174): alta con «buscar primero», aviso de documento existente con espacios, detalle, teléfono normalizado y principal, vehículo con placa normalizada; auditoría verificada en BD.

## Corrección: foto no visible en detalle y edición (2026-10-26)
- **Causa:** el hook `useUrlObjeto` creaba la URL del blob en un `useMemo` y la revocaba en la limpieza de un `useEffect`. En desarrollo, `StrictMode` monta, desmonta y vuelve a montar los efectos: la limpieza revocaba la URL, el `useMemo` no la recreaba y el `<img>` quedaba apuntando a una URL inválida (detalle, edición y logo de organizaciones). El backend (almacenamiento, `id_archivo_foto`, DTO `tieneFoto`, `GET /foto` con permisos) funcionaba: lo confirmaba la prueba de integración de la foto.
- **Corrección:** la URL se crea y revoca en el mismo efecto (`administracion/utilidades.tsx`); prueba que reproduce el doble montaje.
- **Además:** la foto se descarga con `apiBlob` (`shared/api/http.ts`), que renueva la sesión ante un 401 como `apiFetch` (antes un token vencido dejaba la imagen sin cargar); «Descartar selección» ya no marca la foto guardada para eliminarla (antes «Quitar» sobre una foto recién elegida también borraba la actual); «Conservar foto actual» deshace un «Quitar»; el `<img>` tiene texto alternativo; el selector se reinicia para poder elegir el mismo archivo otra vez.

## Pendiente
> [!todo] Pendientes
> - **D-09**: patrones definitivos por documento, país emisor y personas sin documento estándar (hoy rechazadas por formato).
> - **D-17**: formato y ámbito de unicidad de placas (hoy por organización, formato genérico).
> - **D-13/D-14**: proveedor de almacenamiento, limpieza EXIF, retención de fotos reemplazadas.
> - HU-022 propone motivo obligatorio al inactivar: no existe columna; no se implementó (requiere decidir el campo).
> - Registro `tipo_documento` preexistente `DOC-001 · Cédula` en la base de trabajo: no se modificó; conviene inactivarlo si duplica a `CC`.
> - El `VehiculoController` scaffold (`/api/Vehiculo`) sigue con `DELETE` físico y sin reglas; corresponde a [[EP-16 Vehículos]].
> - Paginación del listado (hoy máximo 500 filas).
