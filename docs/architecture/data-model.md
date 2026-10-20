# Data model

The initial schema is single-tenant and uses UTC timestamps.

- `processes`: stable external process key and display metadata.
- `bpmn_elements`: external element ID scoped by process.
- `property_definitions`: configurable property schema and applicability.
- `property_options`: options for select properties.
- `property_values`: one value per element and definition, stored as validated JSONB.

The unique constraint on `(process_id, external_element_id)` prevents accidental cross-process collisions. Property definitions use a stable key and a controlled data type enum. Property values are not a universal key/value store: the relational links and definition type provide integrity, while JSONB supports the different scalar and collection shapes.

No destructive migrations run automatically in production. Apply migrations explicitly with `dotnet ef database update` from a deployment job.
