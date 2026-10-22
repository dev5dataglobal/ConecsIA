---
tipo: adr
estado: aprobado
area: seguridad
origen: PRD §5.2, §12.5
fuentes: [PRD v0.1]
relacionados: [D-03, D-14, HU-006, HU-016, ADR-022]
actualizado: 2026-10-21
tags: [conecsia, adr, seguridad]
---

# ADR-006 Identidad y sesiones

> [!success] Estado: **Aprobado** el 2026-10-19 por el responsable del proyecto: **opción A aceptada**. Ver "Aprobación".
> Área: seguridad · Origen: PRD §5.2, §12.5

## Contexto
El PRD exige una solución estándar de identidad, pero proveedor, sesión, SSO y MFA están pendientes (D-03).

## Decisión propuesta
- Usuario (identidad) separado de Persona; vínculo opcional PersonaUsuario.
- Una identidad puede tener varias membresías de organización con roles independientes y selección explícita de contexto (el PRD lo permite; confirmar en D-03).
- Alta de usuarios por invitación administrativa, sin registro público.
- Contraseñas con derivación segura estándar, límite de intentos, restablecimiento seguro y revocación de sesiones al retirar acceso.
- Opciones a decidir:
  - A) ASP.NET Core Identity con cookie segura (SPA mismo sitio, BFF): menor complejidad inicial.
  - B) Proveedor OIDC (Keycloak, Entra ID, Auth0…) con tokens: facilita SSO/MFA futuros.
- **Recomendación del agente:** A con cookie `HttpOnly`/`SameSite` y anti-CSRF, diseñada para incorporar OIDC externo después.

## Alternativas consideradas
Implementación propia de autenticación: descartada.

## Consecuencias
Condiciona hosting del frontend ([[ADR-015 Frontend SPA con React, TypeScript y Tailwind]]) y conexión SignalR.

## Aprobación 2026-10-19
D-03 cerrada con todas las recomendaciones de [[Opciones para decidir D-03]]:

| Punto | Decisión |
|---|---|
| D-03a Proveedor | **Opción A aceptada:** ASP.NET Core Identity con cookie `HttpOnly`/`Secure`/`SameSite` y anti-CSRF, preparada para añadir OIDC externo más adelante. La opción B queda descartada por ahora. |
| D-03b Multi-membresía | Permitida, con selección explícita de la organización. |
| D-03c Sesión | 8 h de duración máxima, cierre tras 30 min de inactividad y sesiones simultáneas permitidas pero revocables. |
| D-03d Bloqueo | 5 intentos fallidos → 15 min de bloqueo, más un límite adicional por IP. |
| D-03e Contraseñas | Mínimo 12 caracteres, sin caducidad periódica; recuperación con token de un solo uso válido 30 min. |
| D-03f MFA / SSO | MFA no se implementa ahora (tarea de la Fase 3): opcional para usuarios y obligatorio para Superadministrador SaaS y Administrador de organización. SSO diferido hasta elegir un proveedor externo. |
| D-14 (parcial) | Los eventos de inicio de sesión se conservan durante el mismo plazo general de auditoría. |

La autenticación provisional ([[ADR-022 Autenticación de desarrollo provisional]]) queda limitada estrictamente a Development y a las pruebas; nunca se activa en producción.

> [!note] Recomendación del agente, Propuesta: rechazar las contraseñas comunes, sin exigir reglas de composición (NIST 800-63B). Estaba en la consulta pero no se repitió en el mensaje de aprobación; se implementa solo si se confirma.

## Implementación (2026-10-19)
- **Implementado y verificado:** piezas de ASP.NET Core Identity sobre el `Usuario` existente (`PasswordHasher`, cookie de autenticación, sello de seguridad), **sin** `IdentityDbContext` ni `UserManager`, para mantener el DbContext compartido ([[ADR-021 DbContext compartido y ensamblado de migraciones]]). *Elección técnica del agente: Propuesta, revisable.*
- Detalle en [[Estado de implementación]] § HU-006.

> [!warning] Actualización 2026-10-21
> Para el backend API implementado se usa JWT Bearer en lugar de cookie. La decisión de cookie queda pendiente de revisión formal; no debe interpretarse como comportamiento actual.

## Pendientes
- Plazo general de auditoría (D-14), que también rige los eventos de sesión.
- Proveedor externo para SSO (cuando se necesite).

