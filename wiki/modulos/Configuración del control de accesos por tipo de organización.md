---
tipo: modulo
estado: implementado
fuentes: [solicitud del usuario 2026-11-03]
relacionados: [ADR-029, D-50, D-51, D-52, D-42, D-40]
actualizado: 2026-11-05
tags: [conecsia, modulo, control-accesos, configuracion]
---

# Configuración del control de accesos por tipo de organización

Cómo se adapta [[Control de accesos]] a empresas, conjuntos residenciales, clubes, universidades, coworkings y parques industriales **sin cambiar código** ([[ADR-029 Registro de acceso unificado]]).

## Tres niveles

| Nivel | Dónde | Qué define | Quién lo cambia |
|---|---|---|---|
| Plataforma | `plantilla_categoria_acceso` por `tipo_organizacion` | Categorías sugeridas para cada tipo | Migración / plataforma (sin pantalla) |
| Organización | `categoria_acceso`, `horario_base` (sin sede), `organizacion.zona_horaria`, `parametro_organizacion` | Terminología, flujos, horario y políticas propias | Configuración · Editar, API `/api/accesos/configuracion` |
| Sede | `horario_base` con sede, `sede.zona_horaria` | Horario y zona horaria propios de la sede | Configuración · Editar / Sedes |

Al **crear una organización** se copian las plantillas de su tipo y, si faltan, `VISITANTE` y `RECURRENTE` (`ReglasAcceso.SembrarCategoriasAsync`). Después cada organización edita o inactiva sus categorías; los registros existentes conservan su decisión.

## Categoría de acceso

| Campo | Efecto |
|---|---|
| `nombre` | Término visible en pantallas y reportes (Visitante, Invitado, Estudiante externo…) |
| `codigo` | Identificador estable (único por organización) |
| `flujo_aprobacion` | `Automatico` (nace aprobado por política, sin decisor humano) · `Anfitrion` (decide el anfitrión del registro o Autorizaciones·Editar) · `Seguridad` (solo Autorizaciones·Editar) — Propuesta D-52 |
| `modalidades_permitidas` | Tipos de permanencia que ofrece el formulario y acepta la API |
| `exige_motivo` | Motivo obligatorio al crear |
| `permite_vehiculos` / `permite_dispositivos` | Si se pueden asociar objetos |
| `activo` | Inactivar no borra ni cambia registros |

## Plantillas sugeridas (Propuesta)

| Tipo de organización | Categorías (término visible · flujo · permanencias) |
|---|---|
| Empresa | Visitante · Anfitrión · Única/Diaria/Temporal — Contratista · Seguridad · Temporal/Recurrente — Proveedor · Seguridad · Única/Recurrente — Acceso recurrente · Seguridad · Recurrente/Permanente |
| Conjunto residencial | Visitante · Anfitrión (residente) — Domiciliario · Automático · Única — Trabajador · Seguridad · Recurrente/Permanente |
| Club | Invitado · Seguridad · Única/Diaria — Proveedor — Acceso recurrente |
| Universidad | Visitante — Estudiante externo · Temporal/Recurrente — Contratista |
| Coworking | Invitado · Automático · Única/Diaria — Miembro · Recurrente/Permanente — Proveedor |
| Parque industrial | Contratista — Transportador · Única/Recurrente — Visitante · Anfitrión |

## Parámetros de organización (`parametro_organizacion`)

| Clave | Valores | Ausente = | Estado |
|---|---|---|---|
| `accesos.fuera_horario` | `Bloquear` · `Alertar` | Bloquear | Propuesta D-51 |
| `accesos.permite_permanente` | `true` · `false` | no permitido (también lee la clave anterior `autorizaciones.permite_permanente`) | Propuesta D-42 |

## Horario y zona horaria
- `horario_base`: franjas por día ISO (1 = lunes) para la organización (`id_sede` nulo) o una sede; varias por día, sin solapes ni cruce de medianoche (D-06).
- Un registro usa sus franjas propias o marca «usar horario base»: primero el de la sede, si no hay, el de la organización.
- Las horas se evalúan en la **zona horaria de la sede**; `organizacion.zona_horaria` (por defecto `America/Bogota`) es el valor de referencia de la organización (D-50).

## API de configuración
`GET|PUT /api/accesos/configuracion` (zona horaria, fuera de horario, permanente) · `GET|POST|PUT /api/accesos/configuracion/categorias` · `GET|PUT /api/accesos/configuracion/horario-base?idSede=`. Consultar = Control de accesos·Consultar; modificar = **Configuración·Editar**. Todo cambio se audita con valor anterior y nuevo.

> [!todo] Pendiente
> - Pantalla de configuración en el frontend (hoy solo API).
> - Gestión de plantillas por la plataforma y tipos de organización nuevos.
