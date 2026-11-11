# US 2.2.12 — Manage Physical Resources

## 1. User Story Description
**As a** Logistics Operator  
**I want to** register and manage physical resources (create, update, deactivate)  
**So that** they can be accurately considered during planning and scheduling operations.

## 2. Acceptance Criteria
- **AC1 — Resource Types:** The system must support cranes (fixed and mobile), trucks, and other operational equipment directly involved in vessel and yard operations.  
- **AC2 — Unique Identification:** Each resource must have a unique **alpha-numeric code** and a **description**.  
- **AC3 — Operational Capacity:** Each resource must include an **operational capacity** value, which varies according to the resource type.  
- **AC4 — Assigned Area:** If applicable, a resource must reference its **assigned operational area** (e.g., “Dock A”, “Yard B”).  
- **AC5 — Availability Status:** Every resource must include a **current availability status**, which can be one of:
  - `active`
  - `inactive`
  - `under_maintenance`
- **AC6 — Setup Time:** Resources with preparation time must define a **setup time (in minutes)** before they can begin operations.  
- **AC7 — Qualification Requirements:** Each resource must include **qualification requirements**, specifying the minimum certifications required for associated staff.  
- **AC8 — Deactivation/Reactivate:** Deactivation or reactivation **must not delete resource data** — it must preserve the resource for **audit and historical reference**.  
- **AC9 — Search and Filtering:** Resources must be **searchable and filterable** by `code`, `description`, `kind`, and `status`.

## 3. System Sequence Diagram (SSD)
*(See C2 Process View — System Sequence Diagram for US 2.2.12)*  
The Logistics Operator authenticates, registers a new physical resource, modifies existing data, or deactivates/reactivates resources. The system validates data consistency, uniqueness, and audit requirements before persisting changes.

## 4. Sequence Diagram (SD)
*(See C3 Process View — Sequence Diagram for US 2.2.12)*  
Shows the interaction between:
- `Logistics Operator`  
- `PhysicalResourceController`  
- `PhysicalResourceService`  
- `PhysicalResourceRepository`  
- `AuditService`  
and external validation (e.g., IAM for permissions, if applicable).

## 5. Business Rules
- **BR1 — Unique Code:** Each physical resource must have a unique alphanumeric `code` across the system.  
- **BR2 — Non-Destructive Deactivation:** Deactivation/reactivation only toggles the status — data remains stored for traceability.  
- **BR3 — Valid Status Transitions:**
  - `active → under_maintenance`
  - `under_maintenance → active`
  - `active → inactive`
  - `inactive → active`
- **BR4 — Capacity Validation:** Operational capacity must be numeric, positive, and within domain-defined limits per resource type.  
- **BR5 — Setup Time Validation:** If defined, setup time must be a non-negative integer representing minutes.  
- **BR6 — Qualification Requirement Integrity:** Only valid qualification categories (registered in the system) can be assigned to a resource.  
- **BR7 — Audit Trail:** All create/update/deactivate/reactivate operations must log `changedAt`, `changedBy`, and `operationType`.  
- **BR8 — Authorization:** Only users with the **Logistics Operator** role can manage physical resources.  
- **BR9 — Search/Filter Rules:** Resource searches must support partial matches and case-insensitive filters on `code`, `description`, and `kind`.

## 6. Technical Requirements

### 6.1 API Endpoints
| Method | Endpoint | Description |
|--------|-----------|-------------|
| **GET** | `/api/physical-resources` | List and filter all physical resources (`?status=...&type=...&area=...&code=...`). |
| **GET** | `/api/physical-resources/{id}` | Retrieve details of a specific physical resource. |
| **POST** | `/api/physical-resources` | Create a new physical resource (requires unique code and valid attributes). |
| **PATCH** | `/api/physical-resources/{id}` | Update allowed fields (e.g., description, capacity, setup time, status). |
| **POST** | `/api/physical-resources/{id}/deactivate` | Deactivate a resource (status = `inactive`). |
| **POST** | `/api/physical-resources/{id}/reactivate` | Reactivate a resource (status = `active`). |

### 6.2 Domain Model Extract

## 7. Dependencies
  
- **US 2.2.11 — Assign Physical Resources to Visits:** Depends on registered physical resources for allocation.  
- **US 2.2.8 — Create Vessel Visit Notification:** Indirect dependency — resource data may inform visit creation or scheduling constraints.

## 8. Notes
- Resource data must integrate with future **scheduling and planning modules** (to ensure compatibility with operational assignment features).  
- Deactivation does not affect historical references — past planning operations must still reference the inactive resource for traceability.
