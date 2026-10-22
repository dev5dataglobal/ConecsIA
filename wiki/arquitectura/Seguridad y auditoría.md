---
tipo: arquitectura
estado: implementado
fuentes: [PRD v0.1]
relacionados: [RF-25, CA-20, D-14]
actualizado: 2026-10-17
tags: [conecsia]
---

# Seguridad y auditoría

## Integración frontend — 2026-10-21

- El navegador solo llama a la API; nunca recibe ni usa credenciales de PostgreSQL.
- El JWT se conserva en memoria y se envía como `Authorization: Bearer`.
- `401` limpia la sesión y vuelve al ingreso; `403` informa falta de permisos sin cerrar la sesión.
- Se creó el administrador inicial de `dataglobal` usando hash de contraseña; la contraseña en claro no se persiste.

> [!note] Desarrollo local 2026-10-21: la API carga opcionalmente `backend/ApiConecsia/appsettings.Local.json`, ignorado por Git. Usa `appsettings.Local.example.json` como plantilla y completa allí la conexión y JWT.

- Aislamiento total por organización → [[Multi-tenancy y aislamiento]].
- Las operaciones críticas auditan organización, actor, instante, acción, objeto, resultado y valores anterior/nuevo (CA-20). Los usuarios ordinarios no alteran la auditoría.
- Concurrencia e idempotencia en entradas/salidas y en el consumo de autorizaciones únicas (CA-10, CA-12) → [[Movimiento]].
- Secretos con mecanismos estándar; sin contraseñas, tokens ni datos personales innecesarios en logs.
- La inactivación preserva la trazabilidad (RN-08).
- Privacidad, retención, respaldos, RPO/RTO: pendiente (D-14).

## Implementado 2026-10-21

- La API usa JWT Bearer. `POST /api/auth/login` valida el usuario activo y devuelve un DTO con `accessToken`, expiración y datos no sensibles del usuario.
- La configuración obligatoria es externa: `JWT__Key` (mínimo 32 caracteres), `JWT__Issuer`, `JWT__Audience`, `JWT__DurationInMinutes` (1–120) y `CONECSIA_CONNECTION_STRING` o una cadena de conexión configurada.
- Todos los controladores requieren autenticación por política global; el login es explícitamente anónimo. Un token ausente o inválido produce `401`.
- El token contiene `sub`, `email`, `security_stamp`, `organization_id`, roles y permisos efectivos de membresías activas. La validación vuelve a comprobar que el usuario siga activo y que el sello de seguridad no haya cambiado.
- Las entidades que heredan `OrganizacionEntity` se filtran por la organización del claim y las altas/modificaciones se rechazan si el ámbito no coincide. La organización no se acepta como prueba de acceso desde el cuerpo de la petición.
- Las políticas reutilizables viven en `UserAuthorization`; la matriz funcional de qué permiso requiere cada endpoint aún debe cerrarse con D-04 antes de asignarla controlador por controlador.
- No se añadió refresh token: el esquema existente no tenía almacenamiento revocable de sesiones y no se creó una migración sin necesidad funcional confirmada.
