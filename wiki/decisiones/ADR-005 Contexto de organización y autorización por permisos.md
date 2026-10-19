---
tipo: adr
estado: aprobado
area: seguridad
origen: PRD §5.2
fuentes: [PRD v0.1]
relacionados: [RN-10, CA-02, CA-07]
actualizado: 2026-10-17
tags: [conecsia, adr, seguridad]
---

# ADR-005 Contexto de organización y autorización por permisos

> [!success] Estado: **Aprobado** el 2026-10-17 por el responsable del proyecto. Alcance y condiciones en "Aprobación".
> Área: seguridad · Origen: PRD §5.2

## Contexto
El acceso efectivo depende de permiso + organización + sedes/destinos asignados. No se puede confiar en el `organizationId` del navegador ni autorizar por nombre de rol.

## Decisión propuesta
- El contexto de organización se deriva en servidor de una membresía activa del usuario autenticado; si el cliente indica una organización/sede, solo se usa para **seleccionar** entre membresías verificadas.
- Permisos finos (consultar, crear, modificar, inactivar, decidir, revocar, registrar entrada/salida, exportar, administrar) agrupados en roles configurables por organización; los seis perfiles del PRD son plantillas.
- Alcance territorial (sedes, destinos, puntos de acceso) evaluado en cada operación además del permiso.
- Implementación con políticas/handlers de autorización de ASP.NET Core; el código comprueba permisos, nunca nombres de rol.
- El superadministrador SaaS no tiene acceso ordinario a datos personales de clientes; el soporte requiere alcance explícito, temporal y auditado.

## Alternativas consideradas
Roles fijos en código: más simple, incumple configurabilidad (RN-10).

## Consecuencias
Matriz de permisos por rol y alcance como entregable de fase 0. Pruebas positivas y negativas por permiso.

## Pendientes
Matriz de quién cancela/revoca/corrige (D-04); multi-membresía (D-03).


## Revisión 2026-10-17
- [x] Contexto derivado en servidor, permisos finos, roles configurables y alcance territorial: listos para aprobar.
- [ ] **Alcance de la aprobación:** quedan **fuera** la multi-membresía (D-03) y la matriz de quién decide, revoca o cancela (D-04). El mecanismo se aprueba; esas políticas de negocio no.
- [ ] **Precisión:** el acceso de soporte del superadministrador necesita su propia historia; hoy está "por redactar" en [[tablero-scrum]].

## Aprobación 2026-10-17
- Aprobado el mecanismo: contexto derivado de la membresía, permisos finos, roles configurables y alcance territorial.
- **Fuera del alcance:** la multi-membresía (D-03) y la matriz de quién decide, revoca o cancela (D-04).
