---
tipo: modulo
estado: implementado
fuentes: [PRD v0.1]
relacionados: [HU-009, HU-010, HU-011, HU-016, RN-01, RN-08, CA-01, ADR-004, ADR-005, ADR-014, D-01, D-16]
actualizado: 2026-10-19
tags: [conecsia, modulo, ep-03, saas]
---

# Organizaciones y plano SaaS

[[EP-03 Organizaciones y plano SaaS#HU-009 Alta, edición e inactivación de organizaciones|HU-009]], implementada el 2026-10-19 en la rama `loop/core` (sin fusionar). Concepto: [[Organización]].

## Qué existe (Implementado)
- **Plano SaaS separado:** el usuario tiene `permisos_saas` (`saas.organizaciones.consultar`, `saas.organizaciones.administrar`). No conceden acceso a datos de clientes: un superadministrador sin membresía recibe 403 en `/sedes`, aunque envíe `X-Organizacion-Id` (verificado).
- **Endpoints SaaS** `[SinOrganizacionResuelta]`: `GET/POST /api/v1/saas/organizaciones`, `GET/PUT /api/v1/saas/organizaciones/{id}`, `POST .../{id}/inactivar`, `POST .../{id}/reactivar`, `GET .../tipos`.
- **Alta:** nombre único global normalizado (409), tipo del catálogo (400), zona horaria IANA opcional, identificación legal y contacto opcionales. En la misma transacción, `IProvisionOrganizacion` (contrato que implementa Identidad) copia las plantillas de rol y vincula al primer administrador con el rol de administración. Si el correo ya existe, se reutiliza el usuario (D-03b). El caso de uso cambia la organización de escritura con `IContextoPlanoSaas.OperarEnOrganizacion`, que exige el permiso SaaS y no concede permisos de organización.
- **Propia organización:** `GET/PUT /api/v1/organizaciones/{id}` con `organizacion.consultar/modificar`. Otro ID responde 404 (CA-01). El administrador no cambia el tipo ni el estado.
- **Concurrencia optimista** con `xmin` (409 por versión obsoleta).
- **Auditoría:** `organizacion.crear` (incluye el primer administrador), `organizacion.modificar` (anterior/nuevo), `organizacion.inactivar` y `organizacion.reactivar`. El actor es el superadministrador y la organización, la afectada.
- **Plataforma:** `GET /api/v1/me/plataforma` devuelve la identidad y los permisos SaaS sin organización resuelta.
- **SPA:** `/saas/organizaciones`, con lista paginada, alta con primer administrador, edición y confirmación de inactivación con advertencia de efectos. Si un usuario sin membresía tiene permisos SaaS, se le redirige a la consola; si no los tiene, ve un mensaje.
- **Semillas:** `super.saas@ejemplo.test` (solo permisos SaaS).

## Pendiente o interino
> [!warning] D-16 (pendiente): efectos de inactivar
> Comportamiento interino definido en el backlog: los usuarios de una organización inactiva no resuelven contexto (403 en toda operación nueva). Los datos y el historial se conservan. La reactivación es manual y queda auditada. Faltan por decidir la consulta histórica, los procesos de fondo y el plan de cobro.

- **Invitación:** el primer administrador queda creado sin contraseña. El envío del enlace para definirla llega con HU-016 y loop_integraciones (D-12).
- **Nombre único global:** es una propuesta del backlog, implementada.
- **Sin pantalla de la propia organización:** la API existe, pero la SPA no la ofrece todavía.
- **Sin E2E con navegador:** hay Vitest y pruebas de integración.

## Módulos opcionales (HU-011, Implementado 2026-10-19)
- **Catálogo cerrado** `ModulosOpcionales`: `vehiculos` (Vehículos), `equipos` (Equipos externos) y `correo` (Notificaciones por correo). El núcleo siempre está habilitado.
- **Tabla** `organizacion_modulo` (organización, módulo, habilitado, `cambiado_en`, `cambiado_por`). Deshabilitar conserva la fila y los datos.
- **API SaaS:** `GET /api/v1/saas/modulos` y `GET/PUT /api/v1/saas/organizaciones/{id}/modulos` (lista completa de habilitados). Para la organización del usuario: `GET /api/v1/me/modulos`.
- **Guarda** `IModulosHabilitados.ExigirAsync(modulo)`: 403 `MODULO_NO_HABILITADO`. Se lee en cada petición, con efecto inmediato. Se aplica solo a operaciones nuevas.
- **Auditoría** `organizacion.modulos` con habilitados anterior y nuevo.
- ⏸ **Planes:** su contenido depende de D-16; no se modelaron.

## Vocabulario por organización (HU-012, Implementado 2026-10-19)
- **Catálogo cerrado de claves:** `organizacion`, `sede`, `area`, `zona`, `puntoAcceso`, `persona`, `visita` y `autorizacion`, cada una con singular y plural.
- **Tabla** `organizacion_vocabulario` (una fila por organización, `terminos` en jsonb y `xmin`). Solo guarda lo personalizado; vacío = etiquetas por defecto.
- **API:** `GET /api/v1/vocabulario` (cualquier miembro) y `PUT /api/v1/vocabulario` (`configuracion.modificar`, con versión; 409 si está obsoleta). Etiquetas de texto plano: sin `<>&"` ni caracteres de control, hasta 60 caracteres. Una clave desconocida responde 400.
- **SPA:** `AppShell` envuelve la aplicación con el vocabulario de la organización (`aTextos`): menú, título de sedes, etiquetas de contexto y título de áreas. Pantalla `/configuracion/vocabulario`.
- **RN-11:** rutas, permisos y contratos conservan los nombres del núcleo.
- ⏸ **D-01:** las etiquetas por vertical y los valores por defecto de cada plantilla (HU-010).
