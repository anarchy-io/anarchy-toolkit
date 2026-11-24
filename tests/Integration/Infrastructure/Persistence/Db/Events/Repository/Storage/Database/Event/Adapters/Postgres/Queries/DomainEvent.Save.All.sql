INSERT INTO domain_event (type_aggregate_id, aggregate_id, type_event_id, payload, custom_attributes, occurred_at)
SELECT e.type_aggregate_id, e.aggregate_id, e.type_event_id, e.payload::jsonb, e.custom_attributes::jsonb, e.occurred_at
FROM unnest(@TypeAggregateId, @AggregateId, @TypeEventId, @Payload, @CustomAttributes, @Moment)
    AS e(type_aggregate_id, aggregate_id, type_event_id, payload, custom_attributes, occurred_at)
