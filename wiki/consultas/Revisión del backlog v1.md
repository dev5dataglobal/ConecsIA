---
tipo: consulta
estado: vigente
fuentes: [PRD v0.1]
relacionados: [PLAN-DE-TRABAJO, tablero-scrum, Registro de ADR, Decisiones pendientes]
actualizado: 2026-10-17
tags: [conecsia, backlog, revision]
---

# Revisión del backlog v1

Hallazgos al convertir el [[PRD v0.1]] en 19 épicas y 56 historias (2026-10-17). Hay que resolverlos durante el refinamiento.

## Nuevas decisiones registradas
Se añadieron a [[Decisiones pendientes]]:
- **D-17:** normalización y unicidad de placa y serial (HU-047 a HU-050).
- **D-18:** política de la modalidad Permanente. Hasta decidirla, la modalidad queda **desactivada por defecto** (HU-024).
- **D-19:** umbrales de alertas y definición de "persona sin salida". Antes estaban asociados por error a D-12, que trata del correo (HU-040, HU-044).

## Bloqueos parciales no marcados en el catálogo original
- **D-03:** HU-006, HU-007 (selección cuando hay varias membresías) y HU-016 (aceptación de invitación).
- **D-05:** HU-033. El consumo atómico depende de los cupos, además de D-07.
- **D-09:** clave documental definitiva de HU-019. Hasta decidirla se rechazan las personas sin documento estándar.
- **D-10 y D-17:** HU-048 y HU-050.
- **D-04:** HU-039, la bandeja del autorizador.
- **ADR-019:** HU-015 requiere aprobarlo antes del modelo físico.
- **ADR-020:** HU-004. Hay que fijar la respuesta de una entrada denegada (200 o 422) antes de generar el cliente.

## Ajustes de trazabilidad
- **CA-07:** pertenece a la visita ocasional (HU-028 y HU-032). HU-025 solo usa su parte de API.
- **CA-11:** se añadió a HU-024, HU-027 y HU-029.
- **CA-13:** HU-026 lo usa para la regla propuesta de que una revocación no impide la salida. Ningún ADR la recoge todavía; conviene añadirla a [[ADR-009 Ciclos de estado separados y vigencia calculada]].
- **CA genérico para módulos:** no existe. HU-011 se apoya en CA-23.
- **HU-039:** funcionalmente pertenece a F2. Se propone moverla a EP-07 o EP-08.
- **HU-053, HU-054 y HU-056:** no tienen RF; se referencian por secciones del PRD.

## Reglas marcadas como Propuesta en las historias (validar con el PO)
- No permitir quitar el último administrador de una organización.
- Un administrador no puede asignar permisos que él no tiene.
- Rechazar el cambio de zona horaria de una sede con autorizaciones vigentes.
- Rechazar la inactivación de una sede con permanencias abiertas.
- Inactivar una persona con permanencia abierta todavía permite registrar su salida.
- Hasta decidir D-16, una organización inactiva no admite operaciones nuevas.
- Conservar la consulta histórica de un módulo deshabilitado. Es Propuesta en el PRD §5.3; su duración depende de D-14 y D-16.
- Tablas o endpoints nuevos que no están en el PRD: `inconsistencia_acceso`, `alerta_seguimiento` y `GET /api/v1/mis-puntos-acceso`.

## Huecos
- El acceso de soporte del superadministrador (PRD §5.2.6) no tiene historia. Está en "Por redactar" del [[tablero-scrum]].
- [[ADR-007 Concurrencia e idempotencia en accesos]] no define cómo identificar la permanencia a cerrar si D-07 permitiera presencia simultánea en varias sedes.
