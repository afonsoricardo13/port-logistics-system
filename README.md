# Port Logistics System

**English** | [Português](README.pt.md)

A port operations management platform built as the Integrative Project of the 5th semester of the Informatics Engineering degree (LEI) at ISEP, Polytechnic of Porto, academic year 2025/26.

## Overview

The system supports the day-to-day work of a commercial port. It covers the full life cycle of a vessel visit: the shipping agent notifies the arrival, the port authority approves it and assigns a dock, a daily operation plan is generated, and the actual execution is recorded and compared against the plan.

The main users are:

- **Port Authority Officers**: manage vessels, vessel types, docks, storage areas and shipping agent organisations, and review vessel visit notifications.
- **Shipping Agent Representatives**: submit and track vessel visit notifications, including cargo manifests.
- **Logistics Operators**: manage staff, qualifications and physical resources, generate operation plans and record what happens during each visit (executions, incidents, complementary tasks).
- **Administrators**: manage users, roles and the privacy policy.

## Architecture

The solution is a monorepo with three independent services and a single-page application. Services communicate only through REST APIs; no service accesses another service's database.

| Component | Path | Responsibility |
|---|---|---|
| Port Logistics API | `apps/port-logistics/backend` | Master data, vessel visit notifications, users and roles |
| Operations & Execution API (OEM) | `apps/operations-execution/backend` | Operation plans, vessel visit executions, incidents, complementary tasks |
| Scheduling service | `apps/scheduling-planning/backend` | Daily scheduling algorithms, including a genetic algorithm |
| Web client | `apps/port-logistics/frontend/web/spa` | Angular SPA with a 3D view of the port |

Both .NET services follow a layered architecture with Domain-Driven Design:

- **Domain**: aggregates, value objects and business rules
- **Application**: services, DTOs and use cases
- **DataModel**: persistence with Entity Framework Core and repositories
- **WebApi**: REST controllers, authentication and authorisation

Authentication uses Google OAuth 2.0. Authorisation is role-based, with an additional per-resource permission check.

## Tech stack

- **Back end**: .NET 9, ASP.NET Core, Entity Framework Core, MySQL 8 (Pomelo provider), Swagger / OpenAPI
- **Scheduling**: SWI-Prolog (HTTP server, heuristic and genetic algorithms), Docker
- **Front end**: Angular 20, TypeScript, Three.js, tween.js, ngx-translate
- **Testing**: xUnit, Moq, EF Core InMemory, Karma/Jasmine, Cypress
- **Infrastructure**: Azure App Service, Azure Database for MySQL, Azure Storage, Linux VMs, GitHub Actions

## Getting started

### Prerequisites

- .NET SDK 9.0
- Node.js 20 or later
- MySQL 8.0
- SWI-Prolog 9 (or Docker)

### 1. Databases

Create two MySQL databases, one per .NET service:

```sql
CREATE DATABASE absanteedb;
CREATE DATABASE oemdb;
```

Edit the connection strings in `apps/port-logistics/backend/WebApi/appsettings.Development.json` and `apps/operations-execution/backend/WebApi/appsettings.Development.json`, replacing `YOUR_DB_PASSWORD` with your MySQL password. Pending migrations of the Port Logistics API are applied automatically on start-up.

### 2. Port Logistics API

```bash
dotnet run --project apps/port-logistics/backend/WebApi --launch-profile https
```

Available at `https://localhost:7096` (Swagger at `/swagger`).

### 3. Operations & Execution API

```bash
dotnet ef database update --project apps/operations-execution/backend/DataModel --startup-project apps/operations-execution/backend/WebApi
dotnet run --project apps/operations-execution/backend/WebApi --launch-profile https
```

Available at `https://localhost:7001`.

### 4. Scheduling service

```bash
cd apps/scheduling-planning/backend
swipl -s scheduling_server.pl -g start_scheduling_server
```

Available at `http://localhost:5003`. Alternatively, build and run the provided `Dockerfile`, which serves on port 8080.

### 5. Web client

```bash
cd apps/port-logistics/frontend/web/spa
npm install
npm start
```

Available at `http://localhost:4200`.

In development, the APIs accept an `X-User-Email` header in place of a Google login, so each role can be tested from Swagger.

### Tests

```bash
dotnet test apps/port-logistics/backend/PortLogistics.sln
dotnet test apps/operations-execution/backend/OEM.sln
cd apps/port-logistics/frontend/web/spa && npm test
```

## Repository structure

```
apps/
  port-logistics/
    backend/                Port Logistics API (.NET solution and tests)
    frontend/web/spa/       Angular SPA
  operations-execution/
    backend/                OEM API (.NET solution and tests)
  scheduling-planning/
    backend/                Prolog scheduling service
docs/
  sprintA, sprintB, sprintC Requirements, domain model and C4 / 4+1 views (PlantUML)
  ASIST                     Systems administration and business continuity reports
  IARTI                     Scheduling algorithms reports
  SGRAI                     3D visualisation report
  GESTA                     Management report
  LAPR5                     GDPR documentation
.github/workflows/          CI and deployment pipelines
```

## Academic context

This is a group project developed over three sprints by a team of five students. It integrates the following courses:

- **LAPR5**: Laboratory/Project V
- **ARQSI**: Information Systems Architecture
- **SGRAI**: Graphic Systems and Interaction
- **IARTI**: Artificial Intelligence
- **ASIST**: Systems and Infrastructure Administration
- **GESTA**: Management

The original repository belongs to the course organisation and is private. This copy keeps the final code and documentation with the commit history removed, and with credentials and personal data replaced by placeholders. The 3D models in `src/assets/models` are third-party assets and remain the property of their authors.

## Team

| Member | GitHub |
|---|---|
| Afonso Ricardo | [@afonsoricardo13](https://github.com/afonsoricardo13) |
| Pedro Carriço Pereira | [@pedrocarrico6](https://github.com/pedrocarrico6) |
| _Name_ | [@JoaoSousa0655](https://github.com/JoaoSousa0655) |
| _Name_ | [@RFaria05](https://github.com/RFaria05) |
| _Name_ | [@manuzinho7](https://github.com/manuzinho7) |

## My contribution

User stories I was responsible for. Items marked *(shared)* were developed with other team members.

**Back end: Port Logistics API (Sprint A)**
- US 2.2.10: shipping agents can view the status of their vessel visit notifications
- US 2.2.11: registration and management of operating staff (design, implementation and tests)
- US 2.2.13: registration and management of qualifications *(shared)*
- US 2.2.4: registration and update of storage areas *(shared)*
- Domain model, glossary and use case diagram *(shared)*

**Front end, security and architecture (Sprint B)**
- US 3.1.1: initial set-up of the Angular SPA
- US 3.2.4: role-based restriction of actions and features
- US 3.2.5: assignment and update of user roles by administrators
- US 3.4.1: dedicated scheduling module exposing the algorithms through a REST API
- US 3.3.4: textures and visual styling of the 3D port elements

**Operations & Execution (Sprint C)**
- US 4.1.1: OEM module as an independent back-end service *(shared)*
- US 4.1.5: detection of vessel visit notifications without an operation plan, with plan regeneration for a given day
- US 4.1.10: search of vessel visit executions by period, vessel or status, with execution metrics
- US 4.1.15: recording and management of complementary tasks *(shared)*

**Algorithms and 3D visualisation (Sprint C)**
- US 4.3.1: genetic algorithm for daily operation plans, with tunable parameters and single- or multi-crane solutions
- US 4.2.6: animated camera and spotlight transitions between selected elements

**Systems administration (Sprints B and C)**
- US 3.5.4: public shared folder for registered users
- US 3.5.9: logging of remote SSH logins
- US 4.4.4: validation of access management against least-privilege policies, with access auditing
- US 4.4.9: automatic retention-based deletion of database backups
- US 4.4.11: shared network directory (SMB/CIFS or NFS)

**GDPR and management**
- US 4.5.1 to 4.5.4: privacy policy, data subject rights and the data rights area of the SPA *(shared)*
- US 2.3.7, 3.7.3 and 4.6.5: organisational strategy, management information systems and supply chain analysis
