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


> [!note] 2026-11-04 — ADR-029
> La visita es el **episodio** que el servidor crea en cada entrada de un [[Autorización|registro de acceso]] (D-49). Ya no se registra desde una pantalla propia; toda visita nueva tiene registro de acceso (`ck_visita_autorizacion`).
