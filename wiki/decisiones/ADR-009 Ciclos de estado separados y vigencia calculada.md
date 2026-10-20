---
tipo: adr
estado: propuesta
area: backend
origen: PRD §6.7
fuentes: [PRD v0.1]
relacionados: [RN-06, RN-09, CA-11]
actualizado: 2026-10-17
tags: [conecsia, adr, backend]
---

# ADR-009 Ciclos de estado separados y vigencia calculada

> [!warning] Estado: **Propuesta** — no aprobada. No implementar como decisión firme hasta su aprobación.
> Área: backend · Origen: PRD §6.7

## Contexto
Autorización, visita, permanencia y alerta tienen ciclos distintos; "aprobada" no es "vigente".

## Decisión propuesta
- Máquinas de estado independientes en dominio, con transiciones validadas en backend:
  - Autorización: Pendiente, Aprobada, Rechazada, Cancelada, Revocada, Vencida, Agotada.
  - Visita: Pendiente, Autorizada, Ingresó, Finalizada, Rechazada, Cancelada, Vencida.
  - Permanencia: Abierta, Cerrada.
  - Alerta: Nueva, Revisada, Cerrada.
- La vigencia es una **evaluación** (estado + fechas + horario + días + lugar + cupo) en el momento del intento, no un estado persistido.
- Cada transición registra actor e instante y se audita.

## Alternativas consideradas
Un único estado combinado: rompe autorizaciones recurrentes.

## Consecuencias
"Vencida" puede persistirse por proceso de fondo para consultas, pero el control de acceso siempre recalcula.

## Pendientes
Matriz de quién ejecuta cada transición (D-04); aprobación multinivel no se presupone.

