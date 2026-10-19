---
tipo: adr
estado: aprobado
area: seguridad
origen: Implementación de la primera vertical (HU-013)
fuentes: [PRD v0.1]
relacionados: [HU-006, HU-007, HU-013, D-03, ADR-005, ADR-006]
actualizado: 2026-10-19
tags: [conecsia, adr, seguridad]
---

# ADR-022 Autenticación de desarrollo provisional

> [!success] Estado: **Aprobado** el 2026-10-19, limitado **estrictamente a Development y a las pruebas**; nunca se activa en producción. Complementa a [[ADR-006 Identidad y sesiones]] (opción A), no la sustituye.

## Contexto
La primera vertical ([[Sedes]]) necesita un usuario autenticado para derivar organización, permisos y alcance (HU-007, [[ADR-005 Contexto de organización y autorización por permisos]]). La autenticación real (HU-006) está **bloqueada por D-03**. HU-006 ya admitía "un stub de autenticación solo para desarrollo local".

## Decisión propuesta
- Esquema ASP.NET Core `Desarrollo`: la identidad llega en la cabecera `X-Usuario-Desarrollo: <id de usuario>`. Solo afirma quién dice ser; el middleware de contexto verifica que el usuario exista y esté activo (si no, 401) y deriva la membresía.
- Se habilita con `Autenticacion:Desarrollo:Habilitada=true`. Si está habilitado fuera de `Development` o `Testing`, **la API no arranca** (prueba unitaria).
- `GET /api/v1/desarrollo/usuarios` lista usuarios sintéticos solo con el esquema habilitado (404 si no).
- La SPA guarda el usuario elegido en `sessionStorage` y lo envía en la cabecera; ante un 401 lo descarta.
- Todo lo demás (contexto, permisos, alcance, auditoría) es el mecanismo definitivo y no cambia al llegar HU-006: se sustituye solo el esquema de autenticación.

## Alternativas consideradas
- Implementar ya ASP.NET Core Identity con cookie: adelantaría D-03 sin decisión del negocio.
- Bloquear toda la vertical hasta D-03: impide verificar aislamiento, permisos y auditoría de punta a punta.

## Consecuencias
- Cualquiera con acceso al entorno local puede suplantar a cualquier usuario sintético: aceptable solo con datos sintéticos (D-14).
- La CI y Docker Compose lo habilitan; un despliegue real debe tenerlo deshabilitado (el arranque lo garantiza).

## Pendientes
D-03 cerrada el 2026-10-19. HU-006 agrega el esquema real (cookie de Identity); este esquema se conserva solo para Development/Testing. La protección de arranque (la API no inicia con el esquema habilitado en otros entornos) es obligatoria.
