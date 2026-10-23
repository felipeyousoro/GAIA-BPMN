# Operations

## Migrations and backup

Apply SQL migrations in numeric order (`0001` through the current version)
from a deployment job. Do not run destructive schema changes automatically
from the API. Take a PostgreSQL backup and a copy/snapshot of the storage
volume before applying an organizational migration.

The database and the storage volume must be backed up together. Restoring only
one of them can leave diagram or attachment metadata without its object.

## Limits and monitoring

- BPMN imports are limited to 10 MiB.
- Attachments are limited to 25 MiB and are checked against an allowlist of
  extension/MIME pairs.
- Process and organization listing endpoints cap a response at 100 records
  per request.
- Upload, removal, and process transfer emit structured logs with identifiers
  and sizes, never file contents or physical object keys.

Operational checks should reconcile `element_attachments.ObjectKey` with the
storage directory, monitor storage volume usage, and alert on repeated cleanup
failures. Retention and deletion of potentially personal attachment content
must be approved by the data controller before production rollout.
