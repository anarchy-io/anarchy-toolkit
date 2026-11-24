SELECT e.id AS Id, a.name AS TypeAggregate, e.aggregate_id AS AggregateId, t.name AS TypeEvent,
       e.payload AS Payload, e.custom_attributes AS CustomAttributes, e.occurred_at AS `DateTime`
FROM domain_event e JOIN type_registry a ON a.id = e.type_aggregate_id JOIN type_registry t ON t.id = e.type_event_id
ORDER BY e.id LIMIT @Limit OFFSET @Offset
