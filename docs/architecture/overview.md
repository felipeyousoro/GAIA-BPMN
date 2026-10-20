# Architecture overview

GAIA BPMN is a modular monolith. The desktop client is an Electron/React application that communicates with the ASP.NET Core API. The API owns validation, authorization, persistence, and future object-storage operations.

## Boundaries

- **Domain**: process, element, property definition, and property value rules. No framework dependencies.
- **Application**: use-case orchestration and DTOs. It will remain the home for authorization decisions as features expand.
- **Infrastructure**: Entity Framework Core, PostgreSQL, storage providers, and external integrations.
- **API**: HTTP transport, Problem Details, authentication composition, and OpenAPI.
- **Desktop**: BPMN import/viewing and application-owned metadata editing. It never receives storage credentials.

The external BPMN editor contract is identifier-based. A process has a stable external key and elements have identifiers scoped to their process. The API never assumes BPMN element IDs are globally unique.
