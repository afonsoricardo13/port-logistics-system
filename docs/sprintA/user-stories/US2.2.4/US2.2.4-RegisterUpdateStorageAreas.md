# US 2.2.4 — Register & Update Storage Areas

## 1. User Story Description
**As a** Port Authority Officer  
**I want to** register and update storage areas  
**So that** (un)loading and storage operations can be assigned to the correct locations. 

## 2. Acceptance Criteria
- Each storage area must have a **unique identifier**, **type** (e.g., yard, warehouse), and **location within the port**.  
- Storage areas must specify **maximum capacity (in TEUs)** and **current occupancy**.   
- By default, a storage area **serves the entire port** (i.e., all docks). Some areas (namely **yards**) may be **constrained to a subset of docks**—usually the closest ones.   
- **Complementary information** such as **distance between docks and storage areas** must be **manually recorded** to support future logistics planning/optimization.  
- **Updates must never allow** the **current occupancy to exceed** the **maximum capacity**. 

## 3. System Sequence Diagram (SSD)
**Pending** — to be aligned with the finalized domain model (entities `StorageArea`, `Dock`).

## 4. Sequence Diagram (SD)
**Pending** — to be detailed with the definitive DDD services/repositories.

## 5. Business Rules
- **BR1 — Unique Identifier:** Each `StorageArea.id` is globally unique. 
- **BR2 — Capacity Safety:** `currentOccupancyTEU` **≤** `maxCapacityTEU`; reject updates that would violate this constraint.   
- **BR3 — Dock Serving Scope:** By default, `servesAllDocks = true`. If `false`, the `servedDocks[]` list must contain **at least one** dock and only allow **existing** docks.  
- **BR4 — Complementary Distances:** Maintain manually captured `dockDistances[]` (dockId, meters) for planning and future optimization.   
- **BR5 — Audit Trail:** Keep `createdAt`, `updatedAt`, `updatedBy` per project best practices.
- **BR6 — Searchability:** Storage areas are searchable/filterable by `id`, `type`, `location`, and `servedDocks` subset (derived from AC). 

## 6. Technical Requirements

### 6.1 API Endpoints

POST /api/storage-areas
GET /api/storage-areas
GET /api/storage-areas/{id}
PUT /api/storage-areas/{id}
PATCH /api/storage-areas/{id} # optional partial update


**Query (GET /api/storage-areas):**  
`id`, `type`, `location`, `servesAllDocks` (bool), `servedDockId`, `page`, `size`, `sort` (e.g., `id,asc`)

**Request (POST/PUT) — example**
```json
{
  "id": "yard-b2",
  "type": "YARD",
  "location": "North Sector",
  "maxCapacityTEU": 1250,
  "currentOccupancyTEU": 420,
  "servesAllDocks": false,
  "servedDocks": ["dock-a1", "dock-a2"],
  "dockDistances": [
    { "dockId": "dock-a1", "distanceMeters": 180 },
    { "dockId": "dock-a2", "distanceMeters": 240 }
  ],
  "notes": "Primarily for refrigerated containers"
}
```

### 6.2 Domain Model

**Under Development**
> 
> The domain model for this user story is currently being developed as part of the overall system domain design. This section will be updated once the domain model is finalized.


## 7. Dependencies

### 7.1 Upstream Dependencies
- **US 2.2.3**: Docks: needed for valid references in servedDocks and dockDistances

### 7.2 Downstream Dependencies
- Logistics planning & scheduling modules that leverage capacity/occupancy and dock distances.
- Audit/operations reporting (team best practices and assessment criteria).