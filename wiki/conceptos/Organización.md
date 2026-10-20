---
tipo: concepto
estado: base
fuentes: [PRD v0.1]
relacionados: [RF-01, RN-01, CA-01]
actualizado: 2026-10-17
tags: [conecsia]
---

# Organización

**Base.** Cliente SaaS y **frontera de aislamiento** de datos. Todo dato operativo pertenece a una organización.

- Contiene [[Sede]]s, [[Persona]]s, usuarios, catálogos y configuración por tipo/vertical.
- Ningún usuario de A puede listar, leer, modificar, exportar o recibir eventos de B (CA-01). Ver [[Multi-tenancy y aislamiento]].
- Inactivar una organización: efectos pendientes (D-16, [[Decisiones pendientes]]).
