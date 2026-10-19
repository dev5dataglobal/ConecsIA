---
tipo: implementacion
estado: pendiente
fuentes: [PRD v0.1, PLAN-DE-LOOPS.md]
relacionados: [EP-02, EP-03, EP-04, EP-05]
actualizado: 2026-10-21
tags: [conecsia, loop]
---

# loop_core — Núcleo: tenancy, organizaciones, estructura, usuarios y alcance

> [!warning] Estado: **pendiente de condiciones de inicio**
> La autenticación ya no bloquea: `goal_authentication.md` se cerró el 2026-10-21 y sus pendientes se difirieron. Este loop empieza cuando se cumplan **M0** de [`PLAN-DE-LOOPS.md`](PLAN-DE-LOOPS.md) (`main` publicado y CI en verde) y las condiciones de inicio de abajo.

**Épicas:** EP-02, EP-03, EP-04, EP-05 · **Rama:** `loop/core` · **Worktree:** `../ConecsIA-wt/core`

**Objetivo:** Cerrar la base multi-organización que consumen todas las verticales: aislamiento, contexto multi-membresía, auditoría, plano SaaS, estructura física completa, usuarios, roles configurables y alcance por sede, destino y punto de acceso.

## Condiciones de inicio
- [x] Goal de autenticación cerrado (2026-10-21). — [x] M0: `main` con las aprobaciones integradas (55cbd4f).
- [x] Resolver **D-20** (idioma de las rutas) antes de M1. — *2026-10-19: aprobada opción A (español con excepciones técnicas); ADR-020 § Convención de idioma.*
- [x] Rama creada desde un `main` actualizado (2026-10-19, 55cbd4f): `git worktree add ../ConecsIA-wt/core -b loop/core main`.

## Propiedad de archivos
- **Backend:** `Modules.Organizaciones`, `Modules.Identidad`, `Modules.Estructura`, `Modules.Auditoria` (escritura), `ConecsIA.Api/Seguridad`, `Infrastructure/Persistencia`, `SharedKernel/Seguridad` (`Permisos.cs`).
- **Frontend:** `features/organizaciones`, `sedes`, `areas`, `zonas`, `usuarios`, `roles`; selector de organización en `features/sesion`; vocabulario en `shared/i18n`.
- **No tocar:** `Modules.Personas`, `Autorizaciones` y `Accesos`; `docker-compose.yml`; el envío de correo (lo entrega `loop_integraciones`).
- **Compartidos** (`Migraciones/` y su snapshot, `CatalogoModulos.cs`, `EnsambladosModulos.cs`, `Permisos.cs`, `rutas.tsx`, `AppShell.tsx`, `es.ts`, `schema.d.ts`, OpenAPI y wiki): se aplican las reglas de `PLAN-DE-LOOPS.md` §2.1. Hay que tener el **token de migración** para generar una migración, los cambios van en commits aislados, se rebasa antes de cada PR y los artefactos generados se regeneran en lugar de editarse.

## Protocolo de cada iteración
1. Relee este archivo y `PLAN-DE-LOOPS.md`. Ejecuta `git status` y `git fetch`, y rebasa sobre `main`.
2. Oriéntate con `wiki/index.md`, la épica, los ADR y `Decisiones pendientes.md`.
3. Toma **una** tarea sin marcar cuyas dependencias ya estén en `main`.
4. Si necesita una migración, toma el token y anótalo en `wiki/log.md`. Genera la migración después del rebase y fusiónala en un PR propio.
5. Implementa todas las capas (dominio, aplicación, infraestructura, API y frontend), con permisos, aislamiento y auditoría en el backend.
6. Ejecuta las pruebas: `dotnet test backend/ConecsIA.slnx` y, si tocaste el frontend, `npm test` y `npm run build` en `frontend/`. Incluye la prueba negativa con dos organizaciones.
7. Marca la casilla **solo con evidencia** (prueba, archivo o salida) y anótala al lado.
8. Actualiza `wiki/log.md`, `Estado de implementación.md`, `tablero-scrum.md` y la página del módulo.
9. **Detente** en la parte que dependa de una decisión abierta: márcala con ⏸, regístrala como D-xx y sigue con la siguiente tarea independiente.

## Condiciones de bloqueo
- Decisiones abiertas: D-01, D-04 (HU-014) y D-16; ADR-019 (HU-015). ADR-021 aprobado 2026-10-19: aplicar sus condiciones (regla por módulo en HU-005, token de migración, rollback hacia adelante).
- Un ADR del loop que siga en Propuesta y cambie el modelo.
- Otro loop tiene el token de migración y la tarea necesita una migración.
- El CI de `main` está en rojo.

## Tareas (en orden)
- [x] **HU-005** Aislamiento por organización en persistencia. — *Cerrar la DoD con una prueba negativa de dos organizaciones para cada entidad existente y documentar el spike de RLS (ADR-004).*
- [x] **HU-007** Contexto de organización y sede derivado de membresía. — *Selección de organización con multi-membresía (D-03b aprobada). El `organizationId` que envía el cliente se verifica contra la membresía.*
- [x] **HU-008** Registro de auditoría append-only. — *Cerrar la DoD verificando el append-only en la base de datos. La retención sigue pendiente (D-14).*
- [ ] **HU-017** Roles configurables y catálogo de permisos. — *Catálogo de permisos, roles por organización y plantillas. Nunca se otorgan permisos por el nombre del rol.*
- [ ] **HU-010** Tipos de organización y plantilla básica de catálogos. — *Plantilla sembrable. Se coordina con HU-020: los catálogos de persona y documento los crea `loop_personas`.*
- [ ] **HU-015** Gestionar zonas y puntos de acceso. — *ZONA con `es_punto_acceso`. ⏸ si ADR-019 sigue en Propuesta: pedir confirmación.*
- [ ] **HU-018** Alcance por sede, destino y punto de acceso. — *Filtro de alcance reutilizable por todos los módulos. **Al fusionar HU-017, HU-010 y HU-018 se alcanza M1.***
- [ ] **HU-009** Alta, edición e inactivación de organizaciones. — *Los efectos de la inactivación quedan ⏸ por D-16: aislar la parte afectada.*
- [ ] **HU-011** Planes y habilitación de módulos por organización. — *Los módulos opcionales consultan la habilitación. Inactivar un módulo no borra sus históricos.*
- [ ] **HU-016** Invitar y administrar usuarios. — *Alta, inactivación y contraseña inicial. El envío del enlace queda detrás de una interfaz y lo entrega `loop_integraciones` (D-12).*
- [ ] **HU-014** Gestionar áreas/destinos y responsables/autorizadores. — *El alcance de los autorizadores queda ⏸ por D-04 en la parte afectada.*
- [ ] **HU-012** Vocabulario configurable por organización. — *Las etiquetas definitivas dependen de D-01.*
- [ ] **HU-013** Gestionar sedes con zona horaria (cierre). — *Cerrar la DoD con sesión real. La regla de cambio de zona horaria sigue en D-06. **Al cerrar el loop se alcanza M2.***

## Integración
- Cada historia va en un PR propio a `main` desde `loop/core`; no se espera al final del loop.
- **Desbloquea:** M1 → `loop_personas`; M2 → `loop_sectorial`.

## Criterio de cierre
- Todas las casillas están marcadas con evidencia, o con ⏸ y su D-xx registrada.
- Cada historia cumple la DoD de `wiki/PLAN-DE-TRABAJO.md` y el CI de `main` sigue en verde tras la última fusión.
- La wiki está al día y `wiki/log.md` tiene la entrada de cierre.

## Registro de evidencia
| Fecha | Tarea | Evidencia |
|---|---|---|
| 2026-10-19 | HU-008 | 217 pruebas en verde: `MinimizadorValoresAuditoriaTests` (10), rollback y atomicidad en `AislamientoYAuditoriaTests`; append-only ya verificado. ⏸ La lectura aislada por organización pasa a HU-046. IP sin registrar (requiere migración). |
| 2026-10-19 | HU-007 | Backend: 205 pruebas en verde (`ContextoOrganizacionTests`: multi-membresía, `GET /api/v1/me/membresias`, cambio de permisos, denegación auditada, contexto de proceso; prueba de endpoints anónimos). Frontend: 31/31, `tsc` y `build` OK (`SeleccionOrganizacion.test.tsx`). OpenAPI y `schema.d.ts` regenerados. |
| 2026-10-19 | HU-005 | `dotnet test`: 196/196 (unit 60, arquitectura 68, integración 68). Nuevas pruebas: `PersistenciaPorModuloTests`, `SuiteAislamientoTests`, convenciones de índices y FK, y escritura cruzada auditada (`AislamientoYAuditoriaTests`). Spike RLS en ADR-004; adopción en D-22. |
