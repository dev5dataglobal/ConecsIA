| En revisión || En revisión || En curso || En curso |---
tipo: tablero
estado: vigente
actualizado: 2026-10-17
tags: [conecsia, backlog, scrum]
---

# Tablero Scrum

Plan y DoD: [[PLAN-DE-TRABAJO]]. El agente actualiza este tablero cada vez que cambia el estado de una historia.

**Estados:** Backlog → Lista (cumple DoR) → En curso → En revisión → Terminada (cumple DoD) · Bloqueada (decisión D-xx o ADR pendiente).

## Sprint actual
**S1 (iniciado 2026-10-17):** HU-001 a HU-004. Verificado localmente el 2026-10-17. Falta crear el repositorio en GitHub, correr el CI y la revisión del PO.

**Primera vertical funcional (2026-10-17):** HU-013 Gestionar sedes, con la base de HU-005, HU-007 y HU-008. Se adelantó respecto de los sprints propuestos para validar de punta a punta aislamiento, permisos y auditoría. Autenticación provisional ([[ADR-022 Autenticación de desarrollo provisional]]) porque HU-006 sigue bloqueada por D-03. Detalle: [[Sedes]].

## Bloqueada
- [ ] HU-006 Autenticación y sesión de usuario — goal cerrado 2026-10-21; el restablecimiento se difiere a loop_integraciones y MFA, D-21 y ASVS a loop_quality
- [ ] HU-035 Tratamiento de inconsistencias de entrada/salida — parcial: la regularización está bloqueada por D-08
- [ ] HU-052 Documentación de contratistas y límites de invitados — bloqueada por D-11

## En curso
- [x] HU-005 Aislamiento por organización en persistencia — implementada 2026-10-19 en `loop/core` (commit local, sin PR); RLS en D-22
- [ ] HU-007 Contexto de organización y sede derivado de membresía — base implementada; falta autenticación real (D-03) y caché
- [ ] HU-008 Registro de auditoría append-only — base implementada; falta auditar denegaciones de permiso

## En revisión (verificadas localmente; falta CI en GitHub y revisión del PO)
- [ ] HU-001 Solución backend modular y pipeline CI
- [ ] HU-002 Proyecto frontend base — falta E2E Playwright
- [ ] HU-003 Entorno local con Docker Compose — semillas con datos dependen de HU-009
- [ ] HU-004 Observabilidad y formato de errores base
- [ ] HU-019/020/021/022 Personas (registro, catálogos, foto, inactivación) + teléfonos y vehículos — verificadas localmente 2026-10-26 (21 pruebas de integración, 4 de UI, navegador). Ver [[Personas]]. Falta CI, revisión del PO y decisiones D-09/D-13/D-14/D-17
- [ ] HU-013 Gestionar sedes con zona horaria — verificada localmente 2026-10-17 (unitarias, integración con PostgreSQL real, Docker y UI). Falta CI, revisión de código, revisión del PO y E2E automatizado; CA-03 parcial

## Lista
_(vacío; ninguna historia cumple DoR mientras los ADR sigan en Propuesta)_

## Terminada
_(vacío)_

## Backlog por épica

| HU | Historia | Épica | Fase | Tamaño | Estado |
|---|---|---|---|---|---|
| HU-001 | Solución backend modular y pipeline CI | [[EP-01 Fundamentos técnicos]] | F0 | L | En revisión |
| HU-002 | Proyecto frontend base | [[EP-01 Fundamentos técnicos]] | F0 | M | En revisión |
| HU-003 | Entorno local con Docker Compose y semillas sintéticas | [[EP-01 Fundamentos técnicos]] | F0 | M | En revisión |
| HU-004 | Observabilidad y formato de errores base | [[EP-01 Fundamentos técnicos]] | F0 | M | En revisión |
| HU-005 | Aislamiento por organización en persistencia | [[EP-02 Tenancy, identidad y auditoría base]] | F0 | L | Goal cerrado (piezas diferidas) |
| HU-006 | Autenticación y sesión de usuario | [[EP-02 Tenancy, identidad y auditoría base]] | F0 | L | En curso |
| HU-007 | Contexto de organización y sede derivado de membresía | [[EP-02 Tenancy, identidad y auditoría base]] | F0 | M | En curso; selección multi-membresía depende de D-03 |
| HU-008 | Registro de auditoría append-only | [[EP-02 Tenancy, identidad y auditoría base]] | F0 | M | En curso |
| HU-009 | Alta, edición e inactivación de organizaciones | [[EP-03 Organizaciones y plano SaaS]] | F1 | M | Backlog |
| HU-010 | Tipos de organización y plantilla básica de catálogos | [[EP-03 Organizaciones y plano SaaS]] | F1 | M | Backlog |
| HU-011 | Planes y habilitación de módulos por organización | [[EP-03 Organizaciones y plano SaaS]] | F1 | M | Backlog |
| HU-012 | Vocabulario configurable por organización | [[EP-03 Organizaciones y plano SaaS]] | F1 | S | Backlog |
| HU-013 | Gestionar sedes con zona horaria | [[EP-04 Estructura física]] | F1 | M | En revisión |
| HU-014 | Gestionar áreas/destinos y responsables/autorizadores | [[EP-04 Estructura física]] | F1 | M | Backlog |
| HU-015 | Gestionar zonas y puntos de acceso | [[EP-04 Estructura física]] | F1 | M | Backlog |
| HU-016 | Invitar y administrar usuarios | [[EP-05 Usuarios, roles y permisos]] | F1 | M | Backlog; aceptación de invitación depende de D-03 |
| HU-017 | Roles configurables y catálogo de permisos | [[EP-05 Usuarios, roles y permisos]] | F1 | L | Backlog |
| HU-018 | Alcance por sede, destino y punto de acceso | [[EP-05 Usuarios, roles y permisos]] | F1 | M | Backlog |
| HU-019 | Registrar y buscar persona por documento sin duplicados | [[EP-06 Personas]] | F1 | L | En revisión (2026-10-26; clave/normalización provisional D-09) |
| HU-020 | Tipos de persona y tipos de documento | [[EP-06 Personas]] | F1 | S | En revisión (2026-10-26) |
| HU-021 | Fotografía de persona en almacenamiento privado | [[EP-06 Personas]] | F1 | M | En revisión (2026-10-26; almacenamiento local provisional, D-13/D-14) |
| HU-022 | Inactivar persona preservando histórico | [[EP-06 Personas]] | F1 | S | En revisión (2026-10-26; sin motivo obligatorio) |
| HU-023 | Crear autorización directa por usuario facultado | [[EP-07 Autorizaciones]] | F2 | L | Backlog |
| HU-024 | Modalidades, calendario y cupo de ingresos | [[EP-07 Autorizaciones]] | F2 | L | Backlog |
| HU-025 | Solicitar y decidir autorizaciones | [[EP-07 Autorizaciones]] | F2 | M | Backlog |
| HU-026 | Cancelar, revocar y vencer autorizaciones | [[EP-07 Autorizaciones]] | F2 | M | Backlog |
| HU-027 | Evaluador de vigencia de autorización | [[EP-07 Autorizaciones]] | F2 | M | Backlog |
| HU-028 | Visita ocasional desde recepción | [[EP-08 Visitas]] | F2 | M | Backlog |
| HU-029 | Preautorización e invitación | [[EP-08 Visitas]] | F2 | M | Backlog |
| HU-030 | Emitir, rotar e invalidar token QR | [[EP-09 QR]] | F2 | M | Backlog |
| HU-031 | Leer QR en recepción y mostrar validación | [[EP-09 QR]] | F2 | M | Backlog |
| HU-032 | Registrar entrada validada | [[EP-10 Entradas y salidas]] | F3 | L | Backlog |
| HU-033 | Consumo atómico e idempotencia de la entrada | [[EP-10 Entradas y salidas]] | F3 | L | Backlog |
| HU-034 | Registrar salida y cerrar permanencia | [[EP-10 Entradas y salidas]] | F3 | M | Backlog |
| HU-035 | Tratamiento de inconsistencias de entrada/salida | [[EP-10 Entradas y salidas]] | F3 | M | Backlog; regularización bloqueada (D-08) |
| HU-036 | Pantalla de recepción responsiva | [[EP-10 Entradas y salidas]] | F3 | L | Backlog |
| HU-037 | Personas dentro en tiempo real | [[EP-11 Presencia e historial]] | F3 | L | Backlog |
| HU-038 | Historial por persona | [[EP-11 Presencia e historial]] | F3 | M | Backlog |
| HU-039 | Bandeja del autorizador | [[EP-11 Presencia e historial]] | F3 | S | Backlog |
| HU-040 | Generar alertas operativas | [[EP-12 Alertas]] | F4 | L | Backlog; umbrales bloqueados (D-19) |
| HU-041 | Gestionar ciclo de alerta Nueva → Revisada → Cerrada | [[EP-12 Alertas]] | F4 | S | Backlog |
| HU-042 | Outbox y envío de correo con reintentos | [[EP-13 Correo]] | F4 | M | Backlog |
| HU-043 | Plantillas de correo por organización | [[EP-13 Correo]] | F4 | M | Backlog |
| HU-044 | Dashboard operativo | [[EP-14 Dashboard y reportes]] | F4 | M | Backlog |
| HU-045 | Reportes Excel y PDF del núcleo | [[EP-14 Dashboard y reportes]] | F4 | L | Backlog |
| HU-046 | Consulta de auditoría para auditor | [[EP-15 Auditoría consultable]] | F4 | M | Backlog |
| HU-047 | Registrar vehículos y asociarlos a personas y autorizaciones | [[EP-16 Vehículos]] | F5 | M | En revisión (2026-10-26: persona–vehículo y autorización de ingreso del vehículo con ciclo propio; modelo por validar, D-31) |
| HU-048 | Movimientos vehiculares | [[EP-16 Vehículos]] | F5 | M | Backlog; reglas de D-10 y D-17 bloqueadas |
| HU-049 | Registrar dispositivos externos | [[EP-17 Equipos externos]] | F5 | M | En revisión (2026-10-26: CRUD por persona, tipos, autorización de ingreso; D-17, D-31, D-33) |
| HU-050 | Movimientos de dispositivos y equipos dentro | [[EP-17 Equipos externos]] | F5 | M | Backlog; custodia bloqueada (D-10) |
| HU-051 | Plantillas sectoriales completas | [[EP-18 Adaptación sectorial]] | F6 | M | Backlog |
| HU-052 | Documentación de contratistas y límites de invitados | [[EP-18 Adaptación sectorial]] | F6 | Spike | Bloqueada (D-11) |
| HU-053 | Respaldo y restauración verificada | [[EP-19 Preparación productiva]] | F7 | M | Backlog |
| HU-054 | Pruebas de carga y rendimiento p95 | [[EP-19 Preparación productiva]] | F7 | M | Backlog |
| HU-055 | Endurecimiento de seguridad y privacidad | [[EP-19 Preparación productiva]] | F7 | M | Backlog |
| HU-056 | Piloto, capacitación y aceptación | [[EP-19 Preparación productiva]] | F7 | M | Backlog |

## Por redactar
- [ ] Acceso de soporte auditado del superadministrador (PRD §5.2.6, propuesta). Ninguna historia lo cubre todavía; candidata a EP-05.
