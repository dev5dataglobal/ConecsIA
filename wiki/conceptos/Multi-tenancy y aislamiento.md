---
tipo: concepto
estado: base
fuentes: [PRD v0.1]
relacionados: [RN-01, CA-01, CA-19, CA-20]
actualizado: 2026-10-17
tags: [conecsia]
---

# Multi-tenancy y aislamiento

**Base.** Aislamiento por [[Organización]] en consultas, escrituras, archivos, exportaciones, cachés, procesos de fondo y eventos en tiempo real.

- Nunca confiar en un `organizationId` enviado por el cliente sin verificar membresía y permisos.
- **Propuesta:** evaluar Row Level Security de PostgreSQL como defensa adicional (PRD §17).
- Pruebas de aislamiento entre dos organizaciones: entregable de fase 0.

Ver [[Seguridad y auditoría]].
