# Use Case Diagram - Sprint C

## Overview

Sprint C introduces the **Operations & Execution Management (OEM)** module, which bridges the gap between operations planning and execution. This module allows the system to record actual vessel visit activities and compare them with planned schedules.

## New Actors in Sprint C

| Actor | Description |
|-------|-------------|
| Port Operations Supervisor | Responsible for managing complementary task categories and overseeing non-cargo activities |

## New User Stories - Sprint C

### US 4.1.1 - OEM Module Development
As a Project Manager, I want the team to develop the OEM module as an independent back-end service.

### US 4.1.2 - Generate Operation Plans
As a Logistics Operator, I want to automatically generate and store Operation Plans for all VVNs scheduled for a given day.

### US 4.1.3 - Search Operation Plans
As a Logistics Operator, I want to search and list Operation Plans for a given day or period.

### US 4.1.4 - Update Operation Plans
As a Logistics Operator, I want to manually update the Operation Plan of a given VVN.

### US 4.1.5 - Missing Operation Plans
As a Logistics Operator, I want to identify VVNs that do not yet have an Operation Plan.

### US 4.1.6 - Resource Allocation Query
As a Logistics Operator, I want to query resource allocation time for a given period.

### US 4.1.7 - Create VVE
As a Logistics Operator, I want to create a Vessel Visit Execution (VVE) record when a vessel arrives.

### US 4.1.8 - Update VVE Berthing
As a Logistics Operator, I want to update an in-progress VVE with berth time and dock used.

### US 4.1.9 - Update VVE Executed Operations
As a Logistics Operator, I want to update an in-progress VVE with executed operations.

### US 4.1.10 - Search VVEs
As a Logistics Operator, I want to search and list VVEs over a given period or for a specific vessel.

### US 4.1.11 - Complete VVE
As a Logistics Operator, I want to mark a VVE as completed with unberth and departure times.

### US 4.1.12 - Manage Incident Types
As a Port Authority Officer, I want to manage the catalog of Incident Types.

### US 4.1.13 - Record Incidents
As a Logistics Operator, I want to record and manage incidents affecting port operations.

### US 4.1.14 - Manage Complementary Task Categories
As a Port Operations Supervisor, I want to manage the catalog of Complementary Task Categories.

### US 4.1.15 - Record Complementary Tasks
As a Logistics Operator, I want to record and manage Complementary Tasks performed during vessel visits.

## Diagram

![Use Case Diagram](svg/UCD.svg)
