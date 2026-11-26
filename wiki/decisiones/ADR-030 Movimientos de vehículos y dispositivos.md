---
tipo: adr
estado: implementado
fuentes: [PRD v0.1, solicitud del usuario 2026-11-11]
relacionados: [RF-16, RF-17, CA-12, CA-23, CA-24, CA-25, D-10, D-17, D-31, D-33, D-48, D-53, D-54, D-57, ADR-014, ADR-026, ADR-029, HU-048, HU-050]
actualizado: 2026-11-11
tags: [conecsia, adr, control-accesos, vehiculos, dispositivos]
---

# ADR-030 Movimientos de vehículos y dispositivos

**Estado:** Implementado y verificado localmente el 2026-11-11 (sin commit). Es una decisión técnica del agente (**Propuesta**), pendiente de revisión. Las reglas de negocio marcadas *Propuesta* dependen de D-10 y D-57.

## Contexto
[[Autorizaciones de vehículos y dispositivos]] decide si un objeto puede ingresar ([[ADR-026 Autorización de ingreso de vehículos y dispositivos]]), pero en portería no se registraba su entrada ni su salida (RF-16, RF-17; CA-23..CA-25). Las tablas `movimiento_vehiculo` y `movimiento_dispositivo` del modelo inicial no tenían endpoints funcionales, FK ni restricciones, y estaban vacías en `conecsia` y `conecsia_verificacion`. `movimiento_dispositivo.id_zona_punto_acceso` arrastraba el pendiente 5 de D-48.

## Decisión
| # | Decisión | Motivo | Estado |
|---|---|---|---|
| 1 | Una sola tabla **`movimiento_objeto`** (`tipo_objeto` Vehiculo/Dispositivo, `id_vehiculo` o `id_dispositivo`, CHECK de exclusividad) reemplaza `movimiento_vehiculo` y `movimiento_dispositivo` | Mismo patrón que `autorizacion_objeto` (ADR-026): una sola regla y un solo código para ambos tipos. El PRD §12.3 listaba dos tablas como *propuesta* | Implementado |
| 2 | Cada movimiento se vincula a la **decisión del objeto** (`id_autorizacion_objeto`), al **registro de acceso**, al **episodio** (`id_visita`) y a la **persona responsable**, con sede, punto (FK compuesta sede+punto), operador e instante | PRD §8.1–8.2: «cada movimiento se vinculará a una visita y a su responsable» | Implementado |
| 3 | **Solo inserción** (trigger `tr_movimiento_objeto_inmutable`). La salida es una fila nueva con `id_movimiento_entrada`; índice único parcial: una entrada admite una sola salida. «Dentro» = entrada sin salida | ADR-014: no se reescribe el historial; idempotente ante reintentos | Implementado |
| 4 | **Entrada:** decisión del objeto Aprobada y vigente hoy (fecha de la sede), registro Aprobado, objeto activo y asociado a la persona, **persona dentro con ese registro** y objeto no dentro. Se vincula al movimiento de entrada de la persona | La entrada del objeto acompaña un ingreso real; no habilita ingresos por sí sola | Implementado (*Propuesta*, D-57) |
| 5 | **Salida:** cierra la última entrada abierta del objeto. No evalúa vigencia (una revocación posterior no la impide) **ni exige que la persona siga dentro**. Debe registrarse desde el registro con el que entró | Igual que la persona (CA-13). El objeto no queda «atrapado» si su responsable salió antes | Implementado (*Propuesta*, D-10/D-57) |
| 6 | **La salida de la persona no cierra sus objetos**: la respuesta y la pantalla avisan cuáles siguen dentro | PRD §8.2 (propuesta: advertir); CLAUDE.md prohíbe cerrar equipos sin regla aprobada | Implementado |
| 7 | Un mismo vehículo o dispositivo **no puede estar dentro dos veces**, aunque figure en varios registros (bloqueo `FOR UPDATE` de la fila del objeto) | Integridad física; concurrencia | Implementado |
| 8 | Intentos denegados: auditoría con resultado `Denegado` y motivos, **sin** fila de movimiento | A diferencia de la persona (ADR-029 #7), el movimiento de objeto exige visita; el intento queda en la bitácora | Implementado (*Propuesta*) |
| 9 | Permisos: entrada = Control de accesos·Añadir; salida = Control de accesos·Editar; además, alguna acción en el módulo del objeto (Vehículos o Dispositivos). Alcance por sede y punto de la membresía | Coherente con la persona (D-35/D-40). Sin el módulo del objeto no se registra ni se ve | Implementado (*Propuesta*) |

## Contratos
- `POST /api/accesos/{id}/objetos/{idObjeto}/entrada` y `.../salida`: `{ idPuntoAcceso, observacion }` + `Idempotency-Key`. Responden `200` (o réplica idempotente); `409` con `motivos`; `403` sin módulo, permiso o punto asignado; `404` fuera de alcance u otra organización.
- `GET /api/accesos/objetos-dentro?idSede=&tipo=`: objetos con entrada abierta, con persona, sede, punto, hora y `personaDentro`.
- `RegistroAccesoDto.objetos[].dentro/entradaEn`; `PresenciaAccesoDto.objetosDentro`; `MovimientoConfirmadoDto.objetosDentro` en la salida de la persona.

## Migración
`20261111201544_MovimientosObjetoYResponsablesArea`. Se detiene si `movimiento_vehiculo` o `movimiento_dispositivo` tienen filas. Elimina ambas tablas, crea `movimiento_objeto` y añade las FK y la trazabilidad de `area_responsable` (HU-014). Resuelve el pendiente 5 de D-48: la columna `id_zona_punto_acceso` desaparece con la tabla.

## Consecuencias
- Parqueaderos y cupos (D-54), conductor distinto del responsable, acompañantes y transferencia de custodia (D-10) **no** están implementados.
- El historial general (`/api/accesos/historial`) sigue mostrando solo movimientos de personas. Los de objetos se ven en la bitácora del registro y en «Vehículos y dispositivos dentro». Falta un historial y un reporte por vehículo o dispositivo (HU-045).
- Revisión: las reglas #4, #5, #8 y #9 son propuestas del agente y deben validarse con el PO junto con D-10.
