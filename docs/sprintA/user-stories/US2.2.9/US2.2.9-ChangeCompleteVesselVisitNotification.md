# US 2.2.9 — Change / Complete a Vessel Visit Notification

## 1. User Story Description
**As a** Shipping Agent Representative  
**I want to** change and/or complete a Vessel Visit Notification while it is **in progress**  
**So that** I can correct errors, complete missing data, or **withdraw the request** before submitting it for approval.

## 2. Acceptance Criteria
- AC1 - The Vessel Visit Notification can remain **"in progress"** or be **changed to "submitted / approval pending"** by the representative.  
- AC2 - The status **"submitted / approval pending"** makes the Vessel Visit Notification **locked for editing** by the representative until a decision from the Port Authority, aligning with the approval/rejection flow and visible states to agents (in progress, pending, approved/rejected).  
- AC3 - Any changes made **while in "in progress"** must comply with the Vessel Visit Notification domain rules (e.g., cargo manifests and container identifiers according to ISO 6346:2022).  
- AC4 - If the representative opts to **withdraw/cancel** the Vessel Visit Notification, this is only allowed **while it is in "in progress"**; after submission, the formal approval process applies.

## 3. System Sequence Diagram (SSD)


## 4. Sequence Diagram (SD)


## 5. Business Rules
- **BR1 — Editable only in "in progress"**: Only Vessel Visit Notifications in **"in progress"** status can be edited/completed by the representative.  
- **BR2 — Allowed State Transitions**:  
  - `in_progress → in_progress` (save draft)  
  - `in_progress → submitted` (“approval pending”)  
  - `in_progress → canceled` (withdraw request)  
  - No edits allowed in `submitted`, `approved`, or `rejected` states.  
- **BR3 — Completeness for Submission**: To transition to `submitted`, the Vessel Visit Notification must meet the minimum submission requirements (valid manifests, crew info when applicable, etc.).  
- **BR4 — Container Validation**: Whenever the Vessel Visit Notification includes/updates manifests, **all container identifiers** must comply with **ISO 6346:2022** (including valid check digits).  
- **BR5 — Audit Trail**: Log `updatedAt`, `updatedBy`, and the history of state changes for traceability. (Logging/auditing requirement across the system.)  
- **BR6 — Authorization by Organization**: Only representatives from the **same shipping agent organization** as the Vessel Visit Notification can edit/withdraw it while it is in `in progress`.  
- **BR7 — Operational Consistency**: Any update must remain consistent with the **approval process** and later **dock assignment** (when applicable).

## 6. Technical Requirements

### 6.1 API Endpoints

- **GET** `/api/vessel-visit-notifications/{id}` — retrieve Vessel Visit Notification (includes current status and metadata).  
- **PATCH** `/api/vessel-visit-notifications/{id}` — **update allowed fields** when `status = in_progress`.  
- **POST** `/api/vessel-visit-notifications/{id}/submit` — **transition** `in_progress → submitted` (validates completeness).  
- **POST** `/api/vessel-visit-notifications/{id}/cancel` — **withdraw** the Vessel Visit Notification (`in_progress → canceled`).  
- **GET** `/api/vessel-visit-notifications?status=...&vessel=...&rep=...&from=...&to=...` — list/filter Vessel Visit Notifications (aligned with US 2.2.10).  

### 6.2 Domain Model Extract
  

## 7. Dependencies

There is a dependency with US 2.2.8 — Create/Submit Vessel Visit Notification: provides creation and data requirements (valid manifests, crew info, “in progress”/“submitted” states).