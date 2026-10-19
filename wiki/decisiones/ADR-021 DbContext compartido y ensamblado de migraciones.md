---
tipo: adr
estado: aprobado
area: persistencia
origen: Implementación de HU-001 (2026-10-17)
fuentes: [PRD v0.1]
relacionados: [ADR-001, ADR-003, ADR-007, ADR-017, HU-001, HU-005]
actualizado: 2026-10-19
tags: [conecsia, adr, persistencia]
---

# ADR-021 DbContext compartido y ensamblado de migraciones

> [!success] Estado: **Aprobado 2026-10-19** por el responsable (opción A, con tres condiciones; ver § Aprobación). Análisis en [[Opciones para decidir ADR-021]].
> Área: persistencia · Origen: implementación de HU-001

## Contexto
HU-001 pedía decidir entre un `DbContext` por módulo o uno compartido. [[ADR-007 Concurrencia e idempotencia en accesos]] exige que la entrada, el consumo de la autorización y la apertura de la permanencia sean atómicos. Esas entidades viven en módulos distintos. [[ADR-001 Monolito modular por capacidades]] prohíbe que un módulo referencie a otro.

## Decisión propuesta
- Un único `ConecsIADbContext` en `ConecsIA.Infrastructure`, que no conoce a los módulos.
- Cada módulo aporta sus `IEntityTypeConfiguration` y la API registra los ensamblados de módulo (`EnsambladosDelModelo`).
- Las migraciones viven en un ensamblado propio, `ConecsIA.Migrations`, el único que referencia a todos los módulos. La fábrica de diseño de `dotnet ef` también está ahí.
- Tabla de historial: `__ef_migrations_history`, con convención `snake_case`.

## Alternativas consideradas
- **Un `DbContext` por módulo:** aislamiento más fuerte, pero las transacciones entre módulos exigirían compartir la conexión o hacer coordinación explícita, y hay más historiales de migración.
- **Migraciones en `ConecsIA.Api`:** mezcla el host con el esquema y ata las herramientas de migración al proyecto web.

## Consecuencias
- Un módulo podría consultar tablas de otro a través del contexto compartido. Las pruebas de arquitectura actuales no lo impiden: cuando existan entidades habrá que añadir una regla (p. ej. cada módulo solo usa `DbSet` de su propio espacio de nombres).
- Las transacciones locales entre módulos son directas.

## Pendientes
- ~~Aprobar o rechazar.~~ Aprobado 2026-10-19.
- Implementar la regla de arquitectura de la condición 1 (en HU-005).
- Procedimiento concreto de aplicación en producción (quién, cuándo, respaldo previo): se detalla con ADR-017 y HU-053.

## Aprobación 2026-10-19

**Aprobado** por el responsable: opción A (un `ConecsIADbContext` compartido y un ensamblado `ConecsIA.Migrations`), con estas condiciones obligatorias:

1. **Regla de arquitectura por módulo:** cada módulo solo configura y usa entidades de su propio espacio de nombres. Se hace cumplir con una prueba de arquitectura, que se implementa en la primera historia de `loop_core` que toque persistencia (HU-005). Hasta entonces es una regla de revisión.
2. **Token de migración:** solo un loop a la vez genera una migración. Se rebasa sobre `main` antes de generarla y se fusiona en un PR propio y pequeño (`PLAN-DE-LOOPS.md` §2.1). Quien toma el token y quien lo libera se anota en `wiki/log.md`.
3. **Rollback hacia adelante:** en entornos compartidos no se revierte con `Down`; se corrige con una migración nueva. Los cambios destructivos siguen el patrón expandir/contraer y requieren respaldo previo (D-14, HU-053). `Down` se admite solo en local.

Sin cambios en lo ya implementado: Docker y desarrollo local migran al arrancar (solo en Development); el CI genera el script idempotente; en producción se aplica ese script con el rol migrador en un paso controlado del despliegue (ADR-003).
