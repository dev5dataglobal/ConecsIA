---
tipo: adr
estado: aprobado
area: backend
origen: PRD §12.1
fuentes: [PRD v0.1]
relacionados: [RF-01..RF-26]
actualizado: 2026-10-17
tags: [conecsia, adr, backend]
---

# ADR-001 Monolito modular por capacidades

> [!success] Estado: **Aprobado** el 2026-10-17 por el responsable del proyecto. Alcance y condiciones en "Aprobación".
> Área: backend · Origen: PRD §12.1

## Contexto
El PRD exige un núcleo común con módulos habilitables por organización (vehículos, equipos, correo…) y descarta microservicios para empezar.

## Decisión propuesta
- Una sola aplicación desplegable (monolito modular) organizada por capacidades: SaaS/organizaciones, identidad/permisos, estructura física, personas, autorizaciones, visitas/accesos, vehículos, dispositivos, alertas/notificaciones, consultas/reportes, auditoría.
- Cada módulo con capas Dominio / Aplicación / Infraestructura; una capa API común.
- Los casos de uso dependen de interfaces para correo, archivos, reloj y mensajería, nunca de proveedores concretos.
- Dependencias entre módulos solo a través de contratos públicos del módulo (sin acceso directo a tablas ajenas).

## Alternativas consideradas
- Microservicios: más costo operativo, transacciones distribuidas en el flujo de entrada; descartado por el PRD.
- Monolito en capas sin módulos: más simple al inicio, pero mezcla módulos opcionales con el núcleo.

## Consecuencias
- Transacciones locales para entrada/consumo/permanencia ([[ADR-007 Concurrencia e idempotencia en accesos]]).
- Requiere disciplina de fronteras: se propone añadir pruebas de arquitectura que verifiquen dependencias.

## Pendientes
Granularidad final de proyectos/assemblies; se fijará al crear la solución.


## Revisión 2026-10-17
- [x] Coherente con el PRD §12.1 y con los requisitos de módulos habilitables.
- [ ] **Ajuste propuesto:** el módulo de consultas/reportes puede *leer* datos de varios módulos mediante vistas o consultas de solo lectura; la regla "sin acceso a tablas ajenas" aplica a las escrituras.
- [ ] **Ajuste propuesto:** verificar las fronteras con pruebas de arquitectura automáticas en CI.
- Sin bloqueos de negocio. Lista para aprobar con los ajustes.

## Aprobación 2026-10-17
- Aprobado con los dos ajustes de la revisión: lecturas de solo lectura entre módulos para reportes y pruebas de arquitectura en CI.
