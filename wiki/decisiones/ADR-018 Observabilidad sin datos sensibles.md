---
tipo: adr
estado: aprobado
area: despliegue
origen: PRD §12.5, §12.7
fuentes: [PRD v0.1]
relacionados: [D-15]
actualizado: 2026-10-17
tags: [conecsia, adr, despliegue]
---

# ADR-018 Observabilidad sin datos sensibles

> [!success] Estado: **Aprobado** el 2026-10-17 por el responsable del proyecto. Alcance y condiciones en "Aprobación".
> Área: despliegue · Origen: PRD §12.5, §12.7

## Contexto
Se requiere diagnóstico sin exponer datos personales, contraseñas ni tokens.

## Decisión propuesta
- Logs estructurados con identificador de correlación por petición y por mensaje de fondo.
- Métricas y trazas con OpenTelemetry (exportador a definir).
- Lista de exclusión y enmascarado: sin contraseñas, tokens QR, documentos completos ni datos de contacto en logs.
- `organization_id` en logs como identificador técnico, no nombres.
- Medición p95 de búsqueda por documento, QR, validación, entrada y salida.

## Alternativas consideradas
Logs de texto libre: difíciles de filtrar y de sanear.

## Consecuencias
Revisión de logs dentro de la verificación de seguridad.

## Pendientes
Destino de logs y métricas (ligado a D-13); umbrales (D-15).


## Revisión 2026-10-17
- [x] Coherente con PRD §12.5 y §12.7.
- [ ] **Precisión:** logs JSON estructurados con el `ILogger` estándar más OpenTelemetry para métricas y trazas. El exportador se elige con D-13.
- [ ] **Precisión:** el enmascarado se prueba: un test verifica que documento, token QR, correo y teléfono no aparecen en los logs de entrada y salida.

## Aprobación 2026-10-17
- Aprobado: logs JSON con `ILogger`, OpenTelemetry para métricas y trazas, y prueba automática del enmascarado.
- El exportador queda pendiente (D-13).
