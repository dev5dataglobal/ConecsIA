---
tipo: adr
estado: aprobado
area: seguridad
origen: PRD §12.2
fuentes: [PRD v0.1]
relacionados: [RN-01, CA-01, D-16]
actualizado: 2026-10-17
tags: [conecsia, adr, seguridad]
---

# ADR-004 Tenancy con base compartida y organization_id

> [!success] Estado: **Aprobado** el 2026-10-17 por el responsable del proyecto. Alcance y condiciones en "Aprobación".
> Área: seguridad · Origen: PRD §12.2

## Contexto
RN-01 exige aislamiento total entre organizaciones en datos, archivos, exportaciones, cachés, procesos de fondo y eventos.

## Decisión propuesta
- Una base de datos y esquema compartidos; toda entidad de cliente lleva `organization_id NOT NULL`.
- Claves foráneas compuestas `(organization_id, id)` para que sea imposible relacionar objetos de organizaciones distintas.
- Índices únicos que incluyan `organization_id` (p. ej. documento de persona por organización).
- Filtro global obligatorio por organización en EF Core, alimentado solo desde el contexto verificado ([[ADR-005 Contexto de organización y autorización por permisos]]); ignorar el filtro requiere código explícito y revisado.
- Pruebas negativas entre dos organizaciones en API, archivos, exportaciones y eventos (CA-01).
- **Evaluar** Row Level Security de PostgreSQL como segunda defensa, validándola con pool de conexiones y tareas de fondo antes de adoptarla.

## Alternativas consideradas
- Esquema por organización o base por organización: mayor aislamiento físico, alto costo de migraciones y operación con muchos clientes.
- Solo filtros de aplicación sin FKs compuestas: más simple pero permite errores de asociación cruzada.

## Consecuencias
Todo trabajo diferido, caché y canal SignalR debe transportar el `organization_id` verificado.

## Pendientes
Adopción de RLS (requiere prueba técnica); efectos de inactivar una organización (D-16).


## Revisión 2026-10-17
- [x] Base compartida, FKs compuestas, índices únicos por organización y filtro global: listos para aprobar.
- [ ] **Alcance de la aprobación:** RLS queda como **spike de evaluación**, no como decisión adoptada. Se decidirá en un ADR posterior con evidencia.
- [ ] **Precisión:** las tablas del plano SaaS (tipos de organización, planes, módulos) y la búsqueda global por hash de token QR ([[ADR-010 Token QR opaco]]) no llevan `organization_id`. La lista de excepciones es cerrada; cualquier excepción nueva requiere revisión.
- [ ] **Precisión:** HU-005 incluye una prueba automática que falla si una entidad de cliente no tiene `organization_id` ni filtro.

## Aprobación 2026-10-17
- Aprobados la base compartida, `organization_id`, las FKs compuestas, los índices por organización, el filtro global y la lista cerrada de excepciones.
- **RLS no adoptado:** queda como spike de evaluación con su propio ADR futuro.
