---
tipo: concepto
estado: base
fuentes: [PRD v0.1]
relacionados: [RF-13, RF-14, RN-02, CA-09, CA-12]
actualizado: 2026-10-17
tags: [conecsia]
---

# Movimiento

**Base.** Evento de entrada o salida de persona, vehículo o dispositivo.

- Toda entrada: persona, sede, punto de acceso, instante y operador (RN-02).
- Antes de confirmar: identidad, estado de la persona, política y restricciones; si falla se deniega con causa sin abrir [[Permanencia]] (CA-09).
- Idempotente frente a reintentos/doble envío (CA-12).
- Inconsistencias: tratamiento explícito, nunca edición silenciosa del historial.
- Equipos tienen movimientos propios; no se cierran con la salida de la persona sin regla aprobada (D-10).
