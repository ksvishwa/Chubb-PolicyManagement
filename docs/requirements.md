# Chubb APAC — Take-Home Assessment: Full-Stack Developer
## Policy Management Platform


---

## Background

Chubb's APAC operations team manages insurance policies across multiple regions. They currently rely on spreadsheets and disconnected systems. Your task is to build a **Policy Management Platform** — a full-stack application comprising a BFF (Backend-for-Frontend) service and a web-based dashboard that consumes it.

This assessment evaluates your ability to work across the entire stack: API design, backend architecture, database integration, frontend component design, state management, testing, and the engineering discipline that ties it all together.

---

## Structure and Approach

This assessment is structured in two tiers:

### Tier 1: Backend Service *(Required — Start Here)*
Build the BFF service first. This is the foundation and must be completed to a production-quality standard.

### Tier 2: Frontend Dashboard *(Extend If Time Permits)*
Once the backend is solid, extend the platform with a frontend dashboard that consumes your API. The depth of frontend implementation is up to you — a well-integrated partial frontend built on a strong backend will score higher than a rushed full-stack attempt with poor quality on both sides.

> This tiered approach lets you demonstrate **depth before breadth**. A candidate who delivers an excellent backend with a functional but minimal frontend will outscore one who delivers mediocre quality across both tiers.

---

## Technology Choice

**Backend** — choose the stack you are strongest in. Both are actively used across Chubb APAC engineering:

| Stack | Platform |
|---|---|
| C# / .NET | Current stack for the OneHub platform |
| Java / Spring Boot | Stack for the Commercial Insurance platform |

**Frontend:** Angular — required across both platforms.

---

## Tier 1: Backend Service (Required)

### Contract-First API Design

The service must expose a RESTful API. You are expected to take a **contract-first approach**:

- OpenAPI 3.x (Swagger) specification defined **before** implementing endpoints
- Generate server stubs or use the contract to drive your implementation
- The contract should be the **single source of truth** for the API shape

### Core API Endpoints

| Method | Endpoint | Description |
|---|---|---|
| `GET` | `/api/v1/policies` | List policies with pagination, sorting, filtering (by status, line of business, date range, region) and free-text search |
| `GET` | `/api/v1/policies/{id}` | Get a single policy by ID |
| `PATCH` | `/api/v1/policies/flag` | Bulk flag policies for review (accepts array of policy IDs) |
| `GET` | `/api/v1/policies/summary` | Aggregated statistics — counts by status, total premium by line of business, expiring-soon count |

### Pagination and Filtering Contract

The `GET /api/v1/policies` endpoint must support:

| Parameter | Description |
|---|---|
| `page` / `size` | Pagination query parameters (with sensible defaults) |
| `sort` | Field and direction, e.g. `sort=premiumAmount,desc` |
| `status` | Enum filter: `Active`, `Expired`, `Pending`, `Cancelled` |
| `lineOfBusiness` | Enum filter: `Property`, `Casualty`, `A&H`, `Marine` |
| `region` | Region filter |
| `effectiveDateFrom` / `effectiveDateTo` | Date range filter |
| `search` | Free-text search across `policyNumber`, `policyholderName`, `underwriter` |

### Database Integration (Required)

Use a **relational database** with proper schema management via migrations.

- **SQL Server** is preferred (matches the OneHub production stack on Azure SQL)
- PostgreSQL or SQLite are acceptable for local development
- Seed the database with **200+ realistic policy records** covering all status values, lines of business, APAC regions, and a realistic spread of dates and premium amounts

#### Policy Data Schema

| Field | Type | Notes |
|---|---|---|
| `id` | UUID | Primary key |
| `policyNumber` | String | Unique, format: `POL-XXXXXX` |
| `policyholderName` | String | Realistic APAC names |
| `lineOfBusiness` | Enum | `Property`, `Casualty`, `A&H`, `Marine` |
| `status` | Enum | `Active`, `Expired`, `Pending`, `Cancelled` |
| `premiumAmount` | Decimal | Range: 1,000 – 5,000,000 |
| `currency` | String | `USD`, `SGD`, `HKD`, `AUD`, `JPY`, `THB` |
| `effectiveDate` | Date | |
| `expiryDate` | Date | |
| `region` | String | Singapore, Hong Kong, Australia, Japan, Thailand, Indonesia, Malaysia, Philippines |
| `underwriter` | String | |
| `flaggedForReview` | Boolean | Default: `false` |
| `createdAt` | Timestamp | |
| `updatedAt` | Timestamp | |

### Clean Architecture (Required)

Demonstrate clear architectural layering with **dependencies pointing inward**. API, service, domain, and infrastructure concerns should be properly separated. Infrastructure details should not leak into domain or service layers.

### Backend Test Automation (Required)

We expect production-quality engineering standards for testing. Think about what a senior engineer would expect to see in a pull request.

### Backend Cross-Cutting Concerns

A production service needs more than just endpoints. Consider:

- Logging
- Error handling
- Health checks
- Externalised configuration
- API documentation
- A runnable local setup

### Backend Bonus Areas

- Caching for summary statistics or policy listings with a clear invalidation strategy
- Kafka producer publishing flag-for-review events and a consumer listening for policy status changes, with idempotent handling

---

## Tier 2: Frontend Dashboard (Extend If Time Permits)

Build a dashboard that consumes your Tier 1 API. The depth of implementation is your call — **focus on quality over coverage**.

### Core Features

- **Policy table** — paginated, sortable, with server-side filtering and search
- **Bulk flag-for-review** action with multi-select
- **Summary statistics panel** that updates when filters are applied
- **Loading, empty, and error states** handled throughout

### Component Architecture

Composable, single-responsibility components with appropriate granularity, and a clean separation between data and presentation concerns.

### State Management

A state management approach appropriate to the scope:
- Server and client state clearly separated
- URL state used where it makes the application shareable and bookmarkable

### Theming and Design Tokens

- Light and dark themes with a user toggle
- Implementation should reflect modern design token practice
- Preferences persisted and system defaults respected

### Local Storage

Thoughtful, abstracted use of browser storage for user preferences.

### Accessibility

The dashboard must meet modern accessibility standards (**WCAG 2.1 AA**), applied with the same care as any other production requirement.

### Frontend Test Automation

We expect production-quality engineering standards for testing. Think about what a senior engineer would expect to see in a pull request.

### Frontend Bonus Areas

- Micro-Frontend architecture
- Additional testing rigour (accessibility, visual regression, end-to-end)
- Virtual scrolling for large datasets
- Internationalisation readiness

---

## Engineering Principles We Value

Across both tiers, we expect the principles a senior engineer would naturally apply:

- **DRY** (Don't Repeat Yourself)
- **SOLID** principles
- **12-factor** configuration
- **Contract-first** thinking
- **Clean code** and clear separation of concerns

Not as a checklist, but **evident in how the code is structured**.

---

## Deliverables

| Deliverable | Description |
|---|---|
| Git repository | With meaningful commit history showing your development process |
| Working platform | Can be started locally — ideally `docker-compose up` brings up backend, database, and frontend together |
| OpenAPI specification | API contract file |
| AI working journal | A prompt log or equivalent showing what you accepted, what you challenged, and what you overrode, with brief reasoning. This does not need to be polished — a running notes file committed alongside the code is fine |
| Supporting documentation | Architecture decisions, design rationale, trade-off analysis, diagrams (as appropriate) |
| 30–60 minute walkthrough | With the hiring panel |

---

## Walkthrough Format

The walkthrough is 30–60 minutes and is as important as the code itself. This is where we explore your architecture thinking, design decisions, and how you approached the problem under time pressure.

| Segment | Duration | Description |
|---|---|---|
| Your presentation | 15–20 min | Walk through your architecture across both tiers (or the backend if that is where you focused), key decisions, and demonstrate the running platform |
| Panel Q&A | 10–15 min | Technical deep-dive, "why not X?" questions, trade-off discussions |
| What would you do with more time? | 10 min | Walk us through what you would tackle next, in priority order, and how you would approach it |
| Your questions | 5 min | Anything you want to ask us |

> The panel will probe architectural decisions and AI collaboration process. Come prepared to explain every decision — including what you chose not to build, what shortcuts you took, and what you would do differently with more time.

---

## Notes

1. **This is a sprint-format assessment** — the goal is to show what is possible with AI in 2–3 hours. If you feel you need more time, you may extend to a maximum of 5 hours — but treat the 2–3 hour mark as the real target. We are not expecting a finished product — we are evaluating how much you can build, and how well, when you work with AI effectively.

2. **Prioritise ruthlessly.** Start with Tier 1 (backend) and extend to Tier 2 (frontend) only once you have a solid foundation. Most candidates will deliver a backend-only or backend-plus-minimal-frontend submission within the 2–3 hour target — that is expected and fine.

3. **Output is a key measure, but quality matters.** A well-engineered, well-prioritised submission tells us more than a sloppy complete one.

4. **AI is your primary working interface.** We expect AI tooling to drive the bulk of code generation. What we are evaluating is how you direct, challenge, and override it — not whether you used it. Document what you accepted, what you challenged, and what you overrode as you go. You sign off every line you submit; the panel will probe anything you cannot defend.

5. **The walkthrough is where your thinking is explored.** Come prepared to articulate the approaches you took and why, the shortcuts you made under time pressure, and what you would do differently or tackle next with more time. Verbal explanation counts — you don't need a polished ADR for every decision.
