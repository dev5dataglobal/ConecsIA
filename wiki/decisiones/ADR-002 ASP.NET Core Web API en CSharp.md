---
tipo: adr
estado: aprobado
area: backend
origen: PRD §3, §12.1
fuentes: [PRD v0.1]
relacionados: [—]
actualizado: 2026-10-17
tags: [conecsia, adr, backend]
---

# ADR-002 ASP.NET Core Web API en CSharp

> [!success] Estado: **Aprobado** el 2026-10-17 por el responsable del proyecto. Alcance y condiciones en "Aprobación".
> Área: backend · Origen: PRD §3, §12.1

## Contexto
.NET está confirmado; C# y ASP.NET Core Web API son propuesta del PRD.

## Decisión propuesta
- Backend en C# sobre ASP.NET Core Web API, versión LTS vigente de .NET verificada en fase 0.
- Autorización con el sistema de políticas de ASP.NET Core ([[ADR-005 Contexto de organización y autorización por permisos]]).
- Validación de entrada en servidor para todo comando.

## Alternativas consideradas
F# u otros lenguajes .NET: sin beneficio claro y menor disponibilidad de perfiles.

## Consecuencias
Base para SignalR ([[ADR-011 Tiempo real con SignalR]]) y EF Core ([[ADR-003 EF Core con Npgsql y migraciones versionadas]]) en el mismo host.

## Pendientes
Versión exacta de .NET y librerías (fase 0).


## Revisión 2026-10-17
- [x] Coherente con el stack confirmado.
- [ ] **Ajuste propuesto:** fijar **.NET 10 (LTS)**; la próxima versión (.NET 11, noviembre de 2026) no es LTS. Confirmar el parche exacto en fase 0.
- [ ] **Por decidir:** estilo de endpoints. Recomendación: Minimal APIs agrupadas por módulo (alternativa: controladores MVC).
- [ ] **Por decidir:** validación de comandos. Recomendación: FluentValidation en la capa de aplicación (alternativa: validación integrada de .NET).
- Sin bloqueos de negocio.

## Aprobación 2026-10-17
- **.NET 10 (LTS)**; el parche exacto se fija en HU-001.
- **Endpoints con controladores MVC** (decisión del usuario, en lugar de Minimal APIs): un controlador por recurso dentro de cada módulo.
- **Validación con FluentValidation** en la capa de aplicación.
