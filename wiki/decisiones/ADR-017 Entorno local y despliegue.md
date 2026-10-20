---
tipo: adr
estado: aprobado
area: despliegue
origen: PRD §12.1, §15
fuentes: [PRD v0.1]
relacionados: [D-13, D-14, D-15]
actualizado: 2026-10-17
tags: [conecsia, adr, despliegue]
---

# ADR-017 Entorno local y despliegue

> [!success] Estado: **Aprobado** el 2026-10-17 por el responsable del proyecto. Alcance y condiciones en "Aprobación".
> Área: despliegue · Origen: PRD §12.1, §15

## Contexto
El PRD propone contenedores para PostgreSQL y servicios auxiliares en local; la infraestructura productiva está pendiente.

## Decisión propuesta
- Local: Docker Compose con PostgreSQL (volumen persistente), servicio de captura de correo y almacenamiento de objetos emulado.
- La API y el frontend se publican como imágenes de contenedor reproducibles.
- Configuración por variables de entorno; secretos fuera del repositorio (user-secrets en local, gestor de secretos en entornos).
- Pipeline de CI: compilación, pruebas (incluidas las de integración con PostgreSQL) y migraciones revisadas.
- Entornos mínimos: local, pruebas/staging y producción.

## Alternativas consideradas
Instalación nativa sin contenedores: menos reproducible.

## Consecuencias
Con varias instancias: backplane SignalR y bloqueo de tareas programadas.

## Pendientes
Nube/proveedor, respaldos, RPO/RTO, disponibilidad (D-13, D-14, D-15).


## Revisión 2026-10-17
- [x] Entorno local con Docker Compose, imágenes reproducibles, secretos fuera del repositorio y CI con pruebas de integración: listos para aprobar.
- [ ] **Alcance de la aprobación:** **solo local y CI**. Nube, entornos productivos, respaldos y disponibilidad siguen pendientes (D-13, D-14, D-15).
- [ ] **Por decidir:** plataforma de CI y de alojamiento del repositorio. Hoy la carpeta no es un repositorio git.

## Aprobación 2026-10-17
- Aprobado **solo para local y CI**: Docker Compose, imágenes reproducibles y secretos fuera del repositorio.
- **Repositorio en GitHub con GitHub Actions** (decisión del usuario).
- Infraestructura productiva pendiente (D-13, D-14, D-15).
