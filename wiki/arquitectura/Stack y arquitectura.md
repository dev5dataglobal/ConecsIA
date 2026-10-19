---
tipo: arquitectura
estado: propuesta
fuentes: [PRD v0.1]
relacionados: [D-03, D-13]
actualizado: 2026-10-17
tags: [conecsia]
---

# Stack y arquitectura

| Elemento | Estado |
|---|---|
| Backend .NET | **Base (confirmado)** |
| React + TypeScript, Tailwind CSS | **Base (confirmado)** |
| PostgreSQL | **Base (confirmado)** |
| C#, ASP.NET Core Web API (.NET 10 LTS, controladores MVC, FluentValidation) | **Aprobado** — [[ADR-002 ASP.NET Core Web API en CSharp]] |
| EF Core + Npgsql | **Aprobado** — [[ADR-003 EF Core con Npgsql y migraciones versionadas]] |
| Monolito modular: Dominio / Aplicación / Infraestructura / API | **Aprobado** — [[ADR-001 Monolito modular por capacidades]] |
| PostgreSQL en contenedor + captura de correo local; GitHub + Actions | **Aprobado** (local y CI) — [[ADR-017 Entorno local y despliegue]] |
| Versiones de runtime | .NET 10 LTS aprobado; parches y librerías se fijan en HU-001 |

La lógica de negocio vive en el backend; el frontend no decide permisos, vigencias ni cupos.

## Entregables previos al primer flujo (PRD §15)
1. Registro de decisiones de arquitectura: [[Registro de ADR]]. Los 9 ADR de F0 fueron aprobados el 2026-10-17.
2. Modelo físico inicial con claves de organización y restricciones.
3. Contratos API de personas, autorizaciones, visitas, entradas y salidas, con errores de validación y conflicto consistentes.
4. Matriz de permisos por rol y alcance ([[Usuarios, roles y permisos]]).
5. Pruebas de aislamiento y de duplicidad concurrente ([[Multi-tenancy y aislamiento]]).
6. Semillas sintéticas.
