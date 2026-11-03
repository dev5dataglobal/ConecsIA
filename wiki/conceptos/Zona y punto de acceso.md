---
tipo: concepto
estado: obsoleto
fuentes: [PRD v0.1]
relacionados: [RF-05, D-01, ADR-027]
actualizado: 2026-10-31
tags: [conecsia]
---

# Zona y punto de acceso

> [!warning] Contradicción / cambio 2026-10-31 — [[ADR-028 Zona empresarial por sede y retiro de zonas de acceso]]
> Antes (ADR-027, 2026-10-28): Sede → Zona → Punto de acceso; autorizaciones restringibles por zona (`autorizacion_zona`, `autorizaciones.exige_zonas`).
> Ahora (solicitud del usuario 2026-10-31, **Implementado**): se retiran las zonas de acceso. El punto de acceso pertenece directamente a la sede y puede marcarse común de la zona empresarial; la autorización vale en toda su sede. El contenido siguiente se conserva como historia.

**Base:** la zona es un sector físico sujeto a permisos; el punto de acceso es donde se registran entradas/salidas.

> [!note] Propuesta de modelado (PRD §2.3)
> Una sola entidad Zona con `esPuntoAcceso`; si es verdadero configura entrada, salida o ambas. **Verificar antes del modelo físico.**

Destino organizacional ([[Área o destino]]) y zona física siguen siendo conceptos distintos.

> [!warning] Contradicción — 2026-10-28
> - **PRD v0.1 / [[ADR-019 Zona única con atributo de punto de acceso]] (2026-10-17):** una sola entidad Zona con `esPuntoAcceso`.
> - **Solicitud del usuario (2026-10-28):** Organización → Ubicación → Zona → Punto de acceso, con el punto como registro propio.
> Prevalece la solicitud del usuario: **Implementado** en [[ADR-027 Punto de acceso como entidad dentro de la zona]]. Detalle operativo en [[Zonas y puntos de acceso]].
