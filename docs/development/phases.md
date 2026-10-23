# Implementation phases

- [x] Phase 1: architecture and foundation
- [x] Phase 2: backend identity and dynamic properties
- [x] Phase 3: local file storage and attachments (initial upload path)
- [x] Phase 4: Electron/React desktop client with BPMN import/viewing (initial client)
- [x] Phase 5: quality, integration, packaging, and deployment hardening (initial delivery)
- [x] BPMN visualizer delivery: API import/list/diagram/tasks/attachments, desktop viewer, Input/Output persistence, and round-trip download

Phase 2 intentionally excludes audit history and process versioning because those requirements were deferred for this delivery.

The BPMN viewer is read-only for diagram editing. The API preserves the source XML and updates only its GAIA Input/Output extension values.

## Improvement roadmap

- [x] Phase 0: functional contract, inventory reference, vocabulary, permissions, and acceptance criteria
- [x] Phase 1: organizational model and safe legacy migration
- [x] Phase 2: BPMN element metadata contract
- [x] Phase 3: immediate attachment upload and lifecycle
- [x] Phase 4: authenticated PDF printing
- [ ] Phase 5: organizational API and catalog screens
- [x] Phase 6: element editing experience
- [ ] Phase 7: security, performance, and operations hardening
- [ ] Phase 8: automated validation, assisted migration, and rollout

The approved Phase 0 contract is recorded in
`docs/decisions/0002-improvement-functional-contract.md`.
