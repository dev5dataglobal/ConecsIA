---
tipo: adr
estado: aprobado
area: persistencia
origen: PRD §2.3
fuentes: [PRD v0.1]
relacionados: [RF-05, D-01]
actualizado: 2026-10-19
tags: [conecsia, adr, persistencia]
---

# ADR-019 Zona única con atributo de punto de acceso

> [!success] Estado: **Aprobado 2026-10-19** por el responsable, tal como estaba propuesto (ver § Aprobación).
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


## Aprobación 2026-10-19
Aprobado sin cambios por el responsable del proyecto: `Zona` por sede con `es_punto_acceso` y modo `Entrada | Salida | Ambos`, separada de `AreaDestino`, con asignación de operadores a puntos de acceso para el alcance. Se implementa en HU-015 y HU-018 (`loop/core`). La separación en una entidad `PuntoAcceso` queda como alternativa futura si llegan dispositivos (D-13).
