-- language=mysql

CREATE TABLE type_registry (
    id INTEGER AUTO_INCREMENT PRIMARY KEY,
    name VARCHAR(200) NOT NULL UNIQUE
);

CREATE TABLE test_order (
    id BIGINT PRIMARY KEY,
    sku VARCHAR(100) NOT NULL
);

CREATE TABLE domain_event (
    id BIGINT AUTO_INCREMENT PRIMARY KEY,
    type_aggregate_id INTEGER NOT NULL,
    aggregate_id VARCHAR(100) NOT NULL,
    type_event_id INTEGER NOT NULL,
    payload JSON,
    custom_attributes JSON,
    occurred_at DATETIME(6) NOT NULL,
    FOREIGN KEY (type_aggregate_id) REFERENCES type_registry(id),
    FOREIGN KEY (type_event_id) REFERENCES type_registry(id),
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
