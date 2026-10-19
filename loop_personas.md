---
tipo: implementacion
estado: pendiente
fuentes: [PRD v0.1, PLAN-DE-LOOPS.md]
relacionados: [EP-06]
actualizado: 2026-10-21
tags: [conecsia, loop]
---

# loop_personas — Personas

> [!warning] Estado: **pendiente de condiciones de inicio**
> La autenticación ya no bloquea: `goal_authentication.md` se cerró el 2026-10-21 y sus pendientes se difirieron. Este loop empieza cuando se cumplan **M0** de [`PLAN-DE-LOOPS.md`](PLAN-DE-LOOPS.md) (`main` publicado y CI en verde) y las condiciones de inicio de abajo.

**Épicas:** EP-06 · **Rama:** `loop/personas` · **Worktree:** `../ConecsIA-wt/personas`

**Objetivo:** Registro reutilizable de personas por organización, búsqueda por documento sin duplicados (también bajo concurrencia), catálogos e inactivación que preserva el histórico.

## Condiciones de inicio
- [x] Goal de autenticación cerrado (2026-10-21). — [ ] M0: falta tener `main` publicado y CI en verde.
- [ ] M1 en `main` (HU-010, HU-017 y HU-018) y D-20 resuelta.
- [ ] Rama creada desde un `main` actualizado: `git worktree add ../ConecsIA-wt/personas -b loop/personas main`.

## Propiedad de archivos
- **Backend:** `Modules.Personas`.
- **Frontend:** `features/personas` (salvo el componente de foto, que entrega `loop_integraciones`).
- **No tocar:** los módulos de core (solo consumir sus interfaces), `Autorizaciones` y el almacenamiento de archivos.
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
- Decisiones abiertas: **D-09** (si sigue abierta, se detiene la restricción final de la clave única) y D-14.
- Un ADR del loop que siga en Propuesta y cambie el modelo.
- Otro loop tiene el token de migración y la tarea necesita una migración.
- El CI de `main` está en rojo.

## Tareas (en orden)
- [ ] **HU-020** Tipos de persona y tipos de documento. — *Catálogos sembrables para la plantilla de HU-010. Los países emisores dependen de D-09.*
- [ ] **HU-019** Registrar y buscar persona por documento sin duplicados. — *Clave única con restricción en la base de datos y prueba de concurrencia. La clave final queda ⏸ si D-09 sigue abierta: aislarla.*
- [ ] **HU-022** Inactivar persona preservando histórico. — *Inactivación lógica y auditada; la consumen HU-027 y HU-032 (CA-09). **Al fusionar se alcanza M3.***

## Integración
- Cada historia va en un PR propio a `main` desde `loop/personas`; no se espera al final del loop.
- **Desbloquea:** M3 → `loop_autorizaciones` y HU-021 en `loop_integraciones`.

## Criterio de cierre
- Todas las casillas están marcadas con evidencia, o con ⏸ y su D-xx registrada.
- Cada historia cumple la DoD de `wiki/PLAN-DE-TRABAJO.md` y el CI de `main` sigue en verde tras la última fusión.
- La wiki está al día y `wiki/log.md` tiene la entrada de cierre.

## Registro de evidencia
| Fecha | Tarea | Evidencia |
|---|---|---|
