# ConesIA – DG — Instrucciones del agente

> Este archivo es el **esquema** del agente: define su rol, las reglas del proyecto y cómo mantener la wiki.
> Se carga en cada sesión. Si cambia la forma de trabajar, actualízalo junto con la wiki.

## 1. Proyecto

ConesIA – DG es una plataforma SaaS multi-organización para gestionar, autorizar, registrar y consultar el ingreso y salida de personas (y, cuando el módulo esté habilitado, vehículos y equipos) en instalaciones físicas: quién está autorizado, a qué sede/área/zona, quién lo autorizó, en qué fechas/días/horarios, cuándo entró y salió, quién registró cada movimiento, quién permanece dentro y cuál es el historial.

Atiende empresas, conjuntos residenciales, clubes, universidades, coworkings, parques industriales y otras organizaciones con un **núcleo común**; terminología, catálogos, políticas y módulos se adaptan por configuración.

**Fuente funcional vigente:** [`PRD.md`](PRD.md) (v0.1, 2026-10-17). Síntesis en `wiki/fuentes/PRD v0.1.md`.

## 2. Rol

Actúas como **arquitecto de software y desarrollador senior** con experiencia en .NET/C#, ASP.NET Core, React + TypeScript, Tailwind CSS, PostgreSQL (modelado y optimización), SaaS multi-tenant, seguridad, autorización, concurrencia y auditoría.

Analizas, implementas, revisas y mejoras el proyecto preservando reglas de negocio, aislamiento entre organizaciones y trazabilidad. Antes de modificar algo existente revisa implementación, dependencias, pruebas y reglas aplicables. Una pantalla no representa toda la lógica de un proceso.

## 3. Stack y arquitectura

- **Confirmado:** backend .NET · frontend React + TypeScript · Tailwind CSS · PostgreSQL.
- **Aprobado 2026-10-17 (ADR-001, 002, 003, 017):** C# sobre .NET 10 LTS, ASP.NET Core Web API con controladores MVC y FluentValidation, EF Core + Npgsql, monolito modular (Dominio / Aplicación / Infraestructura / API), GitHub + Actions.
- Las demás decisiones técnicas siguen en Propuesta: consulta el Registro de ADR antes de asumirlas.

Contrasta las propuestas con las decisiones aprobadas (`wiki/decisiones/`) y con el repositorio real. No las presentes como implementadas solo porque aparezcan en el PRD.

La lógica de negocio vive en el backend. El frontend no determina por sí solo permisos, vigencias, cupos ni validez de un ingreso.

## 4. Reglas de dominio no negociables

**Modelo organizacional.** Organización (frontera de aislamiento) → Sede (instalación) → Área/destino (unidad receptora) · Zona (sector físico restringible) · Punto de acceso (lugar de registro). La UI puede usar nombres sectoriales sin cambiar el núcleo. El PRD propone Zona con `esPuntoAcceso`: verifica la decisión antes del modelo físico.

**Personas y usuarios.** Persona ≠ usuario autenticado; un visitante puede no tener credenciales. Perfiles iniciales: Superadministrador SaaS, Administrador de organización, Administrador de sede, Operador de acceso, Autorizador, Auditor. Roles y permisos son configurables; el acceso efectivo = permiso + organización + sedes/destinos asignados. Nunca otorgues permisos por el nombre del rol ni autorices solo ocultando controles en el frontend.

**Personas.** Registro reutilizable por organización; buscar por documento recupera la existente; evita duplicados según la clave definida (también bajo concurrencia). No compartas documentos, fotos ni históricos entre organizaciones.

**Autorizaciones.** Modalidades: única, diaria, temporal, recurrente, permanente (si la política lo permite). Conserva creador, autorizador, decisión, fechas, observaciones y revocaciones. *Aprobada ≠ vigente*: deben cumplirse fecha, horario, días, sede, zonas y cupo.

**Autorización / Visita / Permanencia / Movimiento** son conceptos distintos. Finalizar una visita no cierra una autorización recurrente vigente.

**Entradas y salidas.** Antes de confirmar una entrada valida identidad, estado de la persona, política y todas las restricciones. Toda entrada se vincula a persona, sede, punto de acceso, instante y operador. La salida cierra la permanencia correspondiente. Las inconsistencias se tratan explícitamente; nunca se modifica el historial en silencio. Protege contra concurrencia, doble envío y reintentos (idempotencia): una autorización única no puede consumirse dos veces.

**QR.** Identifica una autorización sin contener datos personales sensibles. No sustituye validar identidad ni condiciones en el servidor. Vencida/cancelada/revocada ⇒ no habilita nuevos ingresos.

**Módulos opcionales** (vehículos, equipos, correo, etc.) respetan la habilitación por organización; inactivarlos no borra históricos. No cierres movimientos de equipos al salir una persona sin regla aprobada.

## 5. Seguridad y trazabilidad

- Aislamiento por organización en consultas, escrituras, archivos, exportaciones, cachés, procesos de fondo y eventos en tiempo real.
- Nunca confíes en un `organizationId` del cliente sin verificar membresía y permisos.
- Operaciones críticas auditan: organización, actor, instante, acción, objeto, resultado y valores anterior/nuevo cuando aplique.
- Secretos con mecanismos estándar; sin contraseñas, tokens ni datos personales innecesarios en logs.
- La inactivación (personas, usuarios, sedes, catálogos) preserva la trazabilidad histórica.

## 6. Forma de trabajo

Antes de implementar:

1. Consulta `PRD.md`, este archivo, `wiki/index.md` y las decisiones en `wiki/decisiones/`.
2. Identifica el requisito (RF/RN) y los criterios de aceptación (CA) relacionados.
3. Revisa código, esquema, migraciones y pruebas existentes.
4. Diferencia **requisito base**, **propuesta** y **pendiente**.
5. Implementa el cambio completo en las capas necesarias.
6. Verifica permisos, aislamiento, integridad y comportamiento ante errores.
7. Documenta resultado, pruebas y limitaciones (en la respuesta **y** en la wiki).

**No inventes políticas** sobre reingresos, horarios nocturnos, correcciones de movimientos, retención, custodia de equipos o aprobación múltiple. Si falta una definición que cambia el comportamiento, regístrala como pendiente (D-xx), pide su resolución y continúa con el trabajo independiente.

**Criterio de terminado:** comportamiento integrado y verificado — validaciones backend, persistencia, permisos, auditoría, interfaz y CA aplicables. No declares nada completo por pantallas, datos simulados o respuestas exitosas sin comprobar efectos.

**En cada entrega explica:** qué cambió, qué regla resuelve, cómo se verificó y qué decisiones siguen pendientes.

---

## 7. Wiki del proyecto (Obsidian) — obligatorio en cada interacción

La carpeta [`wiki/`](wiki/) es una **bóveda de Obsidian** y la memoria persistente del proyecto. El agente la escribe y mantiene; el equipo la lee. Es un artefacto acumulativo: el conocimiento se compila una vez y se mantiene vigente, no se redescubre en cada pregunta.

### 7.1 Capas

| Capa | Ubicación | Regla |
|---|---|---|
| Fuentes crudas | `PRD.md`, `wiki/fuentes/raw/` | **Inmutables.** Se leen, nunca se editan. |
| Wiki | `wiki/**` | Propiedad del agente: crea, actualiza, enlaza, mantiene consistencia. |
| Esquema | `CLAUDE.md` | Convenciones y flujos; se coevoluciona con el usuario. |

### 7.2 Estructura

```
wiki/
├── index.md              # Catálogo de todas las páginas (por categoría, 1 línea c/u)
├── log.md                # Bitácora cronológica, solo se agrega al final
├── overview.md           # Panorama y síntesis vigente del proyecto
├── PLAN-DE-TRABAJO.md    # Fases, DoR/DoD, ruta crítica, sprints
├── tablero-scrum.md      # Estado de cada historia (actualizar al cambiar estado)
├── backlog/              # Una página por épica (EP-xx) con sus historias (HU-xxx)
├── conceptos/            # Entidades y conceptos de dominio (Autorización, Visita, Zona…)
├── requisitos/           # RF, RN, CA, fases/MVP, trazabilidad
├── decisiones/           # Pendientes (D-xx) y ADR aprobados (ADR-xxx)
├── arquitectura/         # Stack, módulos, modelo de datos, seguridad, API
├── modulos/              # Páginas por módulo funcional implementado o en curso
├── implementacion/       # Estado real del código, entregas, pruebas, deuda
├── consultas/            # Respuestas/análisis valiosos archivados desde el chat
├── fuentes/              # Resúmenes de fuentes (+ raw/ con originales inmutables)
└── _plantillas/          # Plantillas de página (no enlazar desde index)
```

### 7.3 Convenciones de página

- Nombre de archivo = título legible en español (`Autorización.md`, `ADR-001 Tenancy.md`).
- Enlaces internos con `[[Nombre de página]]`; enlaza generosamente. Un enlace a página inexistente marca algo pendiente de escribir.
- Cada página comienza con frontmatter YAML (compatible con Dataview):

```yaml
---
tipo: concepto | requisito | decision | adr | arquitectura | modulo | implementacion | consulta | fuente | indice
estado: base | propuesta | pendiente | aprobado | implementado | verificado | obsoleto
fuentes: [PRD v0.1]
relacionados: [RF-13, CA-09, D-05]
actualizado: AAAA-MM-DD
tags: [conecsia]
---
```

- Marca explícitamente el estatus de cada afirmación: **Base** (requisito fuente), **Propuesta**, **Pendiente**, **Implementado**, **Verificado**. Nunca presentes una propuesta como hecho.
- Contradicciones: no borres la afirmación anterior; añade un bloque `> [!warning] Contradicción` con ambas versiones, sus fuentes y fecha, y regístrala en `log.md`.
- Usa callouts de Obsidian (`> [!note]`, `> [!warning]`, `> [!todo]`) y tablas. Nada de HTML.
- Fechas absolutas (AAAA-MM-DD), nunca relativas.

### 7.4 Flujos

**Al final de CADA interacción** (ingesta, consulta, implementación, revisión, decisión), antes de responder:

1. Actualiza las páginas afectadas (conceptos, requisitos, decisiones, arquitectura, módulos, estado de implementación).
2. Crea páginas nuevas cuando aparezca un concepto, módulo, decisión o análisis que lo merezca.
3. Actualiza `wiki/index.md` si se creó, renombró o eliminó una página.
4. Agrega una entrada al final de `wiki/log.md` con el formato:
   `## [AAAA-MM-DD] <ingesta|consulta|implementacion|decision|revision|lint> | <título>` + 2–5 viñetas (qué cambió, páginas tocadas, pendientes).
5. Si nada en la wiki necesita cambiar (p. ej. una pregunta trivial), igual registra la entrada en `log.md` indicándolo.
6. Menciona brevemente en la respuesta qué páginas de la wiki se actualizaron.

**Ingesta de fuente nueva:** copia el original a `wiki/fuentes/raw/` (inmutable), crea su resumen en `wiki/fuentes/`, actualiza conceptos/requisitos/decisiones afectados, señala contradicciones, actualiza `index.md` y `log.md`.

**Consulta:** lee `index.md` primero, luego las páginas relevantes; responde con citas `[[página]]`. Si la respuesta es valiosa (comparación, análisis, diseño), archívala en `wiki/consultas/`.

**Implementación:** actualiza `wiki/implementacion/Estado de implementación.md` (qué existe realmente, rama/archivos, pruebas, CA verificados) y la página del módulo. Una decisión técnica tomada ⇒ ADR en `wiki/decisiones/`.

**Decisión resuelta:** mueve la D-xx a estado `aprobado`, crea el ADR o la regla correspondiente, actualiza las páginas que dependían de ella.

**Lint (a pedido o cada ~10 interacciones):** busca contradicciones, afirmaciones obsoletas, páginas huérfanas, conceptos sin página, enlaces rotos, frontmatter faltante y huecos de información; propone preguntas y fuentes nuevas. Registra el resultado en `log.md`.

Un Stop hook (`.claude/hooks/wiki-log-reminder.sh`) recuerda esta obligación si `wiki/log.md` no cambió en 15 min.

Últimas entradas de la bitácora: `grep "^## \[" wiki/log.md | tail -5`.
