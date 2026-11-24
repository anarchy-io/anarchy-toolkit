#!/bin/sh
set -eu

mysql --protocol=TCP --host=127.0.0.1 --connect-timeout=3 \
  --user="${MYSQL_USER}" --password="${MYSQL_PASSWORD}" "${MYSQL_DATABASE}" \
  --execute='SELECT e.id FROM domain_event e CROSS JOIN it_outbox o WHERE 1 = 0' >/dev/null
