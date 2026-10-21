# GAIA BPMN Properties

Desktop application and API for managing application-owned metadata, custom properties, and attachments associated with BPMN processes and elements.

The application does not rewrite BPMN XML. Imported BPMN files are treated as read-only input for discovering process and element identifiers; custom metadata is stored by the API and can be edited without changing the source diagram.

## Current delivery

- Phase 1: repository foundation, architecture, local services, CI, and documentation.
- Phase 2: backend process/element identity and dynamic property definitions/values.
- Authentication is prepared for a configurable OIDC provider. Set `Authentication__Authority` and `Authentication__Audience` to enable JWT validation.
- The initial attachment upload path uses private S3-compatible storage through MinIO.
- The desktop client imports BPMN XML read-only and lets users inspect identifiers.
- Audit history, BPMN round-trip export, and signed desktop packaging remain out of scope for this delivery.

## Prerequisites

- .NET 8 SDK
- Node.js 20+
- Docker Desktop

## Local development

See [TUTORIAL.md](TUTORIAL.md) for the short, complete run guide.

The API exposes OpenAPI at `/openapi/v1.json` and health at `/health`.

## Configuration

Copy `.env.example` to `.env` for local service values. The API uses PostgreSQL connection settings from `ConnectionStrings__Postgres` and optional OIDC settings from the `Authentication__*` variables.

## Tests

```powershell
dotnet test backend/BpmnProperties.sln
```

## Architecture

See [docs/architecture/overview.md](docs/architecture/overview.md), [docs/architecture/data-model.md](docs/architecture/data-model.md), and [docs/decisions/0001-foundation.md](docs/decisions/0001-foundation.md).
