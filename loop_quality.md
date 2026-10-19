---
tipo: implementacion
estado: pendiente
fuentes: [PRD v0.1, PLAN-DE-LOOPS.md]
relacionados: [EP-01 (cierre), EP-19]
actualizado: 2026-10-21
tags: [conecsia, loop]
---

# loop_quality — Calidad, endurecimiento y preparación productiva

> [!warning] Estado: **pendiente de condiciones de inicio**
> La autenticación ya no bloquea: `goal_authentication.md` se cerró el 2026-10-21 y sus pendientes se difirieron. Este loop empieza cuando se cumplan **M0** de [`PLAN-DE-LOOPS.md`](PLAN-DE-LOOPS.md) (`main` publicado y CI en verde) y las condiciones de inicio de abajo.

**Épicas:** EP-01 (cierre), EP-19 · **Rama:** `loop/quality` · **Worktree:** `../ConecsIA-wt/quality`

**Objetivo:** Cerrar la DoD de EP-01 con el CI de GitHub; respaldo y restauración, carga p95, endurecimiento de seguridad y privacidad, y preparación del piloto.

## Condiciones de inicio
- [x] Goal de autenticación cerrado (2026-10-21). — [ ] M0: falta tener `main` publicado y CI en verde.
- [ ] Cada tarea tiene su propio requisito: cierre de EP-01, ninguno más; HU-053, HU-021 en `main`; HU-054, M5; HU-055, M7; HU-056, M8.
- [ ] Rama creada desde un `main` actualizado: `git worktree add ../ConecsIA-wt/quality -b loop/quality main`.

## Propiedad de archivos
- **Backend:** `.github/workflows`, `backend/tests`, `Api/Observabilidad` y la configuración; las correcciones transversales se coordinan con el loop dueño del código.
- **Frontend:** pruebas E2E (Playwright, si se aprueba).
- **No tocar:** funcionalidad de dominio nueva.
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
- Decisiones abiertas: **D-02, D-13, D-14 y D-15**; el nivel OWASP ASVS; MFA (D-03f) y D-21.
- Un ADR del loop que siga en Propuesta y cambie el modelo.
- Otro loop tiene el token de migración y la tarea necesita una migración.
- El CI de `main` está en rojo.

## Tareas (en orden)
- [ ] **HU-001..004** Cerrar la DoD de EP-01. — *CI en verde en GitHub, revisión por otra persona, y OpenAPI y Compose verificados.*
- [ ] **HU-006 (diferido)** MFA, límite por IP (D-21) y revisión ASVS. — *Diferido desde el goal (2026-10-21). Cada pieza espera su decisión: MFA (D-03f), D-21 y el nivel ASVS.*
- [ ] **HU-053** Respaldo y restauración verificada. — *RPO y RTO dependen de D-14.*
- [ ] **HU-054** Pruebas de carga y rendimiento p95. — *Los umbrales dependen de D-15.*
- [ ] **HU-055** Endurecimiento de seguridad y privacidad. — *Transversal: cada corrección va en el módulo dueño, una vez cerrado su loop.*
- [ ] **HU-056** Piloto, capacitación y aceptación. — *Materiales y evidencia; la aceptación la da el responsable.*

## Integración
- Cada historia va en un PR propio a `main` desde `loop/quality`; no se espera al final del loop.
- **Desbloquea:** M8 → HU-056.

## Criterio de cierre
- Todas las casillas están marcadas con evidencia, o con ⏸ y su D-xx registrada.
- Cada historia cumple la DoD de `wiki/PLAN-DE-TRABAJO.md` y el CI de `main` sigue en verde tras la última fusión.
- La wiki está al día y `wiki/log.md` tiene la entrada de cierre.

## Registro de evidencia
| Fecha | Tarea | Evidencia |
|---|---|---|
