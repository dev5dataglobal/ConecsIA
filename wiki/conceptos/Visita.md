---
tipo: concepto
estado: base
fuentes: [PRD v0.1]
relacionados: [RF-11, RF-12, CA-07]
actualizado: 2026-10-17
tags: [conecsia]
---

# Visita

**Base.** Episodio operativo con propósito y [[Área o destino|destino]]. Distinto de la [[Autorización]] que lo habilita.

- Visita ocasional pendiente no ingresa si la política exige aprobación (CA-07).
- Preautorización e invitación (RF-12) recuperable por [[QR]].
- Finalizar una visita **no** cierra una autorización recurrente vigente.

## Implementación (2026-10-26)

**Implementado:** ciclo persistido Pendiente → Autorizada → Ingreso → Finalizada (+ Rechazada/Cancelada/Vencida), entrada y salida transaccionales e idempotentes, una permanencia por visita. Detalle: [[Visitas]].
