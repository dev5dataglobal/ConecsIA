---
tipo: adr
estado: propuesta
area: seguridad
origen: PRD §8.3
fuentes: [PRD v0.1]
relacionados: [CA-01, D-13, D-14]
actualizado: 2026-10-17
tags: [conecsia, adr, seguridad]
---

# ADR-013 Almacenamiento privado de archivos

> [!warning] Estado: **Propuesta** — no aprobada. No implementar como decisión firme hasta su aprobación.
> Área: seguridad · Origen: PRD §8.3

## Contexto
Fotos y documentos son datos personales; no se usan URLs públicas.

## Decisión propuesta
- Binarios en almacenamiento de objetos privado detrás de una interfaz `IFileStorage`; PostgreSQL guarda metadatos (organización, dueño, tipo, tamaño, hash).
- Rutas/claves con prefijo de organización.
- Descargas solo a través de la API con verificación de organización y permiso, o URLs firmadas de vida corta generadas tras esa verificación.
- Validación de tipo y tamaño en servidor.
- En local: sistema de archivos o emulador compatible S3.

## Alternativas consideradas
Binarios en PostgreSQL (`bytea`): simple, pero engorda respaldos y la base.

## Consecuencias
Respaldos deben cubrir base y archivos de forma coherente (CA-22).

## Pendientes
Proveedor, tipos, tamaños, antivirus/controles de contenido y retención (D-13, D-14).

