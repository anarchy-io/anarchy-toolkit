INSERT INTO domain_event (type_aggregate_id, aggregate_id, type_event_id, payload, custom_attributes, occurred_at)
SELECT * FROM unnest(@TypeAggregateId, @AggregateId, @TypeEventId, @Payload, @CustomAttributes, @Moment)
