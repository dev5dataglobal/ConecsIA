---
tipo: implementacion
estado: pendiente
fuentes: [PRD v0.1, PLAN-DE-LOOPS.md]
relacionados: [EP-12, EP-14, EP-15]
actualizado: 2026-10-21
tags: [conecsia, loop]
---

# loop_reporting — Alertas, dashboard, reportes y auditoría consultable

> [!warning] Estado: **BLOQUEADO, no está listo para ejecutar**
> `goal_authentication.md` no está completo: siguen pendientes MFA, D-21, el restablecimiento y la revisión ASVS. Este loop no se ejecuta hasta alcanzar **M0** de [`PLAN-DE-LOOPS.md`](PLAN-DE-LOOPS.md) y cumplir las condiciones de inicio de abajo.

**Épicas:** EP-12, EP-14, EP-15 · **Rama:** `loop/reporting` · **Worktree:** `../ConecsIA-wt/reporting`

**Objetivo:** Alertas operativas con su ciclo de estados, dashboard, reportes Excel y PDF, y consulta de la auditoría para el auditor.

## Condiciones de inicio
- [ ] M0 alcanzado: goal de autenticación cerrado o diferido formalmente, `main` publicado y CI en verde.
- [ ] M5 en `main`; ADR-016 aprobado para HU-045.
- [ ] Rama creada desde un `main` actualizado: `git worktree add ../ConecsIA-wt/reporting -b loop/reporting main`.

## Propiedad de archivos
- **Backend:** nuevos `Modules.Alertas` y `Modules.Reportes`; lectura en `Modules.Auditoria`.
- **Frontend:** `features/alertas`, `dashboard`, `reportes` y `auditoria`.
- **No tocar:** escrituras en `Accesos` o `Autorizaciones` (solo consultas y eventos).
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
- Decisiones abiertas: **D-12 y D-19** (umbrales); D-13, D-14 y D-15.
- Un ADR del loop que siga en Propuesta y cambie el modelo.
- Otro loop tiene el token de migración y la tarea necesita una migración.
- El CI de `main` está en rojo.

## Tareas (en orden)
- [ ] **HU-040** Generar alertas operativas. — *La parte que depende de los umbrales queda ⏸ por D-19 y D-12.*
- [ ] **HU-041** Gestionar ciclo de alerta Nueva → Revisada → Cerrada. — *Cada transición queda auditada.*
- [ ] **HU-044** Dashboard operativo. — *Aislado por organización y por alcance.*
- [ ] **HU-046** Consulta de auditoría para auditor. — *Exige el permiso de auditor y no expone datos sensibles innecesarios.*
- [ ] **HU-045** Reportes Excel y PDF del núcleo. — *Exportaciones aisladas y auditadas (ADR-016). **Al fusionar se alcanza M7.***

## Integración
- Cada historia va en un PR propio a `main` desde `loop/reporting`; no se espera al final del loop.
- **Desbloquea:** M7 → HU-035 completa y HU-055.

## Criterio de cierre
- Todas las casillas están marcadas con evidencia, o con ⏸ y su D-xx registrada.
- Cada historia cumple la DoD de `wiki/PLAN-DE-TRABAJO.md` y el CI de `main` sigue en verde tras la última fusión.
- La wiki está al día y `wiki/log.md` tiene la entrada de cierre.

## Registro de evidencia
| Fecha | Tarea | Evidencia |
|---|---|---|
