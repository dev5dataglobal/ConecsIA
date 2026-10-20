---
tipo: arquitectura
estado: base
fuentes: [PRD v0.1]
relacionados: [RF-25, CA-20, D-14]
actualizado: 2026-10-17
tags: [conecsia]
---

# Seguridad y auditoría

- Aislamiento total por organización → [[Multi-tenancy y aislamiento]].
- Las operaciones críticas auditan organización, actor, instante, acción, objeto, resultado y valores anterior/nuevo (CA-20). Los usuarios ordinarios no alteran la auditoría.
- Concurrencia e idempotencia en entradas/salidas y en el consumo de autorizaciones únicas (CA-10, CA-12) → [[Movimiento]].
- Secretos con mecanismos estándar; sin contraseñas, tokens ni datos personales innecesarios en logs.
- La inactivación preserva la trazabilidad (RN-08).
- Privacidad, retención, respaldos, RPO/RTO: pendiente (D-14).
