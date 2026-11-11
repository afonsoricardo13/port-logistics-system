# US 2.2.5 – Register new shipping agent organizations

## Scope and context
- Actor: Port Authority Officer
- Domain concepts: ShippingAgentOrganization, Representative
- Out of scope: complex external approval workflows, billing, deep integration with external systems (except notifications)

## Assumptions
- ShippingAgentOrganization is an aggregate root with identity (`OrganizationID` / UUID).
- Each organization must have at least one representative at registration time.
- Representative emails and phone numbers are used for notifications and authentication (2FA, approvals).
- Addresses stored as text; a geospatial VO may be introduced later without breaking API.

## Data model (aligned with domain)
ShippingAgentOrganization
- organizationId (UUID / strong typed)
- identifier (OrganizationIdentifier)
- legalName (LegalName)
- alternativeName (AlternativeName)
- address (Address)
- taxNumber (TaxNumber)
- representatives: list of Representative
- metadata: createdAt, updatedAt, createdBy, updatedBy

Representative
- representativeId (UUID)
- name
- citizenId
- nationality
- email
- phoneNumber
- roleInOrganization (optional)

## Invariants / Business rules
- identifier, legalName, taxNumber are required.
- At least one representative must be present at registration.
- representative.email must be a valid email; phoneNumber should preferably follow E.164.
- citizenId required for each representative.
- identifier and taxNumber must be unique across organizations.

## API design (REST)
Base path: /api/organizations

- POST /api/organizations
  - Purpose: create a new organization
  - Request: CreateOrganizationRequest
  - Responses: 201 Created (OrganizationResponse + Location), 400, 409 (identifier/taxNumber duplicate), 422 (validation)

- PUT /api/organizations/{id}
  - Purpose: update an existing organization (full replacement)
  - Request: UpdateOrganizationRequest
  - Responses: 200 OK (OrganizationResponse), 404 Not Found, 409, 422

- GET /api/organizations[?identifier|taxNumber|name]
  - Purpose: list / filter organizations

## Validation rules
- identifier, legalName: not empty; trimmed; length 2..200 chars
- taxNumber: basic format validation per jurisdiction
- representative.email: valid email format
- representative.phoneNumber: accept international formats; validate minimum digits
- At least one representative in payload
- Uniqueness of identifier/taxNumber enforced at service/repository level

## Authorization & Authentication
- Requires authenticated user.
- Role/claim: PortAuthorityOfficer required for POST/PUT.
- GET endpoints may be accessible to other roles per product policy.

## Internationalization (i18n)
- Error messages use resource keys; translations EN/PT.
- Field/display names and validation messages localizable.

## Auditing & Logging
- Record who created/updated and when; persist createdAt/updatedAt and createdBy/updatedBy where infra supports it.
- Log audit events for create/update with before/after snapshots when feasible.

## Non-functional requirements
- Performance: POST/PUT typical <300ms.
- Idempotency: creating with same identifier/taxNumber should return 409.
- Security: sensitive data transported via TLS; avoid sensitive data in logs.

## Acceptance criteria / Tests
1) Create organization — happy path
- Given valid payload, when POST /api/organizations, then 201 with body and Location header, and organization persisted.

2) Create organization — duplicate identifier/taxNumber
- Given existing org with same identifier or taxNumber, when POST same identifier/taxNumber, then 409 Conflict.

3) Update organization — happy path
- Given existing organization, when PUT with valid updates, then 200 and values updated.

4) Update organization — not found
- When PUT to unknown id, then 404.

5) Validation — missing representative
- When POST without representatives, then 422 with validation error key.

6) Validation — invalid email
- When representative email invalid, then 422.

## Request / Response examples

CreateOrganizationRequest
```json
{
  "identifier": "AGENT-001",
  "legalName": "Iberian Shipping Agents, Lda",
  "alternativeName": "IberShip",
  "address": "Rua Porto 123, Lisboa",
  "taxNumber": "PT123456789",
  "representatives": [
    {
      "name": "Ana Silva",
      "citizenId": "12345678A",
      "nationality": "PT",
      "email": "ana.silva@ibership.example",
      "phoneNumber": "+351912345678"
      
    }
  ]
}


# OrganizationResponse

```json
{
  "id": "a1b2c3d4-0000-1111-2222-333344445555",
  "identifier": "AGENT-001",
  "legalName": "Iberian Shipping Agents, Lda",
  "alternativeName": "IberShip",
  "address": "Rua Porto 123, Lisboa",
  "taxNumber": "PT123456789",
  "representatives": [
    {
      "representativeId": "r1r2r3-0000-1111",
      "name": "Ana Silva",
      "citizenId": "12345678A",
      "nationality": "PT",
      "email": "ana.silva@ibership.example",
      "phoneNumber": "+351912345678",
      "roleInOrganization": "Manager"
    }
  ],
  "createdAt": "2025-10-12T11:00:00Z",
  "updatedAt": "2025-10-12T11:00:00Z",
  "createdBy": "user@example.com",
  "updatedBy": "user@example.com"
}
