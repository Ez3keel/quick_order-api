-- One Postgres container, four databases — each service still owns its own database
-- (nothing crosses schemas, no service reads another's tables), this just shares the
-- server process instead of running four separate Postgres containers, which would be
-- wasteful for a local/demo compose stack. A real deployment would likely give each
-- service its own managed instance; this is a docker-compose-scale simplification.
CREATE DATABASE quickorder_catalog;
CREATE DATABASE quickorder_ordering;
CREATE DATABASE quickorder_delivery;
CREATE DATABASE quickorder_identity;
