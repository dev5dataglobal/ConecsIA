---
tipo: implementacion
estado: pendiente
fuentes: [PRD v0.1, PLAN-DE-LOOPS.md]
relacionados: [EP-13 (+ HU-021 de EP-06)]
actualizado: 2026-10-21
tags: [conecsia, loop]
---

# loop_integraciones — Integraciones: outbox, correo y archivos privados

> [!warning] Estado: **BLOQUEADO, no está listo para ejecutar**
> `goal_authentication.md` no está completo: siguen pendientes MFA, D-21, el restablecimiento y la revisión ASVS. Este loop no se ejecuta hasta alcanzar **M0** de [`PLAN-DE-LOOPS.md`](PLAN-DE-LOOPS.md) y cumplir las condiciones de inicio de abajo.

**Épicas:** EP-13 (+ HU-021 de EP-06) · **Rama:** `loop/integraciones` · **Worktree:** `../ConecsIA-wt/integraciones`

**Objetivo:** Outbox transaccional y procesos de fondo, correo con reintentos y plantillas, y almacenamiento privado de archivos. También entrega el envío que esperan HU-016, HU-029 y el restablecimiento de HU-006.

## Condiciones de inicio
- [ ] M0 alcanzado: goal de autenticación cerrado o diferido formalmente, `main` publicado y CI en verde.
- [ ] Outbox y correo: **ADR-012 aprobado y D-12 resuelta** (sin D-12 solo se avanza la parte técnica, contra Mailpit). Archivos: M3 en `main`, **ADR-013 aprobado** y D-13 resuelta.
- [ ] Rama creada desde un `main` actualizado: `git worktree add ../ConecsIA-wt/integraciones -b loop/integraciones main`.

## Propiedad de archivos
- **Backend:** nuevos `Modules.Notificaciones` y `Modules.Archivos`; el outbox en `Infrastructure/Persistencia` (cambio aislado que se fusiona primero).
- **Frontend:** `features/plantillas` y el componente de foto en `features/personas/foto`.
- **No tocar:** la lógica de negocio de los módulos que emiten eventos (solo publican).
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
- Decisiones abiertas: **D-12, D-13 y D-14**; ADR-012 y ADR-013.
- Un ADR del loop que siga en Propuesta y cambie el modelo.
- Otro loop tiene el token de migración y la tarea necesita una migración.
- El CI de `main` está en rojo.

## Tareas (en orden)
- [ ] **HU-042** Outbox y envío de correo con reintentos. — *El outbox se escribe en la misma transacción; los reintentos son idempotentes; sin datos personales en los logs.*
- [ ] **HU-043** Plantillas de correo por organización. — *Los destinatarios dependen de D-12.*
- [ ] **HU-016/HU-029 (envío)** Conectar al outbox la invitación de usuario y la preautorización. — *Solo el adaptador de envío; de esos módulos no se toca nada más allá de la interfaz.*
- [ ] **HU-006 (restablecimiento)** Solicitud y confirmación de restablecimiento. — *Solo si se difirió el goal. La respuesta es idéntica exista o no el usuario; el token es de un solo uso y dura 30 min; invalida las sesiones previas.*
- [ ] **HU-021** Fotografía de persona en almacenamiento privado. — *Emulador de almacenamiento de objetos en Compose; acceso por URL firmada y aislado por organización.*

## Integración
- Cada historia va en un PR propio a `main` desde `loop/integraciones`; no se espera al final del loop.
- **Desbloquea:** M6 → envío de HU-016, HU-029, restablecimiento y notificaciones.

## Criterio de cierre
- Todas las casillas están marcadas con evidencia, o con ⏸ y su D-xx registrada.
- Cada historia cumple la DoD de `wiki/PLAN-DE-TRABAJO.md` y el CI de `main` sigue en verde tras la última fusión.
- La wiki está al día y `wiki/log.md` tiene la entrada de cierre.

## Registro de evidencia
| Fecha | Tarea | Evidencia |
|---|---|---|
