---
tipo: adr
estado: aprobado
area: backend
origen: PRD §15, §12.7
fuentes: [PRD v0.1]
relacionados: [CA-09, CA-12, D-20]
actualizado: 2026-10-19
tags: [conecsia, adr, backend]
---

# ADR-020 Contratos API y formato de errores

> [!success] Estado: **Aprobado** el 2026-10-17 por el responsable del proyecto. Alcance y condiciones en "Aprobación".
> Área: backend · Origen: PRD §15, §12.7

## Contexto
El PRD pide contratos documentados y errores de validación y conflicto consistentes; las denegaciones de entrada deben indicar el motivo.

## Decisión propuesta
- API REST JSON versionada (`/api/v1`), documentada con OpenAPI y usada para generar el cliente del frontend.
- Errores con Problem Details (RFC 9457): 400 validación, 401/403 autenticación/permiso, 404 también para recursos de otra organización (no revelar existencia), 409 conflicto/concurrencia, 422 regla de negocio.
- Denegación de entrada como resultado de negocio con código de motivo estable (p. ej. `AUTORIZACION_NO_VIGENTE`, `FUERA_DE_HORARIO`).
- Paginación, filtros y orden en servidor.
- Cabecera `Idempotency-Key` en comandos de entrada/salida ([[ADR-007 Concurrencia e idempotencia en accesos]]).

## Alternativas consideradas
GraphQL: no aporta para flujos transaccionales simples.

## Consecuencias
Catálogo de códigos de motivo documentado en la wiki.

## Pendientes
Ninguna bloqueante.

## Revisión 2026-10-17
- [x] Coherente con PRD §15 y CA-09/CA-12.
- [ ] **Por decidir:** cómo responde una entrada denegada. Recomendación: `200` con `resultado: "Denegada"` y código de motivo. Es un resultado esperado del negocio, se audita y no se confunde con un error técnico. Alternativa: `422`.
- [ ] **Precisión:** `409` solo para conflictos de concurrencia o versión; `422` para comandos que violan una regla (p. ej. aprobar una autorización ya rechazada).
- [ ] **Precisión:** un recurso de otra organización responde `404`, nunca `403`, para no revelar que existe.
- [ ] **Precisión:** las claves de idempotencia se conservan al menos 24 h por organización (valor revisable).
- Sin bloqueos de negocio.

## Aprobación 2026-10-17
- Aprobado con las precisiones de la revisión.
- **Entrada denegada: `200` con `resultado: "Denegada"` y código de motivo** (decisión del usuario). Se audita.
- `409` para conflictos de concurrencia o versión, `422` para reglas violadas, `404` para recursos de otra organización. Las claves de idempotencia se conservan al menos 24 h.

## Convención de idioma (D-20, aprobada 2026-10-19)

**Aprobado** por el responsable: opción A de [[Opciones para decidir D-20]].

| Elemento | Regla | Ejemplo |
|---|---|---|
| Recursos y acciones | Español, plural, minúsculas, kebab-case | `/api/v1/sedes`, `/api/v1/tipos-documento`, `POST …/{id}/inactivar` |
| Excepciones técnicas | Se mantienen en inglés | `auth/login`, `auth/logout`, `auth/csrf`, `me`, `health`, prefijo `saas` |
| Campos JSON | camelCase en español | `creadaEn`, `alcanceTotalSedes` |
| Errores | Claves estándar de RFC 9457 (`type`, `title`, `status`, `detail`, `code`, `errors`); valores de `code` en español, MAYÚSCULAS_CON_GUIONES | `FUERA_DE_ALCANCE` |
| Vocabulario sectorial (HU-012) | Solo cambia etiquetas de la UI, nunca rutas ni contratos | — |

Consecuencia: lo implementado (HU-006, HU-013) ya cumple la regla; las rutas indicativas de EP-03 a EP-06 se tradujeron el 2026-10-19.
