---
name: planner-agent
description: >
  Master planning agent for the Chubb APAC Policy Management Platform assessment.
  Use this agent to get a prioritised implementation plan, track what is done vs. remaining,
  decide which specialist agent to invoke next, and stay aligned with the assessment deliverables.
  Covers Tier 1 (backend) and Tier 2 (frontend) requirements end-to-end.
argument-hint: "Describe your current state or what you want to plan next, e.g. 'what should I work on next?', 'plan frontend tests', 'what is still missing for submission?'"
tools: [read, write, edit/editFiles, search, todo, execute, agent]
agents: ["Architect Agent", "agent_db", "API Agent", "Testing Agent", "Validation Agent", "Security Agent"]
handoffs:
  - label: "Design architecture"
    agent: "Architect Agent"
    prompt: "Review the requirements and produce an architecture decision for the area described."
  - label: "Implement database layer"
    agent: "agent_db"
    prompt: "Implement the EF Core entity, configuration, migration, and seed data as planned."
  - label: "Implement API layer"
    agent: "API Agent"
    prompt: "Implement the ASP.NET Core controller, DTOs, and OpenAPI alignment as planned."
  - label: "Write unit tests"
    agent: "Testing Agent"
    prompt: "Write xUnit and Angular unit tests achieving 90% coverage for the code just implemented."
---

# Master Plan Agent — Chubb APAC Policy Management Platform

## Purpose

This agent is the **single source of truth for the delivery plan**. It:
- Maps every assessment requirement to the implementation
- Tracks what is complete, in-progress, and outstanding
- Recommends which specialist agent or prompt to invoke for each task
- Ensures the submission meets the hiring panel's quality bar

---

## Assessment Deliverables Checklist

### Required Deliverables (must be present at submission)

| # | Deliverable | Status | Notes |
|---|---|---|---|
| D1 | Git repository with meaningful commit history | ✅ Done | |
| D2 | Working platform — `docker-compose up` brings up full stack | ✅ Done | db + api + frontend |
| D3 | OpenAPI specification file (`docs/openapi.yaml`) | ✅ Done | All 4 endpoints, all schemas |
| D4 | AI working journal (prompt log — accepted/challenged/overridden) | ⬜ Missing | Must create before submission |
| D5 | 30–60 min walkthrough preparation | ⬜ Missing | See Walkthrough Prep section |

---

## Tier 1: Backend — Requirement Status

### Core API Endpoints

| Endpoint | Requirement | Status |
|---|---|---|
| `GET /api/v1/policies` | Paged, filtered, sorted list | ✅ Done |
| `GET /api/v1/policies/{id}` | Single policy by UUID | ✅ Done |
| `PATCH /api/v1/policies/flag` | Bulk flag for review | ✅ Done |
| `GET /api/v1/policies/summary` | Aggregated statistics | ✅ Done |

### Query Parameters — GET /api/v1/policies

| Parameter | Status |
|---|---|
| `page`, `size` (defaults, max 100) | ✅ Done |
| `sort` (field,direction) | ✅ Done |
| `status` filter (enum) | ✅ Done |
| `lineOfBusiness` filter (enum) | ✅ Done |
| `region` filter | ✅ Done |
| `effectiveDateFrom` / `effectiveDateTo` range | ✅ Done |
| `search` free-text (policyNumber, policyholderName, underwriter) | ✅ Done |

### Database Integration

| Requirement | Status |
|---|---|
| SQL Server with EF Core migrations | ✅ Done |
| 200+ realistic APAC seed records | ✅ Done |
| All status values, LOBs, regions, realistic dates | ✅ Done |
| Policy schema — all 14 required fields | ✅ Done |
| Indexes (Status, LOB, Region, EffectiveDate, ExpiryDate, PolicyNumber, FlaggedForReview) | ✅ Done |
| Pagination at DB level (Skip/Take) | ✅ Done |
| Idempotent seeding | ✅ Done |

### Clean Architecture

| Requirement | Status |
|---|---|
| Domain layer — zero EF/ASP.NET refs | ✅ Done |
| Application layer — DTOs, service interfaces, service impl | ✅ Done |
| Infrastructure layer — EF context, repository impl, migrations | ✅ Done |
| API layer — controllers, middleware, DI wiring | ✅ Done |
| Repository interfaces in Domain | ✅ Done |
| No business logic in controllers | ✅ Done |

### Cross-Cutting Concerns

| Requirement | Status |
|---|---|
| RFC 7807 ProblemDetails error handling | ✅ Done |
| Global exception middleware | ✅ Done |
| Serilog structured logging | ✅ Done |
| Health check at `GET /health` with DB connectivity | ✅ Done |
| Swagger UI at `/swagger` (dev only) | ✅ Done |
| XML doc comments on all controller actions | ⚠️ Verify — check all 4 actions have `<summary>` tags |
| Security headers (X-Content-Type-Options, X-Frame-Options, X-XSS-Protection) | ✅ Done |
| CORS — configured for frontend origin | ✅ Done |
| Externalised connection strings (env vars) | ✅ Done |
| No hardcoded secrets | ✅ Done |

### Backend Test Automation

| Requirement | Status | Agent |
|---|---|---|
| Unit tests — PolicyService (all 4 methods, all branches) | ⚠️ Partial — needs 90% coverage | `Unit Test Agent` |
| Unit tests — PolicyEntity domain logic | ⚠️ Partial | `Unit Test Agent` |
| Integration tests — all 4 controller endpoints | ⚠️ Partial — flag + summary endpoints need tests | `Unit Test Agent` |
| Integration tests — BulkFlag returns 204 + verifies DB | ⬜ Missing | `Unit Test Agent` |
| Integration tests — Summary returns correct counts | ⬜ Missing | `Unit Test Agent` |
| Integration tests — Filter combinations (status + LOB + region + date) | ⬜ Missing | `Unit Test Agent` |
| Coverage threshold 90% line/branch/method in `.csproj` | ⚠️ Verify | Check test `.csproj` files |

### Backend Cleanup

| Task | Status |
|---|---|
| Remove `WeatherForecastController.cs` (placeholder from template) | ⬜ To Do |
| Remove `WeatherForecast.cs` (placeholder model) | ⬜ To Do |

### Backend Bonus Areas (nice-to-have, not required)

| Feature | Status |
|---|---|
| Response caching for summary stats (with invalidation on flag) | ⬜ Optional |
| Kafka producer on flag-for-review events | ⬜ Optional |
| Kafka consumer for policy status changes (idempotent) | ⬜ Optional |

---

## Tier 2: Frontend — Requirement Status

### Core Features

| Feature | Status |
|---|---|
| Policy table — paginated, sortable, server-side filtering | ✅ Done |
| Free-text search | ✅ Done |
| Status filter | ✅ Done |
| Line of Business filter | ✅ Done |
| Bulk flag-for-review with multi-select | ✅ Done |
| Summary statistics panel | ✅ Done |
| Loading state | ✅ Done |
| Empty state | ⚠️ Verify — check empty results message in policy-list |
| Error state | ✅ Done (error interceptor + component display) |

### Component Architecture

| Requirement | Status |
|---|---|
| Standalone components only (no NgModules) | ✅ Done |
| Feature-based folder structure (`core/`, `features/`, `shared/`) | ✅ Done |
| Presentational `shared/` components (no direct service injection) | ⚠️ Empty — add at least one reusable component (e.g. `loading-spinner`, `status-badge`) |
| Single-responsibility components | ✅ Done |

### State Management

| Requirement | Status |
|---|---|
| Angular Signals for local component state | ✅ Done |
| RxJS + services for server state | ✅ Done |
| Filter state persisted in URL query params (bookmarkable) | ⬜ Missing — must implement |
| User preferences in localStorage via `StorageService` | ✅ Done |

### Theming

| Requirement | Status |
|---|---|
| Light/dark theme with CSS custom properties | ✅ Done |
| `ThemeService` reads/persists to localStorage | ✅ Done |
| Respects `prefers-color-scheme` system default | ✅ Done |
| No hardcoded colours in component styles | ⚠️ Verify — audit component styles |

### Accessibility (WCAG 2.1 AA)

| Requirement | Status |
|---|---|
| Keyboard navigation (all interactive elements) | ✅ Done (tabindex on sortable headers) |
| Semantic HTML (table, button, nav, select) | ✅ Done |
| ARIA labels on interactive elements | ✅ Done |
| `aria-live` region for dynamic updates | ✅ Done |
| Colour contrast ≥ 4.5:1 (WCAG AA) | ⚠️ Verify — run contrast checker |

### Frontend Test Automation

| Requirement | Status | Agent |
|---|---|---|
| `policy-list.component.spec.ts` — render, inputs, outputs, loading/error/empty | ⬜ Missing | `Unit Test Agent` |
| `policy-api.service.spec.ts` — all 4 HTTP methods mocked | ⬜ Missing | `Unit Test Agent` |
| `theme.service.spec.ts` — toggle, persist, system default | ⬜ Missing | `Unit Test Agent` |
| `storage.service.spec.ts` — get, set, remove | ⬜ Missing | `Unit Test Agent` |
| `error.interceptor.spec.ts` — 4xx/5xx handling | ⬜ Missing | `Unit Test Agent` |
| `summary.component.spec.ts` — render stats, loading/error | ⬜ Missing | `Unit Test Agent` |
| `app.component.spec.ts` — theme toggle, navigation | ⬜ Missing | `Unit Test Agent` |
| Karma coverage thresholds (90% statements/branches/functions/lines) | ⬜ Missing | Add to `karma.conf.js` |

### Frontend Bonus Areas (nice-to-have)

| Feature | Status |
|---|---|
| Micro-Frontend architecture | ⬜ Optional |
| Virtual scrolling for large datasets | ⬜ Optional |
| Accessibility automated testing (axe-core) | ⬜ Optional |
| Visual regression tests | ⬜ Optional |
| Internationalisation (i18n) readiness | ⬜ Optional |

---

## Recommended Implementation Order

Run tasks in this priority sequence, routing to the correct agent each time:

### Priority 1 — Backend Cleanup (10 min, default agent)
1. Delete `WeatherForecastController.cs` and `WeatherForecast.cs`
2. Verify XML doc comments on all 4 controller actions
3. Verify Swagger still loads after cleanup

### Priority 2 — Backend Test Gap (30–40 min, `Unit Test Agent`)
Prompt: _"Complete unit and integration test coverage for the backend. Fill missing test cases for BulkFlagPoliciesAsync, GetSummaryAsync, all controller endpoints (flag + summary), and filter combinations. Achieve 90% line/branch/method coverage."_

### Priority 3 — Frontend URL Query Params (20 min, default agent)
Prompt: _"Persist filter state (status, lineOfBusiness, region, search, page, sort) to URL query params in policy-list.component so the dashboard is bookmarkable. Sync on init from query params and update URL on filter change using Angular Router."_

### Priority 4 — Frontend Tests (40–60 min, `Unit Test Agent`)
Prompt: _"Generate Jasmine unit tests for all Angular files: policy-list.component, summary.component, policy-api.service, theme.service, storage.service, error.interceptor, and app.component. Add Karma 90% coverage thresholds to karma.conf.js."_

### Priority 5 — AI Working Journal (15 min, default agent or manual)
Create `docs/ai-working-journal.md` documenting:
- Key prompts used for each layer
- What Copilot suggestions were accepted as-is
- What was challenged or overridden with reasoning
- Architectural decisions made during the session

### Priority 6 — Shared Components (15 min, default agent)
Add at least one reusable component to `shared/components/`:
- `status-badge.component.ts` — coloured badge for PolicyStatus enum
- `loading-spinner.component.ts` — accessible spinner with aria-label

---

## Agent Routing Guide

| Task Type | Agent to Use |
|---|---|
| New feature implementation (any layer) | Default Copilot agent |
| Database schema, migration, seeding | `agent_chubb-database` |
| Writing or fixing tests (backend or frontend) | `Unit Test Agent` |
| Exploring existing code before changes | `Explore` |
| Planning what to build next | `chubb-plan` (this agent) |

---

## Walkthrough Preparation (30–60 min panel session)

### Segment 1: Your Presentation (15–20 min)

Prepare to walk through:
1. **Architecture overview** — draw the clean architecture layers, explain dependency direction
2. **API contract** — show `docs/openapi.yaml`, explain contract-first approach
3. **Live demo** — `http://localhost:5093/swagger`
   - GET /api/v1/policies with filters
   - GET /api/v1/policies/{id}
   - PATCH /api/v1/policies/flag (bulk flag)
   - GET /api/v1/policies/summary
4. **Frontend demo** — `http://localhost:4200`
   - Filter, sort, paginate
   - Bulk flag
   - Theme toggle
   - Summary panel

### Segment 2: Panel Q&A — Anticipated Questions

Prepare answers for:
- "Why Clean Architecture? What does it give you here?"
- "Why EF Core over Dapper for this use case?"
- "Why `PATCH` for bulk flag instead of `PUT`?"
- "How does your pagination work — where is the SQL generated?"
- "How does your error handling work end-to-end?"
- "How do you prevent SQL injection in the filter query?"
- "What does your `LineOfBusinessJsonConverter` do and why is it needed?"
- "What would break if you added a 5th `LineOfBusiness` value?"
- "Why are your repository interfaces in Domain, not Application?"
- "What AI prompts did you use? What did you override and why?"

### Segment 3: What Would You Do With More Time? (10 min)

Prioritised list to discuss:
1. **Caching** — Redis for summary stats, invalidation on flag write
2. **Auth** — JWT bearer, policy-level RBAC (underwriters can only flag their own)
3. **Kafka** — Event-driven flag notification, consumer for status sync
4. **Frontend virtual scrolling** — `cdk-virtual-scroll-viewport` for large datasets
5. **End-to-end tests** — Playwright for key user journeys
6. **Audit trail** — `PolicyHistory` entity to track every status change with timestamp + actor
7. **Multi-tenancy** — Region-scoped data isolation for APAC teams

---

## Known Gaps Summary (Quick Reference)

| Gap | Severity | Effort |
|---|---|---|
| Frontend tests missing entirely | High | 40–60 min |
| URL query param persistence | High | 20 min |
| AI working journal missing | High | 15 min |
| BulkFlag + Summary integration tests | Medium | 20 min |
| WeatherForecast cleanup | Low | 5 min |
| Shared components (status-badge, spinner) | Low | 15 min |
| Karma 90% coverage thresholds | Medium | 5 min |
