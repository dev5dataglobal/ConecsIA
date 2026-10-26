---
tipo: adr
estado: implementado
area: seguridad
origen: Solicitud del usuario 2026-10-24 (ajustes de administración y organizaciones superiores)
fuentes: [PRD v0.1]
relacionados: [ADR-004, ADR-005, ADR-013, ADR-023, D-03, D-16, D-23, D-24, D-25, D-26, D-27, D-28]
actualizado: 2026-10-24
tags: [conecsia, adr, seguridad, autorizacion]
---

# ADR-024 Acciones por módulo y contexto de organización

> [!note] Estado: **Implementado** el 2026-10-24 por solicitud explícita del usuario. Amplía [[ADR-023 Acceso a módulos por rol en base de datos]]; no lo reemplaza.

## Contexto
- [[ADR-023 Acceso a módulos por rol en base de datos]] daba acceso «todo o nada» por módulo. El usuario pidió (2026-10-24) que cada perfil tenga, por módulo, las acciones **Consultar**, **Añadir** y **Editar**, validadas en el backend.
- La pantalla de Administración mezclaba organizaciones (el administrador general listaba usuarios y perfiles de todas a la vez) y dependía de permisos `usuarios.*`/`roles.*` que no estaban sembrados.
- Se pidió que solo el administrador de plataforma pueda cambiar de organización y que, ya seleccionada, solo vea esa.

## Decisión (implementada)
1. **Acciones por asignación.** `modulos_permisos` gana `puede_consultar`, `puede_anadir` y `puede_editar` (boolean). Las filas existentes quedaron con las tres en `true` (se conserva el acceso que tenían).
2. **Sin acciones implícitas.** Ninguna acción concede otra: Añadir o Editar no permiten Consultar. Inactivar/reactivar = **Editar**. Una asignación sin acciones equivale a no tener el módulo. Con varios perfiles, las acciones se **unen** por módulo.
3. **Validación en el servidor.** `[RequiereModulo(codigo)]` deduce la acción del verbo HTTP (GET/HEAD → Consultar, POST → Añadir, PUT/PATCH/DELETE → Editar) en los 55 controladores existentes; `[RequiereModulo(codigo, AccionModulo.X)]` la fija por endpoint y prevalece sobre la deducida para el mismo módulo.
4. **Módulos de administración separados.** `administracion` se dividió en `usuarios`, `perfiles`, `organizaciones` y `zonas_empresariales` (cada uno con ruta propia). Los perfiles que tenían `administracion` recibieron los cuatro con las tres acciones; `administracion` quedó **inactivo** (no se borró). Controladores genéricos remapeados: Membresía* y Usuario → `usuarios`; Rol, RolPermiso, Permiso, PlantillaRol* → `perfiles`; Organizacion → `organizaciones`.
5. **Anti-escalamiento.** Fuera del administrador de plataforma: solo se otorgan o retiran acciones que uno mismo tiene en ese módulo, y solo se asignan a usuarios perfiles cuyas acciones están contenidas en las propias.
6. **Contexto de organización.**
   - Por defecto, la organización activa es la primera membresía activa del token, en una organización activa.
   - Solo el administrador de plataforma (claim `platform_admin`, ver [[Decisiones pendientes|D-23]]) puede enviar `X-Organizacion-Id`; `ContextoOrganizacionMiddleware` la valida **en cada petición** (existe y está activa) y la guarda solo en `HttpContext.Items` (no persiste entre peticiones ni usuarios).
   - Cualquier otro usuario que envíe una organización distinta de la suya recibe **403**. Ningún endpoint de administración acepta la organización por ruta, filtro ni cuerpo; un `idOrganizacion` en el cuerpo se ignora.
   - En el frontend la selección vive en `sessionStorage` asociada al `sub` del usuario; se descarta al cerrar sesión y no la hereda otro usuario. Al cambiarla se descarta toda la caché de consultas.
7. **Escrituras entre organizaciones.** El `DbContext` sigue rechazando escrituras fuera de la organización activa. Las únicas excepciones son operaciones de plataforma ya autorizadas (vincular empresas a una zona) mediante el ámbito explícito `PermitirEscrituraEn([...])`. `UnauthorizedAccessException` ahora responde **403**, no 500.
8. **Inactivación.**
   - Inactivar un **usuario** inactiva su membresía en la organización activa (no afecta otras) y cierra sus sesiones (validación por petición en `OnTokenValidated`).
   - Inactivar una **organización** bloquea el ingreso y las sesiones de sus usuarios; no borra nada.
   - Inactivar un **perfil** retira sus permisos de inmediato (caché invalidada); conserva sus asignaciones.

## Consecuencias
- Los permisos por código (`usuarios.*`, `roles.*`, `saas.*`) ya no gobiernan los endpoints de administración; siguen en `rol_permiso` para compatibilidad y se pueden editar con `IdPermisos` del perfil. El script `backend/scripts/seed-permisos-perfil-1.sql` (2026-10-24) quedó sin efecto práctico.
- Un POST que en realidad consulta (p. ej. una búsqueda por POST) exigiría Añadir: debe declarar `[RequiereModulo(x, AccionModulo.Consultar)]`. Hoy no hay ninguno.
- Las pantallas de administración nunca muestran datos de varias organizaciones a la vez. La única excepción es el **catálogo de organizaciones**, que para el administrador de plataforma lista todas porque es su objeto de trabajo.

## Limitaciones
- Perfiles protegidos: el modelo no define perfiles reservados, así que no se bloquea editar ni inactivar ninguno → [[Decisiones pendientes|D-24]].
- Caché de accesos por instancia (5 min) igual que en ADR-023.
- La organización activa sigue siendo la primera membresía para usuarios con varias (selector solo para plataforma) → [[Decisiones pendientes|D-03]].
