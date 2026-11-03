---
tipo: concepto
estado: implementado
fuentes: [Solicitud del usuario 2026-10-22, Solicitud del usuario 2026-10-24]
relacionados: [Organización, Sede, ADR-024, D-25, D-26, D-27]
actualizado: 2026-10-31
tags: [conecsia, concepto]
---

# Zona empresarial

> [!warning] Contradicción / cambio 2026-10-31 — [[ADR-028 Zona empresarial por sede y retiro de zonas de acceso]]
> Antes (2026-10-24): una organización superior administra la zona y las empresas se vinculan como organizaciones (`zona_empresarial_organizacion`); una empresa en a lo sumo una zona.
> Ahora (solicitud del usuario 2026-10-31, **Implementado**): la zona no se relaciona con organizaciones. Cada [[Sede]] tiene cero o una zona; una organización puede tener sedes en varias zonas. Áreas y puntos comunes de la zona los administra la organización dueña de la sede. La zona es un catálogo de plataforma (solo el administrador de plataforma la gestiona); las demás organizaciones ven solo sus propias sedes dentro de la zona. Datos ambiguos de la migración: [[Decisiones pendientes]] D-48. El contenido siguiente se conserva como historia.

> [!note] No está en el PRD v0.1. Surgió de solicitudes del usuario (2026-10-22 alcance de autorizaciones, 2026-10-24 organizaciones superiores).

Agrupación de empresas (organizaciones) dentro de un parque, zona franca o edificio compartido.

- **Organización superior** (**Implementado**): la [[Organización]] que administra la zona. Es una organización normal (con sus usuarios y perfiles), enlazada en `zona_empresarial_organizacion` con `es_administradora = true`. Hay una sola activa por zona (índice único parcial).
- **Empresas** (**Implementado**): organizaciones enlazadas con `es_administradora = false`. Cada empresa pertenece como máximo a **una** zona activa (índice único parcial `UX_zona_empresarial_organizacion_empresa`). Retirar una empresa desactiva el vínculo, no lo borra.
- **Sin anidamiento** (**Implementado**, regla conservadora): una organización superior no puede ser empresa de ninguna zona → [[Decisiones pendientes|D-26]].
- **Acceso compartido** (**Base de la solicitud 2026-10-22**): la zona administra los accesos de sus áreas comunes (sedes con `id_zona_empresarial`, creadas por la organización superior); cada empresa conserva la administración de sus propios accesos. Los datos de una empresa no se mezclan con los de otra: los vínculos solo exponen nombre, identificación y estado.
- **Visibilidad** (**Implementado**): el administrador de plataforma ve todas las zonas con sus empresas. La organización superior ve sus empresas. Una empresa ve su zona, pero no las demás empresas → [[Decisiones pendientes|D-27]].
