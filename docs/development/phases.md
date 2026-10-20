# Implementation phases

- [x] Phase 1: architecture and foundation
- [x] Phase 2: backend identity and dynamic properties
- [x] Phase 3: S3-compatible file storage and attachments (initial upload path)
- [x] Phase 4: Electron/React desktop client with BPMN import/viewing (initial client)
- [x] Phase 5: quality, integration, packaging, and deployment hardening (initial delivery)

Phase 2 intentionally excludes audit history and process versioning because those requirements were deferred for this delivery.

The initial delivery intentionally keeps BPMN XML read-only: it discovers IDs and names for metadata editing, but does not claim round-trip preservation for arbitrary vendor extensions.
