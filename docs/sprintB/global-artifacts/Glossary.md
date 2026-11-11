| ***TEA*** (EN)                      | ***Description*** (EN)                                                                                     |
| :---------------------------------- | :--------------------------------------------------------------------------------------------------------- |
| Administrator                       | User responsible for managing system accounts, permissions, and configuration of general parameters.       |
| Bay                                 | A lengthwise section of a vessel where containers are stored.                                              |
| Cargo Manifest                      | A structured list of containers (IDs, contents, and positions) submitted with a Vessel Visit notification. |
| Container                           | A standardized unit of cargo, measured in TEUs, identified using ISO 6346 codes.                           |
| Container Yard                      | A dedicated port area for temporary storage and stacking of containers.                                    |
| Crew Information                    | Basic data about the vessel crew, including captain name and crew count; required in visit notifications.  |
| Dangerous Cargo (HAZMAT)            | Hazardous materials requiring special handling and presence of designated safety officers.                 |
| Dock (Quay)                         | A port facility where vessels berth for loading and unloading operations.                                  |
| Dock Assignment Algorithm           | Algorithm used by the Port Authority to assign docks to vessels, optimizing resources and schedules.       |
| ETA (Estimated Time of Arrival)     | The expected arrival time of a vessel to the port.                                                         |
| ETD (Estimated Time of Departure)   | The expected departure time of a vessel from the port.                                                     |
| Feeder Vessel                       | A small vessel serving regional routes and connecting to larger ports.                                     |
| Human Resources                     | Qualified staff (e.g., crane operators, drivers, planners) required to operate physical resources.         |
| IMO Number                          | Unique vessel identifier defined by the International Maritime Organization.                               |
| Loading Manifest                    | A manifest listing containers to be loaded onto a vessel.                                                  |
| Logistics Operator                  | User role responsible for defining and scheduling operational tasks for approved Vessel Visits.            |
| Operating Staff                     | Human resources allocated to operate port equipment, such as crane operators or truck drivers.             |
| Operational Capacity                | The performance measure of a resource (e.g., containers/hour for cranes, containers/trip for trucks).      |
| Operational Window                  | The defined periods when resources or staff are available for operations.                                  |
| Panamax Vessel                      | A vessel of maximum size able to pass through the old Panama Canal (~5,000 TEUs).                          |
| Physical Resources                  | Equipment used in port operations, such as cranes, trucks, and warehouses.                                 |
| Port Authority Officer              | User role responsible for reviewing Vessel Visits, assigning docks, and overseeing compliance.             |
| Post-Panamax Vessel                 | A vessel larger than Panamax, requiring deeper berths and more cranes.                                     |
| Qualification                       | A skill/certification (such as STS Crane Operator) required to operate certain resources.                  |
| Resource Allocation Algorithm       | Algorithm used to assign cranes, trucks, staff, and other resources to tasks during Vessel Visits.         |
| Resource Status                     | Operational state of a resource (e.g., active, inactive, under maintenance).                               |
| Row                                 | A container section across the vessel’s width.                                                             |
| Serial Number (Container)           | Six-digit number in ISO 6346 code uniquely identifying a container.                                        |
| Setup Time                          | Preparation time required before a resource becomes operational (e.g., refueling a truck).                 |
| Shipping Agent                      | Organization representing vessel owners in port operations, authorized to submit Vessel Visits.            |
| Shipping Agent Representative       | A person acting on behalf of a Shipping Agent to interact with the system.                                 |
| Simulation                          | A test run of port operations, including scheduling, cargo handling, and resource allocation.              |
| Storage Area                        | Generic port location for container storage (yard or warehouse), with capacity and current occupancy.      |
| STS Crane (Ship-to-Shore)           | Large fixed crane at docks, used to load/unload containers from vessels.                                   |
| Task Sequencing                     | The order in which operational tasks (e.g., unloading, moving, stacking) are performed.                    |
| TEU (Twenty-foot Equivalent Unit)   | Standard unit of container measurement based on a 20-foot container.                                       |
| Tier                                | A vertical stack of containers on a vessel, above or below deck.                                           |
| Truck (Terminal Tractor)            | Vehicle used to transport containers between docks, yards, and warehouses.                                 |
| Ultra Large Container Vessel (ULCV) | Vessel capable of carrying more than 18,000 containers.                                                    |
| Unloading Manifest                  | A manifest listing containers to be offloaded from a vessel.                                               |
| Vessel                              | A ship carrying containers, identified by an IMO number and categorized by size/type.                      |
| Vessel Visit                        | The planned arrival and departure of a vessel, including cargo handling and resource allocation.           |
| Vessel Visit Notification           | A formal request submitted by a shipping agent to announce and schedule a Vessel Visit.                    |
| Vessel Visit Status                 | Lifecycle of a visit notification (e.g., in progress, submitted/pending, approved with dock, rejected).    |
| Warehouse                           | A port facility for cargo requiring inspection, customs clearance, or special handling.                    |
| Yard Gantry Crane                   | Mobile crane used for stacking and moving containers within a yard.                                        |
| API Gateway                         | Component that centralizes and exposes APIs, handling routing, authentication, and rate limiting.          |
| Webhook                             | Mechanism to notify external systems via HTTP callbacks when specified events occur.                       |
| OpenAPI / Swagger                   | Specification and tooling for describing REST APIs and generating documentation and clients.               |
| DTO (Data Transfer Object)          | Structure used to transfer data between layers or services, decoupling domain models from API contracts.  |
| Idempotence                         | Property of an operation that allows it to be applied multiple times without changing the result.         |
| Rate Limiting                       | Control mechanism that limits the number of requests a client can make in a given time window.           |
| Pagination (cursor / offset)        | Techniques to return result sets in pages using either a cursor or an offset for large lists.             |
| JWT Authentication                  | Authentication method using JSON Web Tokens to assert the identity of a caller.                           |
| RBAC (Role-Based Access Control)    | Access control model that grants permissions based on user roles.                                         |
| Saga                                | Pattern for managing distributed transactions and ensuring eventual consistency across services.          |
| CQRS                                | Command Query Responsibility Segregation; separation of read and write models for scalability and clarity. |
| Event Sourcing                      | Persisting state changes as a sequence of events instead of storing only the current state.               |
| Gate-In / Gate-Out                  | Moments when a truck or vehicle enters or leaves the terminal; used in yard/terminal operations.         |
| Berth Window                        | Time window allocated for a vessel to arrive and berth at a dock.                                        |
| Turnaround Time (TAT)               | Total elapsed time to complete port operations for a vessel visit.                                       |
| Reefer                              | Refrigerated container used for temperature-sensitive cargo.                                             |
| Gross Tonnage (GT)                  | Measurement of a vessel's internal volume used for regulations and port fees.                            |
| Stevedoring                         | Activity of loading and unloading cargo from ships at the quayside.                                      |
