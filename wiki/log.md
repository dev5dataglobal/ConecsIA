---
tipo: indice
estado: vigente
actualizado: 2026-10-17
tags: [conecsia]
---

# Bitácora

Solo se agregan entradas al final. Formato: `## [AAAA-MM-DD] tipo | título`.

## [2026-10-17] ingesta | Creación de la wiki e ingesta del PRD v0.1
- Creado el esquema del agente en `CLAUDE.md` y la bóveda Obsidian en `wiki/`.
- PRD copiado a `fuentes/raw/` (inmutable) y resumido en [[PRD v0.1]].
- Creadas 12 páginas de conceptos, [[Reglas de negocio]], [[Criterios de aceptación]], [[Fases y MVP]], [[Decisiones pendientes]], [[Stack y arquitectura]], [[Seguridad y auditoría]] y [[Estado de implementación]].
- Pendiente: resolver D-01..D-16; iniciar fase 0.

## [2026-10-17] decision | Hook de recordatorio de la wiki
- Agregado Stop hook en `.claude/settings.json` → `.claude/hooks/wiki-log-reminder.sh`.
- Si `wiki/log.md` no cambió en 15 min, bloquea el cierre del turno una vez y pide actualizar la wiki (CLAUDE.md §7.4).
- Sin cambios en páginas de contenido.

## [2026-10-17] decision | ADR propuestos a partir del PRD
- Revisadas las decisiones técnicas implícitas del PRD (§5.2, §6.5–6.7, §7, §8.3, §9.2, §10.3, §11, §12, §15).
- Creados ADR-001..ADR-020 en `decisiones/`, todos con `estado: propuesta`, y el [[Registro de ADR]].
- Actualizados [[index]], [[overview]] y [[Stack y arquitectura]].
- Bloqueadas por decisiones de negocio: ADR-006 (D-03), ADR-007 (D-05, D-07), ADR-008 (D-06), entre otras. No se implementó nada.

## [2026-10-17] revision | Revisión de ADR-001, 002, 010 y 020 para aprobación
- Añadida sección "Revisión 2026-10-17" a [[ADR-001 Monolito modular por capacidades]], [[ADR-002 ASP.NET Core Web API en CSharp]], [[ADR-010 Token QR opaco]] y [[ADR-020 Contratos API y formato de errores]].
- Por decidir antes de aprobar: estilo de endpoints y validación (002), contenido del QR (010), respuesta de una entrada denegada (020).
- Siguen en estado Propuesta, a la espera de la aprobación del usuario.

## [2026-10-17] consulta | Solicitud repetida de ADR propuestos
- El usuario repitió la solicitud de proponer ADR; ya estaba atendida (ADR-001..ADR-020, [[Registro de ADR]]).
- Sin cambios en la wiki.

## [2026-10-17] decision | Backlog Scrum a partir del PRD v0.1
- Creados [[PLAN-DE-TRABAJO]] (fases F0–F7, DoR, DoD general, ruta crítica, sprints orientativos) y [[tablero-scrum]].
- 19 épicas en `backlog/` con 56 historias (HU-001..HU-056), cada una con objetivo, actores, dependencias, contrato funcional, CA, tareas backend y frontend, pruebas, DoD, riesgos y decisiones.
- Nuevas decisiones D-17 (placa/serial), D-18 (modalidad Permanente) y D-19 (umbrales de alertas). Hallazgos en [[Revisión del backlog v1]].
- ADR-002 renombrado a "…en CSharp" porque `#` rompe los enlaces de Obsidian.
- El PRD sigue en v0.1 para revisión y los ADR en Propuesta: ninguna historia cumple todavía la DoR.

## [2026-10-17] decision | Aprobación de los ADR de F0
- Aprobados ADR-001, 002, 003, 004, 005, 014, 017, 018 y 020, con revisión, alcance y condiciones anotados en cada uno.
- Decisiones del usuario: controladores MVC (en lugar de Minimal APIs), FluentValidation, entrada denegada = `200` con resultado "Denegada", repositorio en GitHub con GitHub Actions. .NET 10 LTS.
- Exclusiones: RLS no adoptado (spike); multi-membresía (D-03) y matriz de decisiones (D-04) fuera de ADR-005; infraestructura productiva fuera de ADR-017.
- Actualizados [[Registro de ADR]], [[Stack y arquitectura]], [[overview]], [[index]], [[tablero-scrum]] y el contrato de denegación de HU-032 en [[EP-10 Entradas y salidas]].
- Siguen en Propuesta: ADR-006 (D-03), 007, 008, 009, 010, 011, 012, 013, 015, 016, 019.

## [2026-10-17] implementacion | Fundaciones técnicas (en curso)
- Frontend creado (Vite, React, TypeScript estricto, Tailwind, TanStack Query): shell responsivo, cliente HTTP con Problem Details, i18n con vocabulario configurable. Lint, typecheck, 14 pruebas y build en verde.
- Backend escrito (monolito modular, 7 módulos, pruebas de arquitectura primero, unitarias e integración con Testcontainers), más Docker Compose, roles de PostgreSQL y CI de GitHub Actions. **Sin compilar todavía.**
- Bloqueado: falta instalar el SDK de .NET 10 y arrancar Docker Desktop.

## [2026-10-17] implementacion | Fundaciones técnicas: build y pruebas
- SDK .NET 10.0.402 instalado con winget. Solución `ConecsIA.slnx` compilada sin advertencias; `dotnet format` limpio.
- Pruebas de arquitectura 51/51 (con prueba de mutación: 3 violaciones detectadas), unitarias backend 25/25, frontend 14/14, con lint, tipos y build correctos. Migración `Inicial` generada, OpenAPI exportado y tipos del frontend generados.
- **Sin verificar:** pruebas de integración con PostgreSQL y `docker compose up` (Docker Desktop no estaba en ejecución).
- Nuevas páginas: [[Plataforma base]] y [[ADR-021 DbContext compartido y ensamblado de migraciones]] (Propuesta). Actualizados [[Estado de implementación]], [[tablero-scrum]] (S1 iniciado), [[index]], [[overview]] y [[Registro de ADR]].

## [2026-10-17] implementacion | Pruebas de integración bloqueadas por Docker Desktop
- `dotnet test` de integración falla antes de empezar: no hay motor Docker disponible (Testcontainers no puede conectarse).
- Docker Desktop 4.41.2 arranca y se cierra solo: falla al eliminar sockets antiguos en `%LOCALAPPDATA%\Docker\run\` (`dockerInference`, `userAnalyticsOtlpHttp.sock`), que aparecen sin acceso.
- Integración y `docker compose up` siguen **sin verificar**; sin cambios de código.

## [2026-10-17] implementacion | Fundaciones técnicas verificadas con Docker
- Docker Desktop reparado por el usuario. Integración 13/13, arquitectura 51/51 y unitarias 25/25 sobre PostgreSQL 18 real (Testcontainers).
- Defecto encontrado y corregido: el 400 de validación perdía `errors`; `FiltroValidacion` ahora usa la `ProblemDetailsFactory` de MVC.
- `docker compose up --build` correcto: API y SPA (proxy) en `ready` = Healthy, Mailpit activo, migración aplicada por `conecsia_migrator`. Puerto de host de PostgreSQL configurable (`POSTGRES_HOST_PORT`) porque 5432 estaba ocupado.
- HU-001 a HU-004 pasan a "En revisión". Falta crear el repositorio en GitHub, correr el CI y la revisión del PO. Actualizados [[Estado de implementación]], [[tablero-scrum]], [[overview]] y [[Plataforma base]].

## [2026-10-17] revision | Modelo ER db.md
- Revisado `db.md` contra PRD v0.1 y ADR-004/007/008/009/010/013/014/019; `db.md` sin cambios.
- Hallazgos clave: SEDE duplicada, PUNTO_ACCESO sin definir (choca con ADR-019), falta organización en tablas hijas, faltan PERMANENCIA, TOKEN_QR, MEMBRESIA, plano SaaS, movimientos de vehículo/dispositivo, idempotencia.
- Nueva página [[Revisión del modelo ER db.md]]; actualizado [[index]].
- Pendientes: ADR-019, INVITACION vs AUTORIZACION, sedes por autorización, D-03, D-05, D-07, D-09, D-17.

## [2026-10-17] implementacion | db.md v2 aplicando la revisión del modelo ER
- `db.md` reescrito: SEDE única, ZONA como punto de acceso (ADR-019), `id_organizacion` en tablas hijas, plano SaaS, membresías, ARCHIVO, PERMANENCIA, TOKEN_QR (hash), transiciones, movimientos de vehículo/dispositivo, idempotencia, outbox.
- INVITACION reemplazada por autorización preaprobada + INVITACION_ENVIO (propuesta); una sede por autorización (confirmar).
- Actualizada [[Revisión del modelo ER db.md]]. Sigue siendo diagrama conceptual, no migración; render Mermaid no verificado.
- Pendientes: ADR-019, D-03, D-05, D-07, D-09, D-16–D-19, convención de nombres.

## [2026-10-17] implementacion | Primera vertical funcional: gestión de sedes (HU-013)
- TDD: pruebas en rojo primero (dominio, resolvedor de contexto, integración con PostgreSQL real, Vitest); luego backend (Organizaciones, Identidad, Estructura, Auditoria, contexto y autenticación de desarrollo) y frontend (`/ingresar`, `/sedes`).
- Verificado: build Release limpio, arquitectura 51/51, unitarias 52/52, integración 48/48, Vitest 22/22, mutaciones detectadas, `docker compose` con flujo de 19 pasos por nginx y UI en navegador (escritorio y móvil). Defectos corregidos: CA1861 en Docker, JSON malformado ⇒ 500, nginx HTTP/1.0, 401 en rutas inexistentes.
- HU-013 pasa a **En revisión** (no Terminada: falta CI en GitHub, revisión de código y del PO, E2E automatizado; CA-03 parcial). HU-005, HU-007 y HU-008 pasan a **En curso**.
- Nuevas: [[Sedes]], [[ADR-022 Autenticación de desarrollo provisional]] (Propuesta), D-20 (idioma de rutas). Actualizadas: [[EP-04 Estructura física]], [[EP-02 Tenancy, identidad y auditoría base]], [[tablero-scrum]], [[PLAN-DE-TRABAJO]], [[Estado de implementación]], [[Registro de ADR]], [[Decisiones pendientes]], [[index]], [[overview]].
- Pendientes: D-03 (autenticación real), D-06 (cambio de zona horaria), regla de inactivar sede con personas dentro, D-20.

## [2026-10-17] consulta | Intento de ejecutar goal_authentication.md
- Se pidió ejecutar el checklist de `goal_authentication.md`, pero el archivo no existe en el proyecto.
- El directorio tampoco es un repositorio git (`git status` falla).
- Sin cambios en la wiki ni en el código; a la espera de que el usuario aporte el archivo.

## [2026-10-17] consulta | goal_authentication.md sigue sin encontrarse
- El usuario indicó que agregó el archivo en la raíz, pero no aparece ahí ni en Documentos, Escritorio o Descargas (búsqueda de hasta 4 niveles).
- Sin cambios en el código ni en la wiki.

## [2026-10-17] implementacion | Frontend ajustado al mockup de Figma y a la paleta oficial
- Tokens de color en Tailwind (`marca`, `acento`, `tinta`, `superficie`, `fondo`…) e Inter; shell con barra lateral oscura colapsable, cabecera con organización y avatar; Sedes con migas, KPIs, filtros y tabla al estilo del mockup.
- Verificado: lint, tipos, Vitest 22/22, build y Docker; revisión visual en navegador (1440 px y 375 px sin scroll horizontal).
- Nueva página [[Sistema visual]]; actualizados [[Sedes]] e [[index]]. Desviaciones: solo módulos existentes en el menú, sin buscador/selector de sede/notificaciones.
- Pendiente: confirmar nombre ConecsIA vs ConesIA – DG (D-01); colores derivados (`tinta-suave`, `marca-tenue`, `linea`) para revisión del diseño.

## [2026-10-17] decision | Creado goal_authentication.md (HU-006)
- Nuevo `goal_authentication.md` en la raíz: protocolo por iteración y checklist en 7 fases (0 decisiones a 6 cierre).
- La Fase 0 (D-03a–f y D-14 parcial) bloquea las fases 2 a 6; la Fase 1 puede avanzar sin decisión.
- Pendiente: el responsable debe resolver D-03 y fijar el nivel de OWASP ASVS. La carpeta aún no es un repositorio git.

## [2026-10-17] implementacion | goal_authentication.md — Fase 1 completa
- `git init -b main` (sin commit). Línea base: 151 pruebas en verde.
- Inventario del punto de sustitución de la autenticación en `Estado de implementación.md`.
- Nuevas pruebas: `ArchitectureTests/AutenticacionTests.cs` (8; validada con una mutación) e `IntegrationTests/ContratoAutenticacionTests.cs` (3, PostgreSQL real). Total: 162/162.
- Detenido en la Fase 0: D-03a–f y D-14 parcial siguen sin resolver y bloquean las fases 2 a 6.

## [2026-10-17] consulta | Opciones para decidir D-03
- Nueva página `consultas/Opciones para decidir D-03.md` con las opciones de D-03a–f y D-14 parcial, y la recomendación del agente (Propuesta).
- Recomendación: Identity con cookie, multi-membresía, 8 h/30 min, bloqueo 5/15 min, NIST 800-63B, MFA obligatorio solo para los roles administradores.
- D-03 sigue abierta, a la espera del responsable.

## [2026-10-19] decision | D-03 aprobada: ADR-006 opción A (ASP.NET Core Identity)
- El responsable aprobó todas las recomendaciones de [[Opciones para decidir D-03]]: Identity + cookie, multi-membresía, 8 h / 30 min, bloqueo 5 / 15 min + límite por IP, contraseñas de 12+ caracteres, recuperación de 30 min, MFA en Fase 3, SSO diferido y D-14 parcial.
- ADR-006 y ADR-022 pasan a aprobado (ADR-022 solo Development/pruebas). Actualizados: Decisiones pendientes, Registro de ADR, index, tablero-scrum, EP-02 (HU-006 en curso) y goal Fase 0.
- Pendiente de confirmar: rechazo de contraseñas comunes (estaba en la recomendación pero no se repitió en la aprobación).

## [2026-10-19] implementacion | goal_authentication.md — Fase 2, modelo de credencial (parcial)
- `Usuario`: hash de contraseña, sello de seguridad (rota al inactivar o cambiar la contraseña), intentos fallidos y bloqueo (5 / 15 min se configurarán en la API). Migración `CredencialUsuario` con relleno del sello para los usuarios existentes.
- Pruebas: unitarias 56/56 (4 nuevas), arquitectura 59/59. **Integración sin ejecutar: Docker Desktop no está corriendo** (`dockerDesktopLinuxEngine` no existe).
- La casilla no se marca hasta aplicar la migración en PostgreSQL real. Detenido a la espera de Docker.
- Nota de diseño (Propuesta, sin ADR todavía): se usarán las piezas de ASP.NET Core Identity (`PasswordHasher`, cookie, sello de seguridad) sobre el `Usuario` existente, sin `IdentityDbContext`, para respetar ADR-021.

## [2026-10-19] implementacion | goal_authentication.md — Fase 2 completa (backend HU-006)
- Inicio y cierre de sesión con cookie de Identity, bloqueo 5 / 15 min, revocación por sello, anti-CSRF y auditoría sin secretos. Migración `CredencialUsuario` aplicada en PostgreSQL real.
- Pruebas: 177/177 (+3 unitarias, +11 de integración en `SesionTests`). Se corrigió una violación de capas: `HasherContrasenas` pasó de Infrastructure a Api.
- Páginas: Estado de implementación (§ HU-006), ADR-006 (§ Implementación), goal.
- Limitaciones: cierre de sesión sin estado en el servidor (revocar una sesión concreta queda para la Fase 3), sin prueba de los límites de 8 h / 30 min, OpenAPI sin regenerar, sin flujo de contraseña inicial (HU-016).

## [2026-10-19] implementacion | goal_authentication.md — Fase 3 parcial (registro de sesiones)
- Nuevo `SesionUsuario` (migración `SesionesUsuario`): la cookie lleva el id de sesión; cerrar o revocar una sesión la invalida en el servidor. Endpoints para listar y revocar las sesiones propias, con auditoría `sesion.revocar`.
- El analizador ASP0026 detectó que `[AllowAnonymous]` a nivel de clase anulaba `[Authorize]`; se movió a cada método antes de publicar.
- Marcadas con evidencia: bloqueo (423) y revocación por inactivación. Pruebas: 180/180 en verde (+3 de integración).
- **Detenido:** límite por IP (nueva D-21: valor y proxies de confianza), restablecimiento (D-12 correo, ADR-012 outbox) y MFA (la aprobación dice «no se implementa ahora»).

## [2026-10-20] implementacion | goal_authentication.md — /loop iteración 1: Fase 4 (frontend) y cierre de Fase 5
- Frontend con sesión real: formulario de ingreso, cierre de sesión que revoca en el servidor, token anti-CSRF con un reintento, 401 global → `/ingresar`, selector de desarrollo solo si la API lo habilita.
- Defecto encontrado por las pruebas: el reintento CSRF modificaba los `Headers` ya enviados; ahora parte del `init` original.
- Pruebas: frontend 28/28 (+6), build OK. Marcadas con evidencia: CA HU-006 #1–#4, integración, aislamiento, arranque protegido y HU-016 desbloqueada en EP-05.
- Siguen detenidos: MFA, D-21 (límite por IP), restablecimiento (D-12) y nivel OWASP ASVS. E2E real pendiente (sin semillas con contraseña).

## [2026-10-20] implementacion | goal_authentication.md — /loop iteración 2: E2E real
- E2E manual en el navegador contra Docker Compose: error genérico, ingreso, escritura con CSRF (201), cookie HttpOnly y cierre que revoca. Casilla E2E marcada.
- Defecto corregido: `/auth/csrf` daba 500 por la cookie Secure sobre HTTP local; `PoliticaSecure` es `SameAsRequest` solo en Development (+4 unitarias, 60/60; integración 65/65).
- Nuevas `SemillasContrasenas` (contraseña desde .env; `.env.example` y `docker-compose.yml` actualizados).
- Hallazgos para EP-19: `X-Forwarded-Proto` detrás del proxy (D-21) y persistencia de las claves de Data Protection.
- Las casillas restantes dependen de decisiones (MFA, D-21, D-12, OWASP ASVS); el loop se detiene.

## [2026-10-21] implementacion | goal_authentication.md — cierre del núcleo y commit
- Verificación completa: backend 184/184 (arquitectura 59, unitarias 60, integración 65 con PostgreSQL real) y frontend 28/28 + build.
- Auditoría de sesión marcada como parcial (falta el restablecimiento). HU-006 pasa a «núcleo verificado» en tablero y EP-02; no se marca verificada.
- Siguen bloqueadas por decisiones: MFA (D-03f, alcance), límite por IP (D-21), restablecimiento (D-12 + ADR-012) y nivel OWASP ASVS.
- Commit inicial preparado en `main` (el repositorio no tenía commits). loop_core.md no se ejecutó.

## [2026-10-19] implementacion | Correcciones del CI de `main` (rama `fix/ci-main`)
- `.github/workflows/ci.yml`: `dotnet ef migrations script` usa `--configuration Release` (antes `-c`, que es `--context`). Verificado en local: el script se genera.
- `.gitleaks.toml` nuevo: reglas por defecto + allowlist para `RedactorDatosSensiblesTests.cs` (valores ficticios). Sin verificar: falta ejecutar gitleaks (en CI o en local).
- Sin push. Pendiente: publicar la rama, ver el CI en verde y fusionar para alcanzar M0.

## [2026-10-19] revision | Identidad de git corregida a la cuenta empresarial
- Repo configurado con `user.name=dev5dataglobal` y el correo corporativo (config local; la global no cambia).
- Reescritos con el nuevo autor los 2 commits de `fix/ci-main` (sin cambios de contenido). `main` conserva su historia con la identidad personal, por decisión del responsable.
- Pendiente: force-push de `fix/ci-main` por el responsable.

## [2026-10-19] revision | Historia de git reescrita a la identidad empresarial
- Por pedido del responsable, los 19 commits locales (`main`, `evaluacion/pre-loop-core`, `fix/ci-main`) pasan de la identidad personal a `dev5dataglobal` con el correo corporativo (`git filter-branch --env-filter`).
- Verificado: los árboles de las tres ramas son idénticos a los de antes; solo cambian los hashes. Respaldo previo en un bundle del scratchpad de la sesión.
- Pendiente: force-push de `main` y `fix/ci-main` por el responsable. `evaluacion/pre-loop-core` no está publicada.

## [2026-10-19] revision | /loop (una iteración) detenido: M0 sigue sin cumplirse
- El force-push ya está hecho: `main` (023113b) y `fix/ci-main` (816adbc) coinciden con `origin`.
- El CI de `main` está en cola con el `ci.yml` antiguo; sin fusionar `fix/ci-main` volverá a fallar. Siguen abiertas D-20 y ADR-021 (Propuesta).
- No se implementó ninguna historia ni se creó worktree.

## [2026-10-19] consulta | Opciones para decidir D-20 (idioma de las rutas)
- Nueva página `wiki/consultas/Opciones para decidir D-20.md`: inventario del código y del OpenAPI (todo en español, salvo login/logout/csrf/me/health) y del backlog (EP-03 a EP-06 en inglés, EP-07 en adelante en español).
- Recomendación (propuesta): opción A, español con excepciones técnicas estándar. D-20 sigue pendiente hasta que la apruebe el responsable.
- `index.md` actualizado. Sin cambios de código.
## [2026-10-21] consulta | Integración en main antes de loop_core
- Rama `evaluacion/pre-loop-core`. Nueva página [[Integración en main antes de loop_core]]; index actualizado.
- Autenticación: núcleo verificado y registrado; MFA, D-21, D-12 y ASVS siguen bloqueados.
- Antes del loop: publicar main (push sin credenciales en la sesión), CI en verde y aportar `loop_core.md`, que no existe.

## [2026-10-21] decision | Plan de loops para todo el backlog (Propuesta)
- Nuevos `PLAN-DE-LOOPS.md` (matriz de HU-001 a HU-056, conflictos compartidos, hitos M0 a M8, paralelismo, worktrees) y 9 `loop_*.md` en la raíz: core, personas, autorizaciones, accesos, integraciones, reporting, opcionales, sectorial y quality.
- Todos los loops están **BLOQUEADOS**: `goal_authentication.md` no está completo (MFA, D-21, restablecimiento y ASVS). Se propone diferir esas casillas a integraciones y quality para alcanzar M0.
- Regla nueva propuesta: «token de migración» (un solo loop genera migraciones a la vez, por el snapshot único de ADR-021).
- No existen `AGENTS.md`, `wiki/vision-general.md` ni `wiki/pendientes.md`; se usaron CLAUDE.md, overview.md y Decisiones pendientes.md. D-20 debe resolverse antes de M1.

## [2026-10-21] decision | goal_authentication.md cerrado con pendientes diferidos
- El responsable cerró el goal: el restablecimiento (solicitud, confirmación, expiración del token y su auditoría) pasa a `loop_integraciones.md`; MFA, D-21 y la revisión OWASP ASVS pasan a `loop_quality.md`.
- HU-006 queda como «goal cerrado (piezas diferidas)» en el tablero y en EP-02.
- Los loops ya no están bloqueados por la autenticación. M0 sigue pendiente porque falta publicar `main` y tener el CI en verde; core necesita además D-20.

## [2026-10-21] consulta | Orden de ejecución de los loops y estado ejecutable
- Se explicó el orden de los loops (core → personas → autorizaciones → accesos → integraciones → reporting → sectorial → opcionales → quality) y qué puede correr en paralelo, según `PLAN-DE-LOOPS.md`.
- Comprobado: el stack de Docker Compose responde (SPA 200, API Healthy). Hoy se puede ver el inicio y cierre de sesión y la gestión de sedes.
- Sin cambios en otras páginas de la wiki.

## [2026-10-21] consulta | Guía de ejecución del proyecto
- Se entregó la guía de ejecución, basada en README.md.
- README.md corregido: decía que no había inicio de sesión real; ahora describe HU-006 y los usuarios sintéticos con contraseña.
- Sin otras páginas de la wiki afectadas.

## [2026-10-21] revision | Arranque local: 502 por puerto y credenciales
- Causas: el puerto 5432 no estaba disponible, el volumen de PostgreSQL tenía contraseñas de roles distintas a las de `.env` y las semillas no reasignan contraseña a usuarios que ya tienen una.
- Solución aplicada en local: `POSTGRES_HOST_PORT=5434`, `ALTER ROLE` de migrator/app y reasignación de la contraseña de los usuarios sintéticos. El inicio de sesión responde 204.
- README.md: nueva sección «Problemas frecuentes».
- Pendiente (propuesta): que las semillas actualicen la contraseña en Development cuando cambie el valor.

## [2026-10-19] revision | /loop detenido antes de la primera iteración: M0 no alcanzado
- Verificado: goal de autenticación cerrado; stack Docker arriba (postgres healthy, api, web, mailpit); `main` local = `origin/main` (85be740); ADR-004 aprobado.
- Bloqueos: el último CI de `main` (run 39266546348, sha 85be740) terminó en **failure**, así que M0 no se cumple; **D-20** (idioma de rutas) sigue abierta; **ADR-021** (migraciones) y **ADR-019** (HU-015) siguen en Propuesta.
- `AGENTS.md` no existe (se usa `CLAUDE.md`, como indica `PLAN-DE-LOOPS.md`).
- No se creó worktree, rama ni código. Pendiente: arreglar el CI de `main`, resolver D-20 y aprobar ADR-021.

## [2026-10-19] revision | Causa del CI rojo en `main` (run 39266546348)
- Fallan 2 pasos; el frontend, la compilación y las pruebas pasan.
- **backend / Script de migraciones idempotente:** `dotnet ef ... -c Release` usa `-c` como `--context`, por eso sale «No DbContext named 'Release'». Con `--configuration Release` se genera bien en local (261 líneas).
- **secretos / gitleaks:** no hay logs sin autenticación ni binario local. Probable: los valores ficticios de `RedactorDatosSensiblesTests.cs` (JWT de ejemplo, `api-key`). En `main` las contraseñas reales vienen de `${...}` o son de ejemplo. Propuesta: `.gitleaks.toml` con allowlist para esa ruta, pendiente de confirmar con el log del run.
- Sin cambios de código todavía (solo propuesta).

## [2026-10-19] decision | D-20 aprobada: rutas y contratos en español (opción A)
- `Decisiones pendientes.md`: D-20 aprobada. `ADR-020`: nueva sección «Convención de idioma» (español, plural, kebab-case; excepciones `auth/login|logout|csrf`, `me`, `health`, `saas` y claves de RFC 9457).
- Rutas indicativas traducidas en EP-03, EP-04, EP-05 y EP-06. Se quitó la nota de desviación en `Sedes` y en EP-04.
- Actualizados `PLAN-DE-LOOPS.md`, `loop_core.md`, `PLAN-DE-TRABAJO.md` e `index.md`. Nueva página `consultas/Opciones para decidir D-20` (el análisis también está en `fix/ci-main`, commit d1421b0).
- Sin cambios de código: lo implementado ya cumple la convención. `loop_core` sigue esperando M0 (CI de `main`) y ADR-021.

## [2026-10-19] consulta | Opciones para decidir ADR-021 (migraciones)
- Nueva página `consultas/Opciones para decidir ADR-021`: propuesta vigente, alternativas (contexto por módulo, migraciones en la API) e impacto en Docker, desarrollo local, CI, producción, rollback y pruebas.
- Recomendación (propuesta): aprobar A con tres condiciones: regla de arquitectura por módulo, token de migración y rollback hacia adelante.
- ADR-021 sigue en Propuesta. `index.md` actualizado. Sin cambios de código.

## [2026-10-19] decision | ADR-021 aprobado (contexto compartido y ensamblado de migraciones)
- Aprobado por el responsable: opción A con 3 condiciones (regla de arquitectura por módulo en HU-005, token de migración, rollback hacia adelante con expandir/contraer).
- Actualizados: ADR-021 (§ Aprobación), Registro de ADR, Estado de implementación, `loop_core.md`, `PLAN-DE-LOOPS.md` y la consulta de ADR-021.
- Pendiente: la prueba de arquitectura (HU-005) y el procedimiento de producción (ADR-017/HU-053). `loop_core` solo espera ya M0 (CI de `main` en verde).

## [2026-10-19] revision | loop_core no inicia: M0 sin cumplir
- D-20 aprobada (cd68fe6) y ADR-021 aprobado (b7649d7): OK.
- M0 falla: el último CI de `main` (023113b) terminó en failure. `fix/ci-main` está publicada, pero no se ha fusionado ni tiene un PR (el CI solo corre en push a `main` y en pull requests).
- HU-005 no se empezó. Pendiente del responsable: abrir el PR `fix/ci-main` → `main`, verificar el CI y fusionar.

## [2026-10-19] revision | loop_core detenido antes de HU-005: aprobaciones fuera de `main`
- M0 cumplido: CI de `main` en verde (a9bd4d0, PR #1).
- `main` no tiene `PLAN-DE-LOOPS.md`, los `loop_*.md`, la aprobación de D-20 ni la de ADR-021 (allí sigue en Propuesta). Están solo en `evaluacion/pre-loop-core`, que va 12 commits por delante.
- Fusionar esa rama en `main` daría conflictos en `wiki/consultas/Opciones para decidir D-20.md`, `wiki/index.md` y `wiki/log.md`; se resuelven conservando ambas versiones o entradas.
- No se creó worktree ni código. Pendiente: integrar `evaluacion/pre-loop-core` en `main`.

## [2026-10-19] revision | `evaluacion/pre-loop-core` rebasada sobre `origin/main`
- 13 commits reaplicados sobre a9bd4d0 (CI en verde). Conflictos: `log.md` e `index.md` resueltos conservando ambas entradas; en `consultas/Opciones para decidir D-20.md` se conservó la versión aprobada.
- Verificado: no hay entradas de la bitácora perdidas ni duplicadas. Fuera de la wiki, la rama solo difiere de antes en la corrección del CI que viene de `main`. Se quitó del índice la línea antigua de D-20, que estaba duplicada.
- Respaldo local en `respaldo/pre-loop-core-antes-rebase`. Lista para abrir el PR (requiere force-push de la rama).

## [2026-10-19] implementacion | HU-005 Aislamiento por organización en persistencia (loop_core)
- Verificado: `main` (55cbd4f) integra D-20, ADR-021 y `loop_core.md`; los checks `frontend` y `secretos` en verde; `backend` aún en curso al iniciar. Worktree `../ConecsIA-wt/core`, rama `loop/core`. Sin token de migración (no hubo migración).
- Pruebas primero: regla por módulo de ADR-021, convenciones de índice y FK, suite Org A/B reutilizable y auditoría `Denegado` de la escritura cruzada (estaba en rojo y pasó a verde). `dotnet test` 196/196.
- Wiki: ADR-004 (§ Spike RLS), `Decisiones pendientes` (nueva D-22), EP-02, `tablero-scrum`, `Estado de implementación`, `loop_core.md`.
- Pendiente: PR `loop/core` → `main` (no se hizo push) y D-22.

## [2026-10-19] implementacion | HU-007 Contexto de organización derivado de membresía (loop_core)
- Pruebas primero: `ContextoOrganizacionTests` (8), `SeleccionOrganizacion.test.tsx` (3) y prueba de endpoints anónimos. Backend 205 en verde, frontend 31/31, build OK.
- Nuevo: `GET /api/v1/me/membresias`, auditoría de denegaciones de permiso, contexto de proceso y selector de organización en la SPA. OpenAPI y `schema.d.ts` regenerados.
- Wiki: EP-02, `tablero-scrum`, `Estado de implementación`, `loop_core.md`.

## [2026-10-19] implementacion | HU-008 Auditoría append-only (cierre, loop_core)
- Pruebas primero: minimización de secretos (unitarias), rollback y atomicidad (integración). 217 pruebas en verde.
- Nuevo `MinimizadorValoresAuditoria`. ⏸ Aislamiento de lectura en HU-046; IP sin registrar.
- Wiki: EP-02, `tablero-scrum`, `Estado de implementación`, `loop_core.md`.

## [2026-10-19] implementacion | HU-017 Roles configurables y catálogo de permisos (loop_core)
- **Token de migración tomado por loop_core** para `RolesConfigurables`. Sigue tomado mientras dure este loop, porque las historias siguientes también migran; se libera al fusionar.
- Pruebas primero: `RolTests`, `RolesTests` (matriz plantilla × endpoint) y `RolesPage.test.tsx`. Backend 260 y frontend 35 en verde.
- Nuevo: catálogo, roles, plantillas propuestas, anti-escalamiento, último administrador (propuesta), pantalla `/roles` y semillas de roles.
- Wiki: nueva página [[Roles y permisos]], EP-05, `tablero-scrum`, `index`, `Estado de implementación`, `loop_core.md`. ⏸ CA-07 a EP-07 (D-04).

## [2026-10-19] implementacion | HU-009 Alta, edición e inactivación de organizaciones (loop_core)
- Orden: HU-009 antes que HU-010, porque HU-010 depende de ella. HU-015 queda ⏸ porque ADR-019 sigue en Propuesta (se pedirá confirmación).
- Pruebas primero: `OrganizacionesTests`, `OrganizacionTests` y `OrganizacionesSaasPage.test.tsx`. Backend 275 y frontend 38 en verde. Migración `OrganizacionesSaas` (token de loop_core).
- Plano SaaS separado de los permisos de organización. La inactivación sigue el comportamiento interino de D-16.
- Wiki: nueva [[Organizaciones y plano SaaS]], EP-03, `tablero-scrum`, `index`, `Estado de implementación`, `loop_core.md`.
