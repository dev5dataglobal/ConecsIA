# Ejecutar ConecsIA con Docker

Requisitos: Docker Desktop (o Docker Engine) con Docker Compose v2.

## Servicios

| Servicio | Qué es | URL / puerto en el host | Notas |
|---|---|---|---|
| `postgres` | PostgreSQL 18 | `127.0.0.1:${POSTGRES_HOST_PORT}` (5433 por defecto) | Datos en el volumen `postgres-datos` |
| `migraciones` | Aplica las migraciones EF Core pendientes y termina | — | Nunca borra datos; la API espera a que termine bien |
| `mailpit` | Captura local de correo | http://localhost:8025 (SMTP 1025) | Los correos de la API llegan aquí |
| `api` | API ASP.NET Core (.NET 10) | http://localhost:${API_HOST_PORT} (5080) · Swagger en `/swagger` | Archivos subidos en el volumen `api-archivos` |
| `web` | SPA React servida por nginx | http://localhost:${WEB_HOST_PORT} (8080) | nginx reenvía `/api/` a `api:8080` |

Los servicios hablan entre sí por la red interna de Compose usando el nombre del servicio
(`postgres`, `api`, `mailpit`). El navegador solo usa `web`; la SPA llama a `/api` en el mismo origen
y nginx la reenvía a la API. Los puertos solo se publican en `127.0.0.1`.

## Variables (`.env`)

Copia `.env.example` a `.env` y cambia los valores. `.env` no se versiona.

| Variable | Uso | Ejemplo |
|---|---|---|
| `POSTGRES_DB` / `POSTGRES_USER` | Base y usuario del contenedor | `conecsia` |
| `POSTGRES_PASSWORD` | Contraseña de PostgreSQL (obligatoria) | `cambia-esto-postgres` |
| `POSTGRES_HOST_PORT` | Puerto del host para PostgreSQL | `5433` |
| `JWT_KEY` | Clave JWT, mínimo 32 caracteres (obligatoria) | — |
| `JWT_ISSUER` / `JWT_AUDIENCE` | Emisor y audiencia del token | `ConecsIA` / `ConecsIA.Client` |
| `JWT_DURATION_MINUTES` | Vida del token (1–120) | `60` |
| `API_HOST_PORT` / `WEB_HOST_PORT` | Puertos del host | `5080` / `8080` |

La contraseña de PostgreSQL solo se aplica al **crear** el volumen; cambiarla después no modifica una base existente.

> [!NOTE]
> El volumen `conecsia_postgres-data` pertenece a la integración Docker anterior (otro esquema y otros usuarios).
> Este compose usa `postgres-datos` y no lo toca. Si ya no lo necesitas, puedes borrarlo tú a mano.

## Comandos

```bash
cp .env.example .env          # una sola vez, luego edita los valores
docker compose up -d --build  # construir e iniciar
docker compose ps             # estado
docker compose logs -f api    # registros (api, web, postgres, migraciones, mailpit)
docker compose stop           # detener sin borrar contenedores
docker compose down           # detener y quitar contenedores; los volúmenes (datos) se conservan
```

> [!WARNING]
> `docker compose down -v` borra los volúmenes: la base de datos y los archivos subidos.

Las semillas de verificación (`backend/scripts/*.sql`) no se ejecutan solas. Para cargarlas a mano:

```bash
docker compose exec -T postgres psql -U conecsia -d conecsia < backend/scripts/seed-permisos-perfil-1.sql
```

## Copiar la base local a Docker

La base de Docker nace vacía. Para usar los usuarios y datos del PostgreSQL local (el de `dotnet run`),
cópialos. **Reemplaza todo el contenido de la base de Docker**; la base local solo se lee.

```bash
export MSYS_NO_PATHCONV=1   # solo en Git Bash
docker run --rm -e PGPASSWORD='<contraseña local>' postgres:18-alpine pg_dump -h host.docker.internal -U postgres -d conecsia -Fc --no-owner --no-privileges > conecsia-local.dump
docker compose stop api web
docker compose exec -T postgres psql -U conecsia -d conecsia -c "drop schema public cascade; create schema public;"
docker compose exec -T postgres pg_restore -U conecsia -d conecsia --no-owner --no-privileges < conecsia-local.dump
docker compose up -d        # `migraciones` aplica lo que le falte a la copia
```

Borra `conecsia-local.dump` al terminar: contiene datos personales.

## Flujo de desarrollo sin Docker

Sigue igual: `dotnet run` en `backend/ApiConecsia` (puerto 5191, con `appsettings.Local.json`) y
`npm run dev` en `frontend/` (puerto 5173, proxy a `API_PROXY_TARGET`). Con PostgreSQL de Docker,
apunta la cadena de conexión local a `Host=localhost;Port=5433;...`.
