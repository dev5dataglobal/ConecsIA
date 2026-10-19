---
tipo: concepto
estado: propuesta
fuentes: [PRD v0.1]
relacionados: [RF-05, D-01]
actualizado: 2026-10-17
tags: [conecsia]
---

# Zona y punto de acceso

**Base:** la zona es un sector físico sujeto a permisos; el punto de acceso es donde se registran entradas/salidas.

> [!note] Propuesta de modelado (PRD §2.3)
> Una sola entidad Zona con `esPuntoAcceso`; si es verdadero configura entrada, salida o ambas. **Verificar antes del modelo físico.**

Destino organizacional ([[Área o destino]]) y zona física siguen siendo conceptos distintos.
