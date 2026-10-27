---
tipo: indice
estado: vigente
actualizado: 2026-10-26
tags: [conecsia]
---

# Índice de la wiki

Inicio: [[overview]] · Bitácora: [[log]]

## Conceptos
- [[Organización]] — cliente SaaS y frontera de aislamiento
- [[Sede]] — instalación física
- [[Área o destino]] — unidad receptora de la visita
- [[Zona y punto de acceso]] — sector físico; propuesta `esPuntoAcceso`
- [[Persona]] — individuo por organización, ≠ usuario
- [[Usuarios, roles y permisos]] — perfiles y acceso efectivo
- [[Autorización]] — modalidades, vigencia, consumo
- [[Visita]] — episodio operativo
- [[Permanencia]] — intervalo entrada–salida
- [[Movimiento]] — eventos de entrada/salida, idempotencia
- [[QR]] — identificador sin datos sensibles
- [[Multi-tenancy y aislamiento]] — reglas de aislamiento
- [[Zona empresarial]] — organización superior que agrupa empresas; acceso compartido

## Requisitos
- [[Reglas de negocio]] — RN-01..RN-11 y su trazabilidad
- [[Criterios de aceptación]] — CA-01..CA-26
- [[Fases y MVP]] — secuencia propuesta (MVP = fases 0–4)

## Decisiones
- [[Decisiones pendientes]] — D-01..D-28
- [[Registro de ADR]] — ADR-001..ADR-020: 9 aprobados, 11 en Propuesta
- Aprobados 2026-10-17: ADR-001, 002, 003, 004, 005, 014, 017, 018, 020
- [[ADR-006 Identidad y sesiones]] — Aprobada 2026-10-19: opción A (ASP.NET Core Identity + cookie); cierra D-03
- [[ADR-021 DbContext compartido y ensamblado de migraciones]] — Propuesta, implementada de forma provisional en HU-001
- [[ADR-022 Autenticación de desarrollo provisional]] — Aprobada 2026-10-19, solo Development y pruebas
- [[ADR-023 Acceso a módulos por rol en base de datos]] — Implementado 2026-10-22: app_modulos, modulos_permisos y denegación por defecto
- [[ADR-024 Acciones por módulo y contexto de organización]] — Implementado 2026-10-24: Consultar/Añadir/Editar por módulo, selector de organización solo para plataforma
- [[ADR-026 Autorización de ingreso de vehículos y dispositivos]] — Implementado 2026-10-26: ciclo propio Pendiente/Aprobada/Rechazada/Revocada, separado de la autorización de la persona, historial inmutable
- [[ADR-025 Sesión renovable y recuperación de contraseña]] — Implementado 2026-10-26: cookie HttpOnly de renovación («Recordarme», 8 h) y restablecimiento por correo de un solo uso

## Backlog Scrum
- [[PLAN-DE-TRABAJO]] — fases F0–F7, DoR/DoD, ruta crítica, bloqueos y sprints propuestos
- [[tablero-scrum]] — estado de HU-001..HU-056
- [[EP-01 Fundamentos técnicos]] — F0 · HU-001..004
- [[EP-02 Tenancy, identidad y auditoría base]] — F0 · HU-005..008
- [[EP-03 Organizaciones y plano SaaS]] — F1 · HU-009..012
- [[EP-04 Estructura física]] — F1 · HU-013..015
- [[EP-05 Usuarios, roles y permisos]] — F1 · HU-016..018
- [[EP-06 Personas]] — F1 · HU-019..022
- [[EP-07 Autorizaciones]] — F2 · HU-023..027
- [[EP-08 Visitas]] — F2 · HU-028..029
- [[EP-09 QR]] — F2 · HU-030..031
- [[EP-10 Entradas y salidas]] — F3 · HU-032..036
- [[EP-11 Presencia e historial]] — F3 · HU-037..039
- [[EP-12 Alertas]] — F4 · HU-040..041
- [[EP-13 Correo]] — F4 · HU-042..043
- [[EP-14 Dashboard y reportes]] — F4 · HU-044..045
- [[EP-15 Auditoría consultable]] — F4 · HU-046
- [[EP-16 Vehículos]] — F5 · HU-047..048
- [[EP-17 Equipos externos]] — F5 · HU-049..050
- [[EP-18 Adaptación sectorial]] — F6 · HU-051..052
- [[EP-19 Preparación productiva]] — F7 · HU-053..056

## Arquitectura
- [[Stack y arquitectura]] — confirmado vs propuesto
- [[Seguridad y auditoría]] — controles transversales
- [[Sistema visual]] — paleta oficial, tokens Tailwind y patrones del mockup de Figma

## Módulos
- [[Plataforma base]] — fundaciones técnicas F0 (HU-001 a HU-004)
- [[Sedes]] — primera vertical funcional: HU-013 con aislamiento, contexto por membresía y auditoría
- [[Administración]] — usuarios, perfiles (matriz de acciones), organizaciones (logo) y zonas empresariales
- [[Personas]] — registro por organización, catálogos, foto privada, teléfonos 1:N, vehículos N:M y dispositivos 1:N
- [[Autorizaciones de vehículos y dispositivos]] — dispositivos por persona y autorizaciones de ingreso de objetos con historial y auditoría inmutables

## Implementación
- [[Estado de implementación]] — qué existe realmente en el código

## ImplementaciÃ³n reciente
- [[Fase 1 backend hexagonal]] â€” reorganizaciÃ³n .NET, modelo EF Core y evidencia de migraciÃ³n

## Fuentes
- [[PRD v0.1]] — PRD funcional y técnico (original en `fuentes/raw/`)

## Consultas
- [[Revisión del backlog v1]] — inconsistencias y bloqueos detectados al crear el backlog
- [[Revisión del modelo ER db.md]] — 35 adaptaciones propuestas al diagrama ER frente a PRD y ADR
- [[Opciones para decidir D-03]] — opciones de identidad y sesión (D-03a–f) con la recomendación del agente
- [[Opciones para decidir D-20]] — idioma de rutas y contratos; aprobada la opción A (español)
- [[Opciones para decidir ADR-021]] — DbContext compartido y migraciones: impacto en Docker, CI, producción, rollback y pruebas
- [[Integración en main antes de loop_core]] — qué integrar en main y decisiones previas al loop core
