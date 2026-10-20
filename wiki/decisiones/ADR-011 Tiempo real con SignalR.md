---
tipo: adr
estado: propuesta
area: backend
origen: PRD §7.2
fuentes: [PRD v0.1]
relacionados: [RF-19, RF-23, CA-14]
actualizado: 2026-10-17
tags: [conecsia, adr, backend]
---

# ADR-011 Tiempo real con SignalR

> [!warning] Estado: **Propuesta** — no aprobada. No implementar como decisión firme hasta su aprobación.
> Área: backend · Origen: PRD §7.2

## Contexto
Se exige presencia y pendientes actualizados en tiempo real sin depender de eventos perdidos.

## Decisión propuesta
- ASP.NET Core SignalR para notificar cambios confirmados (entradas, salidas, pendientes, alertas). No es chat.
- Eventos publicados **después** del commit (vía outbox, [[ADR-012 Outbox transaccional y procesos de fondo]]).
- Grupos por organización y sede; suscripción validada con permisos vigentes.
- Los eventos llevan identificadores mínimos; el cliente consulta la API para el detalle.
- Al reconectar, el cliente recarga el estado desde la API.
- La UI muestra estado de conexión y última actualización.

## Alternativas consideradas
Polling: más simple, más carga y latencia; queda como respaldo.

## Consecuencias
Con más de una instancia se necesita backplane (p. ej. Redis o servicio administrado) — ligado a [[ADR-017 Entorno local y despliegue]].

## Pendientes
Frescura máxima de pantalla (D-15).

