INSERT INTO type_registry (name) VALUES (@Name)
ON CONFLICT (name) DO UPDATE SET name = EXCLUDED.name RETURNING id
