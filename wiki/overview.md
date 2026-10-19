---
tipo: indice
estado: vigente
fuentes: [PRD v0.1]
actualizado: 2026-10-17
tags: [conecsia]
---

# Panorama del proyecto

**ConesIA – DG**: plataforma SaaS multi-organización para autorizar, registrar y consultar ingresos y salidas en instalaciones físicas. Fuente: [[PRD v0.1]].

## Síntesis vigente
- Núcleo: [[Organización]] → [[Sede]] → [[Área o destino]] / [[Zona y punto de acceso]]; [[Persona]] ≠ usuario ([[Usuarios, roles y permisos]]).
- Cadena operativa: [[Autorización]] → [[Visita]] → [[Movimiento]] de entrada → [[Permanencia]] → movimiento de salida; [[QR]] como identificador.
- Restricciones transversales: [[Multi-tenancy y aislamiento]], [[Seguridad y auditoría]], idempotencia y concurrencia.
- Stack: [[Stack y arquitectura]]. Decisiones técnicas propuestas: [[Registro de ADR]] (20 ADR: 9 aprobados el 2026-10-17 para F0, 11 en Propuesta).

## Dónde estamos
- Fase 0 en curso: fundaciones técnicas implementadas ([[Plataforma base]]); verificadas localmente, incluidas integración y Docker Compose ([[Estado de implementación]]).
- Primera vertical funcional (2026-10-17): gestión de sedes ([[Sedes]]) con aislamiento, permisos, alcance y auditoría verificados de punta a punta; autenticación todavía provisional ([[ADR-022 Autenticación de desarrollo provisional]]).
- Bloqueos principales: D-03 identidad, D-04/D-05 políticas de autorización, D-06 calendario, D-09 identificación ([[Decisiones pendientes]]).
