#!/bin/sh
# ADR-003 / ADR-014: dos roles separados.
#  - conecsia_migrator: dueño del esquema; aplica migraciones (DDL).
#  - conecsia_app: rol de la aplicación; solo DML, sin DDL. La auditoría le quitará además
#    UPDATE/DELETE sobre su tabla cuando exista (HU-008).
# Las contraseñas llegan por variables de entorno; nunca se versionan.
set -eu

psql -v ON_ERROR_STOP=1 --username "$POSTGRES_USER" --dbname "$POSTGRES_DB" \
  -v migrator_pwd="$CONECSIA_MIGRATOR_PASSWORD" \
  -v app_pwd="$CONECSIA_APP_PASSWORD" <<'SQL'
CREATE ROLE conecsia_migrator LOGIN PASSWORD :'migrator_pwd';
CREATE ROLE conecsia_app LOGIN PASSWORD :'app_pwd';

SELECT format('ALTER DATABASE %I OWNER TO conecsia_migrator', current_database()) \gexec
SELECT format('REVOKE ALL ON DATABASE %I FROM PUBLIC', current_database()) \gexec
SELECT format('GRANT CONNECT ON DATABASE %I TO conecsia_migrator, conecsia_app', current_database()) \gexec

ALTER SCHEMA public OWNER TO conecsia_migrator;
REVOKE ALL ON SCHEMA public FROM PUBLIC;
GRANT USAGE ON SCHEMA public TO conecsia_app;

ALTER DEFAULT PRIVILEGES FOR ROLE conecsia_migrator IN SCHEMA public
  GRANT SELECT, INSERT, UPDATE, DELETE ON TABLES TO conecsia_app;
ALTER DEFAULT PRIVILEGES FOR ROLE conecsia_migrator IN SCHEMA public
  GRANT USAGE, SELECT ON SEQUENCES TO conecsia_app;
SQL
