#!/bin/sh
set -eu

healthcheck.sh FREEPDB1
printf '%s\n' \
  'WHENEVER OSERROR EXIT FAILURE' \
  'WHENEVER SQLERROR EXIT FAILURE' \
  'SELECT e.id FROM domain_event e CROSS JOIN it_outbox o WHERE 1 = 0;' \
  'EXIT SUCCESS' \
  | sqlplus -L -s "${APP_USER}/${APP_USER_PASSWORD}@//127.0.0.1:1521/FREEPDB1" >/dev/null
