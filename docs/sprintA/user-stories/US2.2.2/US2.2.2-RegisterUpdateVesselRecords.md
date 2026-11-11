
# US 2.2.2 — Register & Update Vessel Records

## 1. User Story Description
**As a** Port Authority Officer  
**I want to** register and update vessel records  
**So that** valid vessels can be referenced in visit notifications. :contentReference[oaicite:0]{index=0}

## 2. Acceptance Criteria
- Each vessel record must include key attributes such as **IMO number**, **vessel name**, **vessel type**, and **operator/owner**.  
- The system must validate that the **IMO number** follows the official format (**seven digits with a check digit**); otherwise reject it.  
- Vessel records must be **searchable** by **IMO number**, **name**, or **operator**. 

## 3. System Sequence Diagram (SSD)
**Pending** — to be aligned with the finalized domain model (entities `Vessel`, `VesselType`).

## 4. Sequence Diagram (SD)
**Pending** — to be detailed with the definitive DDD services/repositories.

## 5. Business Rules
- **BR1 — IMO uniqueness:** Each IMO uniquely identifies a vessel; duplicates are rejected. (Implied by AC.)  
- **BR2 — IMO validation:** Apply the official check-digit validation before persisting (weighted Mod 10).  
- **BR3 — Referential integrity:** `Vessel.type` must reference an existing **VesselType** (US 2.2.1). 
- **BR4 — Audit trail:** Keep `createdAt`, `updatedAt`, and `updatedBy` for accountability (project best practices).  
- **BR5 — Search & sort:** Support combined filters (e.g., IMO + operator) and sorting by name/IMO.

## 6. Technical Requirements

### 6.1 API Endpoints

POST /api/vessels
GET /api/vessels
GET /api/vessels/{imo}
PUT /api/vessels/{imo}
PATCH /api/vessels/{imo} # optional partial update

**Query Parameters:**


**Query (GET /api/vessels):** `imo`, `name`, `operator`, `page`, `size`, `sort` (e.g., `name,asc`)

**Request (POST/PUT) — example**
```json
{
  "imo": "9387421",
  "name": "Atlantic Pioneer",
  "typeCode": "CONTAINER",
  "operator": "Atlantic Lines"
}
{
  "data": [
    {
      "imo": "9387421",
      "name": "Atlantic Pioneer",
      "type": { "code": "CONTAINER", "name": "Container Ship" },
      "operator": "Atlantic Lines",
      "createdAt": "2025-09-25T10:30:00Z",
      "updatedAt": "2025-10-01T09:45:00Z",
      "updatedBy": "officer-007"
    }
  ],
  "pagination": { "page": 1, "size": 10, "totalElements": 1, "totalPages": 1 }
}
```

### 6.2 Domain Model

**Under Development**
> 
> The domain model for this user story is currently being developed as part of the overall system domain design. This section will be updated once the domain model is finalized.


## 7. Dependencies

### 7.1 Upstream Dependencies
- **US 2.2.1**: Vessel Types
- **US 2.2.8**: Vessel Visit Notification creation/view relies on Vessel
- **US 2.2.10**: Vessel Visit Notification creation/view relies on Vessel

### 7.2 Downstream Dependencies
- Authentication system (to identify current representative and their organization)
- Authorization system (to enforce organization-based access control)