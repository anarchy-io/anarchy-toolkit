#!/bin/sh
set -eu

PGPASSWORD="${POSTGRES_PASSWORD}" psql --no-password --host=127.0.0.1 \
  --username="${POSTGRES_USER}" --dbname="${POSTGRES_DB}" \
  --command='SELECT e.id FROM domain_event e CROSS JOIN it_outbox o WHERE 1 = 0' >/dev/null
