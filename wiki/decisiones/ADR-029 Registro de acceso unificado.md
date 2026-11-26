---
tipo: adr
estado: implementado
fuentes: [solicitud del usuario 2026-11-03]
relacionados: [D-49, D-50, D-51, D-52, D-53, D-54, D-55, D-40, D-41, D-42, ADR-026, ADR-028]
actualizado: 2026-11-05
tags: [conecsia, adr, control-accesos]
---

# ADR-029 Registro de acceso unificado

**Estado:** Implementado y verificado 2026-11-04 (fases 1–5). La decisión de fondo D-49 la aprobó el usuario; el resto de reglas marcadas *Propuesta* dependen de D-50..D-55.

## Contexto
[[Visitas]] y [[Control de accesos]] duplicaban pantallas, menús y endpoints (`/api/visitas`, `/api/autorizaciones-acceso`, `/api/autorizaciones-objeto`), y la entrada dependía de registrar antes una visita. El usuario pidió (2026-11-03) un solo módulo, un solo punto de creación y configuración por tipo de organización sin duplicar código. Análisis: [[Unificación de Visitas y Control de accesos - análisis inicial]].

## Decisiones

| # | Decisión | Por qué | Estado |
|---|---|---|---|
| 1 | El **registro de acceso es `autorizacion`**; no se crea una tabla `AccessRecord` | Ya tenía modalidades, vigencia, cupo con consumo atómico, incompatibles, transiciones y auditoría verificados | Implementado |
| 2 | **`visita` se conserva como episodio** creado por el servidor en cada entrada | CLAUDE.md §4: autorización, visita, permanencia y movimiento son conceptos distintos; finalizar una visita no cierra un recurrente | Aprobado por el usuario (D-49), implementado |
| 3 | «Visitante», «invitado», «contratista»… son **categorías de acceso** por organización (`categoria_acceso`), sembradas desde **plantillas por tipo de organización** | Terminología, flujo y campos cambian por configuración, no por código | Implementado; plantillas = Propuesta |
| 4 | Estados persistidos solo de decisión (Pendiente, Aprobada, Rechazada, Revocada, Cancelada); **Vigente / Programada / FueraDeHorario / Vencida / Consumida se derivan** en cada consulta e intento con el mismo código (`ReglasAcceso`) | «Aprobada ≠ vigente»; evita procesos de fondo y estados desincronizados | Implementado |
| 5 | Horario: franjas propias (varias por día) o **horario base** de la sede o, si no tiene, de la organización | Requisito de horarios por defecto con sobrescritura por registro | Implementado |
| 6 | Vehículos y dispositivos conservan su **decisión independiente** (`autorizacion_objeto`, ADR-026) pero cuelgan del registro | Permite aprobar a la persona y rechazar su vehículo (y viceversa) con motivo | Implementado |
| 7 | Todo intento denegado se guarda como **`movimiento_acceso` con resultado `Denegado`** + auditoría | Trazabilidad de intentos fallidos sin abrir permanencia | Implementado |
| 8 | Fuera de horario: **Bloquear** por defecto o **Alertar** por parámetro (`accesos.fuera_horario`); la vigencia por fechas, estado o cupo bloquea siempre | Requisito «bloquear o alertar según configuración» | Propuesta (D-51) |
| 9 | Restricciones nuevas en base **`NOT VALID`** (categoría obligatoria, motivo en rechazo/revocación/cancelación, visita con registro) | Obligan a filas nuevas sin reescribir el histórico migrado | Implementado |
| 10 | Migración **aditiva**: no se borra ninguna tabla ni columna; bitácora `migracion_accesos_adr029` | Sin pérdida de historial y reversible | Implementado |
| 11 | Persona: se busca o crea en [[Personas]] desde el mismo formulario; el registro solo recibe `idPersona` | No se duplica la validación de documentos ni se crean personas repetidas | Implementado |
| 12 | Validación estructural con **FluentValidation** (filtro global) y reglas de negocio en el controlador/servicio puro | ADR-003; las reglas que dependen de la base o la hora local no caben en un validador | Implementado |

## Consecuencias
- Retirados `VisitasController`, `AutorizacionesAccesoController`, `AutorizacionesObjetoController`, las pantallas `/visitas` y `/autorizaciones` (redirigen) y sus entradas de menú. Los módulos `visitas` y `autorizaciones` quedan **solo como permisos** (`Autorizaciones · Editar` aprueba y revoca).
- La zona horaria de la sede sigue mandando; la de la organización es el valor por defecto (D-50).
- Detalle técnico: [[Modelo unificado de control de accesos]] · Configuración: [[Configuración del control de accesos por tipo de organización]] · Migración: [[Guía de migración al control de accesos unificado]].
