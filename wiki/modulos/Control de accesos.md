---
tipo: modulo
estado: implementado
fuentes: [PRD v0.1]
relacionados: [RF-09, RF-10, D-06, D-18, D-34, D-40, D-41, D-42, ADR-007, ADR-008, ADR-009, ADR-024]
actualizado: 2026-10-28
tags: [conecsia, modulo, autorizaciones, control-accesos]
---

# Control de accesos

Solicitud, aprobación y rechazo de [[Autorización|autorizaciones]] de ingreso de **personas ya registradas** en [[Personas]]. Ruta `/control-accesos` (módulo `control_accesos`). Implementado y verificado el 2026-10-28.

## Flujo (Implementado y Verificado)
1. En [[Personas]] (listado o detalle), **«Gestionar acceso»** navega a `/control-accesos?persona=ID`. Solo aparece con *Control de accesos · Añadir* y persona activa.
2. Se abre «Solicitar acceso» con la persona precargada: no se busca ni se crea otra vez (verificado: un solo registro por documento).
3. Se elige el **tipo de autorización**; el formulario muestra solo sus campos y el servidor ignora los demás.
4. Al guardar nace **Pendiente** («Pendiente de aprobación»): no habilita ingresos.
5. Quien tiene *Autorizaciones · Editar* aprueba (observación opcional) o rechaza (motivo obligatorio). Se guardan decisor, instante y motivo; la rechazada queda en el historial.

## Modalidades (servidor: `Services/ReglasAutorizacion.cs`)
| Tipo | Campos | Reglas |
|---|---|---|
| Única | día, franja opcional | `fecha_fin = fecha_inicio`, `cupo_ingresos = 1`; el ingreso la consume (UPDATE condicional de Visitas) ⇒ vigencia «Consumida» |
| Diaria | día, franja opcional | día ≥ hoy (zona de la sede); franja fin > inicio |
| Temporal | fecha+hora de inicio y fin | fin > inicio y fin > ahora; la entrada valida los instantes (`MotivosFranja`) |
| Recurrente | días ISO, horario, desde, hasta opcional | ≥ 1 día; horario en `autorizacion_horario` (único por día); algún día dentro del periodo |
| Permanente | desde | sin fecha final; solo si `autorizaciones.permite_permanente = true` en `parametro_organizacion` (**Propuesta**, D-42) |

Sin franjas nocturnas ni festivos (provisional hasta D-06). CHECK en BD: `ck_autorizacion_modalidad`, `_estado`, `_fechas`, `_horas`, `ck_autorizacion_horario_horas`.

## Endpoints
| Operación | Endpoint | Permiso (**Propuesta**, D-40) |
|---|---|---|
| Listar, detalle, opciones | `GET /api/autorizaciones-acceso`, `/{id}`, `/opciones` | Control de accesos · Consultar |
| Solicitar | `POST /api/autorizaciones-acceso` | Control de accesos · Añadir |
| Aprobar / rechazar | `POST …/{id}/aprobar`, `…/{id}/rechazar` | Autorizaciones · Editar |

Organización = la activa; alcance por sedes de la membresía (D-34; helpers movidos a `AdministracionControllerBase`).

## Trazabilidad e integridad
- `autorizacion`: `id_usuario_crea` (ahora con FK), nuevas `id_usuario_decide` (FK), `decidido_en`, `motivo_decision`, `hora_inicio`, `hora_fin`. `autorizacion_transicion` con FK compuesta, una fila por cambio de estado. Auditoría `Solicitar`, `Aprobar`, `Rechazar`.
- **Incompatibles (Propuesta, D-41):** 409 si existe otra Pendiente/Aprobada no vencida ni consumida de la misma persona, sede y destino con periodo que se cruza, sin importar la modalidad. Bloqueo `pg_advisory_xact_lock(-org, persona)`: 5 solicitudes paralelas ⇒ 1.
- Decisión concurrente: `version` como token ⇒ dos decisiones simultáneas dejan una sola transición.
- Ingreso: Visitas sigue siendo la única vía de entrada; pendiente, rechazada o consumida ⇒ 409 sin permanencia.

## Verificación (2026-10-28)
- `AutorizacionesAccesoTests` (7): flujo persona → solicitud → pendiente bloquea → aprobación → ingreso único consumido; rechazo con motivo e historial; validación por modalidad; incompatibles en paralelo; decisiones simultáneas; permisos y aislamiento; reglas puras. Suite **94/94**.
- `vitest`: 4 nuevas (`ControlAccesosPage.test.tsx`), total 38. Una prueba de Administración es sensible a tiempos en la suite completa y pasa aislada.
- Navegador (API :5192 + frontend :5174 sobre `conecsia_verificacion`, eliminada): alta de persona → «Gestionar acceso» → Recurrente L–V 07–17 → aprobación; BD con organización, creador, decisor, 5 horarios y auditoría; móvil 375 px sin desborde.

## Pendiente
> [!todo] Pendientes
> - **D-40** matriz de permisos (¿acción propia «Aprobar»? ¿autoaprobación?). **D-41** incompatibles. **D-42**/D-18 Permanente. **D-06** franjas nocturnas.
> - Cancelar, revocar o editar solicitudes; vencimiento persistido por proceso de fondo: no implementados.
> - La vigencia mostrada de Recurrente es por fecha; el horario lo valida la entrada.
> - Migración `20261028223749_AutorizacionesAccesoYRetiroTipoAcceso` aplicada a `conecsia` el 2026-10-28.
