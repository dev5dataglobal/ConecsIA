---
tipo: decision
estado: pendiente
fuentes: [PRD v0.1]
relacionados: [D-01..D-16]
actualizado: 2026-10-19
tags: [conecsia]
---

# Decisiones pendientes

Al resolver una: marcarla **Aprobada (ADR-xxx)**, crear el ADR en esta carpeta y actualizar páginas dependientes. **No inventar políticas.**


| ID | Definición pendiente | Momento de cierre |
|---|---|---|
| D-01 | Confirmar nombre comercial y términos internos Sede/Área; etiquetas por vertical | Antes de consolidar UI y contratos |
| D-02 | Aprobar MVP y necesidad de vehículos/equipos en el primer cliente | Antes de planificar entregas |
| D-03 | ~~Elegir identidad, sesión, múltiples membresías, invitación, SSO y MFA~~ — **Aprobada 2026-10-19 ([[ADR-006 Identidad y sesiones]], opción A)**; MFA en Fase 3, SSO diferido | Antes de autenticación y producción según alcance |
| D-04 | Definir cuándo se exige autorización, quién decide/revoca/cancela y destinos de cada autorizador | Antes del flujo de autorización |
| D-05 | Definir reingresos, cupos por modalidad y modificación de autorizaciones aprobadas | Antes de validar entradas |
| D-06 | Fijar zona horaria por sede, horarios nocturnos, límites inclusivos/exclusivos y festivos | Antes de reglas de calendario |
| D-07 | Confirmar duplicados de presencia por sede y simultaneidad entre sedes | Antes de restricciones transaccionales |
| D-08 | Definir regularización de entradas/salidas omitidas, permisos y evidencia | Antes del piloto operativo |
| D-09 | Definir identificación documental, países emisores y atención de personas sin documento estándar | Antes de modelo de persona |
| D-10 | Precisar custodia y salida de equipos, cambios de responsable y movimientos vehiculares | Antes de RF-16/RF-17 |
| D-11 | Especificar documentación de contratistas, jerarquías residenciales y límites de invitados | Antes de adaptación sectorial completa |
| D-12 | Seleccionar correo, remitentes, destinatarios, umbrales y política de reintentos | Antes de notificaciones/alertas |
| D-13 | Elegir infraestructura, archivos privados, formatos, tamaños y lectores compatibles | Antes de integrar servicios |
| D-14 | Definir privacidad, retención, eliminación, respaldos, RPO/RTO y responsables operativos — *parcial 2026-10-19: los eventos de inicio de sesión siguen el plazo general de auditoría, aún por definir* | Antes de datos reales/producción |
| D-15 | Acordar volumen, concurrencia, p95, frescura de pantalla y disponibilidad | Antes de pruebas de carga/aceptación |
| D-16 | Confirmar plan SaaS, módulos y efectos operativos de inactivar una organización | Antes de activar clientes |
| D-17 | Normalizar placa y serial y definir su ámbito de unicidad (vehículos y equipos) — *nueva 2026-10-17, surgió del backlog* | Antes de RF-16/RF-17 |
| D-18 | Política de la modalidad Permanente: quién la permite y bajo qué condiciones — *nueva 2026-10-17* | Antes de HU-024 |
| D-19 | Umbrales de alertas (próximo vencimiento, permanencia prolongada) y definición de "persona sin salida" — *nueva 2026-10-17; el backlog los asociaba a D-12* | Antes de HU-040/HU-044 |
| D-20 | ~~Convención de idioma en rutas y contratos de la API~~ — **Aprobada 2026-10-19: opción A, español con excepciones técnicas** ([[ADR-020 Contratos API y formato de errores]] § Convención de idioma; análisis en [[Opciones para decidir D-20]]) | Antes de la segunda vertical |
| D-21 | Límite de intentos de inicio de sesión por IP (aprobado en D-03d sin valor): intentos por ventana de tiempo y proxies de confianza para obtener la IP real detrás de nginx — *nueva 2026-10-19* | Antes de exponer el inicio de sesión fuera de la red local |
| D-22 | Adoptar o descartar RLS de PostgreSQL como segunda defensa de aislamiento — *nueva 2026-10-19; el spike documental de HU-005 propone no adoptarla en el MVP* ([[ADR-004 Tenancy con base compartida y organization_id]] § Spike RLS) | Antes de producción o del primer SQL crudo (reportes y exportaciones) |

**Riesgos principales:** alcance sectorial sin detalle, confusión entre visita y autorización, duplicación por concurrencia, filtros de tenant incompletos, horarios ambiguos, presencia desactualizada, fallos de correo y retención indefinida de datos personales. Cada riesgo se aborda con una decisión identificada, controles en servidor y criterios de aceptación; no se declara resuelto hasta verificarlos.
