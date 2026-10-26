---
tipo: indice
estado: vigente
actualizado: 2026-10-17
tags: [conecsia]
---

## [2026-10-21] implementacion | Control de errores del login
- La API distingue usuario no registrado, contraseña incorrecta y usuario bloqueado.
- El login devuelve `ProblemDetails` en español y el frontend presenta el detalle sin `Unauthorized` ni `traceId` para errores `401`.

# Bitácora

## [2026-10-21] implementacion | Configuración local segura para arranque
- La API carga opcionalmente `appsettings.Local.json` para desarrollo.
- Se agregó `appsettings.Local.example.json` sin credenciales.
- `appsettings.Development.json` permanece libre de secretos versionados.
- El frontend se conectó a `POST /api/auth/login` y al JWT Bearer; `npm run build` pasó.
- Vitest quedó pendiente por un worker que no termina en este entorno.
- Se creó idempotentemente `dataglobal` y `desarrollador5@dataglobal.com` con membresía activa y rol administrativo.
- La segunda ejecución confirmó que no se duplicaron registros; el sembrador temporal fue eliminado.
- Se corrigió el destino predeterminado del proxy Vite de `5080` a `5191`, coincidiendo con `launchSettings.json`.

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

## [2026-10-19] revision | Push de `loop/core` e intento de PR de HU-005
- `loop/core` publicada en origin (926e34c, 9798ede). El PR no se creó: GitHub no tenía sesión en el navegador integrado y no hay `gh`. Queda pendiente que el responsable lo abra.
- El detalle de HU-005 está en la bitácora de `loop/core`. Este checkout (`evaluacion/pre-loop-core`) no cambió fuera de esta entrada.
- Siguiente: completar las historias restantes de `loop_core.md` en `loop/core`.

## [2026-10-19] revision | Avance de loop_core en la rama loop/core
- En `loop/core` (worktree `../ConecsIA-wt/core`) están implementadas HU-005, 007, 008, 009, 011, 012, 014, 016 y 017; HU-018 parcial; HU-015 en pausa (ADR-019). El detalle está en la bitácora de esa rama.
- Sigue: HU-010 y el cierre de HU-013. Esta entrada solo registra el avance; este checkout no cambió.

## [2026-10-19] revision | Push de loop/core y PR preparado
- `loop/core` publicada en origin (hasta 3877e82), al día con `origin/main`.
- Formulario del PR `loop/core` → `main` listo con título y descripción en el navegador integrado. No se creó porque GitHub no tiene sesión ahí y no hay `gh`. Falta que el responsable pulse «Create pull request» y revise el CI.

## [2026-10-19] revision | Vincular PR de loop/core: no encontrado
- La API de GitHub solo lista el PR #1 (`fix/ci-main`, cerrado); no existe ningún PR con head `loop/core`. No se vinculó nada ni se revisó CI.

## [2026-10-20] implementacion | HU-006 pantalla de ingreso ConecsIA
- Se alineó `/ingresar` con la referencia visual: panel informativo oscuro, formulario claro responsive y accesibilidad básica.
- Se conservó el flujo real de cookie HttpOnly, CSRF, login, estados de error/carga y redirección; se retiró el selector de usuarios sintéticos del login.
- Verificado: build, lint y 26 pruebas frontend en verde.
- Pendientes documentados: recuperación, Microsoft y “Recordarme” requieren alcance/backend aprobado.

## [2026-10-20] correccion | Redirección inicial al login
- La ruta raíz `/` mostraba el inicio sin sesión porque no estaba detrás de `RequiereSesion`.
- Se protegió la ruta raíz; ahora un despliegue limpio redirige a `/ingresar` y una sesión válida conserva el inicio.

## [2026-10-20] implementacion | Separacion de entidades por archivo
- Se reemplazÃ³ `Domain/Entities/Entities.cs` por un archivo individual para cada entidad y la clase base `OrganizacionEntity`.
- Los contratos y repositorios ya estaban separados individualmente; no se crearon clases vacÃ­as de controllers porque no existen endpoints implementados en esta fase.
- Verificado: `dotnet build backend/ApiConecsia/ApiConecsia.csproj --no-restore` correcto, 0 errores.

## [2026-10-20] implementacion | Fase 1 backend hexagonal y migracion PostgreSQL
- Se crearon los cuatro proyectos de `backend/`, entidades desde `db.md`, contexto EF Core/Npgsql, repositorios especÃ­ficos y factory de diseÃ±o.
- Se verificÃ³ `dotnet build` correctamente y se generÃ³ `20261020205103_InitialCreate`.
- `dotnet ef database update` quedÃ³ pendiente por error PostgreSQL `28P01`; requiere `CONECSIA_CONNECTION_STRING` vÃ¡lida.
- La Fase 2 (unidad de trabajo y DTOs) queda documentada sin implementarse.

## [2026-10-20] revision | Puerto 5432 ocupado por PostgreSQL local de Windows
- Causa confirmada: el proceso `postgres` del sistema (PID 8464) escucha en 5432, y `.env` había vuelto a `POSTGRES_HOST_PORT=5432`.
- Solución: `.env` con 5434 y stack levantado; la API responde Healthy.
- Sin cambios en otras páginas (el README ya documenta el caso en «Problemas frecuentes»).

## [2026-10-21] implementacion | Tamaño de marca en feature/frontend
- En `feature/frontend`, la marca del login usa los tamaños de `feature/login`: ícono 2.9rem, nombre 1.35rem, subtítulo 0.78rem.
- Los demás cambios sin commit del usuario (fondo, IngresarPage) quedan intactos. Sin commit.
## [2026-10-21] implementacion | Autenticación JWT y aislamiento multi-organización
- Se implementó login JWT con claims derivados de membresías, roles y permisos existentes.
- Se protegieron globalmente los endpoints y se validan usuario activo y sello de seguridad.
- Se centralizó el filtrado y validación de escritura de entidades `OrganizacionEntity`.
- Se eliminaron secretos de configuración versionada.
- Pendientes: matriz D-04 por endpoint y refresh tokens revocables; no hay migración requerida.

## [2026-10-22] implementacion | Rediseño visual del login «Pase de acceso»
- `IngresarPage.tsx` e `ingresar.css` reescritos: credencial con profundidad, inclinación 3D, QR decorativo con estados, etiquetas flotantes, feed ilustrativo.
- Animaciones siguiendo la skill de Emil Kowalski (ease-out fuerte, transform/opacity, reduced-motion).
- Contrato de autenticación sin cambios; pruebas de componente 2/2 y `tsc` en verde.
- Páginas: [[Estado de implementación]]. Pendiente: revisión visual y limpiar `.login-*` en `styles.css`.

## [2026-10-22] revision | Revisión visual del login en escritorio y tablet
- Revisado en navegador a 1440×900, 768×1024 y 1024×768; estado de error comprobado contra la API local.
- Corregido: patrón QR a rayas (ahora con esquinas tipo QR), peso del título, contraste del divisor y entrada que dependía de JS (ahora solo CSS).
- Se agregó `.claude/launch.json` (servidor `frontend`). Páginas: [[Estado de implementación]].

## [2026-10-22] implementacion | Ajustes de layout del login y burbujas flotantes
- Tablet (≤1024px): titular y etiqueta centrados y más arriba; pase de ingreso más ancho (32rem).
- Pase más grande en escritorio (30rem, campos 3.6rem, QR 4.8rem); en pantallas anchas el contenido se agrupa en 1360px para reducir el vacío central.
- Seis burbujas de colores flotando (solo transform, se detienen con movimiento reducido); la etiqueta flotante ya no se superpone al autocompletado.
- Verificado en navegador a 1900×950 y 820×1180; pruebas 2/2. Páginas: [[Estado de implementación]].

## [2026-10-22] implementacion | Shell autenticado (menú) y dashboard con el lenguaje del login
- Nuevo `AppShell` como layout protegido de `/dashboard` y `/administracion`; menú por permisos, colapsable y deslizable en móvil; módulos sin pantalla marcados «Pronto».
- Dashboard rediseñado solo con datos de `GET /api/dashboard`; se retiraron controles sin funcionalidad (buscador, selectores de la barra, notificaciones).
- Tokens `--ui-*` en `styles.css` y limpieza de las reglas `.login-*` sin uso.
- Pruebas 4/4, build OK, revisión visual en escritorio/tablet/móvil con datos de demostración. Páginas: [[Estado de implementación]].
- Pendiente: probar con usuario real y datos reales; crear pantallas para los módulos «Pronto».

## [2026-10-22] implementacion | Rediseño del módulo de Administración
- Administración con banda oscura, pestañas por permiso, tablas/tarjetas y modal coherentes con el dashboard.
- Mensaje específico para 403; pruebas nuevas en `AdministracionPage.test.tsx` (6/6 en total).
- Pendiente: diagnosticar el 403 del usuario real en `GET /api/administracion/usuarios`. Páginas: [[Estado de implementación]].

## [2026-10-22] implementacion | Acceso dinámico a módulos por rol (app_modulos / modulos_permisos)
- Backend: entidades, configuraciones, migración con catálogo y traslado desde rol_permiso, servicios, endpoints, política `[RequiereModulo]` en 55 controladores, caché invalidable y auditoría.
- Frontend: menú y rutas desde `/api/app-modulos/mios`; gestión de módulos por perfil en Administración.
- Corregidos dos errores previos: filtro de organización del DbContext fijado en -1 y migración manual de zonas con argumentos mal ubicados.
- Pruebas: backend 10/10 (nuevo proyecto ConecsIA.Tests sobre PostgreSQL temporal), frontend 9/9. Migración no aplicada a la base local.
- Páginas: [[ADR-023 Acceso a módulos por rol en base de datos]], [[Decisiones pendientes]] (D-23), [[Registro de ADR]], [[Estado de implementación]], [[index]].

## [2026-10-22] implementacion | Migraciones aplicadas a la base local
- Aplicadas `AddBusinessZonesAndAuthorizationScopes` y `AddAppModulosYModulosPermisos` en `conecsia` (respaldo `pg_dump` previo).
- Verificado: 4 migraciones en el historial, 12 módulos, tabla `zona_empresarial` creada, datos existentes intactos.
- Pendiente: reiniciar la API en ejecución para cargar el código nuevo. Páginas: [[Estado de implementación]].

## [2026-10-24] consulta | Comandos de ejecución
- Se entregaron comandos para levantar backend (dotnet, puertos 5191/7127), migraciones EF y frontend (Vite).
- Sin cambios en otras páginas de la wiki.

## [2026-10-24] implementacion | Permisos de administración para el perfil 1
- Catálogo `permiso` estaba vacío en la BD local: se sembraron 19 códigos usados por `AdministracionAuthorization` (incluye variantes `saas.*` de plataforma).
- Asignados todos al rol 1 "Administrador plataforma" (org 1) vía `backend/scripts/seed-permisos-perfil-1.sql` (idempotente).
- Pendiente: llevar el catálogo de permisos a un seed/migración EF y definir qué perfiles de plantilla reciben cada permiso.

## [2026-10-24] implementacion | Administración completa, acciones por módulo y zonas empresariales
- Consultar/Añadir/Editar por módulo en `modulos_permisos`, validado en backend por verbo HTTP o acción explícita; `administracion` dividido en usuarios, perfiles, organizaciones y zonas_empresariales (migración `20261024213105`, aplicada en la base local).
- Usuarios, perfiles, organizaciones (con logo privado) y zonas empresariales con organización superior: CRUD con inactivación, confirmaciones y aislamiento; selector de organización solo para plataforma (`X-Organizacion-Id` validado por petición).
- Pruebas: backend 24/24, frontend 12/12, verificación en navegador con base desechable `conecsia_verificacion`.
- Páginas: nuevas [[ADR-024 Acciones por módulo y contexto de organización]], [[Administración]], [[Zona empresarial]]; actualizadas [[ADR-023 Acceso a módulos por rol en base de datos]], [[Registro de ADR]], [[Decisiones pendientes]] (D-24..D-28), [[Estado de implementación]], [[index]].

## [2026-10-26] implementacion | Fábrica de diseño EF alineada con Program.cs
- Error: `dotnet ef database update` fallaba con "Defina CONECSIA_CONNECTION_STRING…" porque `DesignTimeDbContextFactory` solo leía esa variable, mientras la API usa `appsettings.Local.json`/`DefaultConnection`.
- Cambio: la fábrica lee `appsettings*.json` del proyecto de inicio + variables de entorno con la precedencia de `Program.cs`; se agregaron `Microsoft.Extensions.Configuration.Json` y `.EnvironmentVariables` 9.0.6 a Infrastructure.
- Verificado: `migrations list` (5 migraciones, ninguna pendiente), `database update` → "already up to date", `has-pending-model-changes` → sin cambios.
- Pendiente (seguridad): `appsettings.Development.json` y `appsettings.Local.example.json` están versionados con la contraseña real de PostgreSQL; retirarla y rotarla.
- Páginas: [[Estado de implementación]], [[Fase 1 backend hexagonal]].
