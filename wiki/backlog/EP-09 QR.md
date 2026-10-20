---
tipo: epica
estado: propuesta
fase: F2
requisitos: [RF-15, RN-06, CA-08, CA-09, CA-10]
adr: [ADR-004, ADR-005, ADR-007, ADR-010, ADR-014, ADR-018]
decisiones: [D-13, D-05]
actualizado: 2026-10-17
tags: [conecsia, backlog, epica]
---

# EP-09 QR

> [!note] Fase: [[PLAN-DE-TRABAJO#F2]] · Requisitos: RF-15 · ADR: [[ADR-010 Token QR opaco]] (Propuesta)

## Objetivo de la épica
Emitir, rotar e invalidar un [[QR]] opaco que identifica una [[Autorización]] sin datos personales, y permitir a recepción leerlo para obtener validación y contexto. La lectura **no consume** la autorización ni registra entrada; la confirmación es un comando aparte (HU-032/HU-033). Base: PRD §7.1.

## Alcance / fuera de alcance
- **Dentro:** token ≥128 bits (CSPRNG), hash SHA-256 en base de datos, índice único global, rotación e invalidación, lectura con validación en servidor, auditoría de lecturas y reutilizaciones.
- **Fuera:** validez offline (no se asume, Base), QR como prueba de identidad, confirmación de entrada y consumo atómico (EP-10), alertas por reutilización (HU-040), selección de lectores físicos (D-13).

## Historias
| ID | Historia | Tamaño | Estado | Bloqueada por |
|---|---|---|---|---|
| HU-030 | Emitir, rotar e invalidar token QR | M | Lista para refinar | — (contenido token vs. URL por decidir en ADR-010) |
| HU-031 | Leer QR en recepción y mostrar validación | M | Lista para refinar | D-13 (lectores compatibles) |

---

## HU-030 Emitir, rotar e invalidar token QR

**Como** usuario facultado **quiero** que cada autorización elegible tenga un token QR opaco que pueda rotarse e invalidarse **para** identificar la autorización sin exponer datos personales.

| Campo | Valor |
|---|---|
| Épica / Fase | EP-09 / F2 |
| Requisitos | RF-15, RN-06, CA-08 |
| Tamaño relativo | M (propuesto, sin estimación aprobada) |
| Estado | Lista para refinar |

### Objetivo
Implementar la propuesta [[ADR-010 Token QR opaco|ADR-010]]: token aleatorio opaco, solo hash persistido, rotación al reenviar invitación o ante sospecha de filtración, invalidación al cancelar/revocar.

### Actores
Autorizador, Administrador de sede (emitir/rotar); sistema (invalidar en transiciones de HU-026).

### Dependencias
- Historias: HU-023, HU-026, HU-029.
- ADR: [[ADR-010 Token QR opaco]], [[ADR-004 Tenancy con base compartida y organization_id]], [[ADR-014 Auditoría append-only]], [[ADR-018 Observabilidad sin datos sensibles]] (Propuesta).
- Decisiones: D-13 (formato/lectores); caducidad propia del token pendiente en ADR-010.

### Contrato funcional
- **Entradas:** `autorizacionId`; rotación: `motivo`.
- **Reglas (servidor):** permiso `qr.emitir` + alcance sobre la sede/destino de la autorización; solo autorizaciones Aprobadas no terminales; un único token activo por autorización; el token en claro se devuelve **solo** en la respuesta de emisión/rotación (o se renderiza en la invitación) y nunca se persiste ni se registra en logs; rotar invalida el anterior en la misma transacción; cancelar/revocar/vencer invalida el token (HU-026).
- **Efectos persistidos:** `qr_token` (`organization_id`, `autorizacion_id`, `hash` SHA-256 con índice único global, `emitidoEn`, `emitidoPor`, `invalidadoEn`, `motivoInvalidacion`).
- **Errores:** 403; 404 otra organización; 409 autorización en estado terminal.
- **Auditoría:** `qr.emitido`, `qr.rotado`, `qr.invalidado` (sin token ni hash en la auditoría visible).
- **Endpoints propuestos:** `POST /api/v1/autorizaciones/{id}/qr`, `POST /api/v1/autorizaciones/{id}/qr/rotar`, `GET /api/v1/autorizaciones/{id}/qr/imagen` (solo renderiza un token recién emitido; indicativo).

### Criterios de aceptación
- [ ] **Dado** una autorización Aprobada **cuando** se emite el QR **entonces** el código contiene solo un token opaco sin datos personales ni IDs secuenciales (CA-08).
- [ ] **Dado** un token rotado **cuando** se lee el anterior **entonces** se rechaza como inválido.
- [ ] **Dado** una autorización revocada **cuando** se lee un QR ya mostrado en correo o pantalla **entonces** se rechaza (RN-06, PRD §7.1).
- [ ] **Dado** cualquier emisión **entonces** la base de datos y los logs no contienen el token en claro.

### Tareas backend
- [ ] Generador CSPRNG (≥128 bits, base64url), hash SHA-256, tabla con índice único global y migración.
- [ ] Comandos emitir/rotar; suscriptor de `AutorizacionInvalidada` (HU-026).
- [ ] Filtro de logs que redacta tokens.

### Tareas frontend
- [ ] Mostrar/descargar QR tras emisión; acción "rotar" con motivo y confirmación.

### Pruebas
- [ ] Unitarias: entropía/longitud, formato, no reversibilidad.
- [ ] Integración (PostgreSQL real): unicidad del hash, invalidación transaccional al revocar, un solo token activo.
- [ ] Aislamiento / permisos negativos: emitir QR de autorización de org B ⇒ 404; sin permiso ⇒ 403.
- [ ] Concurrencia: dos rotaciones simultáneas ⇒ un único token activo.
- [ ] E2E / UI: emitir, rotar, verificar que el antiguo falla en HU-031.

### Definition of Done
Cumple la [[PLAN-DE-TRABAJO#Definition of Done general|DoD general]] y además:
- [ ] Revisión de logs y auditoría de una ejecución real sin tokens en claro.

### Riesgos y decisiones pendientes
> [!todo] ADR-010 (Propuesta) — Contenido del QR (solo token vs. URL) y caducidad/rotación periódica.

> [!todo] D-13 — Formatos y lectores compatibles.

---

## HU-031 Leer QR en recepción y mostrar validación

**Como** operador de acceso **quiero** escanear un QR y ver si la autorización es vigente con su contexto autorizado **para** decidir si continúo con la verificación de identidad y la confirmación de entrada.

| Campo | Valor |
|---|---|
| Épica / Fase | EP-09 / F2 |
| Requisitos | RF-15, RN-03, RN-06, CA-08, CA-09 |
| Tamaño relativo | M (propuesto, sin estimación aprobada) |
| Estado | Lista para refinar (lectores: D-13) |

### Objetivo
Resolver el token en servidor, aplicar el [[EP-07 Autorizaciones#HU-027 Evaluador de vigencia de autorización|evaluador de vigencia]] (HU-027) para el punto del operador y devolver resultado + contexto mínimo. **La lectura no consume la autorización ni abre permanencia** (Base PRD §6.2, §7.1).

### Actores
Operador de acceso.

### Dependencias
- Historias: HU-027, HU-030, HU-018 (alcance por punto), HU-021 (foto para verificar identidad, opcional); consumidor HU-032/HU-036.
- ADR: [[ADR-010 Token QR opaco]], [[ADR-005 Contexto de organización y autorización por permisos]], [[ADR-014 Auditoría append-only]] (Propuesta).
- Decisiones: D-13 (lectores: cámara web/móvil o lector tipo teclado).

### Contrato funcional
- **Entradas:** `token` (en el cuerpo, nunca en la URL), `puntoAccesoId`.
- **Reglas (servidor):** permiso `accesos.validar` + alcance sobre el punto; búsqueda por hash global; si la organización del token ≠ contexto verificado del operador ⇒ "no encontrado" y se audita (revisión ADR-010); token invalidado ⇒ rechazado; luego evaluación completa de vigencia; respuesta sin efectos sobre cupo, estado ni visita. Limitación de tasa por operador (Propuesta).
- **Salidas:** `resultado` (Vigente/NoVigente/Inválido), `causas[]`, contexto mínimo: nombre de la persona, tipo/últimos dígitos del documento, foto (URL firmada de corta duración), sede, destino, responsable, modalidad, ventana actual, `autorizacionId` para el comando de entrada.
- **Errores:** 403 sin alcance; 404 token inexistente, invalidado o de otra organización (respuesta indistinguible); 429 exceso de lecturas.
- **Auditoría:** `qr.leido` con resultado y causas; lecturas de tokens invalidados o ajenos marcadas para alertas (HU-040). Nunca el token en claro.
- **Endpoints propuestos:** `POST /api/v1/accesos/validar-qr`.

### Criterios de aceptación
- [ ] **Dado** un QR válido de autorización Única **cuando** se lee dos veces **entonces** ambas lecturas muestran Vigente y `ingresosConsumidos` no cambia (la lectura no consume).
- [ ] **Dado** autorización no vigente, sede/zona incorrecta o fuera de horario **cuando** se lee **entonces** se muestra NoVigente con causas y no se abre permanencia (CA-09).
- [ ] **Dado** un QR de la organización B **cuando** lo lee un operador de A **entonces** responde "no encontrado" y se audita (CA-01).
- [ ] **Dado** una lectura **entonces** la respuesta permite recuperar la autorización sin que el QR contenga datos personales (CA-08).

### Tareas backend
- [ ] Query `ValidarQr` que resuelve hash, verifica organización y delega en el evaluador (HU-027).
- [ ] Proyección de contexto mínimo; URL firmada de foto (ADR-013); rate limiting.

### Tareas frontend
- [ ] Captura por cámara (librería a elegir según D-13) y por lector tipo teclado; campo oculto, sin mostrar el token.
- [ ] Tarjeta de resultado con causas legibles, foto para verificación de identidad y botón "Confirmar entrada" que invoca HU-032 (no implícito en la lectura).

### Pruebas
- [ ] Unitarias: mapeo de resultados y causas.
- [ ] Integración (PostgreSQL real): lectura sin escrituras salvo la auditoría; token invalidado ⇒ 404.
- [ ] Aislamiento / permisos negativos: token de otra organización; operador sin alcance sobre el punto ⇒ 403.
- [ ] Concurrencia: lecturas simultáneas del mismo token no alteran cupo (el doble escaneo con confirmación se prueba en HU-033, CA-10).
- [ ] E2E / UI: escanear QR de prueba y ver resultado en recepción.

### Definition of Done
Cumple la [[PLAN-DE-TRABAJO#Definition of Done general|DoD general]] y además:
- [ ] Verificado que ninguna lectura cambia estado, cupo, visita ni permanencia.
- [ ] Token nunca aparece en URL, logs ni auditoría.

### Riesgos y decisiones pendientes
> [!todo] D-13 — Lectores compatibles y formato; hasta decidir se soportan cámara del navegador y lector tipo teclado.

> [!todo] D-14 — Datos mínimos mostrables (documento parcial, foto) según política de privacidad.
