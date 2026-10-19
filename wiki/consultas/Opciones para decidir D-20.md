---
tipo: consulta
estado: propuesta
fuentes: [PRD v0.1, código en main 023113b, backend/openapi/ConecsIA.Api.json, wiki/backlog]
relacionados: [D-20, ADR-020, HU-013, EP-03, EP-04, EP-05, EP-06]
actualizado: 2026-10-19
tags: [conecsia, api, decision]
---

# Opciones para decidir D-20 (idioma de rutas y contratos)

> [!note] Análisis, no decisión
> [[Decisiones pendientes|D-20]] sigue **pendiente** hasta que la cierre el responsable. Este análisis solo propone una opción.

## Estado actual (verificado 2026-10-19)

| Elemento | Idioma actual | Evidencia |
|---|---|---|
| Rutas implementadas | Español: `sedes`, `sistema/version`, `desarrollo/usuarios`, `auth/sesiones`, `{id}/inactivar` | Controladores en `backend/src`; `ConecsIA.Api.json` |
| Términos técnicos estándar | Inglés: `auth/login`, `auth/logout`, `auth/csrf`, `me`, `health` | Ídem |
| DTO y campos JSON | Español: `SedeDto`, `alcanceTotalSedes`, `creadaEn`, `elementos` | OpenAPI |
| Códigos de error | Español: `CSRF_INVALIDO`, `FUERA_DE_ALCANCE`… La clave es `code` (RFC 9457) | `DetallesProblema.cs` |
| Permisos | Español: `sedes.consultar`, `sedes.crear`… | `Permisos.cs` |
| Frontend | Consume `/api/v1/sedes` y otras 10 rutas; las rutas de la SPA en español (`/sedes`, `/ingresar`) | `frontend/src` |
| Backlog | **Mixto**: EP-03 a EP-06 en inglés (`/sites`, `/zones`, `/users`, `/persons`, `/saas/organizations`); EP-07 en adelante en español (`/autorizaciones`, `/accesos/entradas`, `/visitas`, `/vehiculos`) | `wiki/backlog/EP-*.md` |

## Alternativas

### A) Español en todo el contrato (formalizar lo implementado)
- **Backend:** no hay que cambiar nada. Las historias nuevas usan sustantivos en español, en plural y kebab-case.
- **Frontend:** no hay que cambiar nada. `schema.d.ts` no varía.
- **OpenAPI:** no hay que cambiar nada.
- **Pruebas:** no hay que cambiar nada.
- **Documentación:** hay que corregir las rutas indicativas de EP-03 a EP-06 (unas 25 líneas) y anotar la convención en ADR-020.
- **Costo:** muy bajo. Además coincide con el dominio: el PRD, el modelo de datos, los permisos y los códigos de error ya están en español.

### B) Inglés en rutas y JSON
- **Backend:** hay que renombrar las rutas de 6 controladores y, para que el contrato sea coherente, también los DTO y las propiedades JSON (o fijar un `JsonPropertyName` por campo). Lo mismo con los códigos de error y probablemente con los permisos.
- **Frontend:** hay que cambiar las llamadas en `features/sedes`, `sesion` y `sistema`, regenerar `schema.d.ts` y ajustar los tipos usados.
- **OpenAPI:** se regenera por completo y el contrato publicado cambia.
- **Pruebas:** hay que actualizar las URL y los JSON esperados en las pruebas de integración (`SedesTests`, `SesionTests`, `ContratoAutenticacionTests`…) y en las de frontend.
- **Documentación:** hay que corregir EP-07 a EP-19, la página de [[Sedes]] y el README.
- **Costo:** medio ahora y creciente con cada vertical. Además crea una brecha de traducción entre el dominio (español) y la API (inglés).

### C) Mixto: rutas en inglés y JSON en español
- Exige el mismo cambio de rutas que B y deja un contrato incoherente (`/sites` devuelve `SedeDto`). No se recomienda.

## Recomendación (propuesta)

**Opción A**, con estas reglas para ADR-020:
1. Recursos y acciones en español, en plural, en minúsculas y en kebab-case (`/tipos-dispositivo`, `/{id}/inactivar`).
2. Se mantienen en inglés los términos técnicos de facto: `auth/login`, `auth/logout`, `auth/csrf`, `me`, `health`, y las claves estándar de RFC 9457 (`type`, `title`, `status`, `detail`, `code`, `errors`).
3. Campos JSON en camelCase y en español. Códigos de error en MAYÚSCULAS_CON_GUIONES y en español.
4. La terminología sectorial (HU-012) solo cambia etiquetas en la UI, nunca rutas ni contratos.

**Por qué:** es la opción que ya está implementada y probada. Coincide con el lenguaje ubicuo del PRD, de `db.md` y de los permisos, no reabre pruebas en verde y tiene un costo casi nulo antes de M1. El backlog en inglés era solo indicativo y además ya es incoherente consigo mismo. La única ventaja real de B, la convención internacional para integradores externos, no es un requisito del PRD v0.1.

> [!todo] Si se aprueba A
> Mover D-20 a `aprobado`; añadir la convención a ADR-020; corregir las rutas indicativas de EP-03 a EP-06; quitar la nota de desviación en [[Sedes]] y EP-04.
