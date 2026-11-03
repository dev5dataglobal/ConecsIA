---
tipo: adr
estado: propuesta
fuentes: [solicitud del usuario 2026-11-03]
relacionados: [D-49, ADR-026, ADR-028]
actualizado: 2026-11-03
tags: [conecsia, adr, control-accesos]
---

# ADR-029 Registro de acceso unificado

## Contexto
[[Visitas]] y [[Control de accesos]] duplicaban pantallas y endpoints; la entrada dependía de `visita`. El usuario pidió un solo módulo configurable por tipo de organización (2026-11-03).

## Decisión (Propuesta; D-49 aprobada por el usuario 2026-11-03)
- El registro de acceso es `autorizacion`, ampliada con `categoria_acceso` configurable por organización.
- `visita` **se conserva como episodio** (decisión del usuario 2026-11-03), siempre vinculado a un registro de acceso; deja de tener pantalla y endpoints propios.
- Vehículos y dispositivos mantienen su decisión independiente ([[ADR-026 Autorización de ingreso de vehículos y dispositivos]]) colgada del registro.
- Vigente/Vencida/Consumida se derivan; solo se persisten estados de decisión.

## Consecuencias
- Se retiran `/visitas`, `/autorizaciones` y `/api/visitas/**` tras migrar los datos.
- Detalle: [[Modelo unificado de control de accesos]].
