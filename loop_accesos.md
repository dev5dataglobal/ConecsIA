---
tipo: implementacion
estado: pendiente
fuentes: [PRD v0.1, PLAN-DE-LOOPS.md]
relacionados: [EP-10, EP-11]
actualizado: 2026-10-21
tags: [conecsia, loop]
---

# loop_accesos — Operación: entradas, salidas, presencia e historial

> [!warning] Estado: **pendiente de condiciones de inicio**
> La autenticación ya no bloquea: `goal_authentication.md` se cerró el 2026-10-21 y sus pendientes se difirieron. Este loop empieza cuando se cumplan **M0** de [`PLAN-DE-LOOPS.md`](PLAN-DE-LOOPS.md) (`main` publicado y CI en verde) y las condiciones de inicio de abajo.

**Épicas:** EP-10, EP-11 · **Rama:** `loop/accesos` · **Worktree:** `../ConecsIA-wt/accesos`

**Objetivo:** Registrar entradas validadas y salidas con consumo atómico e idempotente, cerrar permanencias, mostrar quién está dentro en tiempo real y el historial, y construir la pantalla de recepción.

## Condiciones de inicio
- [x] Goal de autenticación cerrado (2026-10-21). — [ ] M0: falta tener `main` publicado y CI en verde.
- [ ] M4 en `main`; ADR-007 y ADR-011 aprobados.
- [ ] Rama creada desde un `main` actualizado: `git worktree add ../ConecsIA-wt/accesos -b loop/accesos main`.

## Propiedad de archivos
- **Backend:** `Modules.Accesos` (más el hub de SignalR). La bandeja de HU-039 es una consulta en `Modules.Autorizaciones`: es la única escritura fuera de su módulo y se hace después de M4.
- **Frontend:** `features/recepcion` (salvo `lectura-qr`), `features/presencia` y la bandeja en `features/autorizaciones`.
- **No tocar:** las reglas de vigencia (se consumen, no se reimplementan), las alertas y el correo.
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
- Decisiones abiertas: **D-07 y D-08**; D-13, D-14 y D-15.
- Un ADR del loop que siga en Propuesta y cambie el modelo.
- Otro loop tiene el token de migración y la tarea necesita una migración.
- El CI de `main` está en rojo.

## Tareas (en orden)
- [ ] **HU-032** Registrar entrada validada. — *Antes de confirmar, el servidor valida identidad, estado de la persona, política y restricciones.*
- [ ] **HU-033** Consumo atómico e idempotencia de la entrada. — *Una autorización única no se consume dos veces; incluir pruebas de concurrencia y de reintento. La restricción entre sedes queda ⏸ (D-07).*
- [ ] **HU-034** Registrar salida y cerrar permanencia. — *La salida cierra la permanencia correspondiente, pero no una autorización recurrente.*
- [ ] **HU-037** Personas dentro en tiempo real. — *Eventos aislados por organización. La frescura depende de D-15.*
- [ ] **HU-038** Historial por persona. — *La retención depende de D-14.*
- [ ] **HU-039** Bandeja del autorizador. — *Parcialmente ⏸ por D-04.*
- [ ] **HU-036** Pantalla de recepción responsiva. — *Tablet y móvil, uso por teclado y estados de error. Los lectores dependen de D-13.*
- [ ] **HU-035** Tratamiento de inconsistencias de entrada/salida. — *Solo la detección. La regularización queda ⏸ por D-08; la alerta y la consulta llegan con M7.*

## Integración
- Cada historia va en un PR propio a `main` desde `loop/accesos`; no se espera al final del loop.
- **Desbloquea:** M5 → `loop_reporting`, `loop_opcionales` y HU-054.

## Criterio de cierre
- Todas las casillas están marcadas con evidencia, o con ⏸ y su D-xx registrada.
- Cada historia cumple la DoD de `wiki/PLAN-DE-TRABAJO.md` y el CI de `main` sigue en verde tras la última fusión.
- La wiki está al día y `wiki/log.md` tiene la entrada de cierre.

## Registro de evidencia
| Fecha | Tarea | Evidencia |
|---|---|---|
