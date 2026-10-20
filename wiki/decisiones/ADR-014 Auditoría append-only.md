---
tipo: adr
estado: aprobado
area: seguridad
origen: PRD §12.6
fuentes: [PRD v0.1]
relacionados: [RF-25, CA-20]
actualizado: 2026-10-17
tags: [conecsia, adr, seguridad]
---

# ADR-014 Auditoría append-only

> [!success] Estado: **Aprobado** el 2026-10-17 por el responsable del proyecto. Alcance y condiciones en "Aprobación".
> Área: seguridad · Origen: PRD §12.6

## Contexto
La auditoría debe ser inalterable por operaciones ordinarias y cubrir todas las acciones críticas.

## Decisión propuesta
- Tabla de auditoría solo de inserción: organización, actor (usuario o sistema), instante, acción, objeto, resultado, valores anterior/nuevo e IP si está disponible.
- Escrita en la misma transacción que la operación auditada.
- El rol de base de datos de la aplicación no tiene `UPDATE`/`DELETE` sobre la tabla.
- Incluye intentos denegados, exportaciones, regularizaciones y accesos de soporte (propuesta del PRD).
- Sin secretos ni copias innecesarias de datos personales completos.

## Alternativas consideradas
Auditoría solo en logs: no consultable ni confiable como evidencia.

## Consecuencias
Crecimiento de la tabla: se propone particionar por fecha más adelante.

## Pendientes
Retención (D-14).


## Revisión 2026-10-17
- [x] Cumple RF-25 y CA-20.
- [ ] **Precisión:** la inmutabilidad se garantiza con privilegios: el rol de aplicación no puede modificar ni borrar registros (ver revisión de [[ADR-003 EF Core con Npgsql y migraciones versionadas]]).
- [ ] **Precisión:** los intentos denegados y las exportaciones se auditan; es propuesta del PRD que este ADR adopta.
- Particionado y retención quedan fuera de la aprobación (D-14).

## Aprobación 2026-10-17
- Aprobado: inmutabilidad por privilegios de base de datos e inclusión de los intentos denegados y las exportaciones.
- Particionado y retención pendientes (D-14).
