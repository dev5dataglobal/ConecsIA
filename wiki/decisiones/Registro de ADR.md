---
tipo: indice
estado: vigente
fuentes: [PRD v0.1]
actualizado: 2026-10-17
tags: [conecsia, adr]
---

# Registro de ADR

Decisiones técnicas extraídas de las decisiones implícitas del [[PRD v0.1]] (2026-10-17). **Al 2026-10-17 hay 9 aprobados** (alcance y condiciones en cada ADR) y 11 en Propuesta. Al aprobar una: cambia `estado: aprobado` en su frontmatter, quita el aviso, registra la fecha y quién la aprobó, y anótalo en [[log]].

| ADR | Área | Estado | Bloqueada por |
|---|---|---|---|
| [[ADR-001 Monolito modular por capacidades]] | backend | **Aprobado 2026-10-17** | — |
| [[ADR-002 ASP.NET Core Web API en CSharp]] | backend | **Aprobado 2026-10-17** | — |
| [[ADR-003 EF Core con Npgsql y migraciones versionadas]] | persistencia | **Aprobado 2026-10-17** | D-13 (despliegue de migraciones) |
| [[ADR-004 Tenancy con base compartida y organization_id]] | seguridad / persistencia | **Aprobado 2026-10-17** | Prueba técnica de RLS |
| [[ADR-005 Contexto de organización y autorización por permisos]] | seguridad | **Aprobado 2026-10-17** | D-03, D-04 |
| [[ADR-006 Identidad y sesiones]] | seguridad | **Aprobado 2026-10-19** (opción A) | D-14 (plazo de auditoría) |
| [[ADR-007 Concurrencia e idempotencia en accesos]] | persistencia | Propuesta | D-05, D-07 |
| [[ADR-008 Modelo de tiempo y zonas horarias]] | persistencia | Propuesta | D-06 |
| [[ADR-009 Ciclos de estado separados y vigencia calculada]] | backend | Propuesta | D-04 |
| [[ADR-010 Token QR opaco]] | seguridad | Propuesta | — |
| [[ADR-011 Tiempo real con SignalR]] | backend | Propuesta | D-15 |
| [[ADR-012 Outbox transaccional y procesos de fondo]] | backend | Propuesta | D-12 |
| [[ADR-013 Almacenamiento privado de archivos]] | seguridad / despliegue | Propuesta | D-13, D-14 |
| [[ADR-014 Auditoría append-only]] | seguridad | **Aprobado 2026-10-17** | D-14 (retención) |
| [[ADR-015 Frontend SPA con React, TypeScript y Tailwind]] | frontend | Propuesta | ADR-006 |
| [[ADR-016 Reportes y exportaciones Excel y PDF]] | backend | Propuesta | D-15 |
| [[ADR-017 Entorno local y despliegue]] | despliegue | **Aprobado 2026-10-17** | D-13, D-14, D-15 |
| [[ADR-018 Observabilidad sin datos sensibles]] | despliegue | **Aprobado 2026-10-17** | D-13 |
| [[ADR-019 Zona única con atributo de punto de acceso]] | persistencia | **Aprobado 2026-10-19** | — |
| [[ADR-020 Contratos API y formato de errores]] | backend | **Aprobado 2026-10-17** | — |
| [[ADR-021 DbContext compartido y ensamblado de migraciones]] | persistencia | **Aprobado 2026-10-19** (con 3 condiciones) | — |
| [[ADR-022 Autenticación de desarrollo provisional]] | seguridad | **Aprobado 2026-10-19** (solo Development/pruebas) | — |

## Origen de cada propuesta
- **Propuesta explícita del PRD:** ADR-001, 002, 003, 004, 007, 008, 009, 010, 011, 012, 013, 014, 019 (el PRD ya las enuncia; aquí se concretan).
- **Implícita en el PRD y concretada por el agente:** ADR-005, 006, 015, 016, 017, 018, 020. Incluyen elecciones del agente (p. ej. Problem Details, TanStack Query, OpenTelemetry, cookie + BFF) que deben revisarse con especial atención.

## Candidatas a aprobar primero (sin bloqueos de negocio)
ADR-001, ADR-002, ADR-010, ADR-020; luego ADR-003, ADR-004 y ADR-014, necesarias para iniciar la fase 0 ([[Fases y MVP]]).
