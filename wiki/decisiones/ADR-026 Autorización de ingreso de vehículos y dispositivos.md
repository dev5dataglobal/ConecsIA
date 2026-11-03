---
tipo: adr
estado: implementado
area: dominio
origen: PRD §6.2, §6.7, §8.1, §8.2; solicitud del usuario 2026-10-26
fuentes: [PRD v0.1]
relacionados: [RF-16, RF-17, CA-23, CA-24, ADR-004, ADR-008, ADR-009, ADR-014, ADR-024, D-04, D-10, D-17, D-31, D-32, D-33]
actualizado: 2026-10-26
tags: [conecsia, adr, autorizaciones, vehiculos, equipos]
---

# ADR-026 Autorización de ingreso de vehículos y dispositivos

> [!info] Estado: **Implementado 2026-10-26** a pedido del usuario. El modelo (entidad propia separada de la autorización de la persona) es **Propuesta del agente**, revisable; contrasta con el modelo del PRD (ver advertencia).

## Contexto

- **Base (PRD §8.1/§8.2):** vehículos y dispositivos se asocian a personas y autorizaciones; una persona y una autorización admiten varios dispositivos. El modelo lógico del PRD propone las tablas de vínculo `autorizacion_vehiculo` / `autorizacion_dispositivo` (objeto incluido en la autorización de la persona).
- **Solicitud del usuario (2026-10-26):** autorizar el ingreso del objeto con su propio ciclo (solicitar, aprobar, rechazar, revocar), vigencia, horario, motivo de decisión y trazabilidad; *la autorización del objeto no debe conceder por sí sola el ingreso de la persona*, y ambas reglas deben mantenerse separadas pero vinculables.
- La autorización de personas ([[EP-07 Autorizaciones]]) **aún no está implementada**: no hay flujo al que adjuntar el objeto.

> [!warning] Contradicción con el modelo del PRD
> - PRD v0.1 (2026-10-17): el objeto se incluye en la autorización de la persona (`autorizacion_vehiculo`, `autorizacion_dispositivo`), sin ciclo propio.
> - Solicitud 2026-10-26: ciclo propio del objeto, separado de la persona.
> - Resolución provisional: nueva entidad `autorizacion_objeto` con ciclo propio y vínculo **opcional** `id_autorizacion` a la autorización de la persona. Las tablas de vínculo del PRD se conservan sin uso para EP-07. Validar con producto ([[Decisiones pendientes|D-31]]).

## Decisión (implementada)

| Aspecto | Implementado |
|---|---|
| Entidad | `autorizacion_objeto`: `tipo_objeto` (`Vehiculo`\|`Dispositivo`), `id_vehiculo` **o** `id_dispositivo` (CHECK), persona, sede, área opcional, `id_autorizacion` opcional, `fecha_inicio`/`fecha_fin` (obligatorias), `hora_inicio`/`hora_fin` (ambas o ninguna), motivo, estado, motivo de decisión, solicitante, aprobador, instantes |
| Historial | `autorizacion_objeto_transicion`: estado anterior/nuevo, motivo, actor, sede, instante. **Solo inserción** (trigger) |
| Estados persistidos | `Pendiente → Aprobada \| Rechazada`; `Aprobada → Revocada`. Terminales: Rechazada, Revocada (ADR-009) |
| Vigencia | Calculada, no persistida ([[ADR-009 Ciclos de estado separados y vigencia calculada]]): `Vencida` si `fecha_fin` < hoy en la zona horaria de la sede ([[ADR-008 Modelo de tiempo y zonas horarias]]); `Programada` / `Vigente` para aprobadas. El horario se evaluará en cada movimiento (HU-048/HU-050) |
| Separación de reglas | Aprobar un objeto no crea ni modifica `autorizacion` de la persona (verificado por prueba) |
| Integridad | FKs compuestas `(id_organizacion, id_…)` a persona, vehículo, dispositivo, sede, área y autorización; `ON DELETE RESTRICT` |
| Duplicados | Sin otra `Pendiente`/`Aprobada` no vencida para el mismo objeto y sede con periodo que se cruce; serializado con `pg_advisory_xact_lock` por objeto |
| Concurrencia de decisiones | `xmin` como token: dos decisiones simultáneas ⇒ una 200 y otra 409; una sola transición |
| Permisos (módulos existentes, sin permisos nuevos) | Consultar: Autorizaciones·Consultar (y solo objetos de módulos que el perfil tiene). Solicitar: Autorizaciones·Añadir **+** Vehículos/Dispositivos·Añadir. Decidir: Autorizaciones·Editar **+** acceso al módulo del objeto |
| Actor | Solicitante y aprobador salen del token; campos como `idUsuarioDecide` enviados por el cliente se ignoran |
| Auditoría | `auditoria` con acción `Solicitar`/`Aprobar`/`Rechazar`/`Revocar`, organización, actor, IP, correlación, valores anterior/nuevo; tabla protegida contra UPDATE/DELETE |

## Validaciones del servidor
- Persona activa; vehículo activo con vínculo `persona_vehiculo` activo; dispositivo activo cuyo responsable es la persona.
- Sede activa de la organización; área activa de esa sede; `id_autorizacion` de la misma persona.
- `fecha_fin ≥ fecha_inicio`, `fecha_fin ≥ hoy` (no se crean vencidas), `hora_fin > hora_inicio`.
- Al aprobar se revalidan persona, objeto y vencimiento (409 si cambió).
- Rechazar y revocar exigen motivo.

## Consecuencias
- El flujo de entrada (HU-048/HU-050) deberá exigir **ambas** autorizaciones: la de la persona y la del objeto.
- Si producto prefiere el modelo del PRD (objeto dentro de la autorización de la persona), habrá que migrar `autorizacion_objeto` a `autorizacion_vehiculo/dispositivo` + transiciones de la autorización.

## Pendientes
[[Decisiones pendientes|D-31]] (modelo), [[Decisiones pendientes|D-32]] (quién decide/cancela; hoy el solicitante no puede cancelar), [[Decisiones pendientes|D-33]] (efecto de inactivar objeto o persona sobre autorizaciones aprobadas), D-04, D-10, D-17, D-18 (permanente: hoy se exige fecha de fin).
