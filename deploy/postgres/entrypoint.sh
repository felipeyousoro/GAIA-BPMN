#!/bin/sh
set -eu

if [ ! -s "${PGDATA:-/var/lib/postgresql/data}/PG_VERSION" ]; then
  cp /migrations/ensure.sql /docker-entrypoint-initdb.d/001_ensure.sql
  exec docker-entrypoint.sh postgres
fi

docker-entrypoint.sh postgres &
pid=$!

shutdown() {
  kill -TERM "$pid" 2>/dev/null || true
  wait "$pid" 2>/dev/null || true
}
trap shutdown TERM INT

until pg_isready -U "$POSTGRES_USER" -d "$POSTGRES_DB"; do
  if ! kill -0 "$pid" 2>/dev/null; then
    wait "$pid"
    exit 1
  fi
  sleep 1
done

psql -v ON_ERROR_STOP=1 --username "$POSTGRES_USER" --dbname "$POSTGRES_DB" -f /migrations/ensure.sql

wait "$pid"
