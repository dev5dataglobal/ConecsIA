---
tipo: requisito
estado: base
fuentes: [PRD v0.1]
relacionados: [CA-01..CA-26]
actualizado: 2026-10-17
tags: [conecsia]
---

# Criterios de aceptación

Copia de trabajo de PRD §14. La verificación se lleva en [[Estado de implementación]].

## 14.1 Núcleo y MVP propuesto

| ID | Escenario verificable | Requisitos |
|---|---|---|
| CA-01 | Un usuario de A no puede listar, leer, modificar, exportar, descargar archivos ni recibir eventos de B aunque conozca sus IDs | RF-01, RF-06; RN-01 |
| CA-02 | Una organización administra varias sedes y limita un operador a las asignadas; el backend rechaza el acceso a otra sede | RF-03, RF-06 |
| CA-03 | Tipos, destinos, responsables y zonas pueden configurarse e inactivarse conservando referencias históricas | RF-02, RF-04, RF-05, RF-08, RF-26 |
| CA-04 | Buscar un documento existente recupera la persona; dos altas concurrentes no duplican la clave de identificación definida | RF-07 |
| CA-05 | Persona visitante y usuario autenticado se administran de forma independiente | RF-06, RF-07 |
| CA-06 | Crear una autorización conserva persona, destino, modalidad, condiciones, creador y decisión atribuible | RF-09, RF-10, RF-20 |
| CA-07 | Una visita ocasional pendiente no ingresa cuando la política exige aprobación; un usuario sin permiso no puede aprobarla por API | RF-11, RF-13 |
| CA-08 | Una invitación permite recuperar la autorización por QR sin exponer datos personales directamente en el código | RF-12, RF-15 |
| CA-09 | Entradas con persona inactiva, autorización no vigente, sede/zona incorrecta o fuera de horario se deniegan con causa y sin abrir permanencia | RF-13, RF-15, RF-21 |
| CA-10 | Dos intentos simultáneos de consumir una autorización única producen una sola entrada confirmada | RF-10, RF-13 |
| CA-11 | Una autorización recurrente permite visitas distintas en sus ventanas válidas sin cerrarse tras la primera salida | RF-10; RN-09 |
| CA-12 | Reenviar la misma solicitud de entrada o salida no crea movimientos duplicados | RF-13, RF-14 |
| CA-13 | La salida cierra el ingreso correcto y calcula duración; una autorización revocada después del ingreso no impide esa salida según la regla propuesta | RF-14 |
| CA-14 | La vista de personas dentro coincide con permanencias abiertas; tras reconectar recupera estado persistido sin depender de eventos perdidos | RF-19 |
| CA-15 | El historial conserva autorizadores, responsables, duración y movimientos aun después de inactivar catálogos | RF-18, RF-20; RN-08 |
| CA-16 | Los escenarios de alerta de RF-21 se generan con su motivo y transitan Nueva → Revisada → Cerrada con seguimiento | RF-21 |
| CA-17 | Los eventos de correo usan plantillas del tenant y destinatarios configurados; una falla genera reintento sin duplicar la operación de acceso | RF-22 |
| CA-18 | El dashboard respeta sedes/permisos y distingue entradas del día, personas dentro y visitas pendientes | RF-23 |
| CA-19 | Excel/PDF reflejan filtros, ámbito y corte temporal; no se descarga una exportación de otro tenant | RF-24 |
| CA-20 | Las operaciones críticas registran actor, organización, instante, acción y objeto; los usuarios ordinarios no alteran la auditoría | RF-25 |
| CA-21 | Los flujos de buscar, solicitar, decidir, entrar y salir funcionan en computador, tablet y móvil con validaciones visibles | Requisitos no funcionales |
| CA-22 | Una restauración recupera datos coherentes y archivos vinculados según los objetivos acordados antes de producción | Requisitos no funcionales |

## 14.2 Alcance completo

| ID | Escenario verificable | Requisitos |
|---|---|---|
| CA-23 | Con vehículos habilitados se registra placa, responsable, autorización y movimientos vinculados; al deshabilitar no se crean nuevos movimientos del módulo | RF-16 |
| CA-24 | Una persona/autorización admite varios dispositivos y se conoce qué equipos tienen entrada sin salida | RF-17 |
| CA-25 | Cada movimiento de equipo identifica sede, zona/punto, responsable, visita y operador; el histórico sigue accesible con permiso | RF-17 |
| CA-26 | Cambiar la plantilla sectorial no modifica permisos existentes ni elimina personalizaciones o históricos; los catálogos quedan ajustables | RF-26 |

