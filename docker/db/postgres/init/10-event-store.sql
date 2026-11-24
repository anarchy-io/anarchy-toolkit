-- language=postgresql

CREATE TABLE type_registry (
    id SERIAL PRIMARY KEY,
    name VARCHAR(200) NOT NULL UNIQUE
);

CREATE TABLE test_order (
    id BIGINT PRIMARY KEY,
    sku VARCHAR(100) NOT NULL
);

CREATE TABLE domain_event (
    id BIGSERIAL PRIMARY KEY,
    type_aggregate_id INTEGER NOT NULL REFERENCES type_registry(id),
    aggregate_id VARCHAR(100) NOT NULL,
    type_event_id INTEGER NOT NULL REFERENCES type_registry(id),
    payload JSONB,
    custom_attributes JSONB,
    occurred_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    UNIQUE (type_aggregate_id, aggregate_id, type_event_id)
);

CREATE TABLE it_order (id INTEGER PRIMARY KEY);
CREATE TABLE it_stock (id INTEGER PRIMARY KEY);
CREATE TABLE it_event (id INTEGER PRIMARY KEY);

CREATE TABLE it_outbox (
    id INTEGER PRIMARY KEY,
    published INTEGER DEFAULT 0 NOT NULL,
    age_days INTEGER DEFAULT 0 NOT NULL
);
