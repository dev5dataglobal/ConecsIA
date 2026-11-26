---
tipo: consulta
estado: propuesta
fuentes: [PRD v0.1, PLAN-DE-TRABAJO, tablero-scrum, Estado de implementación, código en develop]
relacionados: [RF-01..RF-26, CA-01..CA-26, D-02, D-04, D-05, D-06, D-08, D-12, D-19, ADR-029]
actualizado: 2026-11-11
tags: [conecsia, backlog, brechas]
---

# Brechas de requisitos frente al PRD (2026-11-10)

Contraste entre [[PRD v0.1]], [[PLAN-DE-TRABAJO]], [[tablero-scrum]], [[Estado de implementación]] y el código de la rama `develop` (con cambios sin commit de ADR-029). Se revisaron controladores, rutas del frontend y búsquedas puntuales (SignalR, QR, Excel/PDF, procesos de fondo y correo).

> [!warning] El tablero está desactualizado
> [[tablero-scrum]] y [[PLAN-DE-TRABAJO]] tienen fecha del 2026-10-26. No reflejan [[ADR-028 Zona empresarial por sede y retiro de zonas de acceso]] ni [[ADR-029 Registro de acceso unificado]]. Por ejemplo, HU-023..HU-027 siguen en *Backlog*, aunque el registro unificado ya crea, aprueba, rechaza, revoca y cancela autorizaciones con modalidades y horario. HU-015 se apoya en zonas que ya se retiraron. Hay que reconciliarlo antes de planificar el siguiente sprint.

## Estado por requisito funcional

Leyenda: ✅ implementado y verificado localmente (falta CI/PO) · 🟡 parcial · ❌ no implementado (a lo sumo hay un controlador genérico de scaffold).

| RF | Tema | Estado | Evidencia / lo que falta |
|---|---|---|---|
| RF-01 | Alta/inactivación de organizaciones | 🟡 | `OrganizacionesAdministracionController` + UI. Falta el efecto operativo de inactivar (D-16) |
| RF-02 | Tipo e información general | 🟡 | Tipo con plantillas de categorías ([[Configuración del control de accesos por tipo de organización]]); `TipoOrganizacionController` es scaffold y no hay pantalla de tipos |
| RF-03 | Sedes | ✅ | [[Sedes]]. Falta la regla de inactivar una sede con personas dentro |
| RF-04 | Áreas y responsables | ✅ *(2026-11-11)* | Pantalla y API de responsables y autorizadores ([[Sedes]]); efecto de «autorizador» en D-56. Antes: Áreas, sí. **Sin pantalla para designar responsables/autorizadores** (`AreaResponsableController` es scaffold); D-36 |
| RF-05 | Puntos de acceso | ✅ | [[Puntos de acceso]] (ADR-028) |
| RF-06 | Usuarios, roles y permisos | 🟡 | Perfiles y matriz Consultar/Añadir/Editar ([[ADR-024 Acciones por módulo y contexto de organización]]). **Falta:** asignar sedes/puntos a un usuario desde la UI (HU-018; hoy sin asignación = todo, D-34), alta por invitación (HU-016), acciones separadas decidir/revocar/exportar (PRD §5.2.4, D-35/D-40) |
| RF-07 | Personas sin duplicados | ✅ | [[Personas]]; clave provisional D-09 |
| RF-08 | Tipos de persona | ✅ | Catálogo por organización |
| RF-09/10/20 | Autorizaciones, modalidades, trazabilidad | 🟡 | Registro unificado (ADR-029). Siguen abiertas D-04 (quién decide), D-05 (reingresos/cupos), D-06 (nocturnos/festivos) y D-18/D-42 (Permanente) |
| RF-11 | Visita ocasional | 🟡 | Integrada al registro de acceso; la decisión por responsable designado depende de RF-04 |
| RF-12 | Preautorización e **invitación** | ❌ | No se envía invitación (`InvitacionEnvioController` es scaffold) y no hay correo de negocio |
| RF-13 | Entrada validada | ✅ | `POST /api/accesos/{id}/entrada`: idempotencia, concurrencia y motivos (CA-09, CA-10, CA-12) |
| RF-14 | Salida | ✅ | `POST /api/accesos/permanencias/{id}/salida` (CA-13). La regularización (HU-035) está bloqueada por D-08 |
| RF-15 | **QR** | ❌ | Solo el scaffold `TokenQrController`. No hay emisión, lectura ni validación (HU-030/HU-031, ADR-010) |
| RF-16 | Vehículos | ✅ *(2026-11-11, núcleo)* | Entradas y salidas en portería ([[ADR-030 Movimientos de vehículos y dispositivos]]); siguen D-10, D-54 y D-57. Antes: Registro, asociación y autorización del objeto. **Faltan movimientos vehiculares** en portería (HU-048; D-10, D-17, D-54 parqueaderos) |
| RF-17 | Dispositivos | ✅ *(2026-11-11, núcleo)* | Movimientos y «equipos dentro» (ADR-030); custodia en D-10. Antes: Registro y autorización. **Faltan movimientos y la consulta de «equipos dentro»** (HU-050; D-10, D-33) |
| RF-18 | Historial | 🟡 | Historial general y bitácora por registro. Falta el **historial por persona** completo (HU-038, PRD §10.1.4) |
| RF-19 | Presencia en tiempo real | 🟡 | `GET /api/accesos/presencia` con sondeo cada 30 s; **sin SignalR** (ADR-011, HU-037) |
| RF-21 | **Alertas** | ❌ | No hay generación ni ciclo Nueva → Revisada → Cerrada (`AlertaController` es scaffold). El ingreso fuera de horario solo se audita como «Alerta». D-19 sigue abierta |
| RF-22 | **Correo de negocio** | ❌ | Hay SMTP solo para recuperar la contraseña. Faltan el outbox, los reintentos, las plantillas por organización y los eventos (HU-042/HU-043; D-12, D-30). No existe ningún proceso de fondo (`BackgroundService`) |
| RF-23 | Dashboard | 🟡 | `GET /api/dashboard` con 4 indicadores. Faltan: pendientes, autorizadas y rechazadas; próximas a vencer; sin salida; franjas horarias. **Posible defecto:** filtra `Estado == "Activa"`, que no figura entre los estados del registro unificado (Pendiente/Aprobada/Vigente…), así que podría mostrar 0. Por verificar |
| RF-24 | **Reportes Excel/PDF** | ❌ | Solo hay exportación CSV del historial (auditada). No hay librería Excel/PDF (ADR-016, HU-045) |
| RF-25 | Auditoría | 🟡 | El registro append-only funciona. Falta la **consulta para el auditor** (HU-046) y auditar los 403 por permiso (HU-008) |
| RF-26 | Configuración sectorial | 🟡 | Categorías y plantillas por tipo. Faltan vocabulario configurable (HU-012), plantillas completas (HU-051) y contratistas (HU-052, D-11). La **pantalla de configuración del control de accesos** tiene backend pero no frontend |

## Hallazgos de riesgo (no son RF nuevos)

> [!warning] Controladores genéricos de scaffold expuestos
> Hay alrededor de 30 controladores de 109 líneas con `api/[controller]` que hacen CRUD sobre la entidad completa, incluido `DELETE` físico. Dos chocan con reglas no negociables:
> - `AutorizacionController` (`/api/Autorizacion`): `POST`/`PUT`/`DELETE` sobre `autorizacion`, que es el registro de acceso de ADR-029. **Se salta las validaciones, el ciclo de estados y la auditoría** de `AccesosController`.
> - `AuditoriaController`: expone `DELETE`. Los triggers append-only deberían rechazarlo, pero el endpoint no debería existir (CA-20).
> - Los demás (`Rol`, `MembresiaSede`, `Organizacion`, `Alerta`, `Outbox`, `TokenQr`…) exponen entidades internas y no está probado su aislamiento con dos organizaciones (CA-01).
>
> **Propuesta:** retirarlos o reemplazarlos por casos de uso, como ya se hizo con Personas y Visitas. Prioridad alta: es deuda de seguridad, no una funcionalidad.

> [!note] Resuelto 2026-11-10
> Se retiraron 35 controladores scaffold. `UsuarioController` conserva solo `/api/Usuario/me`, porque su listado genérico devolvía `Usuario.PasswordHash`. Prueba: `ControladoresGenericosRetiradosTests`. Ver [[Estado de implementación]].

- **Migraciones ADR-028/029 sin aplicar** a `conecsia` ni a la base de Docker (solo a `conecsia_verificacion`). Ver [[Guía de migración al control de accesos unificado]].
- **Cambios de ADR-029 sin commit** en `develop` (el estado de git muestra borrados de Visitas y Autorizaciones).
- **CI sin ejecutar** en GitHub. Ninguna historia cumple la DoD y el tablero no tiene ninguna *Terminada*.
- **Sin E2E** (Playwright). Hay 2 pruebas de frontend inestables por tiempos en Administración.

## Requisitos sin historia en el backlog

| Origen PRD | Requisito | Propuesta |
|---|---|---|
| §5.2.6 | Acceso de soporte auditado del superadministrador | Nueva HU en EP-05 (ya figuraba en «Por redactar») |
| §4.2 | Instrumentar métricas: tiempo de atención, tiempo de decisión, reutilización de registro, latencia p50/p95 | Nueva HU en EP-14 o EP-19 |
| §9.2 | Selección visible de **sede operativa** en recepción, para no registrar en un contexto equivocado | Criterio a añadir a HU-036 |
| §9.2 / §7.2 | Indicador de estado de conexión y última actualización | Criterio a añadir a HU-037 |
| §8.2 | Advertir al operador si la persona sale con equipos pendientes | Criterio a añadir a HU-050 (después de D-10) |
| ADR-029 | Pantalla de configuración del control de accesos (categorías, horario base, `accesos.fuera_horario`, `autorizaciones.permite_permanente`) | Nueva HU en EP-07 o EP-18 |
| §12.5 / D-55 | Aviso de privacidad y autorización de tratamiento de datos (Ley 1581) al registrar personas | Nueva HU en EP-19, bloqueada por D-55 |
| — | Retirar los controladores scaffold | Nueva HU técnica en EP-02 |

## CA del MVP (F0–F4) sin evidencia

CA-08 (QR), CA-14 (reconexión: sin tiempo real), CA-16 (alertas), CA-17 (correo), CA-18 (dashboard completo), CA-19 (Excel/PDF y aislamiento de exportaciones), CA-21 (recorrido completo en móvil y tablet sin E2E) y CA-22 (restauración). Están cubiertos o parciales: CA-01..CA-07, CA-09..CA-13, CA-15 y CA-20 (registro, sin consulta).

## Decisiones que bloquean lo que falta

| Bloqueo | Decisión |
|---|---|
| Alertas y dashboard «sin salida» | D-19 (umbrales), D-12 (correo) |
| Correo e invitación | D-12, D-30 (proveedor y URL pública) |
| Reingresos y cupos por modalidad | D-05 |
| Horarios nocturnos y festivos | D-06 |
| Quién decide/revoca, responsables por destino | D-04, D-36, D-40 |
| Movimientos de vehículos y equipos | D-10, D-17, D-31, D-33, D-54 |
| Regularización | D-08 |
| Datos reales / piloto | D-14, D-55, D-22 (RLS antes del SQL crudo de reportes) |
| Alcance del MVP | **D-02 sigue abierta** (el plan asume F0–F4; F5 se adelantó en parte) |

## Orden sugerido (Propuesta)

1. **Higiene:** commit de ADR-029, retirar los scaffolds peligrosos, revisar el filtro `"Activa"` del dashboard, aplicar las migraciones pendientes, poner el CI en verde y reconciliar el tablero.
2. **Cerrar F1:** responsables por área (HU-014), alcance por sede/punto en la UI (HU-018), invitación de usuarios (HU-016) y pantalla de configuración de accesos.
3. **F2 pendiente:** QR (HU-030/HU-031) e invitación al visitante (HU-029). Los dos dependen del correo para el envío; el QR se puede mostrar o imprimir sin correo.
4. **F3 pendiente:** SignalR (HU-037), historial por persona (HU-038), bandeja del autorizador (HU-039) y pantalla de recepción con QR (HU-036).
5. **F4 (cierre del MVP):** outbox y correo (HU-042/043), alertas (HU-040/041), dashboard completo (HU-044), Excel/PDF (HU-045) y consulta de auditoría (HU-046).
6. F5–F7 según D-02.
