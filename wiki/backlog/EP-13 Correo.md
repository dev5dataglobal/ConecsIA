---
tipo: epica
estado: propuesta
fase: F4
requisitos: [RF-22, CA-17]
adr: [ADR-004, ADR-012, ADR-014, ADR-018]
decisiones: [D-12, D-14, D-16]
actualizado: 2026-10-17
tags: [conecsia, backlog, epica]
---

# EP-13 Correo

> [!note] Fase: [[PLAN-DE-TRABAJO#F4]] · Requisitos: RF-22 · ADR: [[ADR-012 Outbox transaccional y procesos de fondo]] (Propuesta)

## Objetivo de la épica
Enviar correos de eventos de negocio con plantillas por organización mediante una cola durable con reintentos y deduplicación, sin que una falla del proveedor revierta operaciones de acceso.

## Alcance / fuera de alcance
- **Dentro (Base, PRD §11.2):** eventos solicitud, aprobación/rechazo, invitación, llegada, próximo vencimiento, revocación; plantillas por organización.
- **Dentro (Propuesta):** outbox, reintentos con backoff, registro de resultado.
- **Fuera:** proveedor, remitentes, destinatarios por política y política de reintentos concreta (D-12); no se afirma entrega garantizada; módulo de correo respeta habilitación por organización (D-16).

## Historias
| ID | Historia | Tamaño | Estado | Bloqueada por |
|---|---|---|---|---|
| HU-042 | Outbox y envío de correo con reintentos | M | Lista para refinar (envío real bloqueado) | D-12 |
| HU-043 | Plantillas de correo por organización | M | Lista para refinar | D-12 (destinatarios) |

---

## HU-042 Outbox y envío de correo con reintentos

**Como** administrador de organización **quiero** que los correos se envíen de forma confiable y desacoplada **para** que una falla de correo no afecte la operación de acceso.

| Campo | Valor |
|---|---|
| Épica / Fase | EP-13 / F4 |
| Requisitos | RF-22, CA-17 |
| Tamaño relativo | M (propuesto, sin estimación aprobada) |
| Estado | Lista para refinar; adaptador de proveedor **bloqueado por D-12** |

### Objetivo
Mensajes de correo escritos en outbox en la misma transacción del evento, procesados por `BackgroundService` con reintentos, deduplicación y registro de resultado, detrás de una interfaz `IEmailSender`.

### Actores
Sistema; Administrador de organización (consulta de envíos).

### Dependencias
- Historias: HU-033 (evento llegada), historias de autorización/invitación (F2), HU-043.
- ADR: [[ADR-012 Outbox transaccional y procesos de fondo]], [[ADR-018 Observabilidad sin datos sensibles]], [[ADR-014 Auditoría append-only]] (Propuesta).
- Decisiones: **D-12** (proveedor, remitentes, política de reintentos), D-14 (retención de registros de envío), D-16 (módulo habilitado).

### Contrato funcional
- **Entradas:** fila outbox `{organization_id, evento, entidad_id, clave_dedup}`.
- **Reglas (servidor):** solo si el módulo correo está habilitado para la organización; clave de deduplicación única por `(organization_id, evento, entidad_id)`; reintentos con backoff y máximo **parametrizable** (valor Pendiente D-12); tras agotar ⇒ estado `Fallido` visible; el envío nunca participa en la transacción de acceso.
- **Efectos:** `correo_envio(estado: Pendiente/Enviado/Fallido, intentos, ultimo_error_sin_PII)`.
- **Errores:** falla del proveedor ⇒ reintento; destinatario sin correo ⇒ `SIN_DESTINATARIO` registrado.
- **Auditoría/logs:** sin direcciones completas ni tokens QR en logs (ADR-018).
- **Endpoints propuestos:** `GET /api/v1/correos/envios?estado=` (admin org).

### Criterios de aceptación
- [ ] **Dado** una entrada confirmada y el proveedor caído **cuando** se procesa el correo de llegada **entonces** la entrada permanece confirmada y el correo se reintenta (CA-17).
- [ ] **Dado** un reintento exitoso **cuando** se completa **entonces** no se duplica la operación de acceso ni el correo (CA-17).
- [ ] **Dado** una organización con el módulo inactivo **cuando** ocurre el evento **entonces** no se encola correo.

### Tareas backend
- [ ] Tabla `correo_envio`, procesador de fondo con lock, `IEmailSender` con implementación de desarrollo (Mailpit/archivo) hasta D-12.

### Tareas frontend
- [ ] Vista de estado de envíos para administrador de organización.

### Pruebas
- [ ] Unitarias: política de backoff y deduplicación.
- [ ] Integración (PostgreSQL real): rollback de entrada ⇒ sin correo encolado.
- [ ] Concurrencia/idempotencia: dos procesadores ⇒ un envío.
- [ ] Aislamiento / permisos negativos: envíos de otra organización invisibles; operador ⇒ 403.
- [ ] E2E: invitación genera correo en servidor SMTP de desarrollo.

### Definition of Done
Cumple la [[PLAN-DE-TRABAJO#Definition of Done general|DoD general]] y además:
- [ ] Prueba con proveedor simulado en falla verificando CA-17.

### Riesgos y decisiones pendientes
> [!todo] D-12 — proveedor de correo, remitentes, política de reintentos. No se elige proveedor en esta historia.

---

## HU-043 Plantillas de correo por organización

**Como** administrador de organización **quiero** configurar las plantillas de cada evento de correo **para** comunicar con la terminología y marca de mi organización.

| Campo | Valor |
|---|---|
| Épica / Fase | EP-13 / F4 |
| Requisitos | RF-22, CA-17 |
| Tamaño relativo | M (propuesto, sin estimación aprobada) |
| Estado | Lista para refinar |

### Objetivo
CRUD versionado de plantillas por evento y organización con variables permitidas, render seguro y plantilla base del sistema.

### Actores
Administrador de organización.

### Dependencias
- Historias: HU-042.
- ADR: [[ADR-004 Tenancy con base compartida y organization_id]], [[ADR-014 Auditoría append-only]] (Propuesta).
- Decisiones: D-12 (destinatarios por evento), D-01 (terminología).

### Contrato funcional
- **Entradas:** evento, asunto, cuerpo, idioma, activo.
- **Reglas (servidor):** permiso `correo.plantillas.gestionar`; solo variables de una lista blanca por evento (sin datos sensibles; el QR se referencia por enlace/token, no por datos personales); HTML sanitizado; se usa la plantilla de la organización del evento, nunca de otra.
- **Efectos:** `plantilla_correo` con versión; el envío registra versión usada.
- **Errores:** `VARIABLE_NO_PERMITIDA` (422), `PLANTILLA_INVALIDA` (422).
- **Auditoría:** creación/modificación con valores anterior/nuevo.
- **Endpoints propuestos:** `GET/PUT /api/v1/correos/plantillas/{evento}` · `POST …/{evento}/vista-previa`.

### Criterios de aceptación
- [ ] **Dado** una plantilla personalizada **cuando** ocurre el evento **entonces** se usa la plantilla del tenant (CA-17).
- [ ] **Dado** una variable fuera de la lista **cuando** se guarda **entonces** se rechaza.
- [ ] **Dado** un administrador de otra organización **cuando** intenta leer la plantilla **entonces** recibe 404.

### Tareas backend
- [ ] Motor de plantillas con lista blanca y sanitización; versionado.

### Tareas frontend
- [ ] Editor con variables disponibles y vista previa.

### Pruebas
- [ ] Unitarias: render y lista blanca; escape de HTML.
- [ ] Integración (PostgreSQL real): versión usada registrada en el envío.
- [ ] Concurrencia: edición simultánea con concurrencia optimista.
- [ ] Aislamiento / permisos negativos: plantilla de otra organización inaccesible.
- [ ] E2E: editar y previsualizar.

### Definition of Done
Cumple la [[PLAN-DE-TRABAJO#Definition of Done general|DoD general]] y además:
- [ ] Plantillas base para los 6 eventos de PRD §11.2.

### Riesgos y decisiones pendientes
> [!todo] D-12 — destinatarios configurados por evento y remitentes.
