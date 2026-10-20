# ADR 0001: Initial foundation

## Decision

Use a .NET 8 modular monolith with PostgreSQL, a future Electron/React client, and an S3-compatible storage abstraction. Authentication is configurable OIDC rather than a custom username/password protocol.

## Context

The BPMN editor is not selected. The system must support metadata from different editors without rewriting their XML or coupling the API to a vendor-specific extension API.

## Consequences

The desktop can import BPMN for viewing and identifier discovery, while metadata remains server-owned. Round-trip export and editor-specific adapters are deferred. Single-tenant deployment keeps the first schema smaller; organization isolation can be added before multi-tenant hosting.
