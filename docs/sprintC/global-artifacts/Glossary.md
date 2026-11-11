# Glossary - Sprint C

## New Terms - Operations & Execution Management

| Term (EN) | Description (EN) |
|:----------|:-----------------|
| Operations & Execution Management (OEM) | Independent module responsible for managing execution data of port activities, bridging planning and execution. |
| Operation Plan | A structured record containing the sequence of planned cargo operations for a VVN, including assigned resources and time windows. |
| Vessel Visit Execution (VVE) | A record of the actual execution of a vessel visit, including real arrival, berthing, operations, and departure times. |
| Executed Operation | An individual operation (loading/unloading) that has been performed, with actual start/end times and resources used. |
| Incident | An unexpected event or disruption affecting port operations (e.g., equipment failure, weather conditions). |
| Incident Type | A classification category for incidents, supporting hierarchical structuring (e.g., Environmental > Fog). |
| Incident Severity | Classification of incident impact: Minor, Major, or Critical. |
| Complementary Task | A non-cargo-related activity performed during vessel visits (e.g., inspections, maintenance, cleaning). |
| Complementary Task Category | A classification for complementary tasks (e.g., Safety, Maintenance, Cleaning). |
| Turnaround Time (TAT) | Total elapsed time from vessel arrival to departure, including all operations and waiting times. |
| Berth Occupancy Time | Time the vessel spends docked at a berth, from berthing to unberthing. |
| Waiting Time for Berthing | Time elapsed between vessel arrival at port and actual berthing. |
| Arrival Delay | Difference between expected arrival time and actual arrival time. |
| Departure Delay | Difference between expected departure time and actual departure time. |
| Blocking Task | A complementary task that suspends ongoing cargo operations until completed. |
| Parallel Task | A complementary task that runs concurrently with cargo operations without interruption. |

## Inherited Terms from Sprint B

| Term (EN) | Description (EN) |
|:----------|:-----------------|
| Administrator | User responsible for managing system accounts, permissions, and configuration. |
| Cargo Manifest | A structured list of containers submitted with a Vessel Visit notification. |
| Container | A standardized unit of cargo, measured in TEUs, identified using ISO 6346 codes. |
| Dock (Quay) | A port facility where vessels berth for loading and unloading operations. |
| ETA (Estimated Time of Arrival) | The expected arrival time of a vessel to the port. |
| ETD (Estimated Time of Departure) | The expected departure time of a vessel from the port. |
| IMO Number | Unique vessel identifier defined by the International Maritime Organization. |
| Logistics Operator | User role responsible for defining and scheduling operational tasks for approved Vessel Visits. |
| Operating Staff | Human resources allocated to operate port equipment. |
| Physical Resources | Equipment used in port operations, such as cranes, trucks, and warehouses. |
| Port Authority Officer | User role responsible for reviewing Vessel Visits and assigning docks. |
| Qualification | A skill/certification required to operate certain resources. |
| Shipping Agent | Organization representing vessel owners in port operations. |
| Shipping Agent Representative | A person acting on behalf of a Shipping Agent. |
| Storage Area | Generic port location for container storage. |
| Vessel | A ship carrying containers, identified by an IMO number. |
| Vessel Visit Notification (VVN) | A formal request submitted by a shipping agent to schedule a Vessel Visit. |

## Technical Terms

| Term (EN) | Description (EN) |
|:----------|:-----------------|
| IAM (Identity and Access Management) | External service responsible for authentication and authorization. |
| RBAC (Role-Based Access Control) | Access control model that grants permissions based on user roles. |
| ABAC (Attribute-Based Access Control) | Access control model using attributes for fine-grained permissions. |
| REST API | Architectural style for web services using HTTP methods. |
| OpenAPI / Swagger | Specification for describing REST APIs. |
| DTO (Data Transfer Object) | Structure used to transfer data between layers or services. |
| JWT (JSON Web Token) | Authentication method using tokens to assert identity. |
