---
tipo: modulo
estado: implementado
fuentes: [PRD v0.1]
relacionados: [HU-015, HU-018, RF-05, CA-02, CA-03, ADR-019, ADR-004, D-13]
actualizado: 2026-10-19
tags: [conecsia, modulo, ep-04]
---

# Zonas y puntos de acceso

[[EP-04 Estructura física#HU-015 Gestionar zonas y puntos de acceso|HU-015]] según [[ADR-019 Zona única con atributo de punto de acceso]] (aprobado el 2026-10-19). Implementada en `loop/core`. Concepto: [[Zona y punto de acceso]].

## Qué existe (Implementado)
- **Tabla `zona`:** FK compuesta `(organizacion_id, sede_id)` → `sede`, nombre único por sede, `es_punto_acceso`, `modo` (`Entrada | Salida | Ambos`) y estado. El `CHECK ck_zona_modo_solo_en_punto_de_acceso` (`es_punto_acceso = (modo IS NOT NULL)`) está verificado en PostgreSQL. La clave `(organizacion_id, id)` sirve para las FK de movimientos (EP-10).
- **API:** `GET/POST /api/v1/sedes/{sedeId}/zonas` (`?operacion=Entrada|Salida` devuelve solo los puntos activos que la admiten y que el usuario puede operar), `GET/PUT /api/v1/zonas/{id}` y `POST /api/v1/zonas/{id}/inactivar`. Permisos `zonas.*`, alcance de sede y auditoría `zona.crear/modificar/inactivar`.
- **Alcance por punto (HU-018):** `membresia.puntos_asignados`. Vacío = cualquier punto de sus sedes; con puntos, solo esos (`ExigirPunto`, 403 `FUERA_DE_ALCANCE`). Un punto asignable es una zona activa marcada como punto, de una sede asignada (si no, 422 `PUNTO_ACCESO_INVALIDO`; de otra organización, 404). Quien tiene puntos asignados solo concede un subconjunto no vacío de los suyos. `/me` expone `puntosAsignados`.
- **SPA:** `/sedes/:sedeId/zonas` y la selección de puntos en «Roles y alcance» de usuarios.
- **Plantillas:** Administrador de sede recibe `zonas.*`; Operador de acceso, `zonas.consultar` (propuesta).

## Pendiente
- Registrar movimientos solo en zonas que son punto de acceso: restricción que se materializa en EP-10 (loop_accesos), que debe usar `ExigirPunto`.
- Dispositivos y lectores asociados a puntos: D-13 (fuera de alcance).
