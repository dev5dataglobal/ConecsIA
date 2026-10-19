---
tipo: adr
estado: aprobado
area: persistencia
origen: PRD §12.1
fuentes: [PRD v0.1]
relacionados: [RF-25]
actualizado: 2026-10-17
tags: [conecsia, adr, persistencia]
---

# ADR-003 EF Core con Npgsql y migraciones versionadas

> [!success] Estado: **Aprobado** el 2026-10-17 por el responsable del proyecto. Alcance y condiciones en "Aprobación".
> Área: persistencia · Origen: PRD §12.1

## Contexto
PostgreSQL confirmado; el PRD propone Entity Framework Core con Npgsql y migraciones versionadas.

## Decisión propuesta
- EF Core + Npgsql como ORM principal; migraciones versionadas en el repositorio y aplicadas en despliegue controlado (no automáticas al arrancar en producción).
- Convención `snake_case` en tablas/columnas.
- Se permite SQL explícito para operaciones críticas de concurrencia (bloqueos, `ON CONFLICT`) y para reportes pesados.
- Instantes como `timestamptz` ([[ADR-008 Modelo de tiempo y zonas horarias]]).
- Columna de concurrencia optimista (`xmin` de PostgreSQL o versión) en agregados con decisiones y cupos.

## Alternativas consideradas
- Dapper/SQL puro: más control, más código repetitivo; se admite puntualmente.
- Migraciones con herramienta externa (Flyway, DbUp): válida si se prefieren scripts SQL revisables.

## Consecuencias
Pruebas de integración con PostgreSQL real (contenedor), no con proveedor en memoria, por exigencia de §12.7.

## Pendientes
Herramienta de migraciones definitiva y estrategia de despliegue de migraciones (ligada a D-13).


## Revisión 2026-10-17
- [x] Coherente con PRD §12.1 y §12.7: pruebas con PostgreSQL real.
- [ ] **Ajuste:** las migraciones de EF Core se generan como script SQL idempotente, se revisan en el PR y las aplica el pipeline. La forma concreta de aplicarlas en producción queda para D-13, sin bloquear la aprobación.
- [ ] **Ajuste:** `snake_case` con una convención de nombres automática; concurrencia optimista con `xmin`.
- [ ] **Ajuste:** dos roles de base de datos. Uno de migraciones (DDL) y otro de aplicación, sin DDL y sin `UPDATE`/`DELETE` sobre la auditoría. Lo exige [[ADR-014 Auditoría append-only]].

## Aprobación 2026-10-17
- Aprobado con los ajustes de la revisión: script SQL idempotente revisado en el PR, `snake_case`, `xmin` y roles separados de migración y aplicación.
- La forma de aplicar las migraciones en producción sigue pendiente (D-13).
