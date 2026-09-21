# Remaining Program Foundations Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add runnable foundations for Parking.Operator, Parking.TerminalAgent, Parking.Worker, and Parking.FeeTester without implementing their deferred business features.

**Architecture:** Field UI and monitoring programs use local configuration and HTTP only; Parking.Operator calls EdgeService, Parking.TerminalAgent observes configured processes, Parking.Worker polls Parking.Api, and Parking.FeeTester calls Parking.FeeEngine with an in-memory sample configuration. All projects join the existing solution, and one Windows batch file starts the complete test stack with explicit pauses.

**Tech Stack:** .NET 9, WinForms, Worker Service, Microsoft.Extensions.Hosting, HttpClient, Parking.Contracts, Parking.FeeEngine.

**Spec:** `docs/superpowers/specs/2026-09-20-remaining-program-foundations-design.md`

## Global Constraints

- Target .NET 9 and retain PascalCase JSON contracts.
- Field programs never connect directly to central MySQL or Parking.Api.
- Store no password or connection string in source-controlled settings.
- Implement foundations only; do not add deferred payment, restart, update, aggregation, or production fee-test features.
- Per user direction, create all four foundations first, then run build and regression tests together.

## Review Focus

- An unavailable EdgeService must leave Parking.Operator running and display a visible failure state.
- A missing monitored process must be logged as stopped without starting or terminating it.
- An unavailable Parking.Api must be logged by Parking.Worker without terminating its loop.
- An exit time not later than entry time must be rejected visibly by Parking.FeeTester.
- The batch runner must fail early when `PARKING_TEST_CONNECTION` is absent and pause between every launch.

---

### Task 1: Parking.Operator

**Files:**
- Create: `src/Edge/Parking.Operator/*`
- Modify: `ParkingSystem.sln`

**Interfaces:**
- Consumes: `GET api/v1/management/status` and `Parking.Contracts.EdgeServiceStatus`.
- Produces: runnable WinForms status screen configured by `EdgeService:BaseUrl`.

- [ ] Create the project, host bootstrap, typed HTTP client, status form, and settings.
- [ ] Register the project in the solution.

### Task 2: Parking.TerminalAgent

**Files:**
- Create: `src/Edge/Parking.TerminalAgent/*`
- Modify: `ParkingSystem.sln`

**Interfaces:**
- Consumes: `TerminalAgent:MonitorIntervalSeconds` and `TerminalAgent:Programs`.
- Produces: Windows-service-capable observer that logs running/stopped process state only.

- [ ] Create the project, options, process observer loop, service bootstrap, and settings.
- [ ] Register the project in the solution.

### Task 3: Parking.Worker

**Files:**
- Create: `src/Central/Parking.Worker/*`
- Modify: `ParkingSystem.sln`

**Interfaces:**
- Consumes: Parking.Api root endpoint and `ParkingApi:BaseUrl`, `ParkingApi:CheckIntervalSeconds`.
- Produces: Windows-service-capable health polling loop with nonfatal failure logging.

- [ ] Create the project, typed HTTP client, worker loop, service bootstrap, and settings.
- [ ] Register the project in the solution.

### Task 4: Parking.FeeTester

**Files:**
- Create: `src/Tools/Parking.FeeTester/*`
- Modify: `ParkingSystem.sln`

**Interfaces:**
- Consumes: `ParkingFeeCalculator.Calculate(ParkingFeeRequest)`.
- Produces: runnable WinForms input screen and sample fee result display.

- [ ] Create the project, input form, sample fee configuration, and engine call.
- [ ] Register the project in the solution.

### Task 5: Integrated launch and verification

**Files:**
- Create: `run-foundations.bat`
- Modify: `docs/04-development-status.md`

**Interfaces:**
- Consumes: `PARKING_TEST_CONNECTION` and optional URL/data-directory environment variables.
- Produces: one batch runner that starts API, Gateway, EdgeService, Operator, TerminalAgent, Worker, and FeeTester with a pause after each stage.

- [ ] Add explicit environment defaults, connection-string mapping, separate process launches, and pauses.
- [ ] Build each new project, build the solution, and run `dotnet test ParkingSystem.sln` after all foundations exist.
- [ ] Record the completed foundation scope and launch command in the development-status document.
