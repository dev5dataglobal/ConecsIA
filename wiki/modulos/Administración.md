---
tipo: modulo
estado: implementado
fuentes: [PRD v0.1]
relacionados: [ADR-023, ADR-024, ADR-025, ADR-013, RN-10, D-16, D-24, D-25, D-26, D-27, D-28]
actualizado: 2026-10-26
tags: [conecsia, modulo, administracion]
---

# Administración (usuarios, perfiles, organizaciones, zonas empresariales)

Cuatro módulos de `app_modulos`, cada uno con ruta y acciones propias ([[ADR-024 Acciones por módulo y contexto de organización]]). Todo opera sobre la **organización activa** validada en el servidor.

## Usuarios — `/administracion/usuarios` (`usuarios`)
| Acción | Endpoint | Regla |
|---|---|---|
| Consultar | `GET /api/administracion/usuarios?buscar&estado&idRol`, `GET …/{idMembresia}` | Solo membresías de la organización activa |
| Añadir | `POST /api/administracion/usuarios` | Correo único global (409), contraseña ≥ 8, perfiles activos de la organización y asignables |
| Editar | `PUT …/{idMembresia}`, `PATCH …/{idMembresia}/estado` | Cambiar perfiles, estado y correo; no puedes inactivarte a ti mismo |
| Añadir o Editar | `GET …/perfiles-asignables` | Perfiles activos cuyas acciones no superan las del usuario actual |

- **Implementado:** inactivar = membresía `Inactiva` (no afecta otras organizaciones). Login rechazado con «El usuario está inactivo…». Las sesiones abiertas se cierran porque la membresía se valida en cada petición.
- Si el usuario pertenece a otras organizaciones, solo el administrador de plataforma puede cambiar su correo (es global).

## Perfiles — `/administracion/perfiles` (`perfiles`)
- `GET|POST /api/administracion/perfiles`, `GET|PUT …/{idRol}`, `PATCH …/{idRol}/estado`. Nombre único por organización (sin distinguir mayúsculas).
- Matriz: `GET|PUT /api/roles/{idRol}/modulos` con `{ asignaciones: [{ idAppModulo, consultar, anadir, editar }] }`. Ver la matriz requiere Consultar en perfiles y cambiarla, Editar. Se acepta `idsModulos` por compatibilidad (= las tres acciones).
- Modal «Módulos» ampliado (60rem): tabla con encabezado fijo, columna «Todas» y acciones que no tienes deshabilitadas. Por debajo de 720px cada módulo se muestra como tarjeta con botones de texto.
- **Tarjetas (2026-10-26):** rejilla `auto-fill` con mínimo `min(100%, 19rem)` (21rem desde 1500px), misma altura por fila con el pie alineado abajo, descripción a 3 líneas y acciones que bajan de línea en tarjetas estrechas. Verificado en escritorio y 375px sin scroll horizontal.
- **Acciones con color (2026-10-26):** `BotonIcono` usa tono por intención: Editar violeta tenue, Inactivar rojo tenue, Activar verde tenue (hover sólido). Se aplica también a las tablas de las demás secciones. Las confirmaciones existentes se conservan (no hay borrado físico: «eliminar» es inactivar).
- > [!todo] Perfiles protegidos del sistema: no definidos ([[Decisiones pendientes|D-24]]).

> [!note] Diagnóstico 2026-10-26 — «el usuario inicia sesión pero no recibe los permisos»
> - El servidor aplica bien la matriz: los accesos se leen de `modulos_permisos` por petición (caché de 5 min invalidada al guardar perfiles, usuarios o matriz). Verificado con pruebas de integración (`SesionTests`).
> - **Causa en los datos de la base de trabajo:** la matriz se configuró en el perfil 2 «Administrador Organización», pero el usuario 2 se creó con el perfil 1 «Administrador plataforma» (auditoría 2026-10-26 07:51), que solo tiene `dashboard`. Hay que asignarle el perfil correcto desde Usuarios; no se modificaron datos.
> - **Causa en la SPA (corregida):** la caché de React Query (`mis-modulos`, 60 s) sobrevivía al cerrar sesión y entrar con otro usuario en la misma pestaña, mostrando los módulos del usuario anterior. Ahora la caché se vacía al iniciar y al cerrar sesión.
> - Los claims `permission` del JWT ahora excluyen perfiles inactivos y se recalculan al renovar la sesión ([[ADR-025 Sesión renovable y recuperación de contraseña]]).

## Organizaciones — `/administracion/organizaciones` (`organizaciones`)
- **Campos:** tipo, plan, nombre, identificación, teléfono y correo obligatorios; dirección y logo opcionales; organización superior; estado. Nombre e identificación únicos (409). Teléfono `^\+?[0-9 ()-]{7,30}$`.
- **Plataforma:** lista y busca todas, crea, cambia plan, estado y organización superior. **Resto:** solo ve y edita los datos de su organización.
- **Logo** (`GET|POST|DELETE …/{id}/logo`): PNG, JPEG o WebP detectado por firma, máximo 2 MB. Se guarda en almacenamiento privado local (`Almacenamiento:RutaLocal`, por defecto `App_Data/archivos`) con fila en `archivo` (hash SHA-256). Reemplazar o quitar el logo conserva el archivo anterior. Es una implementación provisional de [[ADR-013 Almacenamiento privado de archivos]].
- Inactivar no borra usuarios, sedes, autorizaciones ni historial ([[Decisiones pendientes|D-16]] sigue abierta para los demás efectos operativos).

## Zonas empresariales — `/administracion/zonas-empresariales` (`zonas_empresariales`)
Ver [[Zona empresarial]].
- Una zona se crea **con su organización superior**: una organización completa con todos los campos, enlazada como `es_administradora`. Las empresas se asocian como vínculos no administradores.
- `GET /api/zonas-empresariales`, `GET …/{id}`, `GET …/empresas-disponibles?idZona`, `POST`, `PUT …/{id}` (datos de la zona, de la organización superior y lista de empresas) y `PATCH …/{id}/estado`. Inactivar la zona inactiva también su organización superior; las empresas siguen activas.
- Solo el administrador de plataforma crea, edita o inactiva. Las demás organizaciones ven las zonas en las que participan; la lista de empresas solo la ve la organización superior.

## Interfaz
- Pestañas por sección según `/api/app-modulos/mios` (que ahora incluye las acciones). Botones Nuevo/Editar/Inactivar según esas acciones; el servidor valida igual.
- Búsqueda con retardo, filtro por estado, carga, vacío, 403 distinto del error, confirmación previa a inactivar y aviso de éxito.
- **Filtro con selector propio (2026-10-26):** `shared/ui/Selector.tsx` (patrón listbox WAI-ARIA): flechas, Inicio/Fin, Enter/Espacio, letra inicial, Escape/Tab cierran sin cerrar el diálogo, clic fuera cierra y el foco vuelve al botón; la opción elegida se marca con ✓. Lo usan los filtros de las cuatro secciones y, desde 2026-10-26, todos los desplegables de la plataforma: tipo, plan y organización superior (Organizaciones), sede (Dashboard) y organización activa (barra superior). Variantes CSS `sel-campo`, `sel-shell` y `sel-oscuro`; estado inválido, marcador gris para «Selecciona…», apertura hacia arriba si no cabe dentro del modal y sin doble apertura dentro de un `<label>`. Ya no quedan `<select>` nativos.
- El selector de organización aparece en la barra superior solo para el administrador de plataforma.
