---
tipo: adr
estado: implementado
area: persistencia
origen: Solicitud del usuario 2026-10-28 (RF-05)
fuentes: [PRD v0.1]
relacionados: [RF-05, ADR-019, D-01, D-43, D-44, D-45, HU-015]
actualizado: 2026-10-28
tags: [conecsia, adr, persistencia]
---

# ADR-027 Punto de acceso como entidad dentro de la zona

> [!note] Estado: **Implementado 2026-10-28** por solicitud explícita del usuario (estructura Organización → Ubicación → Zona → Punto de acceso). Reemplaza la propuesta [[ADR-019 Zona única con atributo de punto de acceso]]. Detalles de diseño del agente por revisar.

## Contexto
[[ADR-019 Zona única con atributo de punto de acceso]] proponía una sola entidad `Zona` con `es_punto_acceso`. El usuario pidió explícitamente que el punto de acceso sea un registro propio que pertenece a una zona, y que la zona pertenezca a una ubicación (= [[Sede]]).

## Decisión
- `zona` (por sede): nombre, descripción, activo y trazabilidad (`creado_en`, `id_usuario_crea`, `actualizado_en`, `id_usuario_actualiza`). Se retiran `es_punto_acceso` y `modo_acceso`. `id_zona_padre` se conserva sin uso.
- `punto_acceso` (nuevo): `id_zona`, `id_sede` (copiada de la zona), nombre, descripción, `modo_acceso` ∈ {Entrada, Salida, Ambos}, activo y trazabilidad.
- **Integridad en base de datos:** FK compuesta `punto_acceso(id_organizacion, id_sede, id_zona) → zona` impide asociar un punto a una zona de otra sede u organización. `movimiento_acceso(id_organizacion, id_sede, id_punto_acceso) → punto_acceso` impide registrar un movimiento con un punto de otra sede u organización.
- **Unicidad:** nombre normalizado (`upper(btrim(nombre))`, columna generada) único por sede, tanto para zonas como para puntos.
- `movimiento_acceso` guarda `id_punto_acceso` y `id_zona` (zona del punto **al momento** del movimiento): mover o inactivar el punto después no altera el historial.
- `membresia_punto_acceso.id_zona` → `id_punto_acceso`.
- Migración `20261029001444_ZonasYPuntosAcceso` con migración de datos: cada zona `es_punto_acceso` se convierte en `punto_acceso` dentro de su zona padre (si es de la misma sede) o de sí misma; asignaciones y movimientos se remapean.

## Consecuencias
- Un punto pertenece a **una** zona. Si un mismo torniquete debiera atender varias zonas, habría que cambiar el modelo (ver [[Decisiones pendientes]] D-45).
- La jerarquía de zonas (`id_zona_padre`) ya no se usa al validar el ingreso: solo cuenta la zona del punto.
- `movimiento_dispositivo.id_zona_punto_acceso` sigue apuntando a `zona` (módulo de dispositivos sin movimientos implementados): pendiente de alinear (D-45).

## Alternativas consideradas
Mantener [[ADR-019 Zona única con atributo de punto de acceso]]: descartada por la solicitud del usuario.
