---
tipo: modulo
estado: implementado
fuentes: [PRD v0.1]
relacionados: [ADR-023, ADR-024, ADR-013, RN-10, D-16, D-24, D-25, D-26, D-27, D-28]
actualizado: 2026-10-24
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
- > [!todo] Perfiles protegidos del sistema: no definidos ([[Decisiones pendientes|D-24]]).

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
- El selector de organización aparece en la barra superior solo para el administrador de plataforma.
