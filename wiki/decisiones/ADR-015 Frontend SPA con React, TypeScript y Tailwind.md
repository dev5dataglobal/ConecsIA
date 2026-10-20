---
tipo: adr
estado: propuesta
area: frontend
origen: PRD §9.2
fuentes: [PRD v0.1]
relacionados: [CA-21, D-01]
actualizado: 2026-10-17
tags: [conecsia, adr, frontend]
---

# ADR-015 Frontend SPA con React, TypeScript y Tailwind

> [!warning] Estado: **Propuesta** — no aprobada. No implementar como decisión firme hasta su aprobación.
> Área: frontend · Origen: PRD §9.2

## Contexto
React, TypeScript y Tailwind están confirmados; la interfaz es responsiva y el servidor es la fuente de verdad.

## Decisión propuesta
- SPA en React + TypeScript (modo estricto) con Tailwind; herramienta de build moderna (p. ej. Vite) a confirmar en fase 0.
- Organización por funcionalidades alineadas con los módulos del backend.
- Librería de estado de servidor con caché e invalidación (p. ej. TanStack Query) y cliente API tipado generado desde OpenAPI.
- Selector visible y persistente de organización y sede operativa; toda petición opera en ese contexto, siempre verificado por el backend.
- Ocultar controles es solo usabilidad, nunca seguridad.
- Bloqueo de doble envío en la UI, con clave de idempotencia generada por intento.
- Vocabulario sectorial configurable mediante un diccionario de etiquetas por organización (preparado para i18n).
- Accesibilidad: teclado, contraste y mensajes que no dependan solo del color.

## Alternativas consideradas
Framework con SSR (Next.js): no requerido; añade un servidor Node.

## Consecuencias
El modelo de sesión ([[ADR-006 Identidad y sesiones]]) define si la SPA se sirve en el mismo origen que la API.

## Pendientes
Lectores QR por cámara o teclado (D-13); terminología (D-01).

