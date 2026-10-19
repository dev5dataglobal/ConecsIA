---
tipo: adr
estado: propuesta
area: persistencia
origen: Implementación de HU-001 (2026-10-17)
fuentes: [PRD v0.1]
relacionados: [ADR-001, ADR-003, ADR-007, HU-001]
actualizado: 2026-10-17
tags: [conecsia, adr, persistencia]
---

# ADR-021 DbContext compartido y ensamblado de migraciones

> [!warning] Estado: **Propuesta**. Ya está implementada de forma provisional en HU-001 porque HU-001 dejaba la decisión "para refinamiento". Si se rechaza, hay que reestructurar la persistencia antes de crear la primera entidad (HU-005/HU-009).
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
- Aprobar o rechazar.
- Regla de arquitectura de acceso a entidades por módulo (al crear las primeras entidades).
