---
tipo: consulta
estado: aprobado
fuentes: [ADR-021, ADR-003, ADR-017, código en evaluacion/pre-loop-core]
relacionados: [ADR-021, ADR-001, ADR-003, ADR-007, ADR-017, HU-001, HU-005]
actualizado: 2026-10-19
tags: [conecsia, persistencia, migraciones, decision]
---

# Opciones para decidir ADR-021 (DbContext y migraciones)

> [!success] Decisión aprobada 2026-10-19
> El responsable aprobó la **opción A** con las tres condiciones. La regla vigente está en [[ADR-021 DbContext compartido y ensamblado de migraciones]] § Aprobación.

## Propuesta vigente (ya implementada de forma provisional en HU-001)

- Un solo `ConecsIADbContext` en `ConecsIA.Infrastructure`. Cada módulo aporta sus `IEntityTypeConfiguration` (`EnsambladosDelModelo`).
- Un ensamblado `ConecsIA.Migrations`: el único que referencia a todos los módulos. Contiene la fábrica de diseño, las 4 migraciones (`Inicial`, `PrimeraVerticalSedes`, `CredencialUsuario`, `SesionesUsuario`) y un único snapshot.
- La tabla de historial es `__ef_migrations_history`, en `snake_case`.
- Hay dos roles de base de datos: `conecsia_migrator` crea o modifica el esquema y `conecsia_app` solo opera (ADR-003).

## Alternativas

| | A) Contexto compartido + ensamblado de migraciones (propuesta) | B) Un DbContext por módulo | C) Migraciones dentro de `ConecsIA.Api` |
|---|---|---|---|
| Transacciones entre módulos (ADR-007: entrada + consumo de autorización + permanencia) | Directas, con una transacción local | Exigen compartir la conexión y la transacción entre contextos | Directas |
| Aislamiento entre módulos | Más débil: un módulo podría usar las tablas de otro; se mitiga con una prueba de arquitectura | Más fuerte | Igual que A |
| Historiales de migración | 1 tabla, 1 snapshot | 1 por módulo; el orden entre módulos es manual | 1 |
| Conflictos entre loops en paralelo | Choque en el snapshot, que se gestiona con el **token de migración** (PLAN-DE-LOOPS §2.1) | Menos conflictos, pero hay más piezas que coordinar | Igual que A |
| Costo de adoptarla hoy | 0 (ya existe) | Alto: reestructurar la persistencia y rehacer las migraciones | Bajo, pero mezcla el host web con el esquema |

## Impacto de la propuesta A

| Ámbito | Cómo funciona hoy | Qué faltaría |
|---|---|---|
| **Docker local** | La API aplica las migraciones al arrancar con el rol migrador (`AplicadorMigraciones`, `Persistencia:AplicarMigracionesAlIniciar=true` solo en Development). La API falla al arrancar si ese indicador se activa en producción. | Nada. |
| **Desarrollo local** | `dotnet ef migrations add <Nombre> -p src/ConecsIA.Migrations -s src/ConecsIA.Migrations`. Una sola migración a la vez (token). | Documentar el comando y el token en el README. |
| **CI** | Compilación, pruebas y `migrations script --idempotent`, publicado como artefacto (corregido en `fix/ci-main`). | Opcional: una verificación de que no hay cambios de modelo sin migración (`dotnet ef migrations has-pending-model-changes`). |
| **Producción** | **Pendiente.** ADR-003 prohíbe migrar al arrancar. Lo previsto es aplicar el script idempotente del CI con el rol migrador en un paso controlado del despliegue y después arrancar la API. | Definir quién lo ejecuta y cuándo (ADR-017 está aprobado, pero no detalla este procedimiento). |
| **Rollback** | Cada migración tiene `Down`, pero el script idempotente solo avanza. | Propuesta: **corregir hacia adelante** con una migración nueva. Para cambios destructivos, patrón expandir/contraer y respaldo previo (D-14, HU-053). `Down` solo en local. |
| **Pruebas** | Integración con Testcontainers y PostgreSQL real, migradas por el mismo camino que la API. Arquitectura: fronteras entre módulos. | Una regla de arquitectura nueva: cada módulo solo configura y usa entidades de su espacio de nombres (pendiente en el propio ADR). Recomendable antes de HU-005/HU-009. |

## Recomendación (propuesta)

**Aprobar A**, con tres condiciones que se añadirían al ADR:
1. **Regla de arquitectura** de acceso a entidades por módulo, que se implementa en la primera historia de `loop_core` que toque persistencia (HU-005).
2. **Token de migración** según PLAN-DE-LOOPS §2.1.
3. **Rollback hacia adelante**, con expandir/contraer en cambios destructivos. Los detalles de despliegue en producción quedan en ADR-017.

**Por qué:** es la única opción que cumple ADR-007 (atomicidad entre módulos) sin coordinar transacciones distribuidas, ya está implementada y probada (4 migraciones, PostgreSQL real en CI), y su principal riesgo, el acoplamiento entre módulos a través del contexto, se controla con una prueba automática. B costaría reestructurar la persistencia sin aportar un beneficio que el MVP necesite.
