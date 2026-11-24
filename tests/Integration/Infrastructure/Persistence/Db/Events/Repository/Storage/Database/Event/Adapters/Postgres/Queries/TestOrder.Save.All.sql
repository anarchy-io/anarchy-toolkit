INSERT INTO test_order (id, sku) SELECT * FROM unnest(@Id, @Sku)
