---
tipo: consulta
estado: propuesta
fuentes: [PRD v0.1]
relacionados: [D-03, D-14, HU-006, HU-016, ADR-006, ADR-022]
actualizado: 2026-10-17
tags: [conecsia, seguridad, identidad]
---

# Opciones para decidir D-03

> [!warning] Todo lo que sigue es **Propuesta** del agente. Ninguna opción está decidida; las cierra el responsable del proyecto en [[Decisiones pendientes]] y en [[ADR-006 Identidad y sesiones]]. Las cifras son puntos de partida usuales (guías OWASP/NIST), no requisitos del PRD.

| Punto | Opciones | Recomendación | Por qué |
|---|---|---|---|
| **D-03a** Proveedor | A) ASP.NET Core Identity con cookie `HttpOnly`/`Secure`/`SameSite=Strict` y anti-CSRF; SPA y API en el mismo sitio (nginx ya lo hace). B) OIDC externo (Keycloak, Entra ID, Auth0) con flujo de código + PKCE. | **A**, preparada para añadir OIDC externo como segundo esquema | Menos infraestructura y costo; sin tokens en el navegador. El reemplazo toca solo `AgregarSeguridad` (ver [[Estado de implementación]]). B solo compensa si un cliente exige SSO corporativo desde el inicio. |
| **D-03b** Multi-membresía | 1) Una organización por identidad. 2) Varias, con selección explícita. | **2** | El PRD lo permite y el backend ya lo soporta: `X-Organizacion-Id` selecciona y, si falta, devuelve 403 `ORGANIZACION_NO_SELECCIONADA`. Falta el selector en la UI. Elegir 1 obligaría a duplicar cuentas para un mismo usuario. |
| **D-03c** Sesión | Duración absoluta, expiración por inactividad y sesiones simultáneas. | 8 h absolutas (un turno), 30 min de inactividad deslizante y sesiones simultáneas permitidas pero revocables | Encaja con turnos de portería. Revocación inmediata con sello de seguridad, ya cubierta por las pruebas de contrato. |
| **D-03d** Bloqueo | Umbral y duración. | 5 intentos fallidos → 15 min de bloqueo, más limitación de tasa por IP; respuesta 423 genérica | Mitiga la fuerza bruta sin permitir que un tercero bloquee cuentas indefinidamente. |
| **D-03e** Contraseñas | Reglas y expiración del token de restablecimiento. | Mínimo 12 caracteres, sin reglas de composición, rechazo de contraseñas comunes, sin caducidad periódica; token de un solo uso de 30 min | Recomendación NIST 800-63B. |
| **D-03f** MFA y SSO | En el MVP o diferidos. | MFA TOTP **opcional** en el MVP y **obligatorio** para Superadministrador SaaS y Administrador de organización; SSO diferido | Los roles con más privilegio acceden a datos personales de otros. El SSO depende de B o de añadir OIDC después. |
| **D-14 (parcial)** Retención de eventos | Plazo de conservación. | Conservar los eventos de autenticación igual que el resto de la auditoría hasta que D-14 fije un plazo general | Evita una política aparte; no se borra nada mientras tanto. |

## Consecuencias si se acepta la recomendación
- Se aprueba [[ADR-006 Identidad y sesiones]] con la opción A y [[ADR-022 Autenticación de desarrollo provisional]] queda solo para Development/Testing.
- La Fase 2 de `goal_authentication.md` se desbloquea; MFA (D-03f) se convierte en una tarea nueva de la Fase 3.
- HU-016 (invitación) puede refinarse sobre Identity.
