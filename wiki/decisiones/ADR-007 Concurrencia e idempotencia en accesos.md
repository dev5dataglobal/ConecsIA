---
tipo: adr
estado: propuesta
area: persistencia
origen: PRD §6.5, §6.6, §12.4
fuentes: [PRD v0.1]
relacionados: [CA-10, CA-12, CA-13, D-07]
actualizado: 2026-10-17
tags: [conecsia, adr, persistencia]
---

# ADR-007 Concurrencia e idempotencia en accesos

> [!warning] Estado: **Propuesta** — no aprobada. No implementar como decisión firme hasta su aprobación.
> Área: persistencia · Origen: PRD §6.5, §6.6, §12.4

## Contexto
Una autorización única no puede consumirse dos veces; reintentos y doble envío no pueden duplicar movimientos; entrada, consumo y permanencia deben ser atómicos.

## Decisión propuesta
- Entrada = una transacción: validar, consumir cupo, crear Movimiento de entrada, abrir Permanencia y actualizar Visita.
- Bloqueo de la fila de autorización (`SELECT … FOR UPDATE`) o actualización condicional del cupo (`UPDATE … WHERE usos < cupo`) para consumo.
- Índice único parcial: una permanencia abierta por persona y sede (`WHERE estado = 'Abierta'`) — **propuesta del PRD sujeta a D-07**.
- Salida cierra la permanencia con actualización condicional (`WHERE estado = 'Abierta'`); un reintento devuelve el resultado ya registrado.
- Clave de idempotencia (`Idempotency-Key`) por comando de entrada/salida, persistida con su resultado por organización.
- Revocación y entrada serializadas sobre la misma fila de autorización.
- Concurrencia optimista en decisiones y cambios de cupo.
- Errores de conflicto con respuesta consistente ([[ADR-020 Contratos API y formato de errores]]).

## Alternativas consideradas
Bloqueos distribuidos o colas por persona: innecesarios en un monolito con una sola base.

## Consecuencias
Pruebas de concurrencia reales contra PostgreSQL (CA-10, CA-12).

## Pendientes
D-05 (reingresos, cupos), D-07 (presencia por sede/entre sedes), D-08 (regularizaciones).

