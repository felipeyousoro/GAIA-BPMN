# Project Specification: BPMN Custom Properties Management System

## 1. Role and Objective

Act as a senior software architect and full-stack engineer. Design and implement a production-oriented desktop application and backend for managing custom properties and file attachments associated with BPMN processes and elements.

The BPMN diagrams themselves are created and edited in a separate application. This system must not implement a BPMN diagram editor. Its responsibility is to manage metadata, custom property values, file attachments, permissions, and the association between stored data and BPMN processes and elements.

Prioritize maintainability, security, clear architecture, testability, and straightforward deployment. Do not overengineer the initial implementation.

## 2. Technology Stack

Use the following technologies:

### Desktop application
- Electron
- React
- TypeScript with strict mode enabled
- Vite
- TanStack Query for server state and caching
- React Hook Form for form management
- Zod for runtime validation
- A consistent, accessible component library selected at the beginning of implementation

### Backend
- C#
- ASP.NET Core Web API on a supported .NET LTS release
- Entity Framework Core
- PostgreSQL
- FluentValidation or an equivalent consistent validation approach
- OpenAPI for API documentation

### File storage
- Local filesystem storage managed by the API (`storage/` directory with persistent Docker volume)
- Encapsulate storage operations behind an application-owned abstraction so the provider can be changed without modifying business logic

### Development and deployment
- Docker and Docker Compose
- Automated CI pipeline
- xUnit for backend tests
- Vitest and React Testing Library for frontend tests
- Playwright for end-to-end tests

Pin compatible dependency versions and use lockfiles. Verify current compatibility before selecting versions.

## 3. Architecture

Use a monorepo and a modular monolith architecture.

Suggested repository structure:

```text
bpmn-properties/
├── apps/
│   └── desktop/
│       ├── src/
│       │   ├── main/
│       │   ├── preload/
│       │   └── renderer/
│       ├── tests/
│       └── package.json
├── backend/
│   ├── src/
│   │   ├── BpmnProperties.Api/
│   │   ├── BpmnProperties.Application/
│   │   ├── BpmnProperties.Domain/
│   │   └── BpmnProperties.Infrastructure/
│   └── tests/
├── contracts/
│   └── openapi/
├── deploy/
│   └── compose.yaml
├── docs/
│   ├── architecture/
│   ├── decisions/
│   └── development/
├── .editorconfig
├── .gitignore
└── README.md
```

Organize application code by business feature, including Processes, Elements, Properties, Attachments, Identity, and Auditing.

The dependency direction must be explicit:

- Domain contains business entities, value objects, and domain rules.
- Application contains use cases, orchestration, validation, and application-level authorization.
- Infrastructure implements persistence, file storage, and external service integrations.
- API contains HTTP endpoints, middleware, request handling, and application composition.

The Domain layer must not depend on ASP.NET Core, Entity Framework Core, file-storage implementations, or UI code. Keep the architecture pragmatic and avoid unnecessary interfaces, repositories, projects, and abstractions.

## 4. Core Functional Requirements

### 4.1 BPMN process and element management

The system must support:

- Registering and identifying BPMN processes.
- Managing metadata associated with a process.
- Identifying individual BPMN elements by their external BPMN element IDs.
- Associating custom properties and attachments with specific elements.
- Searching and retrieving processes and elements.
- Supporting multiple versions of a process or model when required.

Keep internal database IDs separate from external BPMN identifiers. Define how identifiers are scoped, how process versions are represented, and how element IDs map to the external BPMN editor.

Do not assume BPMN element IDs are globally unique.

### 4.2 Dynamic custom properties

Implement a configurable property-definition system.

Property definitions should support, as applicable:

- Text
- Multiline text
- Integer and decimal numbers
- Boolean
- Date
- Date and time
- Single-select dropdown
- Multi-select dropdown
- File attachment or file-reference properties

Each property definition should be capable of specifying:

- Stable identifier
- Display name
- Description
- Data type
- Required or optional status
- Default value, where appropriate
- Validation rules
- Selectable options, when applicable
- Display order
- Whether the property is active
- Applicable process, element, or element-type scope

The application must render property forms dynamically from these definitions.

Validate property values on the backend. Client-side validation improves usability but must never replace server-side validation.

Use relational database structures for core entities and relationships. Use PostgreSQL JSONB selectively for flexible property values when justified. Avoid an unstructured, universal key/value model that makes validation, querying, and integrity unnecessarily difficult.

Define behavior for missing, invalid, deprecated, or changed property definitions.

### 4.3 File attachments

Users must be able to:

- Upload files.
- Associate files with a BPMN process or element.
- Associate files with a specific custom property where applicable.
- View attachment metadata.
- Download authorized files.
- Delete attachments when permitted.
- See upload progress and meaningful error messages.
- Handle interrupted uploads and retries where appropriate.

Store actual file contents in the API's local file storage directory (`storage/`), not in PostgreSQL.

Store metadata in PostgreSQL, including:

- Internal file ID
- Generated storage key
- Original filename
- Verified content type
- File size
- Checksum
- Upload timestamp
- Uploader identity
- Relevant process, element, and property associations
- Retention or deletion status where required

Use private storage by default. Never trust user-provided filenames, MIME types, or file extensions. Enforce upload size limits, validate file contents where feasible, prevent path traversal, and provide antivirus or malware scanning integration where required.

Authorize every upload, download, and deletion on the backend. Never expose permanent storage credentials in the desktop application.

Use a consistent strategy for handling failures between database transactions and file-storage operations. Document cleanup, retries, and orphaned-file handling.

### 4.4 Authentication and authorization

Implement an authentication architecture that supports secure desktop-to-backend communication.

Prefer OAuth 2.0/OpenID Connect with an established identity provider when appropriate. Do not invent a custom authentication protocol.

Design authorization to support future role- or permission-based access control, including:

- Viewing processes and properties
- Creating or changing property definitions
- Editing property values
- Uploading and downloading files
- Deleting files
- Managing users or permissions, if applicable

Apply authorization on the backend for every protected operation. Do not rely on hidden buttons or frontend route guards for security.

If an identity provider is not yet selected, define an authentication interface and document the assumptions rather than implementing insecure placeholder authentication.

### 4.5 Audit history

Design an audit mechanism for important operations, including changes to property definitions, property values, attachments, and permissions.

Capture the actor, action, timestamp, affected resource, and appropriate change details. Avoid storing sensitive file contents, credentials, or unnecessary personal information in logs.

Define retention and access rules for audit records.

### 4.6 External BPMN editor integration

The system must work independently of the BPMN editor.

Define an integration contract using stable process and element identifiers. Provide documented REST API operations that an external editor extension or integration can call.

Do not assume that a specific BPMN editor, plugin interface, or extension API is available. Keep editor-specific integrations separate from core business logic.

If integration details are unknown, document the assumptions and implement the backend API independently of the editor.

Do not modify BPMN XML unless a future requirement explicitly requests XML import, export, or synchronization.

## 5. Database Design

Design a normalized PostgreSQL schema with appropriate foreign keys, constraints, and indexes.

Consider entities such as:

- Processes
- Process versions or model revisions
- BPMN elements
- Property definitions
- Property options
- Property values
- Files
- Element-file associations
- Property-file associations
- Users or external identity references
- Audit records

Do not create redundant tables simply to match this list. Select a coherent model that supports the actual requirements.

Use Entity Framework Core migrations for schema evolution. Commit migrations to version control.

Requirements:

- Use explicit entity configurations.
- Use appropriate unique constraints and indexes.
- Use optimistic concurrency where concurrent editing could overwrite changes.
- Use UTC timestamps and PostgreSQL `timestamptz` for absolute instants.
- Define deletion and retention behavior explicitly.
- Keep database access out of UI code and domain entities.
- Avoid automatic destructive migrations in production startup.

Document the schema and important design decisions.

## 6. Backend API

Use versioned REST endpoints with consistent request and response conventions.

Example endpoints:

```http
GET    /api/v1/processes
POST   /api/v1/processes
GET    /api/v1/processes/{processId}

GET    /api/v1/processes/{processId}/elements
GET    /api/v1/elements/{elementId}

GET    /api/v1/elements/{elementId}/properties
PUT    /api/v1/elements/{elementId}/properties/{propertyId}

GET    /api/v1/property-definitions
POST   /api/v1/property-definitions
PUT    /api/v1/property-definitions/{propertyId}

POST   /api/v1/elements/{elementId}/attachments
GET    /api/v1/attachments/{attachmentId}
DELETE /api/v1/attachments/{attachmentId}
```

Treat these as examples, not a requirement to implement every endpoint before its use case exists.

Use appropriate HTTP methods and status codes, pagination for collections, consistent error responses using Problem Details, and request validation.

Pass cancellation tokens through asynchronous operations. Use asynchronous I/O for database and storage operations.

Generate OpenAPI documentation and TypeScript API types or clients from the API contract where practical. Avoid maintaining duplicated, manually synchronized API types.

## 7. Desktop Application Architecture

Organize the React renderer by feature:

```text
renderer/
├── app/
├── features/
│   ├── processes/
│   ├── properties/
│   ├── attachments/
│   ├── property-definitions/
│   └── users/
├── shared/
│   ├── api/
│   ├── components/
│   ├── hooks/
│   ├── lib/
│   └── types/
└── styles/
```

Implement:

- Process search and selection.
- Element selection by identifier or through an integration interface.
- Dynamic property forms.
- File upload and attachment management.
- Loading, empty, validation, and error states.
- Appropriate confirmation for destructive operations.
- Accessible keyboard navigation and form labels.

Use TanStack Query for remote data and caching. Use React state for local UI state. Use React Hook Form and Zod for complex forms.

Enable strict TypeScript checks. Avoid `any` unless explicitly justified. Validate untrusted API responses at runtime when needed.

### Electron security

- Disable Node integration in renderer processes.
- Enable context isolation.
- Use a restrictive Content Security Policy.
- Expose only the minimum required functionality through the preload script.
- Validate IPC messages.
- Restrict navigation and prevent untrusted remote content from gaining privileged access.
- Keep secrets out of source code, frontend bundles, and packaged desktop binaries.
- Store authentication tokens using an appropriate secure strategy for the target operating system.
- Enforce all sensitive operations on the backend.

## 8. Coding Conventions

### C# and .NET

- Use PascalCase for types and methods.
- Use camelCase for local variables and parameters.
- Enable nullable reference types.
- Use `async`/`await` for asynchronous I/O.
- Suffix asynchronous methods with `Async`.
- Pass `CancellationToken` through I/O boundaries.
- Prefer constructor injection and explicit dependencies.
- Use structured logging.
- Avoid broad catch-all exception handling.
- Use centralized exception handling and consistent error responses.
- Keep controllers or endpoints thin.
- Keep business rules out of controllers and database configurations.
- Use domain objects and explicit DTOs rather than exposing EF entities directly.
- Avoid unnecessary generic repositories and service abstractions.

### TypeScript and React

- Use strict TypeScript.
- Use PascalCase for components and types.
- Use camelCase for functions and variables.
- Use uppercase snake case for true global constants when appropriate.
- Prefer named exports for components and utilities, with a documented exception for application entry points.
- Keep components focused and avoid excessive component nesting.
- Separate server state, UI state, and form state.
- Centralize API communication.
- Avoid duplicating backend validation rules in ways that can diverge.
- Avoid giant components, global mutable state, and unnecessary abstractions.

### General rules

- Use descriptive names and small, cohesive modules.
- Avoid magic strings and unexplained numeric constants.
- Keep functions focused on one clear responsibility.
- Avoid premature optimization.
- Prefer composition over inheritance.
- Document non-obvious decisions, not self-explanatory code.
- Never commit secrets, credentials, local database files, or uploaded user documents.
- Follow consistent formatting rules enforced by tooling.

## 9. Code Quality and Automated Testing

Configure quality checks before implementing substantial functionality.

### Backend
- `.editorconfig`
- .NET analyzers
- Nullable reference types
- Formatting checks
- xUnit unit tests
- API integration tests using PostgreSQL
- Storage integration tests
- Authentication and authorization tests

### Frontend
- ESLint
- Prettier
- TypeScript strict checks
- Vitest
- React Testing Library
- Playwright end-to-end tests

### CI pipeline

For every pull request:

1. Restore and install dependencies.
2. Check formatting and linting.
3. Compile the backend and frontend.
4. Run automated tests.
5. Validate migrations and relevant database integration behavior.
6. Run dependency and secret scanning.
7. Build distributable artifacts where appropriate.

Test important business rules, permission boundaries, upload validation, database persistence, concurrent edits, and failure recovery.

Do not optimize for an arbitrary test-coverage percentage. Prioritize correctness and risk.

## 10. Deployment and Configuration

Provide Docker Compose for local development, including PostgreSQL and the backend API with a persistent storage volume.

Provide:

- Environment-variable-based configuration.
- Example environment configuration without real secrets.
- Database migration instructions.
- Health checks.
- Structured logs.
- Persistent storage configuration.
- Backup and restore instructions.
- Production deployment guidance.
- Desktop build and packaging instructions for supported operating systems.

Do not assume that local development configuration is production-ready.

Document TLS, secret management, storage permissions, database backups, recovery procedures, and file-storage lifecycle policies.

## 11. Documentation

Create:

- Root README with project overview and setup instructions.
- Architecture overview.
- Repository structure documentation.
- Database schema documentation.
- API documentation.
- Local development instructions.
- Testing instructions.
- Deployment guide.
- Security notes.
- Architecture Decision Records for important choices.
- Instructions for adding a new property type.
- Instructions for integrating a new BPMN editor.

Document assumptions and limitations rather than silently inventing requirements.

## 12. Implementation Workflow

Work in small, verifiable increments.

### Phase 1: Architecture and foundation
- Inspect the repository and existing code, if any.
- Identify unresolved requirements and assumptions.
- Propose the architecture and data model.
- Set up the repository, project files, formatting, linting, CI, and local development environment.
- Verify that the initial projects compile and tests run.

### Phase 2: Backend foundation
- Implement process and element identity.
- Implement property definitions and property values.
- Add PostgreSQL persistence and migrations.
- Implement authentication and authorization integration points.
- Implement API documentation and integration tests.

### Phase 3: File storage
- Implement the local file-storage abstraction.
- Implement secure upload, download, and deletion.
- Persist attachment metadata and associations.
- Test authorization, file validation, storage failures, and cleanup.

### Phase 4: Desktop application
- Implement secure Electron setup.
- Build process search and selection.
- Implement dynamic property forms.
- Implement attachment management.
- Add loading, error, validation, and empty states.
- Connect to the backend API.

### Phase 5: Quality and integration
- Add audit history.
- Add concurrency handling.
- Test critical end-to-end workflows.
- Complete deployment documentation and security review.
- Build and verify the desktop package.

Do not attempt to generate the entire production system in one enormous code-generation step. Complete each phase, run the relevant checks, and resolve failures before proceeding.

## 13. Expected Deliverables

Produce:

1. A documented architecture and repository structure.
2. A working backend with PostgreSQL persistence.
3. A working desktop application.
4. Secure file upload and download through local API storage.
5. Dynamic custom property definitions and editors.
6. API documentation and integration contracts.
7. Database migrations.
8. Automated tests.
9. Docker Compose configuration for local development.
10. CI configuration.
11. Setup, testing, and deployment documentation.

At each stage, report what was implemented, which tests were executed, any failures, and the next concrete step.

Do not claim that functionality works unless it has been implemented and verified. Do not leave critical paths as placeholder implementations while describing the system as production-ready.

## 14. Important Unresolved Decisions

Before committing to irreversible design choices, identify these questions and document reasonable defaults:

- Which BPMN editor will be integrated?
- Can administrators define arbitrary custom property schemas, or are schemas predefined?
- Is the system single-tenant or multi-tenant?
- Which identity provider and authorization model will be used?
- Which operating systems must the desktop application support?
- Is deployment self-hosted, cloud-based, or both?
- What file types, maximum sizes, retention rules, and malware-scanning requirements apply?
- Are process versioning, approval workflows, and audit-history retention required?

If answers are unavailable, proceed with clearly documented assumptions and keep the design adaptable.

## 15. Primary Engineering Principle

Deliver the simplest secure, maintainable architecture that meets the current requirements, with clear boundaries that allow future extensions without rewriting the system.
