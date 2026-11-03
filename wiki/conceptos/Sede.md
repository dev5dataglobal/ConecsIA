---
tipo: concepto
estado: base
fuentes: [PRD v0.1]
relacionados: [RF-03, CA-02]
actualizado: 2026-10-31
tags: [conecsia]
---

# Sede

> [!note] 2026-10-31 — **Implementado** ([[ADR-028 Zona empresarial por sede y retiro de zonas de acceso]]): la sede tiene cero o una [[Zona empresarial]], asignada en su formulario (zona activa del catálogo); con áreas o puntos comunes no puede cambiarla. Los puntos de acceso pertenecen directamente a la sede ([[Puntos de acceso]]); ya no hay zonas de acceso.

**Base.** Instalación física de una [[Organización]]. Término de trabajo; en el fuente aparece como *ubicación* (D-01).

- Contiene [[Área o destino]] y [[Zona y punto de acceso]].
- Los operadores pueden limitarse a sedes asignadas; el backend rechaza acceso a otra sede (CA-02).
- Zona horaria por sede: pendiente (D-06).

> [!note] Implementación (2026-10-17)
> Gestión de sedes implementada y verificada localmente como primera vertical: [[Sedes]]. Zona horaria IANA obligatoria, nombre único por organización, inactivación sin borrado.
