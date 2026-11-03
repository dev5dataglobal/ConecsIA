---
tipo: modulo
estado: implementado
fuentes: [PRD v0.1, Solicitud del usuario 2026-10-31]
relacionados: [RF-05, HU-015, ADR-028, D-34, D-43, D-48]
actualizado: 2026-10-31
tags: [conecsia]
---

# Puntos de acceso

Estructura: Organización → [[Sede]] → Punto de acceso. Modelo en [[ADR-028 Zona empresarial por sede y retiro de zonas de acceso]]; reemplaza [[Zonas y puntos de acceso]] (obsoleto).

## Administración — **Implementado 2026-10-31**
- Pestaña **Administración › Puntos de acceso** (`/administracion/puntos-acceso`), módulo `sedes` (Consultar / Añadir / Editar; eliminar = Editar, D-43).
- Consultar, crear, editar, activar/inactivar y **eliminar solo sin historial** (movimientos u operadores asignados ⇒ 409; se inactiva).
- Filtros: búsqueda, sede y estado. El formulario elige la sede (fija al editar); la organización es siempre la activa y el cliente nunca la envía.
- **Punto común de la zona empresarial:** interruptor visible solo si la sede tiene zona; el servidor rechaza (400) la marca en una sede sin zona.
- Reglas: punto solo en sede activa; no cambia de sede; activar exige la sede activa; nombre único por sede (índice sobre nombre normalizado).
- API: `GET/POST /api/puntos-acceso`, `GET/PUT/DELETE /api/puntos-acceso/{id}`, `POST /api/puntos-acceso/{id}/activar|inactivar`. `/api/zonas` ya no existe (404).

## Integración — **Implementado**
- [[Autorización]]: solo por sede; sin zonas permitidas ni política `exige_zonas`.
- [[Visitas]]: la entrada y la salida exigen un punto activo de la sede de la visita; el movimiento guarda sede y punto. El historial muestra la zona histórica (`zona_historica`) solo en movimientos anteriores a la migración.
- [[Sede]]: el formulario asigna la zona empresarial (catálogo `/api/sedes/zonas-empresariales`).

## Verificación
Backend: `PuntosAccesoTests` (8) y `MigracionZonasTests` (1). Frontend: `PuntosAccesoSeccion.test.tsx` (3). Sin verificación en navegador (ver [[Estado de implementación]]).
