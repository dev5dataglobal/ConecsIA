---
tipo: adr
estado: propuesta
area: persistencia
origen: PRD §2.3
fuentes: [PRD v0.1]
relacionados: [RF-05, D-01]
actualizado: 2026-10-17
tags: [conecsia, adr, persistencia]
---

# ADR-019 Zona única con atributo de punto de acceso

> [!warning] Estado: **Propuesta** — no aprobada. No implementar como decisión firme hasta su aprobación.
> Área: persistencia · Origen: PRD §2.3

## Contexto
El PRD propone modelar los puntos de acceso como una Zona con `esPuntoAcceso`, manteniendo el destino organizacional como concepto distinto.

## Decisión propuesta
- Entidad `Zona` por sede con `es_punto_acceso` y, si aplica, modo `Entrada | Salida | Ambos`.
- `AreaDestino` separada de `Zona`.
- Movimientos referencian la zona que actúa como punto de acceso; una restricción impide registrar movimientos en zonas que no son punto de acceso.
- Asignación de operadores a puntos de acceso para el alcance ([[ADR-005 Contexto de organización y autorización por permisos]]).

## Alternativas consideradas
Entidad `PuntoAcceso` separada vinculada a una zona: más explícita si un punto atiende varias zonas o tiene hardware propio.

## Consecuencias
Si en el futuro se integran torniquetes/lectores, puede requerir separar la entidad.

## Pendientes
Confirmar el modelo con producto (verificación exigida por CLAUDE.md).

