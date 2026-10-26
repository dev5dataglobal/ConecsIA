---
tipo: implementacion
estado: verificado
fuentes: [db.md, PRD v0.1]
relacionados: [Estado de implementación, PLAN-DE-TRABAJO, Stack y arquitectura]
actualizado: 2026-10-20
tags: [conecsia, backend, ef-core, postgres]
---

# Fase 1 — backend hexagonal y base de datos

## Resultado

Se creó `backend/ConecsIA.slnx` con `ApiConecsia`, `Application`, `Domain` e `Infrastructure`, con dependencias `Domain <- Application <- Infrastructure <- ApiConecsia`. Las entidades derivadas de `db.md` están en `Domain/Entities`; el contexto, configuración EF Core, repositorios y factory de diseño están en `Infrastructure`.

La migración `backend/Infrastructure/Data/Migrations/20261020205103_InitialCreate` fue generada con EF Core 9 y Npgsql 9 sobre .NET 10. Se incluyeron claves compuestas de asociación, índices únicos documentados, restricciones de idempotencia y relaciones centrales con aislamiento por organización.

## Evidencia

| Comprobación | Resultado |
|---|---|
| `dotnet build backend/ApiConecsia/ApiConecsia.csproj` | Correcto, 0 errores |
| `dotnet ef migrations add InitialCreate ...` | Correcto |
| `dotnet ef database update ...` | Pendiente: PostgreSQL local rechazó `postgres`/`postgres` con `28P01` |

> [!note] 2026-10-26
> `DesignTimeDbContextFactory` resuelve la conexión con la misma precedencia que `Program.cs`: `ConnectionStrings:Conecsia` → `ConnectionStrings:DefaultConnection` (`appsettings.json`, `appsettings.{ASPNETCORE_ENVIRONMENT|Development}.json`, `appsettings.Local.json` del proyecto de inicio, variables de entorno) → `CONECSIA_CONNECTION_STRING`. La variable ya no es obligatoria si existe `appsettings.Local.json`.

La aplicación de la migración debe ejecutarse con una cadena válida, por ejemplo:

```powershell
$env:CONECSIA_CONNECTION_STRING = 'Host=localhost;Port=5432;Database=conecsia_dev;Username=<usuario>;Password=<secreto>'
dotnet ef database update --project backend/Infrastructure/Infrastructure.csproj --startup-project backend/ApiConecsia/ApiConecsia.csproj --context ConecsiaDbContext
```

## Discrepancias y pendientes

`db.md` no fija todas las longitudes, defaults, cardinalidades ni las decisiones D-05, D-06, D-07, D-09, D-10, D-11, D-17 y D-19. No se inventaron reglas para esos casos. La Fase 2 (unidad de trabajo y DTOs) queda documentada como pendiente y no se implementó.
