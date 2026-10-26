---
tipo: adr
estado: implementado
area: seguridad
origen: PRD §5.2, §12.5; solicitud del usuario 2026-10-26
fuentes: [PRD v0.1]
relacionados: [ADR-006, ADR-023, ADR-024, D-03, D-21, D-29, D-30]
actualizado: 2026-10-26
tags: [conecsia, adr, seguridad]
---

# ADR-025 Sesión renovable y recuperación de contraseña

> [!info] Estado: **Implementado 2026-10-26** a pedido del usuario. Los detalles de diseño son **Propuesta del agente**, revisables. Vigencias sujetas a [[Decisiones pendientes|D-29]]; envío de correo en producción, [[Decisiones pendientes|D-30]].

## Contexto

- El backend emite un JWT corto (`JWT:DurationInMinutes`, ≤ 120) que la SPA guardaba solo en memoria: al recargar o reabrir el navegador se perdía la sesión, y «Recordarme» / «¿Olvidaste tu contraseña?» estaban deshabilitados.
- [[ADR-006 Identidad y sesiones]] (D-03c, **Base aprobada**) fija sesión de 8 h máximo, cierre tras 30 min de inactividad y sesiones simultáneas revocables, con cookie `HttpOnly`/`Secure`/`SameSite`.
- Los permisos por módulo ya se validan en cada petición contra la BD ([[ADR-023 Acceso a módulos por rol en base de datos]]); los claims `permission` del JWT solo se recalculaban al iniciar sesión.

## Decisión (implementada)

| Aspecto | Implementado |
|---|---|
| Token de acceso | JWT corto en el cuerpo de la respuesta; la SPA lo guarda **solo en memoria** (nunca `localStorage`/`sessionStorage`). |
| Sesión renovable | Token opaco de 256 bits en la cookie `conecsia_sesion`: `HttpOnly`, `SameSite=Strict`, `Path=/api/auth`, `Secure` fuera de Development (y en Development si llega por HTTPS). La BD guarda solo su SHA-256 (tabla `sesion_refresco`). |
| «Recordarme» | Marcado ⇒ cookie persistente (`Expires`) que sobrevive al cierre del navegador. Sin marcar ⇒ cookie de sesión del navegador. **En ambos casos** la vigencia absoluta es `Sesion:HorasMaximasSesion` (8 h, D-03c). |
| Renovación | `POST /api/auth/renovar` (exige cabecera `X-Conecsia-Sesion`, que fuerza preflight CORS): rota la cookie (revocación condicional, sin doble rotación), conserva el vencimiento absoluto y **recalcula membresías, perfiles y permisos**. Una cookie recién rotada se acepta 30 s más sin rotar (pestañas simultáneas). |
| Invalidación | La sesión guarda el `sello_seguridad` del usuario: cambiar o restablecer la contraseña la invalida. Usuario inactivo o sin membresía activa ⇒ la renovación falla y revoca. `POST /api/auth/cerrar-sesion` revoca y borra la cookie. |
| SPA | Al cargar, intenta renovar antes de evaluar rutas; ante un `401` reintenta una vez tras renovar (una sola renovación en vuelo). Al iniciar y cerrar sesión vacía la caché de React Query. |
| Recuperación | `POST /api/auth/recuperar-contrasena` responde **202 con el mismo mensaje** exista o no la cuenta; token opaco de un solo uso (hash en `recuperacion_contrasena`), vence en `Sesion:MinutosRecuperacion` (30), un enlace nuevo anula los pendientes. Límite: 5 solicitudes por IP cada 15 min (política `recuperacion`). |
| Restablecer | `POST /api/auth/restablecer-contrasena`: consumo condicional del token (no se usa dos veces bajo concurrencia), regla `PoliticaContrasena` (≥ 8, mayúscula, minúscula, número; la misma del cambio de contraseña), nuevo sello de seguridad, revoca todas las sesiones renovables, desbloquea la cuenta y audita. |
| Enlace | `{Sesion:UrlFrontend}/restablecer-contrasena#token=…`: el token va en el fragmento (no llega a logs de servidor ni a `Referer`) y la SPA lo retira de la barra de direcciones. |
| Correo | `IEnviadorCorreo` → `SmtpEnviadorCorreo` (sección `Correo`). **Sin `Correo:Host`/`Correo:Remitente` o sin `Sesion:UrlFrontend` el endpoint responde 503** con un mensaje claro y no crea tokens: nunca se simula un envío. Un fallo del servidor SMTP se registra en el log y no se revela al cliente. |

## Alternativas consideradas

- Mantener solo JWT en memoria (sin renovación): «Recordarme» imposible sin guardar tokens en almacenamiento del navegador ⇒ descartado.
- Guardar el JWT en `localStorage`: prohibido por la solicitud y por ADR-006 ⇒ descartado.
- Cookie de autenticación completa (BFF, opción A de ADR-006): mayor cambio (todas las peticiones con cookie + anti-CSRF). La cookie de renovación acotada a `/api/auth` es un paso intermedio compatible.

## Consecuencias

- Los cambios de perfil se aplican **de inmediato** en los endpoints protegidos por módulo (consulta por petición con caché invalidable) y en el menú al refrescar `mis-modulos`; los claims del JWT se actualizan **al renovar**.
- Recargar la página ya no cierra la sesión (antes sí), dentro de la vigencia de 8 h.

> [!warning] Pendiente
> - El cierre por **30 min de inactividad** (D-03c) **no está implementado** (tampoco lo estaba antes). Ver D-29.
> - Ampliar «Recordarme» más allá de 8 h requiere decisión (D-29).
> - Correo de producción (proveedor, remitente, SPF/DKIM) sin definir (D-30).
