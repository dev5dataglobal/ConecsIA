---
tipo: modulo
estado: implementado
fuentes: [PRD v0.1]
relacionados: [HU-047, HU-049, RF-16, RF-17, CA-01, CA-20, CA-23, CA-24, ADR-026, ADR-014, D-10, D-17, D-31, D-32, D-33]
actualizado: 2026-10-26
tags: [conecsia, modulo, vehiculos, equipos, autorizaciones]
---

# Autorizaciones de vehículos y dispositivos

Gestión de dispositivos de las [[Persona|personas]] y de las autorizaciones de ingreso de vehículos y dispositivos. Diseño: [[ADR-026 Autorización de ingreso de vehículos y dispositivos]]. Épicas: [[EP-16 Vehículos]], [[EP-17 Equipos externos]]. Implementado y verificado por pruebas el 2026-10-26.

## Dispositivos de una persona (Implementado, Verificado)

| Operación | Endpoint | Acción de módulo |
|---|---|---|
| Tipos de dispositivo (por organización) | `GET\|POST /api/dispositivos/tipos` · `PUT …/{id}` · `PATCH …/{id}/estado` | Dispositivos · Consultar / Añadir / Editar |
| Listar dispositivos de la persona | `GET /api/personas/{id}/dispositivos` | Personas · Consultar **y** Dispositivos · Consultar |
| Crear | `POST /api/personas/{id}/dispositivos` | Personas · Editar **y** Dispositivos · Añadir |
| Editar / activar-inactivar | `PUT …/{idDispositivo}` · `PATCH …/{idDispositivo}/estado` | Personas · Editar **y** Dispositivos · Editar |

- Relación **1:N**: un dispositivo tiene una persona responsable (`id_persona_responsable`, FK compuesta con la organización), tomada de la ruta; `idPersonaResponsable`/`idOrganizacion` en el cuerpo se ignoran.
- Obligatorios: tipo (activo, de la organización) e **identificador** (serial, código de inventario…). Opcionales: serial, marca, modelo, color, descripción.
- Identificador normalizado (recortar, espacios internos simples, mayúsculas) y **único por organización** — provisional, [[Decisiones pendientes|D-17]].
- Sin borrado: inactivar conserva autorizaciones y movimientos. Auditoría `Crear`/`Modificar`/`Activar`/`Inactivar` (entidad `Dispositivo`) y `CrearTipo`/… (entidad `TipoDispositivo`).
- Se retiraron los controladores scaffold `DispositivoController`, `TipoDispositivoController`, `AutorizacionDispositivoController` y `AutorizacionVehiculoController`, que permitían escribir o borrar físicamente sin estas reglas.

## Autorizaciones de objetos (Implementado, Verificado)

| Operación | Endpoint | Acción |
|---|---|---|
| Listar con filtros | `GET /api/autorizaciones-objeto?idPersona=&idSede=&estado=&tipoObjeto=&desde=&hasta=&buscar=` | Autorizaciones · Consultar |
| Detalle con historial | `GET /api/autorizaciones-objeto/{id}` | Autorizaciones · Consultar |
| Sedes y áreas del formulario | `GET /api/autorizaciones-objeto/opciones` | Autorizaciones · Consultar |
| Solicitar (queda Pendiente) | `POST /api/autorizaciones-objeto` | Autorizaciones · Añadir + Añadir del módulo del objeto |
| Aprobar / rechazar / revocar | `POST …/{id}/aprobar\|rechazar\|revocar` (`{ motivo }`) | Autorizaciones · Editar |

Reglas, estados, vigencia y concurrencia: ver [[ADR-026 Autorización de ingreso de vehículos y dispositivos]]. La autorización del objeto **no** autoriza a la persona.

## Interfaz
- Detalle de persona (`/personas`): sección **Dispositivos** con alta, edición, inactivación con confirmación, creación rápida de tipo y estados de carga/vacío/error; muestra **Registrado por** (correo y fecha).
- `/autorizaciones` (módulo `autorizaciones`; la migración fija su `ruta`): listado con búsqueda y filtros (estado, tipo de objeto, sede, periodo), solicitud con búsqueda de persona y objetos activos de esa persona, detalle con historial y decisiones (motivo obligatorio para rechazar/revocar). Los botones se muestran según acciones del perfil, pero decide el servidor.

## Base de datos (migración `20261026224617_DispositivosYAutorizacionesObjeto`)

| Cambio | Detalle |
|---|---|
| `dispositivo` | columna `id_usuario_crea` (FK `usuario`); FKs compuestas a `persona` y `tipo_dispositivo`; FK `organizacion`; único `(id_organizacion, codigo_identificador)`; AK `(id_organizacion, id_dispositivo)`; longitudes 60 en identificador/serial |
| `tipo_dispositivo` | FK `organizacion`; único `(id_organizacion, nombre)`; AK `(id_organizacion, id_tipo_dispositivo)` |
| `autorizacion_objeto` | nueva (ver ADR-026), 4 CHECK, 9 FKs |
| `autorizacion_objeto_transicion` | nueva, append-only |
| `auditoria` | trigger `tr_auditoria_inmutable` (rechaza UPDATE/DELETE) |
| `persona` | FK `id_usuario_crea → usuario` |
| `app_modulos` | `ruta = '/autorizaciones'` para `autorizaciones` si estaba vacía |

> [!note] Aplicada en `conecsia` el 2026-10-26 y verificada en navegador (base desechable)
> Reduce a 60 caracteres `dispositivo.codigo_identificador`/`serial`: revisar datos antes de aplicar. Aplicar con `dotnet ef database update --project backend/Infrastructure --startup-project backend/ApiConecsia`.

## Verificación (2026-10-26)
- Backend: `DispositivosAutorizacionesObjetoTests` (13 pruebas de integración contra PostgreSQL real): registrador desde el token, CRUD e inactivación, obligatorios/duplicados/tipo ajeno, aislamiento A/B, módulo deshabilitado ⇒ 403, ciclo completo con actores e historial, motivo obligatorio y transiciones inválidas (409), vencidas/superpuestas/objeto o persona inactivos, vehículo sin vínculo, filtros, inmutabilidad (UPDATE/DELETE ⇒ excepción), decisiones concurrentes ⇒ una sola.
- Frontend (`vitest`): foto en detalle y edición, vista previa, validación de formato/tamaño, descarte que conserva la foto, dispositivos (lista, validación, alta sin ids sensibles), módulo deshabilitado, página de autorizaciones (listado, filtros, rechazo con motivo, sin permisos, validación de solicitud).
- Navegador 2026-10-26: flujo completo verificado (ver [[log]]).

## Pendiente
> [!todo] Pendientes
> - [[Decisiones pendientes|D-31]] modelo separado vs. PRD; [[Decisiones pendientes|D-32]] matriz de quién solicita/decide/cancela; [[Decisiones pendientes|D-33]] efecto de inactivar objeto/persona sobre aprobadas.
> - Movimientos de vehículos y equipos (HU-048, HU-050) y validación conjunta persona + objeto en la entrada.
> - Paginación (máx. 500 filas) y exportación.
