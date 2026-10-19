---
tipo: epica
estado: propuesta
fase: F2
requisitos: [RF-11, RF-12, RF-13, RN-03, RN-09, CA-07, CA-08, CA-11]
adr: [ADR-005, ADR-007, ADR-009, ADR-010, ADR-012, ADR-014]
decisiones: [D-04, D-05, D-12]
actualizado: 2026-10-17
tags: [conecsia, backlog, epica]
---

# EP-08 Visitas

> [!note] Fase: [[PLAN-DE-TRABAJO#F2]] · Requisitos: RF-11, RF-12 · ADR: [[ADR-009 Ciclos de estado separados y vigencia calculada]] (Propuesta)

## Objetivo de la épica
Gestionar [[Visita|visitas]] como episodios distintos de la [[Autorización]]: la visita ocasional creada en recepción y la visita esperada por preautorización/invitación, con el ciclo Pendiente → Autorizada → Ingresó → Finalizada (Propuesta ADR-009, PRD §6.7). Finalizar una visita **no** cierra una autorización recurrente vigente (Base).

## Alcance / fuera de alcance
- **Dentro:** registro de visita ocasional con solicitud al responsable; preautorización con visita esperada e invitación; reutilización de la visita esperada al llegar; registro explícito de la política aplicada cuando no se exige autorización.
- **Fuera:** confirmación de entrada y apertura de permanencia (HU-032/HU-033), envío real de correo (HU-042, D-12), QR (EP-09), bandeja del autorizador completa (HU-039).

## Historias
| ID | Historia | Tamaño | Estado | Bloqueada por |
|---|---|---|---|---|
| HU-028 | Visita ocasional desde recepción | M | Bloqueada parcial | D-04 (cuándo se exige autorización) |
| HU-029 | Preautorización e invitación | M | Lista para refinar | D-12 (envío por correo), D-05 |

---

## HU-028 Visita ocasional desde recepción

**Como** operador de acceso **quiero** registrar una visita no anunciada y solicitar autorización al responsable del destino **para** que la persona solo ingrese tras una decisión válida.

| Campo | Valor |
|---|---|
| Épica / Fase | EP-08 / F2 |
| Requisitos | RF-11, RN-03, RN-10, CA-07 |
| Tamaño relativo | M (propuesto, sin estimación aprobada) |
| Estado | Bloqueada parcial (D-04) |

### Objetivo
Implementar el flujo Base PRD §6.3: identificar/registrar persona → elegir sede, destino, responsable y motivo → registrar visita **Pendiente** + solicitud de autorización (HU-025) → la decisión habilita o impide el proceso de entrada.

### Actores
Operador de acceso (registra), Autorizador/responsable del destino (decide).

### Dependencias
- Historias: HU-019 (persona por documento), HU-025 (solicitud/decisión), HU-027, HU-014, HU-018; consumidores HU-032, HU-039.
- ADR: [[ADR-009 Ciclos de estado separados y vigencia calculada]], [[ADR-005 Contexto de organización y autorización por permisos]], [[ADR-007 Concurrencia e idempotencia en accesos]], [[ADR-014 Auditoría append-only]] (Propuesta).
- Decisiones: D-04 (cuándo una política exige autorización y quién decide).

### Contrato funcional
- **Entradas:** `personaId`, `sedeId`, `destinoId`, `responsableId`, `motivo`, `puntoAccesoId`, `Idempotency-Key`.
- **Reglas (servidor):** permiso `visitas.registrar` + alcance sobre la sede/punto del operador; persona activa de la organización; responsable debe ser autorizador del destino (HU-014); se crea visita Pendiente vinculada a una solicitud de autorización Pendiente (modalidad Única por defecto, Propuesta). El operador **no** puede aprobar por registrar (Base). Rechazo, cancelación o vencimiento de la solicitud ⇒ visita Rechazada/Cancelada/Vencida, inutilizable para entrar.
- **Ingreso sin autorización:** solo si una política de la organización lo permite explícitamente; se registra la política aplicada, persona, operador y destino, **sin fabricar aprobación** (Base). Hasta D-04 la política existe como configuración **deshabilitada por defecto**.
- **Efectos persistidos:** `visita` (estado, sede, destino, responsable, motivo, operador, instante) y autorización Pendiente.
- **Errores:** 403 sin permiso/alcance; 404 entidades de otra organización; 422 responsable no válido para el destino o persona inactiva.
- **Auditoría:** `visita.registrada`, transiciones de visita derivadas de la decisión, uso de política "sin autorización".
- **Endpoints propuestos:** `POST /api/v1/visitas/ocasionales`, `GET /api/v1/visitas?sedeId=&estado=`, `GET /api/v1/visitas/{id}`.

### Criterios de aceptación
- [ ] **Dado** una visita ocasional Pendiente y una política que exige aprobación **cuando** el operador intenta confirmar la entrada **entonces** se deniega y no se abre permanencia (CA-07).
- [ ] **Dado** una visita Pendiente **cuando** el operador intenta aprobarla por API **entonces** 403 (CA-07).
- [ ] **Dado** la solicitud aprobada por el responsable **cuando** se consulta la visita **entonces** pasa a Autorizada con autorizador atribuido (CA-06).
- [ ] **Dado** la solicitud rechazada **cuando** se intenta usar la visita **entonces** queda Rechazada y no habilita entrada.

### Tareas backend
- [ ] Entidad `Visita` con máquina de estados propia y FK a autorización; migración con `organization_id`.
- [ ] Comando `RegistrarVisitaOcasional` transaccional (visita + solicitud + auditoría); handler que sincroniza el estado de la visita al decidirse la autorización.
- [ ] Bandera de política `permiteIngresoSinAutorizacion` (deshabilitada) con registro de política aplicada.

### Tareas frontend
- [ ] Flujo de recepción: búsqueda por documento, alta rápida, selección de destino/responsable filtrada por alcance.
- [ ] Estado de la visita en espera de decisión, sin indicar "autorizado" hasta confirmación del servidor.

### Pruebas
- [ ] Unitarias: transiciones de visita y su sincronía con la decisión.
- [ ] Integración (PostgreSQL real): transacción visita + autorización + auditoría; idempotencia del registro.
- [ ] Aislamiento / permisos negativos: operador de org A no registra visitas con destinos de org B; operador fuera de su sede ⇒ 403; operador no aprueba.
- [ ] Concurrencia: doble envío del registro ⇒ una sola visita.
- [ ] E2E / UI: registrar visita, aprobar como autorizador, ver Autorizada.

### Definition of Done
Cumple la [[PLAN-DE-TRABAJO#Definition of Done general|DoD general]] y además:
- [ ] CA-07 verificado de extremo a extremo junto con HU-032.
- [ ] Ninguna aprobación queda atribuida a quien no decidió.

### Riesgos y decisiones pendientes
> [!todo] D-04 — Cuándo una organización puede permitir ingreso sin autorización y quién decide. Hasta decidir: siempre se exige decisión.

> [!todo] D-12 — Notificación al responsable (correo). Hasta decidir, solo bandeja en la aplicación (HU-039).

---

## HU-029 Preautorización e invitación

**Como** usuario facultado (anfitrión/autorizador) **quiero** preautorizar a un visitante y generar su invitación **para** que al llegar recepción recupere la visita esperada sin volver a capturar datos.

| Campo | Valor |
|---|---|
| Épica / Fase | EP-08 / F2 |
| Requisitos | RF-12, RF-15, RN-09, CA-08, CA-11 |
| Tamaño relativo | M (propuesto, sin estimación aprobada) |
| Estado | Lista para refinar |

### Objetivo
Base PRD §6.4: registrar visitante, fecha/horario esperado, sede, destino, responsable y motivo; crear autorización Aprobada (HU-023) y visita esperada; generar la invitación y, cuando corresponda, el [[QR]] (HU-030). La invitación **no** equivale a entrada ni sustituye la verificación de identidad.

### Actores
Autorizador / Administrador de sede (crea), Operador de acceso (recupera al llegar), visitante (sin credenciales).

### Dependencias
- Historias: HU-023, HU-024, HU-030, HU-019; HU-042/HU-043 para correo.
- ADR: [[ADR-009 Ciclos de estado separados y vigencia calculada]], [[ADR-010 Token QR opaco]], [[ADR-012 Outbox transaccional y procesos de fondo]] (Propuesta).
- Decisiones: D-12 (correo), D-05 (episodios en diaria/temporal).

### Contrato funcional
- **Entradas:** datos de persona (o `personaId`), `sedeId`, `destinoId`, `responsableId`, `motivo`, modalidad y calendario (HU-024), `generarQr` (bool), `Idempotency-Key`.
- **Reglas (servidor):** mismas de HU-023 (permiso `autorizaciones.emitir` + alcance); visita esperada en estado Autorizada vinculada a la autorización; para modalidad recurrente cada permanencia genera un episodio de visita independiente (Propuesta PRD §6.4, CA-11); la invitación contiene solo datos mínimos (sede, fecha, destino) y el QR opaco, nunca documento ni foto.
- **Al llegar:** el operador recupera la visita por QR (HU-031) o por documento; se reutiliza si coincide con la autorización; si no coincide (otra sede/fecha) no se reutiliza y se informa la causa.
- **Efectos persistidos:** autorización, visita esperada, invitación (registro de emisión) y mensaje en outbox si hay correo habilitado.
- **Errores:** 403; 404; 422 condiciones inválidas; módulo de correo inhabilitado ⇒ invitación disponible para descarga/impresión, sin error.
- **Auditoría:** `invitacion.emitida`, `invitacion.reenviada` (rota token, HU-030).
- **Endpoints propuestos:** `POST /api/v1/preautorizaciones`, `GET /api/v1/visitas/esperadas?sedeId=&fecha=`, `POST /api/v1/preautorizaciones/{id}/reenviar-invitacion`.

### Criterios de aceptación
- [ ] **Dado** una preautorización con QR **cuando** se emite la invitación **entonces** el código solo contiene un token opaco y permite recuperar la autorización (CA-08).
- [ ] **Dado** una visita esperada **cuando** el visitante llega y la autorización es vigente **entonces** recepción la recupera sin recapturar datos y aún debe confirmar identidad y entrada.
- [ ] **Dado** una preautorización recurrente **cuando** el visitante sale y vuelve en otra ventana válida **entonces** se crea un nuevo episodio de visita y la autorización sigue Aprobada (CA-11).
- [ ] **Dado** un usuario sin permiso **cuando** llama a crear preautorización **entonces** 403.

### Tareas backend
- [ ] Comando `CrearPreautorizacion` (autorización + visita esperada + token QR + outbox) en una transacción.
- [ ] Consulta de visitas esperadas por sede/fecha local filtrada por alcance.
- [ ] Generación de la invitación (contenido mínimo) y reenvío con rotación de token.

### Tareas frontend
- [ ] Formulario de preautorización y vista de invitación (QR descargable/imprimible).
- [ ] Lista de visitas esperadas en recepción.

### Pruebas
- [ ] Unitarias: reutilización vs. no coincidencia; creación de episodios en recurrentes.
- [ ] Integración (PostgreSQL real): transacción completa incluido outbox; la invitación no contiene datos personales sensibles.
- [ ] Aislamiento / permisos negativos: visitas esperadas de org B invisibles para org A; sin permiso ⇒ 403.
- [ ] Concurrencia: doble envío ⇒ una sola preautorización (idempotencia).
- [ ] E2E / UI: preautorizar, abrir invitación, recuperar en recepción.

### Definition of Done
Cumple la [[PLAN-DE-TRABAJO#Definition of Done general|DoD general]] y además:
- [ ] CA-08 verificado inspeccionando el contenido real del QR emitido.
- [ ] Invitación no marca ingreso ni cambia la visita a Ingresó.

### Riesgos y decisiones pendientes
> [!todo] D-12 — Proveedor y política de correo; hasta decidir, la invitación se entrega por descarga/impresión.

> [!todo] D-05 — Si Diaria/Temporal generan varios episodios (reingresos). Hasta decidir: cupo 1.

> [!todo] Contenido del QR (token vs. URL) por decidir en la revisión de [[ADR-010 Token QR opaco]].
