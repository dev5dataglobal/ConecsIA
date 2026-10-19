---
tipo: implementacion
estado: pendiente
fuentes: [PRD v0.1, PLAN-DE-LOOPS.md]
relacionados: [EP-16, EP-17]
actualizado: 2026-10-21
tags: [conecsia, loop]
---

# loop_opcionales — Módulos opcionales: vehículos y equipos externos

> [!warning] Estado: **pendiente de condiciones de inicio**
> La autenticación ya no bloquea: `goal_authentication.md` se cerró el 2026-10-21 y sus pendientes se difirieron. Este loop empieza cuando se cumplan **M0** de [`PLAN-DE-LOOPS.md`](PLAN-DE-LOOPS.md) (`main` publicado y CI en verde) y las condiciones de inicio de abajo.

**Épicas:** EP-16, EP-17 · **Rama:** `loop/opcionales` · **Worktree:** `../ConecsIA-wt/opcionales`

**Objetivo:** Registrar vehículos y dispositivos y sus movimientos, respetando la habilitación del módulo en cada organización.

## Condiciones de inicio
- [x] Goal de autenticación cerrado (2026-10-21). — [ ] M0: falta tener `main` publicado y CI en verde.
- [ ] M5 en `main` y `loop_accesos` cerrado; **D-02** confirma que estos módulos entran; HU-011 en `main`.
- [ ] Rama creada desde un `main` actualizado: `git worktree add ../ConecsIA-wt/opcionales -b loop/opcionales main`.

## Propiedad de archivos
- **Backend:** nuevos `Modules.Vehiculos` y `Modules.Dispositivos`; extensiones en `Modules.Accesos` (HU-048 y HU-050), solo después de cerrar `loop_accesos`.
- **Frontend:** `features/vehiculos` y `features/dispositivos`; extensiones en `recepcion` y `presencia`.
- **No tocar:** el cierre de movimientos de equipos al salir una persona sin una regla aprobada (D-10).
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
- Decisiones abiertas: **D-02, D-10 y D-17**.
- Un ADR del loop que siga en Propuesta y cambie el modelo.
- Otro loop tiene el token de migración y la tarea necesita una migración.
- El CI de `main` está en rojo.

## Tareas (en orden)
- [ ] **HU-047** Registrar vehículos y asociarlos a personas y autorizaciones. — *La unicidad de la placa depende de D-17.*
- [ ] **HU-049** Registrar dispositivos externos. — *La unicidad del serial depende de D-17.*
- [ ] **HU-048** Movimientos vehiculares. — *El núcleo es Base; las reglas extendidas quedan ⏸ por D-10.*
- [ ] **HU-050** Movimientos de dispositivos y equipos dentro. — *La custodia queda ⏸ por D-10.*

## Integración
- Cada historia va en un PR propio a `main` desde `loop/opcionales`; no se espera al final del loop.
- **Desbloquea:** cierre de F5.

## Criterio de cierre
- Todas las casillas están marcadas con evidencia, o con ⏸ y su D-xx registrada.
- Cada historia cumple la DoD de `wiki/PLAN-DE-TRABAJO.md` y el CI de `main` sigue en verde tras la última fusión.
- La wiki está al día y `wiki/log.md` tiene la entrada de cierre.

## Registro de evidencia
| Fecha | Tarea | Evidencia |
|---|---|---|
