SELECT CAST(e.id AS NUMBER(18)) AS Id, a.name AS TypeAggregate, e.aggregate_id AS AggregateId, t.name AS TypeEvent,
       JSON_SERIALIZE(e.payload RETURNING VARCHAR2(4000)) AS Payload,
       JSON_SERIALIZE(e.custom_attributes RETURNING VARCHAR2(4000)) AS CustomAttributes,
       e.occurred_at AS "DateTime"
FROM domain_event e JOIN type_registry a ON a.id = e.type_aggregate_id JOIN type_registry t ON t.id = e.type_event_id
ORDER BY e.id OFFSET :Offset ROWS FETCH NEXT :Limit ROWS ONLY
