---
tipo: adr
estado: implementado
area: seguridad
origen: Solicitud del usuario 2026-10-22 (gestión dinámica de permisos por módulo)
fuentes: [PRD v0.1]
relacionados: [ADR-004, ADR-006, ADR-020, ADR-021, D-16, D-22, D-23]
actualizado: 2026-10-22
tags: [conecsia, adr, seguridad, autorizacion]
---

# ADR-023 Acceso a módulos por rol en base de datos

## Contexto
- El acceso por módulo vivía en listas codificadas: permisos del token (`permission`) revisados a mano en unos pocos controladores (`AdministracionAuthorization`) y un menú del frontend con códigos de permiso fijos.
- Casi todos los controladores CRUD (`PersonaController`, `VisitaController`…) **solo exigían autenticación**: cualquier usuario autenticado podía invocarlos.
- El usuario pidió (2026-10-22) un catálogo `app_modulos` y una tabla `modulos_permisos` que asigne módulos a roles.

## Decisión (implementada)
1. **Catálogo global** `app_modulos` (no por organización): `codigo` estable (`^[a-z][a-z0-9_]*$`, único, inmutable tras crearse), `nombre`, `descripcion`, `ruta`, `icono`, `orden`, `activo`, `creado_en`.
2. **Asignación** `modulos_permisos`: `id_modulo_permiso`, `id_organizacion`, `id_rol`, `id_app_modulo`, `asignado_en`, `id_usuario_asigna`.
   - Único `(id_rol, id_app_modulo)`; índices `(id_organizacion, id_rol)` y `id_app_modulo`.
   - FK **compuesta** `(id_organizacion, id_rol)` → `rol` (se agregó la clave alterna `AK_rol_id_organizacion_id_rol`): el rol siempre pertenece a la organización de la asignación. FK a `app_modulos` (Restrict), `organizacion` (Restrict), `usuario` (SetNull). Cascade al borrar el rol.
   - Es `OrganizacionEntity`: hereda el filtro global por organización y la validación de escritura del `DbContext`.
3. **Denegación por defecto** con la política `[RequiereModulo(CodigosModulo.X)]` (`ModuloPolicyProvider` + `ModuloAuthorizationHandler`): el servidor consulta membresía activa → roles activos → `modulos_permisos` → módulo activo, para la organización activa. Aplicada a **55 controladores** de negocio y a las acciones no-`/me` de `UsuarioController`.
4. **Administrador general** (regla vigente: usuario 1, ahora centralizada en `UserAuthorization` y emitida como claim `platform_admin`): accede a todos los módulos activos sin asignaciones y es el único que crea, edita o activa/inactiva módulos del catálogo.
5. **Administrador de organización** = módulo `administracion` + permiso existente `roles.administrar`. Solo gestiona roles de su organización y **solo puede otorgar o retirar módulos que él mismo tiene** (evita escalamiento, análogo a `ValidarPermisos` de permisos).
6. **Caché** en memoria (5 min) por organización+usuario, invalidada al cambiar asignaciones de la organización o el catálogo (`ModuloAccesoCacheTokens`).
7. **Auditoría** en `auditoria` de cada alta, retiro, reemplazo y cambio del catálogo (actor, organización, valor anterior/nuevo, IP, correlación).

## Correspondencia controlador → módulo

| Módulo | Controladores |
|---|---|
| `dashboard` | Dashboard |
| `personas` | Persona, PersonaTelefono, PersonaVehiculo, TipoDocumento, TipoPersona, RestriccionAcceso, Archivo |
| `autorizaciones` | Autorizacion, AutorizacionDispositivo/Horario/Transicion/Vehiculo/Zona, TokenQr, InvitacionEnvio |
| `visitas` | Visita |
| `control_accesos` | MovimientoAcceso, Permanencia, SolicitudIdempotente |
| `vehiculos` | Vehiculo, MovimientoVehiculo |
| `dispositivos` | Dispositivo, TipoDispositivo, MovimientoDispositivo |
| `alertas` | Alerta, AlertaTransicion, Notificacion, NotificacionDestinatario |
| `sedes` | Sede, Area, AreaResponsable, Zona, ZonaEmpresarial |
| `auditoria` | Auditoria, Outbox |
| `configuracion` | ParametroOrganizacion, PlantillaNotificacion, Ciudad, TipoOrganizacion, Plan, PlanModulo, Modulo, OrganizacionModulo |
| `administracion` | Administracion, Rol, RolPermiso, Permiso, Membresia*, PlantillaRol*, Organizacion, Usuario (excepto `/me`) |

> [!note] Sin módulo (reglas propias): `AuthController` (anónimo), `UsuarioController /me*`, `AppModuloController`, `ModuloPermisoController`.

## Migración `20261022235936_AddAppModulosYModulosPermisos`
- Crea tablas, índices y la clave alterna de `rol`; **no borra ni altera** roles ni permisos.
- Siembra 12 módulos reales (solo con API o pantalla existentes; «Reportes» no tiene backend y no se sembró).
- Traslada asignaciones desde `rol_permiso` por prefijo de código (`personas.* → personas`, `usuarios.*|roles.*|permisos.* → administracion`, etc.; tabla completa en la migración). `ON CONFLICT DO NOTHING`.
- Conserva el dashboard para todos los roles activos existentes (antes no exigía permiso).
- **No trasladados:** permisos `saas.*` (plataforma; los cubre el administrador general) y cualquier prefijo no listado.
- Se corrigió la migración manual previa `20261022120000_AddBusinessZonesAndAuthorizationScopes` (argumentos posicionales que pasaban `"public"` como tipo de columna y columnas como esquema); nunca se había aplicado en ninguna base.

## Consecuencias
- Un rol sin asignación pierde acceso a endpoints que antes eran abiertos para cualquier autenticado: **cambio intencional** (denegación por defecto).
- Los permisos por acción (`roles.administrar`, `usuarios.crear`…) siguen vigentes donde ya existían; no se crearon permisos CRUD nuevos.
- Roles por sede o zona empresarial: el modelo actual no los tiene; el alcance de sede/zona sigue siendo responsabilidad de cada consulta.

## Limitaciones conocidas
- El administrador general solo gestiona roles de la organización activa de su token: el `DbContext` rechaza escrituras en otra organización.
- La organización activa es el primer claim `organization_id` (usuarios con varias membresías: pendiente de selector).
- Un administrador de organización puede retirarse a sí mismo el módulo `administracion` (sin protección contra auto-bloqueo).
- La caché es por instancia; con varias réplicas la invalidación no se propaga (máx. 5 min de desfase).
- Pendiente [[Decisiones pendientes|D-23]]: reemplazar la regla «usuario 1 = administrador general».
