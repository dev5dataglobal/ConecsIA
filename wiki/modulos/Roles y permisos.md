---
tipo: modulo
estado: implementado
fuentes: [PRD v0.1]
relacionados: [HU-017, HU-007, HU-016, RF-06, RN-10, CA-01, CA-07, ADR-005, ADR-014, D-04]
actualizado: 2026-10-19
tags: [conecsia, modulo, ep-05]
---

# Roles y permisos

[[EP-05 Usuarios, roles y permisos#HU-017 Roles configurables y catálogo de permisos|HU-017]], implementada el 2026-10-19 en la rama `loop/core` (sin fusionar). Modelo: [[ADR-005 Contexto de organización y autorización por permisos]].

## Qué existe (Implementado)
- **Catálogo cerrado** `CatalogoPermisos` (`SharedKernel/Seguridad/Permisos.cs`): claves `recurso.accion` con descripción. `GET /api/v1/permisos`.
- **Rol** por organización (`Modules.Identidad`, tabla `rol`): nombre único por organización (normalizado), descripción, permisos del catálogo, plantilla de origen (solo informativa), estado, `xmin` como versión. Inactivar conserva el historial y deja de conceder permisos.
- **Membresía → roles**: columna `membresia.roles` (`uuid[]`). Permisos efectivos = roles activos asignados ∪ claves directas (transitorias, de semillas y pruebas previas). Se leen en cada petición: un cambio de rol rige en la siguiente, sin caché que invalidar.
- **API** `GET/POST /api/v1/roles`, `GET/PUT /api/v1/roles/{id}`, `POST /api/v1/roles/{id}/inactivar`, `GET /api/v1/roles/plantillas`.
- **Reglas en el servidor:** solo claves del catálogo (400); nombre repetido (409, `ROL_NOMBRE_DUPLICADO`); nadie concede permisos que no tiene (403, `ESCALAMIENTO_DE_PERMISOS`); al modificar solo se exige lo que se agrega; el nombre del rol nunca concede nada (RN-10).
- **Auditoría:** `rol.crear`, `rol.modificar` (valores anterior/nuevo) y `rol.inactivar`.
- **SPA:** `/roles` lista los roles y ofrece un editor con permisos agrupados por recurso y precarga desde una plantilla. Las acciones se ocultan sin `roles.administrar` (solo UX).
- **Semillas:** `SemillasRoles` copia las plantillas en las organizaciones sintéticas y asigna Administrador de organización y Operador de acceso.

## Propuesta (implementada, pendiente de confirmar por el PO)
> [!warning] Propuesta
> - **Último administrador:** no se inactiva ni se le quita `roles.administrar` al único rol activo que lo concede (422 `ULTIMO_ROL_ADMINISTRADOR`). Solo cuenta roles; las claves directas no cuentan.
> - **Plantillas** (`PlantillasRol`): permisos iniciales propuestos. Se amplían cuando cada módulo agregue sus claves (personas, autorizaciones, accesos). El Operador de acceso no recibe permisos de decisión (PRD §6.2).

## Matriz plantilla × permiso (verificada)
Verificada por `RolesTests.Cada_plantilla_accede_solo_a_lo_que_conceden_sus_permisos` (25 casos: 5 plantillas × 5 endpoints). Con el permiso, el endpoint no responde 403; sin él, responde 403.

| Plantilla | sedes.consultar | sedes.crear | sedes.modificar | sedes.inactivar | roles.consultar | roles.administrar |
|---|---|---|---|---|---|---|
| Administrador de organización | ✔ | ✔ | ✔ | ✔ | ✔ | ✔ |
| Administrador de sede | ✔ | — | ✔ | — | — | — |
| Operador de acceso | ✔ | — | — | — | — | — |
| Autorizador | ✔ | — | — | — | — | — |
| Auditor | ✔ | — | — | — | ✔ | — |

| Endpoint | Permiso exigido |
|---|---|
| `GET /api/v1/sedes`, `GET /api/v1/sedes/{id}` | `sedes.consultar` (+ alcance de sede) |
| `POST /api/v1/sedes` | `sedes.crear` + alcance total |
| `PUT /api/v1/sedes/{id}` | `sedes.modificar` (+ alcance) |
| `POST /api/v1/sedes/{id}/inactivar` | `sedes.inactivar` (+ alcance) |
| `GET /api/v1/roles`, `/roles/{id}`, `/roles/plantillas`, `/permisos` | `roles.consultar` |
| `POST /api/v1/roles`, `PUT /roles/{id}`, `POST /roles/{id}/inactivar` | `roles.administrar` (+ anti-escalamiento) |

## Pendiente
- ⏸ CA-07 (operador sin `autorizaciones.decidir` recibe 403 al aprobar): no existe todavía el endpoint de decisión (EP-07) y los permisos de decisión por tipo dependen de [[Decisiones pendientes|D-04]].
- La asignación de roles a usuarios desde la UI llega con HU-016.
- E2E con navegador: no hay (solo Vitest y pruebas de integración).

## Usuarios y alcance (HU-016, HU-018 por sede — Implementado 2026-10-19)
- **Invitación** `POST /api/v1/usuarios/invitaciones` (`usuarios.invitar`): correo, nombre, roles, alcance inicial. Crea el usuario si no existe. Con un correo existente agrega una membresía nueva (D-03b), salvo que ya sea miembro (409). La membresía queda **pendiente** hasta aceptar.
- **Token** de 32 bytes aleatorios; se guarda solo el hash SHA-256 (índice único global, `[UnicidadGlobal]`). Vigencia `Invitaciones:VigenciaHoras`, 72 h por defecto (propuesta, configurable). Es de un solo uso, con concurrencia optimista (`xmin`).
- **Aceptación** anónima `POST /api/v1/invitaciones/aceptar` `{token, contrasena}`: 410 si no existe, venció o ya se usó (sin distinguir el motivo). Si el usuario no tiene contraseña, exige 12 caracteres o más (D-03e). El token va en el cuerpo para no quedar en trazas.
- **Envío:** el enlace solo se devuelve en Development y Testing. El correo es de loop_integraciones (D-12).
- **Administración** (`usuarios.administrar`): `PUT /usuarios/{id}/roles` (roles de la organización, anti-escalamiento), `PUT /usuarios/{id}/alcance`, `POST /usuarios/{id}/inactivar` (revoca las sesiones del usuario mediante el sello) y `/reactivar`. Otro ID de organización responde 404.
- **Alcance por sede (HU-018):** el alcance vacío no da acceso territorial. Quien asigna solo concede sedes dentro de su propio alcance, y solo quien tiene alcance total puede concederlo. Una sede de otra organización responde 404 (contrato `IConsultaSedes`).
- **Auditoría:** `usuario.invitar`, `usuario.aceptar_invitacion`, `usuario.roles`, `usuario.alcance`, `usuario.inactivar` y `usuario.reactivar`.
- **SPA:** `/usuarios` (lista, invitar, roles y alcance, inactivar) y `/invitaciones/:token` (aceptación).
- **Pendiente:** el vínculo con Persona (loop_personas). El alcance por destino llega con HU-014; el de punto de acceso queda ⏸ (HU-015, ADR-019).
