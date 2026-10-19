---
tipo: adr
estado: propuesta
area: persistencia
origen: PRD §12.4
fuentes: [PRD v0.1]
relacionados: [D-06, CA-09, CA-11]
actualizado: 2026-10-17
tags: [conecsia, adr, persistencia]
---

# ADR-008 Modelo de tiempo y zonas horarias

> [!warning] Estado: **Propuesta** — no aprobada. No implementar como decisión firme hasta su aprobación.
> Área: persistencia · Origen: PRD §12.4

## Contexto
Vigencias por fecha, día y horario; sedes potencialmente en distintas zonas horarias; horarios nocturnos pendientes.

## Decisión propuesta
- Instantes (movimientos, decisiones, auditoría) en `timestamptz` (UTC).
- Horarios recurrentes como hora local + días + zona horaria IANA de la sede.
- Zona horaria obligatoria por sede (propuesta del PRD).
- Reloj inyectable en el dominio para pruebas.
- Horarios que cruzan medianoche **rechazados** en validación hasta que D-06 defina la regla.

## Alternativas consideradas
Guardar todo en hora local: ambiguo con cambios de horario y entre sedes.

## Consecuencias
Reportes y exportaciones deben indicar la zona horaria del corte.

## Pendientes
D-06: límites inclusivos/exclusivos, festivos, horarios nocturnos.

