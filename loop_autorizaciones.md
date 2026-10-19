---
tipo: implementacion
estado: pendiente
fuentes: [PRD v0.1, PLAN-DE-LOOPS.md]
relacionados: [EP-07, EP-08, EP-09]
actualizado: 2026-10-21
tags: [conecsia, loop]
---

# loop_autorizaciones — Flujo central: autorizaciones, visitas y QR

> [!warning] Estado: **BLOQUEADO, no está listo para ejecutar**
> `goal_authentication.md` no está completo: siguen pendientes MFA, D-21, el restablecimiento y la revisión ASVS. Este loop no se ejecuta hasta alcanzar **M0** de [`PLAN-DE-LOOPS.md`](PLAN-DE-LOOPS.md) y cumplir las condiciones de inicio de abajo.

**Épicas:** EP-07, EP-08, EP-09 · **Rama:** `loop/autorizaciones` · **Worktree:** `../ConecsIA-wt/autorizaciones`

**Objetivo:** Crear, decidir, cancelar, revocar y vencer autorizaciones con sus modalidades; evaluar su vigencia (aprobada no es lo mismo que vigente); gestionar visitas y preautorizaciones; emitir el QR opaco y leerlo.

## Condiciones de inicio
- [ ] M0 alcanzado: goal de autenticación cerrado o diferido formalmente, `main` publicado y CI en verde.
- [ ] M3 en `main`; ADR-007, 008, 009 y 010 aprobados, o la parte que dependa de ellos aislada.
- [ ] Rama creada desde un `main` actualizado: `git worktree add ../ConecsIA-wt/autorizaciones -b loop/autorizaciones main`.

## Propiedad de archivos
- **Backend:** `Modules.Autorizaciones`.
- **Frontend:** `features/autorizaciones`, `features/visitas` y `features/recepcion/lectura-qr` (solo el componente de lectura).
- **No tocar:** `Modules.Accesos` (consumo de cupo y movimientos), el resto de `features/recepcion` y el envío de correo.
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
- Decisiones abiertas: **D-04, D-05, D-06 y D-18**; D-12 (envío de HU-029) y D-13 (lectores).
- Un ADR del loop que siga en Propuesta y cambie el modelo.
- Otro loop tiene el token de migración y la tarea necesita una migración.
- El CI de `main` está en rojo.

## Tareas (en orden)
- [ ] **HU-023** Crear autorización directa por usuario facultado. — *Exige permiso y alcance sobre el destino. Conserva creador, autorizador y decisión.*
- [ ] **HU-024** Modalidades, calendario y cupo de ingresos. — *Quedan ⏸ la diaria y la temporal (D-05), el calendario (D-06) y la permanente (D-18). Implementar la única y lo que no dependa de esas decisiones.*
- [ ] **HU-027** Evaluador de vigencia de autorización. — *Los casos afectados por D-05 o D-06 **se deniegan**. La vigencia se calcula, no se persiste (ADR-009).*
- [ ] **HU-025** Solicitar y decidir autorizaciones. — *Queda ⏸ quién decide (D-04).*
- [ ] **HU-030** Emitir, rotar e invalidar token QR. — *Token opaco sin datos personales (ADR-010). En la base de datos solo se guarda el hash.*
- [ ] **HU-026** Cancelar, revocar y vencer autorizaciones. — *Invalida el QR. La salida tras una revocación se valida en `loop_accesos` (HU-034).*
- [ ] **HU-028** Visita ocasional desde recepción. — *Queda ⏸ cuándo se exige autorización (D-04).*
- [ ] **HU-029** Preautorización e invitación. — *El envío por correo queda detrás de una interfaz y lo entrega `loop_integraciones` (D-12).*
- [ ] **HU-031** Leer QR en recepción y mostrar validación. — *La validación se hace en el servidor; la foto es opcional (HU-021). **Al fusionar se alcanza M4.***

## Integración
- Cada historia va en un PR propio a `main` desde `loop/autorizaciones`; no se espera al final del loop.
- **Desbloquea:** M4 → `loop_accesos`.

## Criterio de cierre
- Todas las casillas están marcadas con evidencia, o con ⏸ y su D-xx registrada.
- Cada historia cumple la DoD de `wiki/PLAN-DE-TRABAJO.md` y el CI de `main` sigue en verde tras la última fusión.
- La wiki está al día y `wiki/log.md` tiene la entrada de cierre.

## Registro de evidencia
| Fecha | Tarea | Evidencia |
|---|---|---|
