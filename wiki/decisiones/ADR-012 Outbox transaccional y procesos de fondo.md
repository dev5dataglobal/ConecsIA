---
tipo: adr
estado: propuesta
area: backend
origen: PRD §11, §12.1
fuentes: [PRD v0.1]
relacionados: [RF-21, RF-22, CA-17]
actualizado: 2026-10-17
tags: [conecsia, adr, backend]
---

# ADR-012 Outbox transaccional y procesos de fondo

> [!warning] Estado: **Propuesta** — no aprobada. No implementar como decisión firme hasta su aprobación.
> Área: backend · Origen: PRD §11, §12.1

## Contexto
Una falla de correo no debe revertir una entrada; alertas programadas y eventos deben ser durables.

## Decisión propuesta
- Patrón outbox: la operación de negocio escribe en una tabla `outbox` en la misma transacción.
- Un procesador de fondo en el mismo host (`BackgroundService`) publica correos, eventos SignalR y alertas, con reintentos, backoff, deduplicación y registro de resultado.
- Todo mensaje lleva `organization_id` verificado.
- Tareas programadas (vencimientos, permanencia prolongada) como procesos de fondo con bloqueo para evitar ejecución doble.

## Alternativas consideradas
- Broker externo (RabbitMQ, etc.): innecesario al inicio.
- Librerías de jobs (Hangfire, Quartz): opción válida para la programación; decidir en fase 0.

## Consecuencias
No se garantiza entrega en bandeja del destinatario; se registra el resultado del envío.

## Pendientes
Proveedor de correo, remitentes y política de reintentos (D-12).

