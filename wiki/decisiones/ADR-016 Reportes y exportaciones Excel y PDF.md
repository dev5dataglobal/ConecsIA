---
tipo: adr
estado: propuesta
area: backend
origen: PRD §10.3
fuentes: [PRD v0.1]
relacionados: [RF-24, CA-19]
actualizado: 2026-10-17
tags: [conecsia, adr, backend]
---

# ADR-016 Reportes y exportaciones Excel y PDF

> [!warning] Estado: **Propuesta** — no aprobada. No implementar como decisión firme hasta su aprobación.
> Área: backend · Origen: PRD §10.3

## Contexto
Los reportes deben respetar filtros, zona horaria, permisos y aislamiento; los resultados extensos requieren procesamiento en segundo plano.

## Decisión propuesta
- Reportes pequeños: generación síncrona en streaming.
- Reportes grandes: trabajo en segundo plano ([[ADR-012 Outbox transaccional y procesos de fondo]]) que guarda el archivo en almacenamiento privado ([[ADR-013 Almacenamiento privado de archivos]]) con organización y solicitante; descarga verificada y de vida limitada.
- El archivo registra los filtros, el corte temporal y la zona horaria usados.
- Cada exportación se audita.
- Librerías de Excel y PDF a elegir en fase 0 verificando licencia.

## Alternativas consideradas
Generación en el navegador: expone datos completos al cliente y no escala.

## Consecuencias
Límites de filas y tiempos a definir.

## Pendientes
Volumen y límites (D-15).

