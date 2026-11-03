---
tipo: concepto
estado: base
fuentes: [PRD v0.1]
relacionados: [RF-09, RF-10, RF-20, RN-03, RN-04, RN-05, RN-06, RN-09, CA-06, CA-10, CA-11]
actualizado: 2026-10-31
tags: [conecsia]
---

# Autorización

> [!note] 2026-10-31 — **Implementado** ([[ADR-028 Zona empresarial por sede y retiro de zonas de acceso]]): la ubicación de la autorización es la sede (y el punto en el movimiento). Se retiraron las zonas permitidas y la política `autorizaciones.exige_zonas`; una autorización vale en cualquier punto de acceso de su sede. La emisión de zona (`TipoEmisor = ZonaEmpresarial`) ya no exige organización administradora: basta área común en una sede de esa zona.

**Base.** Permiso que define quién puede ingresar, a dónde, en qué periodo y bajo qué condiciones, para uno o varios ingresos.

**Modalidades:** única · diaria · temporal · recurrente · permanente (si la política lo permite).

- Conserva creador, autorizador, decisión, fechas, observaciones y revocaciones (CA-06).
- **Aprobada ≠ vigente:** además deben cumplirse fecha, horario, días, sede, zonas y cupo.
- Vencida/cancelada/revocada no habilita nuevos ingresos (RN-06).
- Única: no puede consumirse dos veces por solicitudes simultáneas (CA-10).
- Recurrente: varias [[Visita]]s en ventanas válidas, no se cierra tras la primera salida (CA-11).

> [!todo] Pendientes
> D-04 (cuándo se exige, quién decide/revoca), D-05 (reingresos, cupos, modificación de aprobadas), D-06 (calendario). Ver [[Decisiones pendientes]].

Relacionado: [[QR]], [[Movimiento]], [[Permanencia]].
