---
tipo: modulo
estado: implementado
fuentes: [PRD v0.1]
relacionados: [HU-001, HU-002, HU-003, HU-004, ADR-001, ADR-002, ADR-003, ADR-017, ADR-018, ADR-020, ADR-021]
actualizado: 2026-10-17
tags: [conecsia, modulo, f0]
---

# Plataforma base

Fundaciones técnicas de [[EP-01 Fundamentos técnicos]] (HU-001 a HU-004). Todavía no incluye funcionalidad de negocio. El estado de verificación está en [[Estado de implementación]].

## Backend (`backend/`)
- **Solución** `ConecsIA.slnx`, .NET 10 (SDK 10.0.402), análisis `latest-recommended` con advertencias como errores y gestión central de paquetes (`Directory.Packages.props`).
- **Proyectos:**
  - `ConecsIA.SharedKernel`: `IClock`, `IModulo`, `ReglaNegocioException` (422) y `RecursoNoEncontradoException` (404).
  - `ConecsIA.Infrastructure`: `ConecsIADbContext`, configuración Npgsql + `snake_case` y `RelojSistema`.
  - Siete módulos (`ConecsIA.Modules.*`): por ahora solo con su punto de entrada `IModulo`.
  - `ConecsIA.Migrations`: migración `Inicial` vacía y fábrica de diseño ([[ADR-021 DbContext compartido y ensamblado de migraciones]]).
  - `ConecsIA.Api`: host con controladores MVC, composición de módulos, Problem Details, validación FluentValidation, salud, OpenAPI, logs JSON redactados y OpenTelemetry.
- **Endpoints:**
  - `GET /api/v1/health`: vida, sin dependencias.
  - `GET /api/v1/health/ready`: comprueba PostgreSQL.
  - `GET /api/v1/sistema/version`: versión y módulos compuestos.
  - `GET /openapi/v1.json`: solo en Development.
- **Errores ([[ADR-020 Contratos API y formato de errores]]):**
  - 400 con errores por campo.
  - 404 tanto para recursos inexistentes como para rutas desconocidas.
  - 409 por concurrencia o violación de unicidad, con los códigos `CONFLICTO_CONCURRENCIA` y `REGISTRO_DUPLICADO`.
  - 422 con un código de motivo.
  - 500 sin detalle interno.
  - Todos llevan `traceId`, que también va en la cabecera `X-Trace-Id`.
- **Logs ([[ADR-018 Observabilidad sin datos sensibles]]):**
  - Formato JSON con `traceId` y `spanId`.
  - Se enmascaran contraseñas, tokens, Bearer, correos y secuencias de 6 o más dígitos. Esto también enmascara las IPv4, que tienen la misma forma que una cédula con puntos.
  - De las excepciones solo se registran el tipo y la pila.
- **Migraciones:**
  - En Development las aplica `AplicadorMigraciones`, usando el rol de migración.
  - La API se niega a arrancar si esa opción está activa en Production.
- **Semillas:** `--sembrar` está limitado a Development. Aún no hay conjuntos de datos porque no existen entidades.

## Base de datos (`database/init/01-roles.sh`)
- `conecsia_migrator`: dueño del esquema; ejecuta DDL.
- `conecsia_app`: solo `USAGE` sobre el esquema, más DML por privilegios por defecto. No puede crear ni borrar tablas.

## Frontend (`frontend/`)
- **Base:** Vite 8, React 19, TypeScript 6 en modo estricto con `noUncheckedIndexedAccess`, Tailwind 4, React Router 7, TanStack Query 5 y oxlint.
- **Shell responsivo:**
  - Navegación lateral en escritorio y menú en móvil.
  - Enlace para saltar al contenido.
  - Indicador de conexión con texto, no solo color.
  - Recuadro de organización/sede que por ahora muestra "Sin sesión iniciada".
- **Cliente HTTP:** `apiFetch` con Problem Details. Una falla de red nunca se presenta como éxito y las mutaciones no se reintentan.
- **Textos:** i18n por claves con vocabulario por organización sobrescribible (prepara HU-012).
- **Tipos de la API:** se generan del contrato OpenAPI con `npm run api:generate`.
- **Contenedor:** nginx con proxy `/api` hacia la API (mismo origen) y cabeceras de seguridad básicas.

## Entorno y CI
- **Docker Compose:** `postgres:18-alpine` (solo en 127.0.0.1; puerto de host configurable con `POSTGRES_HOST_PORT`), Mailpit, la API y la SPA. Las contraseñas vienen de `.env`.
- **GitHub Actions, backend:** formato, build, pruebas de arquitectura y unitarias, pruebas de integración y artefacto con el script idempotente de migraciones.
- **GitHub Actions, frontend:** lint, formato, tipos, pruebas y build.
- **Secretos:** escaneo con gitleaks.

## Fuera de alcance o pendiente
- Almacenamiento de objetos emulado: [[ADR-013 Almacenamiento privado de archivos]] sigue en Propuesta.
- E2E con Playwright en tres tamaños de pantalla (HU-002).
- Semillas con dos organizaciones (dependen de HU-009).
- Prueba 404 entre organizaciones (depende de HU-005).
