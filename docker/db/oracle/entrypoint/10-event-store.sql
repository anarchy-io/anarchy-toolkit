WHENEVER OSERROR EXIT FAILURE
WHENEVER SQLERROR EXIT FAILURE

-- The image runs this hook as SYS after creating APP_USER in FREEPDB1.
ALTER SESSION SET CONTAINER = FREEPDB1;
ALTER SESSION SET CURRENT_SCHEMA = anarchy_test;

@/opt/anarchy/db/oracle/init/10-event-store.sql
