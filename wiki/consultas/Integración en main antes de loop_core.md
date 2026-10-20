---
tipo: consulta
estado: propuesta
fuentes: [PRD v0.1]
relacionados: [HU-006, HU-016, HU-017, HU-019, D-09, D-12, D-20, D-21, ADR-006]
actualizado: 2026-10-21
tags: [conecsia, integracion]
---

# Integración en main antes de loop_core

Evaluación hecha en la rama `evaluacion/pre-loop-core` (2026-10-21).

## 1. Estado de la autenticación (HU-006)

- **Verificado:** núcleo de [[ADR-006 Identidad y sesiones]] (cookie, CSRF, bloqueo 5/15 min, sesiones revocables, auditoría sin secretos, frontend real). Backend 184/184 con PostgreSQL real, frontend 28/28 y build OK. Evidencia en `goal_authentication.md`.
- **Registrado:** [[Estado de implementación]], [[tablero-scrum]], EP-02 y `log.md`.
- **Pendiente (bloqueado por decisiones, no impide el núcleo):** MFA (D-03f), límite por IP (D-21), restablecimiento de contraseña (D-12 + ADR-012) y nivel OWASP ASVS.

## 2. Qué debe estar en main antes de loop_core

| # | Elemento | Estado | Acción |
|---|---|---|---|
| 1 | Los 8 commits locales de `main` (plataforma, sedes, identidad, frontend, wiki) | En main local, **sin publicar** | `git push -u origin main` desde una terminal con credenciales |
| 2 | Esta rama (`evaluacion/pre-loop-core`): solo esta página + `index.md` + `log.md` | Commit listo | Integrar en main (fast-forward) |
| 3 | CI en verde en GitHub (`.github/workflows/ci.yml`) sobre el HEAD de main | Sin ejecutar | Verificar tras el push |
| 4 | Archivo `loop_core.md` | **No existe en el repositorio** | Crearlo/aportarlo antes de iniciar el loop |

> [!warning] Commits intermedios
> Solo el HEAD de main compila completo; los commits intermedios separan archivos pero comparten `Program.cs` y otros. No usar un commit intermedio como base de rama.

## 3. Decisiones a cerrar según lo que cubra loop_core (Pendiente)

- **D-20** idioma de rutas de la API: se necesita antes de la segunda vertical.
- **D-09** identificación documental: bloquea HU-019 (Personas, ruta crítica).
- **D-12** correo: bloquea la invitación de HU-016 y el restablecimiento.
- **D-21** antes de exponer el login fuera de la red local.

> [!todo] Sin `loop_core.md` no se puede confirmar qué historias cubre; la tabla de la §3 sigue la ruta crítica del [[PLAN-DE-TRABAJO]].
