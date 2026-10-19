---
tipo: implementacion
estado: aprobado
fuentes: [PRD v0.1]
relacionados: [HU-006, HU-007, HU-016, D-03, D-14, ADR-005, ADR-006, ADR-022]
actualizado: 2026-10-17
tags: [conecsia, goal, seguridad]
---

# Goal: Autenticación y sesión de usuario (HU-006)

**Objetivo:** reemplazar la autenticación provisional ([[ADR-022 Autenticación de desarrollo provisional]]) por autenticación real, sin tocar el contexto, los permisos, el alcance ni la auditoría, que ya son definitivos ([[ADR-005 Contexto de organización y autorización por permisos]]).

**Fuente:** `wiki/backlog/EP-02 Tenancy, identidad y auditoría base.md` § HU-006 · PRD §5.2 (reglas 1, 7 y 8) y §12.5.

## Protocolo de cada iteración

1. Relee este archivo.
2. Revisa `git status` (si la carpeta todavía no es un repositorio, anótalo en el log).
3. Orienta la sesión: `wiki/index.md`, la entrada HU-006, [[ADR-006 Identidad y sesiones]] y `Decisiones pendientes.md`.
4. Implementa **una** tarea sin marcar: la primera cuyas dependencias estén cumplidas.
5. Ejecuta las pruebas: `dotnet test backend/ConecsIA.slnx` y, si se tocó el frontend, `npm test` y `npm run build` en `frontend/`.
6. Marca la casilla **solo con evidencia** y anótala al lado: prueba, archivo o salida.
7. Agrega la entrada a `wiki/log.md` y actualiza `Estado de implementación.md` y `tablero-scrum.md`.
8. **Detente** si la tarea depende de una decisión de producto sin resolver. Regístrala como D-xx y pide su resolución.

---

## Fase 0 — Decisiones (bloqueante: sin esto no se implementa nada de las fases 2 a 6)

> [!warning] Estas casillas las cierra el **responsable del proyecto**, no el agente. El agente solo registra la respuesta.

- [x] **D-03a** Proveedor: A) ASP.NET Core Identity con cookie `HttpOnly`/`SameSite` y anti-CSRF (recomendada en ADR-006) · B) OIDC externo (Keycloak, Entra ID, Auth0…).
- [x] **D-03b** Multi-membresía: ¿una identidad puede pertenecer a varias organizaciones? Si es así, ¿cómo se selecciona el contexto?
- [x] **D-03c** Política de sesión: duración absoluta, expiración por inactividad y número de sesiones simultáneas.
- [x] **D-03d** Bloqueo por intentos: umbral y duración del bloqueo.
- [x] **D-03e** Contraseñas: longitud mínima y reglas; expiración del token de restablecimiento.
- [x] **D-03f** MFA y SSO: ¿entran en el MVP o se difieren?
- [x] **D-14 (parcial)** Retención de los eventos de sesión y de autenticación.
- [x] ADR-006 aprobado con las respuestas anteriores. — *2026-10-19: aprobado por el responsable; opción A aceptada; ADR-022 limitado a Development y pruebas. Evidencia: `wiki/decisiones/ADR-006 Identidad y sesiones.md` § Aprobación.*

## Fase 1 — Preparación sin decisión (puede avanzar ya)

- [x] Inicializar el repositorio git, con su `.gitignore`, para poder trazar cada iteración. — *2026-10-17: `git init -b main`; el `.gitignore` existente excluye `.env`, `bin/`, `obj/`, `node_modules/` y `dist/` (comprobado con `git add -A -n`, 255 archivos). Sin commit inicial todavía. Línea base: 151 pruebas en verde.*
- [x] Inventariar la autenticación actual (esquema `Desarrollo`, middleware de contexto, `GET /api/v1/me`, `sessionStorage` en la SPA) y documentar el punto exacto de sustitución en `Estado de implementación.md`. — *2026-10-17: sección «Punto de sustitución de la autenticación» en `wiki/implementacion/Estado de implementación.md`.*
- [x] Prueba de arquitectura: ningún controlador ni servicio depende del esquema `Desarrollo` (solo la configuración de autenticación). — *2026-10-17: `ArchitectureTests/AutenticacionTests.cs` (8 casos, en verde). Una mutación con una llamada desde el middleware la hace fallar. Límite: no detecta un `typeof` aislado ni la constante de cabecera, que se compila en línea.*
- [x] Prueba de integración que fije el contrato actual: un usuario inactivo o sin membresía recibe 401/403. Debe seguir pasando tras el cambio. — *2026-10-17: `IntegrationTests/ContratoAutenticacionTests.cs` (sin membresía → 403 `SIN_MEMBRESIA`; membresía inactivada → la siguiente petición da 403; usuario inactivado → la siguiente da 401) más las de `SedesTests` (sin autenticación → 401, inactivo → 401). PostgreSQL real; suite completa 162/162.*

## Fase 2 — Backend: inicio y cierre de sesión (requiere la Fase 0)

- [x] Modelo de credencial y migración EF Core (hash estándar, sello de seguridad, contador de intentos, bloqueo hasta). — *2026-10-19: `Usuario` + migración `CredencialUsuario` (rellena el sello de los usuarios existentes), aplicada en PostgreSQL real; `UsuarioCredencialTests` 4/4.*
- [x] `POST /api/v1/auth/login`: 401 con mensaje genérico, sin revelar si el usuario existe. — *`AutenticacionController` + `ServicioIngreso`; `SesionTests.Contrasena_incorrecta_y_usuario_inexistente_responden_igual` (mismo cuerpo, sin cookie; hash señuelo para igualar tiempos).*
- [x] `POST /api/v1/auth/logout`. — *`SesionTests.Cerrar_sesion_retira_la_cookie_y_queda_auditado`. Límite: la cookie copiada antes del cierre sigue válida hasta 30 min (sesión sin estado en el servidor); ver deuda.*
- [x] Cookie `HttpOnly`/`Secure`/`SameSite` y anti-CSRF (si se elige la opción A). — *`AutenticacionCookie` (`SameSite=Strict`, 30 min deslizante, 8 h absolutas) y `MiddlewareCsrf` + `GET /api/v1/auth/csrf`; pruebas de atributos de la cookie, escritura sin token → 403 `CSRF_INVALIDO`, con token → 201 y token de otra sesión → 403.*
- [x] Usuario inactivo o sin membresía activa no obtiene sesión útil. — *Inactivo → 401 en el inicio de sesión; sin membresía → inicia sesión pero `/me` da 403; inactivar o cambiar la contraseña revoca la sesión en la siguiente petición (sello de seguridad).*
- [x] `GET /api/v1/me` funciona con la sesión real y devuelve las membresías activas. — *`SesionTests.Credenciales_validas_emiten_cookie_protegida_y_me_devuelve_el_contexto`. Suite completa 177/177 (2026-10-19).*

## Fase 3 — Backend: bloqueo, revocación y restablecimiento

- [ ] MFA TOTP: opcional para usuarios y obligatorio para Superadministrador SaaS y Administrador de organización (D-03f). — *⏸ Detenido 2026-10-19: la aprobación dice «no se implementa ahora». Confirmar si se aborda en esta fase o se difiere.*
- [ ] Límite adicional de intentos por IP (D-03d). — *⏸ Detenido 2026-10-19: falta el valor (intentos por ventana) y confirmar los proxies de confianza para obtener la IP real detrás de nginx. Registrado como D-21.*
- [x] Registro de sesiones en el servidor: revocar una sesión concreta y que el cierre de sesión invalide la cookie también en el servidor (D-03c). — *2026-10-19: tabla `sesion_usuario` (migración `SesionesUsuario`), claim de sesión en la cookie, `GET /api/v1/auth/sesiones` y `DELETE /api/v1/auth/sesiones/{id}`. Pruebas: revocar una sesión deja las demás activas; una copia de la cookie no sirve tras cerrar sesión; la sesión de otro usuario da 404.*

- [x] Bloqueo temporal al superar el umbral (D-03d); 423 o 429 según ADR-020. — *423 `CUENTA_BLOQUEADA`; `UsuarioCredencialTests` y `SesionTests.Cinco_fallos_bloquean_la_cuenta…`, con auditoría `sesion.bloquear`.*
- [x] Revocación: al inactivar el usuario o la membresía, la siguiente petición protegida se rechaza (sello de seguridad, PRD §5.2.8). — *Usuario: `SesionTests.Inactivar_al_usuario…` (401). Membresía: `ContratoAutenticacionTests.Inactivar_la_membresia…` (403 en cada petición, porque el middleware no usa caché).*
- [ ] `POST /api/v1/auth/password/reset-request`: respuesta idéntica exista o no el usuario. — *⏸ Detenido: el token se entrega por correo y el proveedor, el remitente y el texto dependen de D-12; además, el envío depende del outbox (ADR-012, en Propuesta).*
- [ ] `POST /api/v1/auth/password/reset`: token de un solo uso con expiración corta (D-03e); invalida las sesiones previas.
- [ ] Auditoría de inicio y cierre de sesión, fallos, bloqueos, restablecimiento y revocación, sin contraseñas ni tokens. — *2026-10-21: parcial. Auditados `sesion.iniciar` (exitoso y denegado: credenciales inválidas, bloqueado), `sesion.bloquear`, `sesion.cerrar` y `sesion.revocar` en `ServicioIngreso`; sin secretos según `SesionTests.La_auditoria_de_sesion_no_contiene_contrasenas_ni_cookies`. Falta el restablecimiento (detenido por D-12).*

## Fase 4 — Frontend

- [x] Pantalla de inicio de sesión con error genérico. — *2026-10-20: `IngresarPage` con correo y contraseña; el mensaje lo redacta el servidor; la contraseña se limpia tras cada intento. `SesionReal.test.tsx` (ingreso con CSRF; error genérico).*
- [x] Cierre de sesión. — *`apiSesion.cerrarSesion` → `POST /auth/logout` con token (revoca la sesión en el servidor). `SesionReal.test.tsx`.*
- [ ] Solicitud y confirmación de restablecimiento. — *⏸ Detenido: depende del backend de restablecimiento (D-12).*
- [x] Manejo global de 401 (redirige al inicio de sesión) y de 423/429. — *401 en cualquier petición → olvida la sesión y vuelve a `/ingresar` (evento global); 423 mostrado; reintento único ante `CSRF_INVALIDO`. `SesionReal.test.tsx`. El 429 se mostrará igual cuando exista (D-21).*
- [x] Retirar el selector de usuario de desarrollo cuando la autenticación real esté activa. — *El selector solo aparece si `GET /api/v1/desarrollo/usuarios` responde (404 fuera de Development/Testing). `SesionReal.test.tsx`. Frontend: 28/28, typecheck, lint, formato y build en verde.*

## Fase 5 — Pruebas y criterios de aceptación

- [ ] Unitarias: política de bloqueo y expiración de tokens. — *Bloqueo: `UsuarioCredencialTests` 4/4. Falta la expiración del token de restablecimiento (detenido por D-12).*
- [x] Integración con PostgreSQL real: ciclo login → uso → revocación. — *`SesionTests` (14 casos): ingreso, `/me`, cierre, revocación por usuario, contraseña y sesión concreta.*
- [x] Aislamiento: una sesión de la organización A no accede a recursos de B; un usuario inactivo es rechazado. — *El esquema de cookie solo aporta el id de usuario; el aislamiento lo aplica el mismo middleware y filtro probados en `SedesTests` (A no ve, lee ni modifica sedes de B: 404) y `ContratoAutenticacionTests`. Usuario inactivo: 401 en el ingreso y en la sesión.*
- [x] Logs y auditoría sin contraseñas ni tokens (prueba automatizada sobre los eventos generados). — *`SesionTests.La_auditoria_de_sesion_no_contiene_contrasenas_ni_cookies`. Los logs de la aplicación no registran cuerpos ni cabeceras; no hay prueba automatizada sobre los logs.*
- [x] E2E / UI: inicio de sesión, error genérico y cierre de sesión. — *2026-10-20: verificado a mano en el navegador integrado contra el stack real de Docker Compose (nginx → API → PostgreSQL) con un usuario sintético: contraseña incorrecta → «Correo o contraseña incorrectos.»; ingreso → `/sedes` con la organización derivada; `POST /api/v1/sedes` con token anti-CSRF → 201; `document.cookie` vacío (cookie HttpOnly); cierre → 204, luego `/me` → 401 y `/sedes` redirige a `/ingresar`. La E2E encontró un defecto (`/auth/csrf` → 500 por cookie Secure sobre HTTP local), ya corregido. Sin automatizar (no hay Playwright); componentes cubiertos por `SesionReal.test.tsx`.*
- [x] CA HU-006 #1: credenciales válidas → sesión protegida y `/me` con membresías. — *`SesionTests.Credenciales_validas_emiten_cookie_protegida…`*
- [x] CA HU-006 #2: membresía inactivada → siguiente petición rechazada. — *`ContratoAutenticacionTests.Inactivar_la_membresia_rechaza_la_siguiente_peticion`*
- [x] CA HU-006 #3: umbral de intentos superado → bloqueo temporal auditado. — *`SesionTests.Cinco_fallos_bloquean_la_cuenta…` (423 + `sesion.bloquear`)*
- [x] CA HU-006 #4: sin contraseñas ni tokens en logs ni auditoría. — *Auditoría verificada automáticamente; logs por diseño (ver la casilla de logs).*

## Fase 6 — Cierre

- [ ] Revisión de seguridad del flujo (nivel OWASP ASVS por acordar: **pendiente de decisión**).
- [x] La API no arranca con `Desarrollo` habilitado fuera de `Development`/`Testing` (se mantiene la prueba de ADR-022). — *`UnitTests/Seguridad/AutenticacionDesarrolloTests.cs` en verde (56/56, 2026-10-19); además, el esquema compuesto solo atiende la cabecera si está habilitado.*
- [ ] Wiki actualizada: HU-006 marcada como verificada, — *2026-10-21: wiki al día (ADR-006 y ADR-022 aprobados, Estado de implementación, tablero, EP-02, index y log). HU-006 queda «núcleo verificado», no «verificada»: faltan MFA, D-21, restablecimiento (D-12) y nivel ASVS.* ADR-006 aprobado, ADR-022 actualizado, `Estado de implementación.md`, `tablero-scrum.md`, `index.md` y `log.md`.
- [x] HU-016 (invitación) desbloqueada en el backlog. — *2026-10-20: EP-05 ya no la bloquea D-03/HU-006; sigue dependiendo de D-12 para enviar la invitación.*

## Registro de evidencia

| Fecha | Tarea | Evidencia |
|---|---|---|
| 2026-10-21 | Verificación de cierre y commit | Backend 184/184 (arq. 59, unit. 60, integ. 65 con PostgreSQL real), frontend 28/28 y build OK. Casillas restantes bloqueadas por decisiones |
| 2026-10-20 | E2E real en navegador | Defecto corregido (Secure en Development) + semillas con contraseña desde .env; unitarias 60/60 (+4) |
| 2026-10-20 | Fase 4 (salvo restablecimiento), Fase 5 y parte de la 6 | Frontend 28/28 (+6), build OK; casillas marcadas con evidencia existente |
| 2026-10-19 | Fase 3: registro de sesiones, bloqueo y revocación | +3 de integración; 180/180 en verde. MFA, límite por IP y restablecimiento detenidos por decisiones abiertas |
| 2026-10-19 | Fase 2 completa (backend) | +3 unitarias, +11 de integración; 177/177 en verde con PostgreSQL real |
| 2026-10-19 | Fase 0 cerrada | Aprobación de D-03 por el responsable; ADR-006 aprobado |
| 2026-10-17 | Fase 1 completa (4 tareas) | git init; inventario en la wiki; +8 pruebas de arquitectura; +3 de integración; 162/162 en verde |
