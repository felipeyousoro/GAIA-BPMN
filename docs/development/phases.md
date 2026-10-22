# Implementation phases

- [x] Phase 1: architecture and foundation
- [x] Phase 2: backend identity and dynamic properties
- [x] Phase 3: local file storage and attachments (initial upload path)
- [x] Phase 4: Electron/React desktop client with BPMN import/viewing (initial client)
- [x] Phase 5: quality, integration, packaging, and deployment hardening (initial delivery)
- [x] BPMN visualizer delivery: API import/list/diagram/tasks/attachments, desktop viewer, Input/Output persistence, and round-trip download

Phase 2 intentionally excludes audit history and process versioning because those requirements were deferred for this delivery.

The BPMN viewer is read-only for diagram editing. The API preserves the source XML and updates only its GAIA Input/Output extension values.
