# US 2.2.6 — Register and manage representatives of a shipping agent organization

## 1. Scope and context
- Actor: Port Authority Officer
- Domain: ShippingAgentOrganization, Representative
- Goal: allow creating, updating and deactivating representatives, ensuring that only authorized individuals act on behalf of the organization.

## 2. Assumptions
- A Representative belongs to exactly one ShippingAgentOrganization.
- Each representative must include: name, citizenId, nationality, email, phoneNumber.
- Endpoints are protected by authentication; write operations require PortAuthorityOfficer permission.

## 3. Data model (aligned with domain)
Representative
- representativeId (UUID)
- name
- citizenId
- nationality
- email
- phoneNumber
- roleInOrganization (optional)
- active (boolean; default true)
- organizationIdentifier (string) — reference to the organization it belongs to

## 4. Invariants / Business rules
- Each representative is associated with exactly one organization (AC1).
- Required fields: name, citizenId, nationality, email, phoneNumber (AC2).
- email must be valid; phoneNumber should accept international formats (prefer E.164).
- Cannot create a representative without an existing organization.
- Deactivating a representative does not delete historical data — only sets `active = false`.
- Audit: record who created/updated/deactivated and timestamps.

## 5. API design (REST)
Base path: /api/organizations

- POST `/api/organizations/{identifier}/representatives`
  - Purpose: create a representative associated with organization `{identifier}`
  - Request: CreateRepresentativeRequest
  - Responses: 201 Created (RepresentativeResponse + Location), 400 Bad Request (validation), 404 Not Found (non-existent organization), 422 Unprocessable Entity (detailed validation)

- PUT `/api/organizations/{identifier}/representatives/{representativeId}`
  - Purpose: update all fields of a representative
  - Request: UpdateRepresentativeRequest
  - Responses: 200 OK (RepresentativeResponse), 400, 404 (org or rep not found), 422

- PATCH `/api/organizations/{identifier}/representatives/{representativeId}/deactivate`
  - Purpose: deactivate (soft-delete) a representative
  - Responses: 200 OK, 404 Not Found, 403 Forbidden (when unauthorized)

- GET `/api/organizations/{identifier}/representatives`
  - Purpose: list an organization's representatives (optionally filter by `active=true|false`)
  - Responses: 200 OK

- GET `/api/representatives/{representativeId}`
  - Purpose: get representative details by id
  - Responses: 200 OK, 404 Not Found

## 6. Validation rules
- `name`: not empty, trimmed, length 2..200
- `citizenId`: not empty
- `email`: valid format
- `phoneNumber`: minimum digits; accept international prefix
- `representative` must indicate `organizationIdentifier` or be provided in the URL; association mandatory
- `active` is managed by the deactivate API; should not be directly set by the client

## 7. Auditing & Logging
- Record createdAt, updatedAt, createdBy, updatedBy for create/update/deactivate operations.
- Audit logs with before/after snapshots when feasible.

## 8. Non-functional
- Performance: typical operations < 300ms.
- Idempotency: creating with the same `representativeId` should return 409; creating via endpoint with generated id should be idempotent when re-sent with the same payload, and deduplicated by business keys (citizenId + organization).
- Security: TLS mandatory; avoid exposing sensitive data in logs.

## 9. Acceptance Criteria / Tests
1) Create representative — happy path
- Given a valid payload and an existing organization, when POST `/api/organizations/{identifier}/representatives`, then 201 with body and Location, and representative persisted (`active = true`).

2) Update representative — happy path
- Given an existing representative, when PUT with a valid payload, then 200 and values updated (`updatedAt` and `updatedBy` updated).

3) Deactivate representative — happy path
- Given an active representative, when PATCH `/deactivate`, then 200 and `active` becomes `false`.

4) Validation — missing required fields
- When POST without `name` or `citizenId` or `email` or `phoneNumber`, then 422 with validation messages.

5) Validation — invalid email
- When POST/PUT with invalid `email`, then 422.

6) Association — non-existent organization
- When attempting to create a representative for a non-existent organization, then 404.

## 10. Request / Response examples

CreateRepresentativeRequest
```json
{
  "name": "Ana Silva",
  "citizenId": "12345678A",
  "nationality": "PT",
  "email": "ana.silva@ibership.example",
  "phoneNumber": "+351912345678",
  "roleInOrganization": "Manager"
}