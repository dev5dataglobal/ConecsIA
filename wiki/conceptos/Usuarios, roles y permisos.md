---
tipo: concepto
estado: base
fuentes: [PRD v0.1]
relacionados: [RF-06, RN-10, CA-02, CA-07, D-03]
actualizado: 2026-10-17
tags: [conecsia]
---

# Usuarios, roles y permisos

**Base.** Usuario = identidad autenticada con membresías, roles y permisos.

Perfiles iniciales: Superadministrador SaaS · Administrador de organización · Administrador de sede · Operador de acceso · Autorizador · Auditor.

- Roles y permisos **configurables**. Acceso efectivo = permiso + organización + sedes/destinos asignados.
- Nunca por nombre de rol ni ocultando controles en el frontend; el backend decide.
- Identidad, sesión, múltiples membresías, SSO y MFA: pendiente (D-03).
- Matriz de permisos por rol y alcance: entregable de fase 0 ([[Fases y MVP]]).
