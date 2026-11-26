---
tipo: modulo
estado: implementado
fuentes: [PRD v0.1]
relacionados: [RF-09, RF-10, D-06, D-18, D-34, D-40, D-41, D-42, ADR-007, ADR-008, ADR-009, ADR-024]
actualizado: 2026-11-11
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

## Módulo unificado — Fase 3 backend (2026-11-04, Implementado; ADR-029 en Propuesta)

Nuevo controlador `/api/accesos` (`AccesosController.cs`, `AccesosController.Movimientos.cs`) y `/api/accesos/configuracion` (`AccesosConfiguracionController.cs`). Reglas puras en `Services/ReglasAcceso.cs`; validación estructural con **FluentValidation** (`Validators/`, filtro global `ValidacionFluentFilter`). OpenAPI con comentarios XML.

| Operación | Endpoint | Permiso (Propuesta, D-40/D-52) |
|---|---|---|
| Listar / detalle / opciones | `GET /api/accesos`, `/{id}`, `/opciones` | Control de accesos·Consultar |
| Crear (único punto de creación) | `POST /api/accesos` | Control de accesos·Añadir (+ Añadir de Vehículos/Dispositivos si lleva objetos) |
| Aprobar / rechazar | `POST /{id}/aprobar`, `/{id}/rechazar` | Autorizaciones·Editar, o el anfitrión si la categoría usa flujo Anfitrion |
| Revocar / cancelar | `POST /{id}/revocar` · `/{id}/cancelar` | Autorizaciones·Editar · Control de accesos·Editar |
| Cambiar horario | `PUT /{id}/horario` | Control de accesos·Editar; si está Aprobado, Autorizaciones·Editar |
| Objetos | `POST /{id}/objetos`, `/{id}/objetos/{idObjeto}/aprobar\|rechazar\|revocar` | Añadir del módulo del objeto · Autorizaciones·Editar |
| Entrada / salida (`Idempotency-Key`) | `POST /{id}/entrada` · `POST /permanencias/{id}/salida` | Control de accesos·Añadir · Editar |
| Presencia | `GET /presencia` | Control de accesos·Consultar |
| Historial / CSV | `GET /historial`, `/historial/exportar` (filtros persona, anfitrión, sede, destino, placa, tipo, resultado, fechas) | Control de accesos·Consultar (exportación auditada) |
| Bitácora | `GET /{id}/bitacora` | Control de accesos·Consultar |
| Categorías, horario base, parámetros | `GET\|POST\|PUT /configuracion/categorias`, `GET\|PUT /configuracion/horario-base`, `GET\|PUT /configuracion` | Consultar · **Configuración·Editar** |

**Reglas (Implementado y Verificado por pruebas):**
- Persona: se selecciona por id; se busca o crea en [[Personas]] desde la misma pantalla (no se duplica la validación de documento).
- La categoría limita los tipos de permanencia, exige motivo y admite o no vehículos/dispositivos. Flujo **Automatico** ⇒ nace Aprobado (`origen = Politica`, sin decisor humano, auditado `AprobarPorPolitica`); objetos también aprobados.
- Recurrente: varias franjas por día sin solape o horario base (sede > organización). Permanente: solo con `accesos.permite_permanente` (o la clave anterior).
- Entrada: bloqueo de fila del registro, vigencia y horario en hora local de la sede; denegado ⇒ `movimiento_acceso` **Denegado** con motivos + auditoría, sin permanencia. Con `accesos.fuera_horario = Alertar` el fuera de horario ingresa con alerta auditada `IngresoFueraDeHorario` (la vigencia siempre bloquea) — Propuesta D-51.
- Cada entrada crea el episodio (`visita`) vinculado; la salida lo finaliza sin cerrar el registro.
- Respuesta de entrada/salida compacta (`MovimientoConfirmadoDto`) también en la réplica idempotente.
- Nuevas organizaciones reciben categorías de su tipo (`ReglasAcceso.SembrarCategoriasAsync`).
- La auditoría guarda ahora `agente_usuario` (User-Agent, 300 caracteres) en todos los controladores de administración.

**Pruebas:** `AccesosTests` (9): temporal/recurrente/permanente con horario; flujo automático; persona aprobada con vehículo rechazado y motivo; motivo obligatorio y decisión del anfitrión; fuera de horario bloqueado y auditado, alertar; entrada–presencia–salida, idempotencia, único no consumido dos veces, historial y CSV; bitácora inmutable; aislamiento; reglas puras.

> [!todo] Pendiente Fase 3/4
> - Los endpoints antiguos (`/api/visitas`, `/api/autorizaciones-acceso`, `/api/autorizaciones-objeto`) siguen activos hasta migrar el frontend (Fase 4); entonces se retiran junto con las restricciones NOT NULL de categoría y de `visita.id_autorizacion`.
> - Exportación Excel nativa (hoy CSV con BOM). Movimientos de vehículos/dispositivos en portería. Cupos de parqueadero (D-54). D-50..D-55 abiertas.

## Módulo unificado — Fase 4 frontend (2026-11-04, Implementado y Verificado en navegador)

`frontend/src/features/control-accesos/`: `ControlAccesosPage.tsx` (pestañas **Registros**, **Personas dentro**, **Historial**), `NuevoAcceso.tsx` (formulario único), `DetalleAcceso.tsx` (aprobación/rechazo/revocación/cancelación, objetos, entrada, historial, movimientos y bitácora), `reglasAcceso.ts` (validación de cortesía), `formato.ts`, `apiControlAccesos.ts` (`/api/accesos`).

- Se **retiraron** las pantallas `features/visitas` y `features/autorizaciones`; `/visitas` y `/autorizaciones` redirigen a `/control-accesos`. Sus estilos se movieron a `control-accesos/`.
- Menú: los módulos `visitas` y `autorizaciones` quedan **solo como permisos** (`SOLO_PERMISO` en `navegacion.ts`); `Autorizaciones · Editar` sigue habilitando aprobar y revocar.
- Formulario: persona por búsqueda (o «Registrar persona nueva» con el formulario de [[Personas]], sin duplicar); la categoría limita los tipos de permanencia; franjas por día con «Agregar franja» o «Usar el horario base»; vehículos y dispositivos activos de la persona como casillas.
- Pruebas `vitest`: `ControlAccesosPage.test.tsx` (4) + ajuste del menú en `DashboardPage.test.tsx`. Suite 42/44: las 2 de Administración que fallan son sensibles a tiempos y pasan aisladas (ya documentado).
- Navegador (API :5192 + frontend :5174 sobre `conecsia_verificacion`, semilla `backend/scripts/seed-verificacion-accesos.sql`): registros migrados visibles; nuevo acceso Contratista · Recurrente L–V 07–17 → aprobado → entrada → «Personas dentro» → salida → historial con 2 movimientos; exportación CSV 200; menú sin Visitas ni Autorizaciones; 375 px sin desborde.

> [!todo] Pendiente
> - Pantalla de configuración (categorías, horario base, parámetros): la API existe (`/api/accesos/configuracion`), falta la interfaz.
> - Retirar en el backend `/api/visitas`, `/api/autorizaciones-acceso` y `/api/autorizaciones-objeto` tras portar sus pruebas de concurrencia (Fase 5) y activar las restricciones NOT NULL diferidas.
> - `seed-verificacion-visitas.sql` quedó obsoleto (usa zonas retiradas por ADR-028).

## Fase 5 — pruebas y cierre de la unificación (2026-11-04, en verificación)

- **Retirados** `VisitasController`, `AutorizacionesAccesoController` y `AutorizacionesObjetoController` (y `VisitasTests`, `AutorizacionesAccesoTests`, la parte de objetos de `DispositivosAutorizacionesObjetoTests`). Único punto de creación: `POST /api/accesos`.
- Migración `20261104204214_RestriccionesRegistroAccesoUnificado`: categoriza autorizaciones sin categoría y crea, `NOT VALID` (no reescriben el histórico), `ck_autorizacion_categoria`, `ck_autorizacion_motivo_decision` y `ck_visita_autorizacion`; `app_modulos.ruta = NULL` para `visitas` y `autorizaciones`.
- Pruebas nuevas en `AccesosTests`: entradas simultáneas (una permanencia, único consumido una vez), registros incompatibles en paralelo y decisiones simultáneas, idempotencia, salida tras revocación y recurrente reutilizable, alcance por sede y permisos de entrada/salida, endpoints antiguos 404, restricciones en base; reglas puras ampliadas (Temporal, Única con franja, Permanente, horario base, normalización).
- `MigracionAccesosTests`: datos del esquema anterior ⇒ sin pérdida de visitas ni movimientos, toda visita con registro, política ⇒ flujo Automatico, bitácora y auditoría.
- `PuntosAccesoTests` portado a `/api/accesos` (usaba los endpoints retirados).
- **Verificado 2026-11-04:** backend 91 pruebas (89 en la corrida completa + las 2 de Puntos de acceso corregidas y re-ejecutadas, 7/7); frontend 42/44 (las 2 de Administración sensibles a tiempos, ya conocidas).

## Portería de vehículos y dispositivos (2026-11-11, Implementado y Verificado)
Decisión: [[ADR-030 Movimientos de vehículos y dispositivos]]. Reglas *Propuesta* en D-57 y custodia en D-10.
- En el detalle del registro, cada vehículo o dispositivo muestra «Dentro» y su hora. Ofrece **Registrar entrada** cuando el objeto y el registro están aprobados y la persona ya está dentro, y **Registrar salida** cuando el objeto está dentro. Se elige el punto: los de solo salida no se ofrecen para entrar y viceversa.
- **Personas dentro** muestra «Con …» por persona y una sección **Vehículos y dispositivos dentro**, que marca en rojo los que quedaron sin su responsable y permite registrar su salida.
- Al registrar la salida de la persona se avisa qué objetos siguen dentro. **No se cierran automáticamente** (D-10).
- Endpoints: `POST /api/accesos/{id}/objetos/{idObjeto}/entrada|salida` (`Idempotency-Key`) y `GET /api/accesos/objetos-dentro`.
- El anfitrión del formulario se limita a los responsables designados del destino, cuando los hay ([[Sedes]], HU-014).
- Pendiente: historial y reporte por vehículo o dispositivo; parqueaderos (D-54); conductor distinto del responsable (D-10).
