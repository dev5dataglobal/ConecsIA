---
tipo: plan
estado: propuesta
fuentes: [PRD v0.1]
relacionados: [Fases y MVP, Registro de ADR, Decisiones pendientes]
actualizado: 2026-10-17
tags: [conecsia, backlog, plan]
---

# Plan de trabajo

> [!warning] Base del backlog
> El backlog sale del [[PRD v0.1]], que sigue siendo v0.1 **para revisión**. La secuencia de fases y el MVP son la propuesta del PRD (D-02 pendiente) y todos los ADR están en estado Propuesta ([[Registro de ADR]]). Los tamaños S/M/L/XL son relativos: **no** son estimaciones aprobadas de tiempo ni costo.

Tablero operativo: [[tablero-scrum]] · Historias por épica: carpeta `backlog/`.

## Marco Scrum propuesto
- **Sprints** de 2 semanas (propuesta, a confirmar con el equipo).
- **Eventos:** planning, daily, refinamiento semanal, review con el responsable de producto y retrospectiva.
- **Product Owner:** responsable del producto; resuelve las D-xx de negocio.
- **Equipo:** arquitectura y desarrollo; proponen las ADR técnicas.
- **Definition of Ready:** la historia tiene sus dependencias terminadas o aceptadas, sus ADR aprobados (o la parte bloqueada aislada), ninguna D-xx abierta que cambie su comportamiento y criterios de aceptación revisados por el PO.

## Definition of Done general
Una historia está **Terminada** solo cuando:
- [ ] Las validaciones y permisos se aplican en el **backend**; el frontend no es la única barrera.
- [ ] La persistencia tiene migración versionada, restricciones e índices revisados.
- [ ] Se aplica el aislamiento por organización y hay una **prueba negativa** con dos organizaciones.
- [ ] Las operaciones críticas quedan en la auditoría (actor, organización, instante, acción, objeto, resultado y valores anterior/nuevo).
- [ ] Hay pruebas unitarias y de integración contra **PostgreSQL real** en verde en CI. Incluye pruebas de concurrencia e idempotencia cuando aplica.
- [ ] La interfaz es responsiva (computador, tablet y móvil), accesible por teclado y muestra estados de error y de carga.
- [ ] Los criterios de aceptación de la historia y los CA-xx del PRD vinculados están verificados con evidencia; no bastan los datos simulados.
- [ ] No hay secretos ni datos personales innecesarios en logs.
- [ ] El contrato OpenAPI está actualizado y el código revisado por otra persona.
- [ ] La wiki está actualizada: página del módulo, [[Estado de implementación]], [[tablero-scrum]] y [[log]].
- [ ] Las decisiones pendientes que surgieron quedaron registradas como D-xx.

## Fases

### F0
**Definiciones y base técnica.** Base técnica, aislamiento, identidad y auditoría.
- Épicas: [[EP-01 Fundamentos técnicos]], [[EP-02 Tenancy, identidad y auditoría base]]
- Para cerrar la fase hay que aprobar ADR-001 a ADR-006 y ADR-014 y decidir **D-03**.
- Entregables del PRD §15: ADR aprobados, modelo físico inicial, contratos API, matriz de permisos, pruebas de aislamiento y de concurrencia, y semillas sintéticas.

### F1
**Administración y personas** (RF-01 a RF-08; RF-26 básico).
- Épicas: [[EP-03 Organizaciones y plano SaaS]], [[EP-04 Estructura física]], [[EP-05 Usuarios, roles y permisos]], [[EP-06 Personas]]
- Decisiones: D-01 (terminología), D-09 (identificación documental), D-16 (inactivar organización).

### F2
**Autorizaciones y visitas** (RF-09 a RF-12, RF-15, RF-20).
- Épicas: [[EP-07 Autorizaciones]], [[EP-08 Visitas]], [[EP-09 QR]]
- Decisiones: **D-04, D-05 y D-06**. Son críticas para las modalidades y la vigencia.

### F3
**Operación de accesos** (RF-13, RF-14, RF-18, RF-19).
- Épicas: [[EP-10 Entradas y salidas]], [[EP-11 Presencia e historial]]
- Decisiones: D-07 (presencia), D-08 (regularización), D-13 (lectores QR).

### F4
**Operación medible** (RF-21 a RF-24; RF-25 transversal).
- Épicas: [[EP-12 Alertas]], [[EP-13 Correo]], [[EP-14 Dashboard y reportes]], [[EP-15 Auditoría consultable]]
- Decisiones: D-12 (correo y umbrales), D-15 (volumen).
- **Cierre del MVP propuesto (F0 a F4).**

### F5
**Vehículos y equipos** (RF-16, RF-17). Se puede adelantar si el primer cliente los necesita (D-02).
- Épicas: [[EP-16 Vehículos]], [[EP-17 Equipos externos]]
- Decisiones: **D-10**.

### F6
**Adaptación por vertical** (RF-26 completo).
- Épica: [[EP-18 Adaptación sectorial]]
- Decisiones: **D-11**.

### F7
**Preparación productiva** (requisitos no funcionales).
- Épica: [[EP-19 Preparación productiva]]
- Decisiones: D-13, D-14, D-15.

## Avance real
| Fecha | Entrega | Historias | Estado | Evidencia |
|---|---|---|---|---|
| 2026-10-17 | Fundaciones técnicas | HU-001 a HU-004 | En revisión | [[Plataforma base]] |
| 2026-10-17 | **Primera vertical funcional: sedes** | HU-013 + base de HU-005, HU-007, HU-008 | HU-013 en revisión; HU-005/007/008 en curso | [[Sedes]], [[Estado de implementación]] |

> [!note] Desviación del plan
> La primera vertical adelanta HU-013 (S3) antes de cerrar S2. Para no esperar a D-03 se usó autenticación provisional ([[ADR-022 Autenticación de desarrollo provisional]], Propuesta) y semillas en lugar de HU-009/HU-016. La DoR no se cumplía formalmente (D-03 abierta, ADR-008 en Propuesta): la parte bloqueada quedó aislada en el esquema de autenticación y en la regla de cambio de zona horaria (D-06).

**Siguiente paso propuesto:** crear el repositorio en GitHub y correr el CI; cerrar D-03 para HU-006; resolver D-20 (idioma de las rutas) antes de la segunda vertical.

> [!note] Ejecución por loops (Propuesta, 2026-10-21)
> El backlog se reparte en loops con propiedad de archivos y hitos de integración: ver `PLAN-DE-LOOPS.md` en la raíz del repositorio. Todos están bloqueados hasta cerrar o diferir `goal_authentication.md`.

## Ruta crítica
HU-001 → HU-005 → HU-006/HU-007 → HU-008 → HU-009 → HU-013 → HU-016/HU-017 → HU-019 → HU-023 → HU-024/HU-027 → HU-032 → HU-033 → HU-034 → HU-037.

## Bloqueos de negocio por fase

| Decisión | Bloquea | Debe cerrarse antes de |
|---|---|---|
| D-03 identidad | HU-006 y todo lo que requiere sesión | Sprint 2 (propuesto) |
| D-09 documento | Clave única de HU-019 | F1 |
| D-04 quién decide | HU-025, HU-026 | F2 |
| D-05 reingresos y cupos | HU-024 (diaria y temporal), HU-033 | F2 |
| D-06 calendario | HU-024, HU-027 | F2 |
| D-07 presencia | Restricción de HU-033 | F3 |
| D-08 regularización | Parte de HU-035 | Piloto |
| D-12 correo | HU-042, HU-040 (umbrales) | F4 |
| D-10 custodia | HU-048, HU-050 | F5 |
| D-11 contratistas | HU-052 | F6 |
| D-14 y D-15 | HU-053 a HU-055 | F7 |

## Sprints propuestos (orientativos)

| Sprint | Objetivo | Historias |
|---|---|---|
| S1 | Esqueleto ejecutable y CI | HU-001, HU-002, HU-003, HU-004 |
| S2 | Aislamiento, identidad y auditoría | HU-005, HU-006 (si D-03 está cerrada), HU-007, HU-008 |
| S3 | Organizaciones y estructura | HU-009, HU-010, HU-011, HU-013 |
| S4 | Estructura, usuarios y roles | HU-012, HU-014, HU-015, HU-016, HU-017 |
| S5 | Alcance y personas | HU-018, HU-019, HU-020, HU-021, HU-022 |
| S6+ | F2 en adelante | Se planifica en refinamiento según las D-xx cerradas |
