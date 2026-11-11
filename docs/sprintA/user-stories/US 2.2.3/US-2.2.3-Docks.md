# US 2.2.3 – Register and update docks

As a Port Authority Officer, I want to register and update docks, so that the system accurately reflects the docking capacity of the port.

## Scope and context
- Actor: Port Authority Officer
- Domain concepts: Dock, VesselType
- Out of scope: scheduling/assignment, crane operations, resource planning

## Assumptions
- A Dock is an aggregate root with identity (`DockID`).
- Allowed vessel types are maintained as `VesselType` values and are referenced by ID from Dock.
- Name/number is unique across the port.
- Location is a human-readable label (e.g., “Quay A – North”). If geospatial is later required, a new VO can be added without breaking this API.

## Data model (aligned with domain)
Dock
- dockId (DockID – UUID/strong typed)
- name (DockName)
- location (DockLocation)
- physical characteristics:
  - maxNumberStsCranes (DockMaxNumberSTSCranes)
  - lengthMeters (DockLength)
  - depthMeters (DockDepth)
  - maxDraftMeters (DockMaxDraft)
- allowedVesselTypeIds: list of `VesselTypeName` (IDs)

Invariants
- name is required, 2–50 chars
- location required, 2–100 chars
- numeric physical characteristics must be positive
- maxDraftMeters <= depthMeters (business plausibility)
- allowedVesselTypeIds non-empty when the dock is intended for operations; can be empty for maintenance-only docks

## API design (REST)
Base path: /api/docks

- POST /api/docks
  - Purpose: create a new dock
  - Request: CreateDockRequest
  - Responses: 201 Created (DockResponse + Location header), 400, 409 (name already exists), 422 (validation)

- PUT /api/docks/{id}
  - Purpose: update an existing dock
  - Request: UpdateDockRequest (partial update not supported; use PATCH in future if needed)
  - Responses: 200 OK (DockResponse), 404 Not Found, 409, 422

## Validation rules
- name/location: not blank; trim; forbid leading/trailing spaces; allowed charset: letters, numbers, spaces, - _ /
- maxNumberStsCranes: integer 0..10 (adjust to domain), length/depth/draft: 0..5000 (meters), draft <= depth
- allowedVesselTypeIds: must reference existing VesselType(s)

## Authorization & authentication
- Requires authenticated user.
- Role/claim: PortAuthorityOfficer required for POST/PUT.
- GET endpoints may be accessible to other roles for read-only as per product policy.

## Internationalization (i18n)
- Return messages via resource keys; UI translations in EN/PT.
- Field display names and validation error messages localized.

## Auditing & logging
- Log who created/updated, when, and what changed.
- Store createdAt/updatedAt and createdBy/updatedBy (if supported by infra).

## Non-functional
- Performance: list endpoints respond <300ms for common filters up to 1k docks.
- Idempotency: creating with same name results in 409.

## Success criteria / Acceptance tests
1) Create dock – happy path
- Given valid payload, when POST /api/docks, then 201 with body and Location, and persisted dock.

2) Create dock – duplicate name
- Given an existing dock with name “Quay A”, when POST with name “Quay A”, then 409 Conflict.

3) Update dock – happy path
- Given a dock exists, when PUT with valid updates, then 200 and values updated.

4) Update dock – not found
- When PUT to an unknown id, then 404.

5) Validation – physical characteristics
- When draft > depth, then 422 with message key dock.validation.draft_le_depth.

6) List with filters
- When GET /api/docks?name=Quay&vesselTypeId=VT-ULCV&location=North, returns docks matching all filters.

## Request/response examples
CreateDockRequest
```
{
  "name": "Quay A",
  "location": "North Basin",
  "maxNumberStsCranes": 2,
  "lengthMeters": 350,
  "depthMeters": 15.5,
  "maxDraftMeters": 14.0,
  "allowedVesselTypeIds": ["FEEDER", "PANAMAX"]
}
```

DockResponse
```
{
  "id": "d7acb0e4-9b9b-4d83-9a10-8c7e5d9f4b6f",
  "name": "Quay A",
  "location": "North Basin",
  "maxNumberStsCranes": 2,
  "lengthMeters": 350,
  "depthMeters": 15.5,
  "maxDraftMeters": 14.0,
  "allowedVesselTypeIds": ["FEEDER", "PANAMAX"],
  "createdAt": "2025-10-12T10:21:00Z",
  "updatedAt": "2025-10-12T10:21:00Z"
}
```

## OpenAPI (concise)

```yaml
openapi: 3.0.3
info:
  title: Docks API
  version: 1.0.0
paths:
  /api/docks:
    post:
      summary: Create dock
      requestBody:
        required: true
        content:
          application/json:
            schema: { $ref: '#/components/schemas/CreateDockRequest' }
      responses:
        '201':
          description: Created
          headers:
            Location:
              schema: { type: string }
          content:
            application/json:
              schema: { $ref: '#/components/schemas/DockResponse' }
        '409': { description: Name already exists }
        '422': { description: Validation error }
  /api/docks/{id}:
    put:
      summary: Update dock
      parameters:
        - in: path
          name: id
          required: true
          schema: { type: string, format: uuid }
      requestBody:
        required: true
        content:
          application/json:
            schema: { $ref: '#/components/schemas/UpdateDockRequest' }
      responses:
        '200':
          description: Updated
          content:
            application/json:
              schema: { $ref: '#/components/schemas/DockResponse' }
        '404': { description: Not found }
        '409': { description: Name already exists }
        '422': { description: Validation error }
components:
  schemas:
    CreateDockRequest:
      type: object
      required: [name, location, lengthMeters, depthMeters, maxDraftMeters]
      properties:
        name: { type: string, minLength: 2, maxLength: 50 }
        location: { type: string, minLength: 2, maxLength: 100 }
        maxNumberStsCranes: { type: integer, minimum: 0, maximum: 10 }
        lengthMeters: { type: number, minimum: 0 }
        depthMeters: { type: number, minimum: 0 }
        maxDraftMeters: { type: number, minimum: 0 }
        allowedVesselTypeIds:
          type: array
          items: { type: string }
    UpdateDockRequest:
      allOf:
        - $ref: '#/components/schemas/CreateDockRequest'
    DockResponse:
      type: object
      properties:
        id: { type: string, format: uuid }
        name: { type: string }
        location: { type: string }
        maxNumberStsCranes: { type: integer }
        lengthMeters: { type: number }
        depthMeters: { type: number }
        maxDraftMeters: { type: number }
        allowedVesselTypeIds:
          type: array
          items: { type: string }
        createdAt: { type: string, format: date-time }
        updatedAt: { type: string, format: date-time }
```

## Test matrix (summary)

- Unit tests
  - Dock name uniqueness enforced by service/repo
  - Draft <= depth validation in Dock aggregate
  - AllowedVesselTypeIds cannot include unknown IDs
- Integration tests (API)
  - POST /api/docks 201 + Location
  - POST duplicate name -> 409
  - PUT unknown id -> 404
  - PUT valid update -> 200

## Diagram references

- C4 Context: docs/sprintA/architecture/c4/C1_Context.puml
- C4 Containers: docs/sprintA/architecture/c4/C2_Containers.puml
- C4 Components (Docks): docs/sprintA/architecture/c4/C3_Components_Docks.puml
- Dynamic (Create/Update Dock): docs/sprintA/architecture/c4/C4_Dynamic_Dock_Create_Update.puml
- SSD – Create/Update Dock: docs/sprintA/architecture/ssd/SSD_US_2_2_3_Docks_Create_Update.puml
 - Level 4 – Code (Docks): docs/sprintA/architecture/c4/C4_L4_Code_Docks.puml
 - Deployment mapping: docs/sprintA/architecture/c4/C2_Deployment_Mapping.puml

## Traceability

- Domain model: Dock aggregate with value objects for Name, Location, PhysicalCharacteristics; relation to VesselType.
- Requirements mapping: US 2.2.3 → Create/Update endpoints; Acceptance tests 1–6.
- Architecture: Controller ↔ AppService ↔ Repository ↔ DbContext (C3 diagram).

## Definition of Done (DoD)

- [ ] API endpoints implemented with input validation and error handling
- [ ] Unit tests for domain invariants (name uniqueness, draft <= depth)
- [ ] Integration tests for API happy/edge paths
- [ ] Authz: only PortAuthorityOfficer can create/update
- [ ] Logging/auditing of create/update
- [ ] OpenAPI updated and published
- [ ] Diagrams (C1–C4, SSD) committed and render in pipeline

## Mapping between views (Lógica, Implementação, Física)

- Lógica (C3 Components): DocksController, DockAppService, DockRepository, DbContext, Dock Aggregate.
- Implementação (L4 Code): Methods and classes realizing C3 responsibilities; DTOs and mappers shown.
- Física (Deployment): SPA → API (ASP.NET Core) → DB (SQL) nodes and network protocols; IAM external.

 
