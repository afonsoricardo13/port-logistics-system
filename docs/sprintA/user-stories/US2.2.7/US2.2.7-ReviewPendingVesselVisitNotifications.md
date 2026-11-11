# US 2.2.7 — Review & Decide Vessel Visit Notifications

## 1. User Story Description
**As a** Port Authority Officer  
**I want to** review pending Vessel Visit Notifications and **approve or reject** them  
**So that** docking schedules remain under port control. 

## 2. Acceptance Criteria
- When **approved**, the officer must **assign a (temporary) dock** where the vessel should berth.  
- When **rejected**, the officer must provide a **reason for rejection** (e.g., missing information). 
- If **rejected**, the shipping agent representative may **review/update** the notification for a **new decision**. 
- **All decisions** (approve/reject) must be **logged** with timestamp, officer ID, and outcome for **auditing**. 

## 3. System Sequence Diagram (SSD)
**Pending** — to be aligned with aggregates `VesselVisitNotification` and `Dock`.

## 4. Sequence Diagram (SD)
**Pending** — expected orchestration:  
`OfficerUI → ApprovalAppService → VVNRepository/DockRepository → Domain Event (VVNStatusChanged)`.

## 5. Business Rules
- **BR1 — Valid states:** `in_progress` → `submitted/pending` → `approved` | `rejected`. (From VVN lifecycle in US 2.2.8/2.2.9.)  
- **BR2 — Dock assignment on approval:** `assignedDock` is **mandatory** and must be compatible with dock constraints (depth/draft/type from US 2.2.3).  
- **BR3 — Rejection reason required:** A textual **reason** is mandatory for `rejected` and visible to the agent (see US 2.2.10).  
- **BR4 — Audit trail:** Each decision records `officerId`, `decision`, `reason` (if any), and `timestamp`. 
- **BR5 — Idempotency & concurrency:** Prevent duplicate/conflicting decisions on the same `submitted` VVN.  
- **BR6 — Notification:** After any decision, notify the submitting representative via email/phone (as per rep data in US 2.2.5/2.2.6). 

## 6. Technical Requirements

### 6.1 REST API

GET /api/vessel-visits/notifications?status=pending
POST /api/vessel-visits/notifications/{vvnId}/approve
POST /api/vessel-visits/notifications/{vvnId}/reject
GET /api/vessel-visits/notifications/{vvnId}/audit


**GET /api/vessel-visits/notifications (pending by default)**  
Query: `status`, `vessel`, `imo`, `agentOrg`, `page`, `size`, `sort`.

**POST /approve — request example**
```json
{
  "assignedDockId": "dock-a1",
  "notes": "Priority slot due to schedule constraints"
}
{
  "reason": "Missing cargo manifest details"
}
{
  "id": "vvn-001",
  "status": "approved",
  "assignedDock": { "id": "dock-a1", "name": "Dock A1" },
  "decidedBy": { "id": "officer-001", "name": "Port Officer Silva" },
  "decidedAt": "2025-10-08T11:05:00Z",
  "auditId": "audit-9f23"
}
{
  "events": [
    { "type": "submitted", "at": "2025-09-26T09:15:00Z", "by": { "id": "rep-001", "role": "agent" } },
    { "type": "approved", "at": "2025-10-01T14:30:00Z", "by": { "id": "officer-001", "role": "officer" }, "details": { "assignedDockId": "dock-a1" } }
  ]
}
```

### 6.2 Domain Model

**Under Development**
> 
> The domain model for this user story is currently being developed as part of the overall system domain design. This section will be updated once the domain model is finalized.


## 7. Dependencies

### 7.1 Upstream Dependencies

- **US 2.2.3**: (Docks) — required to assign a dock. 
- **US 2.2.5**: (Organizations & Representatives) — contacts and permissions. 
- **US 2.2.6**: (Organizations & Representatives) — contacts and permissions. 
- **US 2.2.8**: (Create/Submit & Update VVN) — source of states and content. 
- **US 2.2.9**: (Create/Submit & Update VVN) — source of states and content.

### 7.2 Downstream Dependencies

- **US 2.2.10**: (Agent views status, assigned dock, or rejection reason).