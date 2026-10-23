# Data model

The schema uses UTC timestamps and organizes processes as
`organizations` (Órgãos) → `suborganizations` (Subórgãos) → `processes`.

- `organizations`: active organizational contexts with unique names and optional codes.
- `suborganizations`: active children of an organization with names/codes unique within that organization.
- `processes`: stable external process key, display metadata, original file name, local storage diagram object key, and one required `SuborganizationId`.
- `bpmn_elements`: external element ID scoped by process.
- `element_attachments`: private local file storage metadata associated with a BPMN element.
- `property_definitions`: configurable property schema and applicability.
- `property_options`: options for select properties.
- `property_values`: one value per element and definition, stored as validated JSONB.
- `users`: local user accounts with unique lowercase `Username`, `DisplayName`, PBKDF2-SHA256 `PasswordHash`, `Role` (`Admin` or `Guest`), and optional local storage avatar metadata (`AvatarObjectKey`, `AvatarContentType`).

The unique constraint on `(process_id, external_element_id)` prevents accidental cross-process collisions. Property definitions use a stable key and a controlled data type enum. Property values are not a universal key/value store: the relational links and definition type provide integrity, while JSONB supports the different scalar and collection shapes.

The global `input` and `output` definitions use `MultilineText`. The API stores their values in `property_values` and mirrors them into `gaia:io` under BPMN `extensionElements`, using the namespace `http://gaia-bpmn/schema/1.0` when a diagram is saved.

No destructive migrations run automatically in production. Apply migrations explicitly with `dotnet ef database update` from a deployment job.

Migration `0004_organizations_process_scope.sql` creates the default organization
and suborganization, assigns all existing processes to that context, and adds a
restrictive foreign key. It preserves existing process, element, and storage
identifiers. The deployment bootstrap in `deploy/postgres/ensure.sql` mirrors
this operation idempotently for newly provisioned databases.
