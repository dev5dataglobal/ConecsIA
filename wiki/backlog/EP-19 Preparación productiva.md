---
tipo: epica
estado: propuesta
fase: F7
requisitos: [CA-01, CA-20, CA-21, CA-22, "PRD §12.5", "PRD §12.7", "PRD §13.1"]
adr: [ADR-004, ADR-006, ADR-007, ADR-013, ADR-014, ADR-017, ADR-018]
decisiones: [D-03, D-13, D-14, D-15]
actualizado: 2026-10-17
tags: [conecsia, backlog, epica, produccion]
---

# EP-19 Preparación productiva

> [!note] Fase: [[PLAN-DE-TRABAJO#F7]] · Requisitos: PRD §12.5, §12.7, [[Criterios de aceptación|CA-22]] · ADR: [[ADR-017 Entorno local y despliegue]], [[ADR-018 Observabilidad sin datos sensibles]]

## Objetivo de la épica
Dejar el sistema listo para operar con datos reales: recuperación verificada, rendimiento medido, seguridad endurecida y aceptación del piloto (**Base**, requisitos no funcionales y fase 7).

## Alcance / fuera de alcance
- **Dentro:** respaldo y restauración de BD y archivos; pruebas de carga del flujo crítico; endurecimiento de seguridad/privacidad; piloto, capacitación y aceptación.
- **Parámetros por decidir (no inventar):** RPO/RTO y retención (D-14), volumen, concurrencia y p95 (D-15), SSO/MFA (D-03), infraestructura (D-13).
- **Fuera:** declarar cumplimiento normativo automático.

## Historias
| ID | Historia | Tamaño | Estado | Bloqueada por |
|---|---|---|---|---|
| HU-053 | Respaldo y restauración verificada | M | Lista para refinar | D-14 (RPO/RTO), D-13 |
| HU-054 | Pruebas de carga y rendimiento p95 | M | Lista para refinar | D-15 (umbrales) |
| HU-055 | Endurecimiento de seguridad y privacidad | M | Lista para refinar | D-14, D-03 |
| HU-056 | Piloto, capacitación y aceptación | M | Backlog | D-02, D-15 |

---

## HU-053 Respaldo y restauración verificada

**Como** responsable operativo **quiero** respaldos automáticos de base de datos y archivos con restauración probada **para** recuperar datos coherentes ante fallos.

| Campo | Valor |
|---|---|
| Épica / Fase | EP-19 / F7 |
| Requisitos | CA-22, PRD §12.7 (Continuidad) |
| Tamaño relativo | M (propuesto, sin estimación aprobada) |
| Estado | Lista para refinar; aceptación final depende de D-14 |

### Objetivo
Procedimiento automatizado y ensayado de respaldo/restauración de PostgreSQL y del almacenamiento privado, con coherencia entre filas y archivos.

### Actores
Responsable operativo (Pendiente de designar, D-14), DevOps, Superadministrador SaaS.

### Dependencias
- Historias: HU-003, HU-021.
- ADR: [[ADR-017 Entorno local y despliegue]] (Propuesta), [[ADR-013 Almacenamiento privado de archivos]] (Propuesta), [[ADR-014 Auditoría append-only]] (Propuesta).
- Decisiones: **D-14** (RPO/RTO, retención de respaldos, responsables), D-13 (infraestructura).

### Contrato funcional
- **Entradas:** parámetros configurables RPO, RTO, frecuencia y retención (valores **Pendientes** D-14).
- **Reglas:** respaldos cifrados en reposo y en tránsito; PITR/WAL o equivalente según infraestructura; archivos respaldados con referencia consistente; restauración en entorno aislado, nunca sobre producción sin procedimiento.
- **Salidas:** runbook, scripts, informe de ensayo (tiempo real vs RTO, pérdida vs RPO, verificación de integridad).
- **Errores:** falla de respaldo ⇒ alerta operativa sin datos personales en el mensaje.
- **Auditoría:** ejecuciones de respaldo/restauración registradas (actor de sistema).
- **Endpoints:** no aplica (operación de infraestructura).

### Criterios de aceptación
- [ ] **Dado** un respaldo **cuando** se restaura en un entorno aislado **entonces** los datos son coherentes, los archivos vinculados se descargan y la auditoría está íntegra (CA-22).
- [ ] **Dado** RPO/RTO aprobados **cuando** se ensaya **entonces** el resultado medido los cumple (CA-22, D-14).
- [ ] **Dado** una restauración **entonces** se conserva el aislamiento por organización (CA-01).

### Tareas backend
- [ ] Scripts/IaC de respaldo de BD y archivos; verificación de integridad (conteos, hashes, FKs a archivos).
- [ ] Runbook y alertas de fallo.

### Tareas frontend
- [ ] No aplica.

### Pruebas
- [ ] Integración (PostgreSQL real): restauración completa y PITR a un instante.
- [ ] Aislamiento: consultas cruzadas tras restaurar no exponen otro tenant.
- [ ] Ensayo programado y repetible (simulacro).

### Definition of Done
Cumple la [[PLAN-DE-TRABAJO#Definition of Done general|DoD general]] y además:
- [ ] Informe del simulacro archivado en la wiki con tiempos medidos.

### Riesgos y decisiones pendientes
> [!todo] D-14 — RPO/RTO, retención de respaldos y responsables operativos.

> [!todo] D-13 — Proveedor de infraestructura y almacenamiento.

---

## HU-054 Pruebas de carga y rendimiento p95

**Como** arquitecto **quiero** medir p95 del flujo crítico con el volumen acordado **para** demostrar que la operación de recepción es fluida.

| Campo | Valor |
|---|---|
| Épica / Fase | EP-19 / F7 |
| Requisitos | PRD §12.7 (Rendimiento, Escalabilidad, Consistencia) |
| Tamaño relativo | M (propuesto, sin estimación aprobada) |
| Estado | Lista para refinar; umbrales Pendientes D-15 |

### Objetivo
Suite de carga reproducible sobre búsqueda por documento, lectura QR, validación, entrada y salida, y presencia en tiempo real.

### Actores
Arquitecto, QA, DevOps.

### Dependencias
- Historias: HU-019, HU-031, HU-032, HU-033, HU-034, HU-037, HU-004.
- ADR: [[ADR-007 Concurrencia e idempotencia en accesos]] (Propuesta), [[ADR-011 Tiempo real con SignalR]] (Propuesta), [[ADR-018 Observabilidad sin datos sensibles]] (Propuesta), [[ADR-004 Tenancy con base compartida y organization_id]] (Propuesta).
- Decisiones: **D-15** (volumen, concurrencia, p95, frescura, disponibilidad).

### Contrato funcional
- **Entradas:** perfil de carga parametrizado (organizaciones, sedes, operadores concurrentes, movimientos/día, umbral p95) — valores **Pendientes** D-15.
- **Reglas:** datos sintéticos multi-tenant; entorno representativo; sin datos reales.
- **Salidas:** informe p50/p95/p99, errores, consumo, planes de consulta e índices revisados; recomendaciones.
- **Errores:** degradación ⇒ incidencias priorizadas.
- **Endpoints:** los del flujo crítico existentes.

### Criterios de aceptación
- [ ] **Dado** el perfil acordado **cuando** se ejecuta la carga **entonces** el p95 del flujo crítico cumple el umbral aprobado (D-15).
- [ ] **Dado** carga concurrente sobre la misma autorización única **entonces** hay una sola entrada confirmada (CA-10, CA-12).
- [ ] **Dado** un tenant con alta carga **entonces** los demás no ven datos ajenos (CA-01).

### Tareas backend
- [ ] Generador de datos sintéticos y escenarios (k6/NBomber u otro, Propuesta).
- [ ] Revisión de índices/paginación con EXPLAIN y correcciones.

### Tareas frontend
- [ ] Medición de frescura de "personas dentro" bajo carga (escenario del cliente SignalR).

### Pruebas
- [ ] Carga, estrés y resistencia; verificación de invariantes de consistencia al final de cada corrida.

### Definition of Done
Cumple la [[PLAN-DE-TRABAJO#Definition of Done general|DoD general]] y además:
- [ ] Informe archivado en la wiki y ejecutable en CI/bajo demanda.

### Riesgos y decisiones pendientes
> [!todo] D-15 — Sin umbrales no hay criterio de aprobado: solo se reporta línea base.

---

## HU-055 Endurecimiento de seguridad y privacidad

**Como** responsable de seguridad **quiero** verificar y endurecer seguridad y tratamiento de datos personales **para** operar con datos reales.

| Campo | Valor |
|---|---|
| Épica / Fase | EP-19 / F7 |
| Requisitos | PRD §12.5, CA-01, CA-20, RN-01 |
| Tamaño relativo | M (propuesto, sin estimación aprobada) |
| Estado | Lista para refinar; retención Pendiente D-14 |

### Objetivo
Matriz de pruebas positivas/negativas de aislamiento y permisos, revisión de secretos/logs, límites de intentos y controles de privacidad configurables.

### Actores
Responsable de seguridad, arquitecto, Superadministrador SaaS, Auditor.

### Dependencias
- Historias: HU-005, HU-006, HU-007, HU-008, HU-017, HU-018, HU-021, HU-030, HU-045.
- ADR: [[ADR-004 Tenancy con base compartida y organization_id]], [[ADR-005 Contexto de organización y autorización por permisos]], [[ADR-006 Identidad y sesiones]], [[ADR-010 Token QR opaco]], [[ADR-013 Almacenamiento privado de archivos]], [[ADR-018 Observabilidad sin datos sensibles]] (todas Propuesta).
- Decisiones: **D-14** (finalidad, avisos, retención, eliminación, fotos), **D-03** (SSO/MFA).

### Contrato funcional
- **Reglas:** HTTPS/HSTS, cabeceras seguras, CORS restringido, rate limiting en login y validación QR, secretos en gestor estándar, cero contraseñas/tokens/QR en logs, archivos solo por URL firmada de corta vida con verificación de tenant; acceso de soporte auditado (Propuesta).
- **Salidas:** matriz de seguridad automatizada, informe de escaneo (SAST/dependencias/DAST), parámetros de retención **sin valores** hasta D-14.
- **Errores:** 429 por límite de intentos; respuestas sin filtrar existencia de recursos ajenos.
- **Auditoría:** cambios de permisos, exportaciones y accesos de soporte.

### Criterios de aceptación
- [ ] **Dado** un usuario de A con IDs de B **cuando** lista, lee, modifica, exporta, descarga o se suscribe **entonces** todo se deniega (CA-01).
- [ ] **Dado** los logs de una corrida E2E **cuando** se inspeccionan **entonces** no hay contraseñas, tokens QR ni documentos completos (§12.5).
- [ ] **Dado** la auditoría **cuando** un usuario ordinario intenta alterarla **entonces** no puede (CA-20).

### Tareas backend
- [ ] Matriz de pruebas por endpoint × permiso × tenant; rate limiting; cabeceras; escaneos en CI.
- [ ] Parámetros de retención por organización preparados, inactivos hasta D-14.

### Tareas frontend
- [ ] CSP, manejo de sesión expirada y avisos de privacidad (texto Pendiente D-14).

### Pruebas
- [ ] Aislamiento y permisos negativos automatizados; revisión de logs; pentest externo (Propuesta).

### Definition of Done
Cumple la [[PLAN-DE-TRABAJO#Definition of Done general|DoD general]] y además:
- [ ] Hallazgos críticos/altos cerrados; informe en la wiki.

### Riesgos y decisiones pendientes
> [!todo] D-14 — Retención, eliminación y tratamiento de fotografías: no se fija plazo.

> [!todo] D-03 — SSO/MFA para producción.

---

## HU-056 Piloto, capacitación y aceptación

**Como** responsable de producto **quiero** ejecutar el piloto con capacitación y aceptación formal **para** pasar a producción con evidencia.

| Campo | Valor |
|---|---|
| Épica / Fase | EP-19 / F7 |
| Requisitos | PRD §13.1 F7, CA-21, CA-01..CA-22 aplicables |
| Tamaño relativo | M (propuesto, sin estimación aprobada) |
| Estado | Backlog |

### Objetivo
Piloto con organizaciones de prueba sobre el núcleo común, capacitación por perfil y acta de aceptación contra los CA.

### Actores
Responsable de producto, organización piloto (administradores, operadores, autorizadores, auditores), equipo de soporte.

### Dependencias
- Historias: HU-053, HU-054, HU-055 y el MVP (F0–F4); HU-047..HU-050 solo si el primer cliente los requiere (D-02).
- ADR: [[ADR-017 Entorno local y despliegue]] (Propuesta).
- Decisiones: D-02, D-08 (regularización antes del piloto), D-14, D-15, D-16.

### Contrato funcional
- **Entradas:** plan de piloto, organización(es) piloto, guiones de recorrido por perfil, lista de CA a verificar.
- **Reglas:** sin datos reales hasta cerrar D-14; incidencias registradas y priorizadas.
- **Salidas:** materiales de capacitación, registro de incidencias, acta de aceptación firmada.
- **Auditoría / endpoints:** no aplica más allá de la operación normal.

### Criterios de aceptación
- [ ] **Dado** el piloto **cuando** se ejecutan los recorridos de buscar, solicitar, decidir, entrar y salir en computador, tablet y móvil **entonces** funcionan con validaciones visibles (CA-21).
- [ ] **Dado** la lista de CA del MVP **entonces** cada uno tiene evidencia o incidencia abierta.

### Tareas backend
- [ ] Soporte, corrección de incidencias y monitoreo durante el piloto.

### Tareas frontend
- [ ] Ajustes de usabilidad derivados del piloto.

### Pruebas
- [ ] Pruebas de aceptación de usuario (UAT) guiadas; regresión E2E antes del corte.

### Definition of Done
Cumple la [[PLAN-DE-TRABAJO#Definition of Done general|DoD general]] y además:
- [ ] Acta de aceptación y lecciones archivadas en la wiki.

### Riesgos y decisiones pendientes
> [!todo] D-02 — Alcance del primer cliente (¿vehículos/equipos antes del piloto?).

> [!todo] D-08 — Regularización de movimientos debe resolverse antes del piloto operativo.
