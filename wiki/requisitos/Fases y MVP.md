---
tipo: requisito
estado: propuesta
fuentes: [PRD v0.1]
relacionados: [D-02]
actualizado: 2026-10-17
tags: [conecsia]
---

# Fases y MVP

> [!note] Propuesta de secuenciación; requiere validación del responsable de producto (D-02).

## 13.1 Secuencia

| Fase | Resultado verificable | Alcance |
|---|---|---|
| 0. Definiciones y base técnica | Decisiones críticas, proyectos, migraciones, autenticación, aislamiento y auditoría base | Fundamento |
| 1. Administración y personas | Organizaciones, tipo, sedes, destinos, zonas, roles, usuarios, tipos y personas | RF-01 a RF-08; plantilla básica RF-26 |
| 2. Autorizaciones y visitas | Modalidades, solicitudes, decisiones, revocación, invitación y QR | RF-09 a RF-12, RF-15 y RF-20 |
| 3. Operación de accesos | Validación, entrada/salida, concurrencia, historial y presencia actualizada | RF-13, RF-14, RF-18 y RF-19 |
| 4. Operación medible | Alertas, correo, dashboard y reportes núcleo Excel/PDF | RF-21 a RF-24; RF-25 transversal |
| 5. Vehículos y equipos | Configuración, autorizaciones, movimientos e históricos de activos | RF-16 y RF-17; reportes asociados |
| 6. Adaptación por vertical | Plantillas completas; especificar documentación de contratistas y límites de invitados | RF-26 completo según reglas validadas |
| 7. Preparación productiva | Seguridad, recuperación, carga, capacitación y aceptación | Requisitos no funcionales y cierre |

**MVP propuesto:** fases 0 a 4, con RF-25 desde el primer flujo. El piloto usará el núcleo común en organizaciones de prueba. Vehículos, equipos y capacidades sectoriales avanzadas siguen comprometidos en el alcance completo, pero quedan fuera de este recorte inicial propuesto. Si el primer cliente necesita RF-16/RF-17, se incorporarán antes de su piloto. La priorización requiere validación del responsable del producto.

## 13.2 Fuera del MVP propuesto

Aplicación móvil nativa; biometría/reconocimiento facial; apertura automática de torniquetes o barreras; funcionamiento offline con sincronización; integración con directorios/ERP; SMS, WhatsApp y push; facturación SaaS; IA; constructor de formularios; jerarquías sectoriales avanzadas todavía no especificadas.

La arquitectura deberá permitir incorporar integraciones futuras sin convertirlas en funcionalidades ya aprobadas. Los lectores QR que operen como teclado o cámara web se evaluarán dentro del flujo de recepción.

