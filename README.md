# ConesIA – DG

Plataforma SaaS multi-organización de gestión de visitantes y control de accesos.
Requisitos en [`PRD.md`](PRD.md). La documentación viva del proyecto (decisiones, backlog y estado) está en la bóveda Obsidian [`wiki/`](wiki/index.md).

> Estado: fundaciones técnicas (F0) y **primera vertical funcional: gestión de sedes (HU-013)** con aislamiento por organización, contexto por membresía, permisos y auditoría. Autenticación real con cookie y anti-CSRF (HU-006, ADR-006).

## Estructura

| Carpeta | Contenido |
|---|---|
| `backend/` | API .NET 10: monolito modular (ADR-001), controladores MVC y FluentValidation (ADR-002), EF Core + Npgsql (ADR-003) |
| `backend/src/Modules/` | Un proyecto por módulo: Organizaciones, Identidad, Estructura, Personas, Autorizaciones, Accesos, Auditoria |
| `backend/src/ConecsIA.Migrations/` | Migraciones EF Core (único ensamblado que conoce a todos los módulos) |
| `backend/tests/` | Pruebas de arquitectura, unitarias e integración (Testcontainers + PostgreSQL real) |
| `backend/openapi/` | Contrato OpenAPI exportado; el frontend genera sus tipos a partir de él |
| `frontend/` | SPA React + TypeScript + Tailwind (Vite) |
| `database/init/` | Script de roles de PostgreSQL: `conecsia_migrator` (DDL) y `conecsia_app` (solo DML) |
| `wiki/` | Bóveda Obsidian mantenida por el agente |

## Requisitos locales

- .NET SDK 10 (fijado en `backend/global.json`)
- Node.js 22
- Docker Desktop

## Arranque con Docker Compose

```bash
cp .env.example .env
```

Edita `.env` con contraseñas locales. El archivo nunca se versiona. Luego:

```bash
docker compose up --build
```

| Servicio | URL |
|---|---|
| SPA | http://localhost:8080 |
| API | http://localhost:5080/api/v1/health/ready |
| Captura de correo (Mailpit) | http://localhost:8025 |
| PostgreSQL | localhost:5432, o el puerto de `POSTGRES_HOST_PORT` en `.env` (solo en 127.0.0.1) |

En `Development` la API aplica las migraciones al iniciar usando el rol de migración. En producción esto está prohibido: las aplica el pipeline con un script idempotente revisado (ADR-003).

## Desarrollo sin contenedores para la API

Con PostgreSQL del compose levantado (`docker compose up postgres`):

```bash
cd backend
dotnet user-secrets set "ConnectionStrings:ConecsIA" "Host=localhost;Database=conecsia;Username=conecsia_app;Password=<app>" --project src/ConecsIA.Api
dotnet user-secrets set "ConnectionStrings:ConecsIAMigraciones" "Host=localhost;Database=conecsia;Username=conecsia_migrator;Password=<migrador>" --project src/ConecsIA.Api
dotnet run --project src/ConecsIA.Api
```

```bash
cd frontend
npm install
npm run dev
```

Vite (http://localhost:5173) redirige `/api` a la API en el puerto 5080.

## Verificación

```bash
cd backend
dotnet format --verify-no-changes
dotnet build
dotnet test
```

`dotnet test` incluye las pruebas de integración y necesita Docker. Las pruebas de arquitectura fallan si un módulo referencia a otro, si el dominio depende de frameworks, si un módulo lee el reloj del sistema o si un controlador no sigue las convenciones de ADR-002/ADR-020.

```bash
cd frontend
npm run lint
npm run format:check
npm run typecheck
npm test
npm run build
```

## Migraciones

```bash
cd backend
dotnet tool restore
dotnet ef migrations add <Nombre> -p src/ConecsIA.Migrations -s src/ConecsIA.Migrations -o Migraciones
dotnet ef migrations script --idempotent -p src/ConecsIA.Migrations -s src/ConecsIA.Migrations -o artifacts/migraciones.sql
```

## Contrato API y tipos del frontend

Con la API en Development, exporta `http://localhost:5080/openapi/v1.json` a `backend/openapi/ConecsIA.Api.json`. Luego:

```bash
cd frontend
npm run api:generate
```

## Datos sintéticos

`dotnet run --project src/ConecsIA.Api -- --sembrar` solo funciona en `Development`. Con Docker Compose se siembran al iniciar (`Semillas__SembrarAlIniciar`). Nunca se usan datos reales hasta definir la privacidad (D-14).

Conjunto actual (identificadores fijos, idempotente): dos organizaciones (Los Pinos y Andes), tres sedes y tres usuarios: administradora de Los Pinos, operador limitado a la sede «Portería Norte» con solo consulta, y administrador de Andes.

## Autenticación

El inicio de sesión es real: correo y contraseña, con una cookie `HttpOnly` y protección anti-CSRF (HU-006, ADR-006). Con Docker Compose, los usuarios sintéticos usan la contraseña de `SEMILLAS_CONTRASENA_USUARIOS` (`.env`): `admin.pinos@ejemplo.test`, `operador.pinos@ejemplo.test` y `admin.andes@ejemplo.test`.

En `Development` y en las pruebas sigue disponible la cabecera `X-Usuario-Desarrollo` (ADR-022), y la pantalla `/ingresar` muestra un selector de usuario sintético. Si `Autenticacion:Desarrollo:Habilitada=true` está activo fuera de `Development`/`Testing`, la API **no arranca**.

La organización, los permisos y el alcance por sede se derivan siempre en el servidor de la membresía (HU-007, ADR-005). `X-Organizacion-Id` solo selecciona entre membresías propias; una organización ajena responde 403 y queda auditada.
