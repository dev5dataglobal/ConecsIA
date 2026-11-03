---
tipo: consulta
estado: propuesta
fuentes: [PRD v0.1, solicitud del usuario 2026-11-03]
relacionados: [D-35, D-37, D-40, D-41, D-42, D-46, D-49, D-50, D-51, D-52, D-53, D-54, D-55, ADR-026]
actualizado: 2026-11-03
tags: [conecsia, consulta, control-accesos, visitas]
---

# Unificación de Visitas y Control de accesos: análisis inicial

Solicitud del 2026-11-03: unificar [[Visitas]] y [[Control de accesos]] en un solo módulo «Control de accesos», configurable por tipo de organización. Esta página recoge el análisis previo a la Fase 1. **No hay código.**

## Estado actual (Implementado)

| Pieza | Hoy |
|---|---|
| `autorizacion` (+ `autorizacion_horario`, `autorizacion_transicion`) | Núcleo del permiso: modalidades Única/Diaria/Temporal/Recurrente/Permanente, estados Pendiente/Aprobada/Rechazada…, decisor y motivo. UI `/control-accesos` |
| `visita` | Episodio: persona, sede, destino, responsable, autorización opcional, política «sin autorización». UI `/visitas` («Ingreso extemporáneo») |
| `permanencia`, `movimiento_acceso`, `solicitud_idempotente` | Entrada/salida idempotente, **hoy colgadas de la visita** (`/api/visitas/{id}/entrada`) |
| `autorizacion_objeto` (+ transición inmutable) | Aprobación independiente de vehículos/dispositivos (ADR-026), UI aparte `/autorizaciones` |
| `auditoria` | Append-only con trigger `tr_auditoria_inmutable` |
| Filtro global por organización | `HasQueryFilter` en `ConecsiaDbContext` |
| Zona horaria | Por **sede** (`sede.zona_horaria`), no por organización |

**Conclusión:** el modelo de datos ya está casi unificado; la duplicidad está en pantallas, menús, endpoints y en que la entrada depende de `visita`. Se propone hacer de `autorizacion` el «registro de acceso» y no crear una tabla `AccessRecord` paralela.

## Supuestos propuestos (Propuesta)
1. Entidad principal = `autorizacion` (UI: «Registro de acceso»).
2. «Visitante/invitado/contratista/estudiante» = **categoría de acceso** configurable por organización con plantillas por tipo de organización.
3. Las modalidades actuales se conservan; Única/Diaria/Temporal se agrupan en la UI como «Temporal/puntual».
4. Vigente/Vencido/Consumido se **derivan** al evaluar; se persisten solo Pendiente, Aprobado, Rechazado, Revocado, Cancelado.
5. Horario por defecto heredado sede → organización, sobrescribible por registro.
6. Vehículos/dispositivos se deciden en el mismo flujo reutilizando `autorizacion_objeto`.
7. La `visita` deja de ser obligatoria para entrar: entrada/salida se vinculan al registro de acceso.
8. La migración no borra tablas antiguas en la primera entrega.
9. Exportación CSV en servidor; Excel después.
10. Sin SignalR: presencia por sondeo.

## Decisiones pedidas al usuario
Registradas como D-49..D-55 en [[Decisiones pendientes]].
