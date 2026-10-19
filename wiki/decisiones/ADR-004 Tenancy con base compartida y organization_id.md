---
tipo: adr
estado: aprobado
area: seguridad
origen: PRD §12.2
fuentes: [PRD v0.1]
relacionados: [RN-01, CA-01, D-16]
actualizado: 2026-10-19
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

## Spike RLS (HU-005, 2026-10-19)
> [!note] Evaluación documental. No hay prototipo con RLS en el código ni una decisión de adoptarla. La decisión es la [[Decisiones pendientes|D-22]].

**Defensa vigente (implementada y verificada):** filtro global de EF Core, rechazo al guardar (`EscrituraEntreOrganizacionesException` → 404 auditado como `Denegado`), FK a `organizacion`, índices únicos con `organizacion_id`, pruebas de convención sobre el modelo y la suite parametrizada Org A/B.

| Aspecto | Hallazgo con el stack actual (EF Core + Npgsql, pool de conexiones) |
|---|---|
| Propagar la organización | Exige `SET LOCAL app.organizacion_id` en cada transacción, o `set_config(..., true)` con un interceptor de conexión. Con el pool, un `SET` de sesión se filtraría entre peticiones: solo es seguro el de transacción. |
| Lecturas sin transacción explícita | EF Core no abre una transacción para las consultas, así que habría que forzarla en todas las peticiones o fijar la variable al abrir cada conexión y limpiarla al devolverla. |
| Procesos de fondo y plano SaaS | Necesitan un rol con `BYPASSRLS` o políticas con un modo sistema: es el mismo punto débil que `EsSistema` del filtro de EF. |
| Auditoría y membresías | Ya son excepciones justificadas (`[SinFiltroDeOrganizacion]`): requerirían políticas propias. |
| Beneficio | Una segunda barrera ante SQL crudo o un defecto del filtro global, que hoy cubren las pruebas de convención. |

**Propuesta:** no adoptar RLS en el MVP. Reevaluar cuando haya SQL crudo en producción (reportes HU-046, exportaciones) o antes de la salida a producción, con un prototipo medido (latencia y pool) dentro de EP-19.
