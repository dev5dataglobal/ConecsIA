---
tipo: modulo
estado: implementado
fuentes: [PRD v0.1]
relacionados: [HU-028, HU-029, HU-032, HU-033, HU-034, HU-035, HU-036, HU-037, RF-11, RF-12, RN-03, CA-01, CA-07, CA-09, CA-11, ADR-009, D-04, D-05, D-06, D-07, D-08, D-34, D-35, D-36, D-37]
actualizado: 2026-10-29
tags: [conecsia, modulo, visitas, accesos]
---

# Visitas

Registro y ciclo operativo de las visitas de personas externas: identificar a la [[Persona]], definir sede, destino y responsable, vincular la [[Autorización]] (o aplicar la política de la organización), y registrar entrada, [[Permanencia]] y salida ([[Movimiento|movimientos]]). Épicas: [[EP-08 Visitas]], [[EP-10 Entradas y salidas]], [[EP-11 Presencia e historial]]. Implementado y verificado (pruebas y navegador) el 2026-10-26.

> [!note] Conceptos separados (Base)
> Visita = episodio · Persona = quien visita · Autorización = permiso de ingreso (EP-07) · Permanencia = intervalo entrada–salida · Movimiento = evento. Una persona tiene muchas visitas; una autorización puede habilitar varias.

## Endpoints (Implementado, Verificado)

| Operación | Endpoint | Acción de módulo |
|---|---|---|
| Listar con búsqueda, filtros, orden, paginación e indicadores | `GET /api/visitas?buscar&estado&idSede&idArea&idResponsable&desde&hasta&orden&pagina&tamano` | Visitas · Consultar |
| Detalle (autorización, permanencia, evaluación de ingreso; movimientos si aplica) | `GET /api/visitas/{id}` | Visitas · Consultar (+ Control de accesos · Consultar para movimientos) |
| Opciones (sedes, destinos, responsables, puntos de acceso, política) | `GET /api/visitas/opciones` | Visitas · Consultar |
| Autorizaciones vinculables de la persona | `GET /api/visitas/autorizaciones?idPersona=` | Visitas · Consultar |
| Registrar | `POST /api/visitas` | Visitas · Añadir |
| Vincular autorización | `POST /api/visitas/{id}/autorizacion` | Visitas · Editar |
| Cancelar (motivo obligatorio) | `POST /api/visitas/{id}/cancelar` | Visitas · Editar |
| Registrar entrada (`Idempotency-Key` obligatoria) | `POST /api/visitas/{id}/entrada` | Control de accesos · Añadir |
| Registrar salida (`Idempotency-Key` obligatoria) | `POST /api/visitas/{id}/salida` | Control de accesos · Editar |
| Personas dentro | `GET /api/visitas/presencia?idSede=` | Control de accesos · Consultar |

Matriz de permisos: **Propuesta** ([[Decisiones pendientes|D-35]]); el sistema solo tiene Consultar/Añadir/Editar por módulo. *Decidir* la autorización no está aquí: corresponde a EP-07 (no implementado).

## Solo ingresos extemporáneos (2026-10-27)

> [!note] Propuesta implementada — [[Decisiones pendientes|D-38, D-39]]
> Visitas atiende ingresos **extemporáneos**. Una persona con permiso **Recurrente** recibe 409 al registrar la visita, salvo excepción explícita (`excepcionTipoAcceso = true`) con `motivoExcepcion` ≥ 10 caracteres (400 si falta). La excepción se guarda en `visita.excepcion_tipo_acceso` / `motivo_excepcion` (CHECK `ck_visita_excepcion_motivo`) y se audita `ExcepcionTipoAcceso` con usuario e instante. Para personas extemporáneas la marca se ignora.

- UI: el diálogo indica «Ingreso extemporáneo»; la búsqueda y la persona seleccionada muestran su permiso; si es recurrente se bloquea «Continuar» hasta marcar la excepción y escribir el motivo. Registrar persona nueva reutiliza el formulario de [[Personas]] con Extemporáneo preseleccionado (no duplica datos: la visita referencia `id_persona`).
- Listado y detalle muestran el permiso actual de la persona y la excepción con su motivo.
- Verificado en navegador el 2026-10-27 sobre base desechable; migración aplicada a `conecsia`.

## Estados (Implementado)

Persistidos en `visita.estado` (CHECK): `Pendiente → Autorizada → Ingreso → Finalizada`; antes del ingreso `Pendiente|Autorizada → Rechazada | Cancelada | Vencida`. La UI muestra «Ingreso» como **Dentro**.

- Al registrar: autorización **Aprobada** ⇒ Autorizada; autorización **Pendiente** ⇒ Pendiente; sin autorización ⇒ Pendiente, salvo que el parámetro `visitas.permite_sin_autorizacion = true` (en `parametro_organizacion`, **ausente = no permitido**) ⇒ Autorizada con `politica_aplicada` y auditoría `AplicarPoliticaSinAutorizacion`. No se atribuye aprobación a nadie.
- Una visita Pendiente con autorización que después se aprueba (EP-07) puede ingresar sin otra acción: la vigencia se evalúa en cada entrada.
- Al intentar ingresar, si la autorización quedó Rechazada ⇒ visita **Rechazada**; Cancelada/Revocada ⇒ **Cancelada**; Vencida/Agotada o fecha fin/fecha esperada pasada ⇒ **Vencida** (se persiste y audita `ActualizarEstado`). Propuesta: [[Decisiones pendientes|D-37]].
- Cancelar solo antes del ingreso; con la persona dentro se registra la salida.
- Finalizar una visita **no** cierra la autorización recurrente (Base).

## Validación de entrada (Implementado, Verificado)

En una transacción, tras `SELECT … FOR UPDATE` sobre la visita, el servidor comprueba y devuelve **409 con `motivos`** (lista concreta) si algo falla, sin abrir permanencia:

- Visita en alcance (organización + sede) y punto de acceso activo de la misma sede, con modo Entrada/Ambos y asignado al operador si tiene puntos asignados.
- Persona, sede y destino activos; estado de la visita; fecha esperada (programada/vencida); política sin autorización aún vigente.
- Autorización: estado Aprobada, misma persona/sede/destino, fecha inicio/fin, días y franjas (`autorizacion_horario`), zonas (`autorizacion_zona`, incluye ancestros del punto) y cupo.
- Sin otra permanencia abierta de la persona en la sede ([[Decisiones pendientes|D-07]], Propuesta PRD §6.8).
- Calendario provisional ([[Decisiones pendientes|D-06]]): hora local de la sede, franja `[inicio, fin)`, días ISO 1 = lunes, sin franjas nocturnas ni festivos.
- El horario esperado de la visita es informativo; no se valida.

Efectos al confirmar: `movimiento_acceso` (Entrada, Confirmado, medio Manual) + `permanencia` Abierta + visita Ingreso + auditoría `RegistrarEntrada` + `solicitud_idempotente`, todo o nada.

## Integridad y concurrencia (Implementado, Verificado)

- **Idempotencia**: clave por organización; la réplica devuelve la misma respuesta (cabecera `Idempotent-Replayed`); misma clave con otro cuerpo ⇒ 409. La UI genera una clave por intento y la reutiliza en reintentos.
- **Bloqueo de fila** de la visita para serializar operadores; índices únicos `ux_permanencia_visita` (una permanencia por visita) y `ux_permanencia_abierta_persona_sede` (parcial, `estado = 'Abierta'`) como última defensa.
- **Consumo atómico del cupo**: `UPDATE … WHERE ingresos_consumidos < cupo`; la modalidad Única tiene cupo 1 aunque no lo declare (Base).
- CHECK de permanencia: cerrada ⇒ `cerrada_en >= abierta_en`, `duracion_segundos >= 0` y movimiento de salida.

## Salida (Implementado, Verificado)

Cierra la permanencia abierta de la visita, guarda instante, punto, operador y duración. No evalúa la autorización (revocación o vencimiento posteriores no la impiden). Salida repetida o sin entrada abierta ⇒ 409 explícito con auditoría `Denegado`; nunca modifica el historial. La regularización de omisiones sigue bloqueada por [[Decisiones pendientes|D-08]].

## Alcance por sede y punto (Propuesta, D-34)

- Sedes: las de `membresia_sede`; **sin asignaciones, todas las de la organización**. Fuera de alcance ⇒ 404 en consulta y operación, 403 al registrar en esa sede.
- Puntos: los de `membresia_punto_acceso`; sin asignaciones, todos los de las sedes del alcance.
- Responsable: membresía activa de la organización; si el destino tiene responsables designados (`area_responsable`), debe ser uno de ellos ([[Decisiones pendientes|D-36]]).

## Auditoría (Implementado, Verificado)

Entidad `Visita`: `Registrar`, `AplicarPoliticaSinAutorizacion`, `VincularAutorizacion`, `Cancelar`, `ActualizarEstado`, `RegistrarEntrada`, `RegistrarSalida` (Exitoso) y denegaciones de entrada/salida (`Denegado`, con motivos). Actor, IP y correlación salen del servidor.

## Interfaz (Implementado, Verificado en navegador)

`/visitas` (`frontend/src/features/visitas/`):
- Pestaña **Visitas**: indicadores (pendientes, autorizadas, dentro, finalizadas) que filtran, búsqueda por documento/nombre, filtros por estado, sede, destino, responsable y fechas, orden y paginación en servidor; en móvil la tabla pasa a tarjetas.
- **Nueva visita** en 3 pasos: visitante (búsqueda por documento que reutiliza la persona; si no existe, el formulario de Personas), destino (sede, área, responsable, motivo, fecha/horario esperado), autorización.
- **Detalle**: motivos concretos si no puede ingresar, autorización, permanencia, movimientos y acciones (entrada, salida, cancelar, vincular) según permisos.
- Pestaña **Personas dentro** (con Control de accesos · Consultar): permanencias abiertas con destino, hora de entrada y tiempo transcurrido.

> [!warning] Sin SignalR
> El proyecto no usa SignalR. La presencia se vuelve a pedir cada 30 s, al enfocar la ventana y al reconectar; la API es la fuente persistida.

## Migración

`20261027015608_VisitasCicloOperativo`: columnas nuevas en `visita` (fecha/horario esperado, `motivo_cierre`, `id_usuario_actualiza`, `actualizado_en`) y `permanencia.duracion_segundos`; CHECK de estados; FK compuestas (persona, sede, área, autorización, responsable, visita, movimientos) y a usuario; índices únicos de permanencia; ruta `/visitas` en `app_modulos`. **Corrige** el índice único `zona(org, sede)`, que impedía más de una zona o punto de acceso por sede. Aplicada a `conecsia` el 2026-10-26.

## Retirado

Scaffolds `VisitaController`, `PermanenciaController`, `MovimientoAccesoController` y `SolicitudIdempotenteController`: aceptaban la entidad completa del cliente, permitían `DELETE` físico y no validaban alcance.

## Pendiente

- [ ] Autorizaciones de personas (EP-07: HU-023..027): crear, decidir, revocar. Hoy las visitas solo **leen** `autorizacion`; no se simula ninguna aprobación.
- [ ] Gestión de puntos de acceso y zonas (HU-015) y de responsables por área (HU-014): no tienen pantalla; los datos de verificación se cargan por SQL (`backend/scripts/seed-verificacion-visitas.sql`).
- [ ] QR (EP-09): `token_qr` existe pero no se emite ni lee.
- [ ] Idempotencia opcional del registro de visita; retención de `solicitud_idempotente` (D-14).
- [ ] D-04, D-05 (reingresos), D-06, D-07, D-08, D-34, D-35, D-36, D-37.
- [ ] Tiempo real con SignalR si se adopta.

## Retiro de la excepción por tipo de acceso (2026-10-28)
- Visitas ya no rechaza personas «Recurrentes» ni pide excepción (D-39 obsoleta). `visita.excepcion_tipo_acceso` y `motivo_excepcion` se conservan como histórico.
- La entrada valida además la franja propia de Temporal, Única y Diaria (`ReglasAutorizacion.MotivosFranja`). Ver [[Control de accesos]].

## Persona recurrente en Visitas (2026-10-29, Implementado — Propuesta D-46)
- El diálogo «Nueva visita» se titula **Ingreso extemporáneo**; la búsqueda y la persona elegida muestran su tipo de permiso derivado.
- Si la persona tiene acceso recurrente vigente en la sede elegida, solo puede registrarse vinculando esa autorización (episodio, CA-11); se preselecciona si hay una sola y «Registrar visita» queda bloqueado con un mensaje si no.
- Servidor: `POST /api/visitas` responde 409 con la autorización a vincular y audita `RechazarVisitaRecurrente` (persona, sede, autorizaciones; resultado Rechazado). Las validaciones del vínculo (400) tienen prioridad. Sin excepción hasta resolver D-46.
