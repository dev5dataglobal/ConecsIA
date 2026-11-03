---
tipo: adr
estado: implementado
area: persistencia
origen: Solicitud del usuario 2026-10-31
fuentes: [PRD v0.1, Solicitud del usuario 2026-10-31]
relacionados: [RF-05, RF-13, CA-03, CA-09, ADR-027, ADR-019, D-25, D-26, D-27, D-43, D-44, D-45, D-48]
actualizado: 2026-10-31
tags: [conecsia, adr, persistencia]
---

# ADR-028 Zona empresarial por sede y retiro de zonas de acceso

> [!note] Estado: **Implementado 2026-10-31** por solicitud explícita del usuario. Reemplaza [[ADR-027 Punto de acceso como entidad dentro de la zona]] y deja sin efecto el vínculo organización ↔ zona empresarial de [[Zona empresarial]]. Rama `feature/organizaciones`, sin commit.

## Contexto
- La [[Zona empresarial]] se vinculaba con organizaciones (`zona_empresarial_organizacion`: una organización superior administradora y empresas miembro). Una organización con sedes en zonas distintas no podía modelarse.
- Las zonas de acceso (`zona`) agrupaban puntos de acceso dentro de una sede y podían restringir autorizaciones (`autorizacion_zona`, política `autorizaciones.exige_zonas`).

## Decisión (respuestas del usuario 2026-10-31)
1. **Zona empresarial desde la sede.** Se retira `zona_empresarial_organizacion`. Cada [[Sede]] tiene cero o una zona (`sede.id_zona_empresarial`, ya existente). La zona es un catálogo de plataforma (nombre, descripción, estado). Una organización puede tener sedes en varias zonas; la zona se resuelve siempre desde la sede.
2. **Áreas y puntos comunes los administra la organización dueña de la sede.** Ya no hay organización superior: una autorización `TipoEmisor = ZonaEmpresarial` es válida si el área es común y la sede pertenece a esa zona (se retira la verificación de administradora).
3. **Zonas de acceso retiradas.** El punto de acceso pertenece directamente a la sede (FK `punto_acceso(id_organizacion, id_sede) → sede`). Si la sede tiene zona empresarial, el punto puede marcarse **común de la zona** (`es_comun_zona_empresarial`), igual que las áreas comunes; sigue siendo de la sede y su organización (sin datos compartidos entre empresas).
4. **Restricción solo por sede.** Se retiran `autorizacion_zona` y la política `autorizaciones.exige_zonas`: una autorización vale en cualquier punto de acceso de su sede. Las autorizaciones que estaban restringidas quedan más amplias (documentadas en la bitácora de migración).

## Reglas implementadas
- Asignar zona a una sede: la zona debe existir y estar activa (se conserva la actual aunque se inactive). La sede ya está acotada a la organización activa por el filtro global.
- Una sede con áreas o puntos comunes no puede cambiar ni quitar su zona (409).
- Un punto común exige que su sede tenga zona (400). Un punto no cambia de sede.
- Zonas empresariales: crear/editar/activar/inactivar solo el administrador de plataforma; las demás organizaciones ven las zonas donde tienen sedes y **solo sus propias sedes**.
- Inactivar una zona ya no inactiva ninguna organización.

## Migración `20261031145759_ZonasEmpresarialesPorSedeYRetiroZonasAcceso`
Orden: columnas nuevas → bitácora → traslado de datos → retiro de llaves, tablas y columnas. **No reversible** (`Down` lanza excepción; restaurar respaldo).
- Bitácora `public.migracion_zonas_adr028` (fuera del modelo EF): `origen`, `resultado` ∈ {Migrado, Pendiente, Retirado, Historico}, ids y nombre legible de zona, `detalle`, `resuelto_en`, `observacion_resolucion`.
- Vínculo organización ↔ zona activo: se asigna a la sede **solo** si la organización tiene exactamente una sede, sin zona, y un único vínculo activo. Varias sedes, ninguna sede o varios vínculos ⇒ `Pendiente` (no se asigna arbitrariamente). Vínculo inactivo ⇒ `Historico`.
- `movimiento_acceso.zona_historica` = nombre de la zona en que se registró (referencia legible); se retira `id_zona`.
- Cada zona, punto y restricción de autorización por zona queda en la bitácora con su nombre. `parametro_organizacion` `autorizaciones.exige_zonas` se archiva y elimina.
- `restriccion_acceso.id_zona` (sin FK, sin uso funcional) pasa a la sede de la zona cuando no tenía sede (más amplia, nunca más permisiva).

Identificar pendientes:
```sql
SELECT * FROM public.migracion_zonas_adr028 WHERE resultado = 'Pendiente' AND resuelto_en IS NULL;
```
Resolver: asignar la zona en la sede correcta (Administración › Sedes y áreas) y marcar `resuelto_en`/`observacion_resolucion`.

## Consecuencias
- Supera [[ADR-027 Punto de acceso como entidad dentro de la zona]] y cierra D-43 (parcial), D-44 y D-45 por retiro del concepto.
- La pantalla **Administración › Puntos de acceso** (`/administracion/puntos-acceso`) reemplaza a «Zonas y puntos de acceso». Ver [[Puntos de acceso]].
- Gobierno de la asignación de zona y resolución de datos ambiguos: [[Decisiones pendientes]] D-48.
- Verificación: ver [[Estado de implementación]] (2026-10-31).
