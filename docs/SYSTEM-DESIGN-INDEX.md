# System Design Documentation — Index & Navigation

**Version:** 1.0  
**Last Updated:** 2026-06-09  
**Status:** Active

---

## Quick Navigation

Welcome to the comprehensive system design documentation for the **Chubb APAC Policy Management Platform**. This index helps you navigate all design documents based on your role and needs.

---

## 📋 Documents Overview

### 1. [High-Level Design (HLD)](./SYSTEM-DESIGN-HLD.md)
**Audience:** Architects, Technical Leads, Team Managers  
**Read Time:** 30-45 minutes  
**Content:**
- System overview and mission
- Clean Architecture layers
- Component interactions
- Technology stack rationale
- Data flow & processing
- Security architecture
- Scalability & performance
- Deployment architecture

**Start here if:** You want a comprehensive overview of the entire system architecture.

---

### 2. [Low-Level Design (LLD)](./SYSTEM-DESIGN-LLD.md)
**Audience:** Developers, Software Engineers  
**Read Time:** 45-60 minutes  
**Content:**
- Domain layer design (entities, enums, exceptions)
- Application layer design (services, DTOs, validators)
- Infrastructure layer design (EF Core, repositories, migrations)
- API layer design (controllers, middleware)
- Data model & entity design
- Algorithms & business logic
- Error handling & resilience
- Testing strategy with examples

**Start here if:** You need to implement features or understand the codebase structure.

---

### 3. [Architecture Decision Records (ADRs)](./ARCHITECTURE-DECISION-RECORDS.md)
**Audience:** Developers, Technical Leads  
**Read Time:** 30-40 minutes  
**Content:**
- ADR-001: Clean Architecture
- ADR-002: Contract-First API Design
- ADR-003: EF Core Code-First Migrations
- ADR-004: Async/Await Throughout
- ADR-005: RFC 7807 Problem Details Error Handling
- ADR-006: Immutable DTOs with C# Records
- ADR-007: Repository Pattern with Dependency Injection
- ADR-008: Pagination at Database Level
- ADR-009: JWT Authentication
- ADR-010: Angular Standalone Components

**Start here if:** You want to understand the "why" behind key architectural decisions.

---

### 4. [Data Model & Database Design](./DATA-MODEL-DATABASE-DESIGN.md)
**Audience:** Database Architects, Backend Developers  
**Read Time:** 20-30 minutes  
**Content:**
- Data model overview
- Entity Relationship Diagram (ERD)
- Policy entity details & column specifications
- Indexes & query performance
- Seed data strategy
- Constraints & validation
- Audit & compliance
- Scalability & growth roadmap

**Start here if:** You need to understand the data schema or optimize database performance.

---

### 5. [Deployment & Infrastructure Design](./DEPLOYMENT-INFRASTRUCTURE-DESIGN.md)
**Audience:** DevOps Engineers, System Administrators, Backend Developers  
**Read Time:** 30-40 minutes  
**Content:**
- Three-tier deployment architecture (dev/staging/prod)
- Local development setup with Docker Compose
- Docker & containerization (Dockerfile, multi-stage builds)
- CI/CD pipeline (GitHub Actions workflow)
- Environment configuration management
- Monitoring & observability (Application Insights, Serilog)
- Security hardening (SSL/TLS, Key Vault, NSG)
- Disaster recovery & backup strategy

**Start here if:** You need to deploy, configure, or monitor the application.

---

### 6. [API Design & Contract Documentation](./API-DESIGN-CONTRACT.md)
**Audience:** Frontend Developers, API Consumers, Backend Developers  
**Read Time:** 25-35 minutes  
**Content:**
- API overview (base URL, style, format)
- Authentication & authorization (JWT Bearer tokens)
- Request/response format & headers
- Endpoints specification (5 core endpoints)
- Error handling (RFC 7807 Problem Details)
- Pagination (limit-offset, constraints)
- Filtering & sorting (supported filters, sort fields)
- Rate limiting (tiered policy)
- Versioning strategy

**Start here if:** You're developing a client or integrating with the API.

---

## 🗺️ Document Relationships

```
┌──────────────────────────────────────┐
│        HLD (System Overview)          │
│  - Architecture layers                │
│  - Component interactions             │
│  - Technology stack                   │
└──────────────┬───────────────────────┘
               │
       ┌───────┴────────┐
       │                │
       ▼                ▼
┌──────────────────────┐  ┌─────────────────────────┐
│   LLD               │  │   ADRs                  │
│ (Implementation)    │  │ (Decision Rationale)    │
│ - Services          │  │ - Clean Architecture    │
│ - Entities          │  │ - Contract-First API    │
│ - Repositories      │  │ - JWT Auth              │
└──────────────────────┘  └─────────────────────────┘
       │                          │
       ├──────────┬───────────────┘
       │          │
       ▼          ▼
┌───────────────────────────────────┐
│  Data Model & DB Design           │
│  - Entity specifications          │
│  - Indexes & performance          │
│  - Constraints & validation       │
│  - Scalability roadmap            │
└───────────────────────────────────┘
       │
       ├──────────────────────────────┐
       │                              │
       ▼                              ▼
┌──────────────────────┐  ┌───────────────────────────┐
│ Deployment &         │  │  API Design & Contract    │
│ Infrastructure       │  │  - Endpoints              │
│ - Docker            │  │  - Authentication         │
│ - CI/CD             │  │  - Error handling         │
│ - Monitoring        │  │  - Pagination/filtering   │
└──────────────────────┘  └───────────────────────────┘
```

---

## 👥 Role-Based Navigation

### 🏗️ Solution Architect / Tech Lead
**Goal:** Understand overall system design and justify architectural decisions.

**Reading Order:**
1. HLD (30 min) — Get comprehensive overview
2. ADRs (30 min) — Understand key decisions & trade-offs
3. Deployment & Infrastructure (20 min) — Know operational concerns

**Time:** ~80 minutes

---

### 👨‍💻 Backend Developer
**Goal:** Implement features following the codebase architecture.

**Reading Order:**
1. HLD (20 min) — Understand layers & dependencies
2. LLD (45 min) — Deep dive into services, DTOs, repositories
3. Data Model (15 min) — Know entity schema & relationships
4. ADRs (20 min) — Reference decisions while implementing

**Time:** ~100 minutes

---

### 🎨 Frontend Developer
**Goal:** Integrate with the API and understand data contracts.

**Reading Order:**
1. API Design & Contract (30 min) — Know endpoints, requests, responses
2. HLD (15 min, skim) — Understand backend structure
3. ADR-002 & ADR-010 (10 min) — Contract-first & standalone components

**Time:** ~55 minutes

---

### 🚀 DevOps / Infrastructure Engineer
**Goal:** Deploy, configure, and monitor the application.

**Reading Order:**
1. Deployment & Infrastructure (40 min) — Full details
2. HLD (10 min, skim) — Context on architecture
3. Data Model (5 min, skim) — Database considerations

**Time:** ~55 minutes

---

### 🗄️ Database Engineer
**Goal:** Optimize data model, indexes, and query performance.

**Reading Order:**
1. Data Model & Database Design (25 min) — Entity specs, indexes, constraints
2. ADR-003 & ADR-008 (10 min) — Migration strategy & pagination
3. LLD (20 min, skim) — Understand business logic & queries

**Time:** ~55 minutes

---

### 🔐 Security Engineer
**Goal:** Verify security architecture and compliance.

**Reading Order:**
1. HLD — Security Architecture section (10 min)
2. Deployment & Infrastructure — Security Hardening section (15 min)
3. API Design & Contract — Authentication section (10 min)
4. ADR-009 (10 min) — JWT authentication details

**Time:** ~45 minutes

---

## 📊 Key Metrics at a Glance

### Architecture
| Metric | Value |
|---|---|
| **Layers** | 4 (Domain, Application, Infrastructure, API) |
| **Entities** | 1 (Policy; extensible to 5+ in future) |
| **Endpoints** | 5 core (list, detail, flag, summary, health) |
| **Auth Method** | JWT Bearer tokens |

### Performance
| Metric | Target |
|---|---|
| **API Response Time (P95)** | < 200ms |
| **Database Query Time** | < 50ms (with indexes) |
| **Page Load** | < 3 seconds (frontend) |
| **Concurrent Users** | 1000+ |

### Data
| Metric | Value |
|---|---|
| **Initial Records** | 200+ policies |
| **Growth Phase 1** | 10K-100K records |
| **Growth Phase 2** | 100K-1M records |
| **Key Indexes** | 8 (status, region, LOB, dates, flags) |

### Deployment
| Metric | Value |
|---|---|
| **Environments** | 3 (dev, staging, prod) |
| **Containerization** | Docker + Docker Compose |
| **CI/CD** | GitHub Actions |
| **Database** | SQL Server 2022 / Azure SQL |

### Testing
| Metric | Target |
|---|---|
| **Code Coverage** | 90% minimum |
| **Unit Tests** | All public methods |
| **Integration Tests** | All endpoints |

---

## 🔗 External References

### Standards & Specifications
- **OpenAPI 3.x** — [swagger.io](https://swagger.io)
- **RFC 7807 Problem Details** — [tools.ietf.org/html/rfc7807](https://tools.ietf.org/html/rfc7807)
- **Clean Architecture** — [Uncle Bob's Blog](https://blog.cleancoder.com)
- **WCAG 2.1 Accessibility** — [w3.org/WAI/WCAG21](https://www.w3.org/WAI/WCAG21/)

### Technology Documentation
- **.NET 8** — [microsoft.com/dotnet](https://microsoft.com/dotnet)
- **Entity Framework Core** — [microsoft.com/en-us/p/entity-framework](https://microsoft.com/en-us/p/entity-framework)
- **Angular 17+** — [angular.io](https://angular.io)
- **Docker** — [docker.com/resources/what-container](https://docker.com/resources/what-container)
- **GitHub Actions** — [github.com/features/actions](https://github.com/features/actions)

---

## ❓ Common Questions

### Q: Where should I start if I'm new to this project?
**A:** Start with [HLD](./SYSTEM-DESIGN-HLD.md) for a 30-minute overview, then read the section relevant to your role (see Role-Based Navigation above).

---

### Q: How do I implement a new feature?
**A:** 
1. Read [LLD](./SYSTEM-DESIGN-LLD.md) — understand service & repository patterns
2. Read [Data Model](./DATA-MODEL-DATABASE-DESIGN.md) — know entity schema
3. Implement following Clean Architecture layers (Domain → Application → Infrastructure → API)
4. Write tests targeting 90% coverage (see [Code Coverage instructions](../.github/instructions/code-coverage.instructions.md))

---

### Q: Where is the API specification?
**A:** See [API Design & Contract](./API-DESIGN-CONTRACT.md) for full endpoint specifications, authentication, error handling, and examples.

---

### Q: How do I deploy the application?
**A:** See [Deployment & Infrastructure](./DEPLOYMENT-INFRASTRUCTURE-DESIGN.md) for local development, Docker Compose, CI/CD pipeline, and cloud deployment instructions.

---

### Q: What are the key architectural decisions?
**A:** See [Architecture Decision Records](./ARCHITECTURE-DECISION-RECORDS.md) for 10 key decisions with rationale, consequences, and alternatives.

---

### Q: How do I optimize database performance?
**A:** See [Data Model & Database Design](./DATA-MODEL-DATABASE-DESIGN.md) — specifically the Indexes & Query Performance section. Also reference [ADR-008](./ARCHITECTURE-DECISION-RECORDS.md#adr-008-pagination-at-database-level) on pagination strategy.

---

### Q: How is error handling done?
**A:** See [ADR-005](./ARCHITECTURE-DECISION-RECORDS.md#adr-005-rfc-7807-problem-details-error-handling) and [API Design — Error Handling](./API-DESIGN-CONTRACT.md#error-handling) for RFC 7807 Problem Details implementation.

---

### Q: How does authentication work?
**A:** See [ADR-009](./ARCHITECTURE-DECISION-RECORDS.md#adr-009-jwt-authentication) for JWT strategy and [API Design — Authentication](./API-DESIGN-CONTRACT.md#authentication--authorization) for implementation details.

---

## 📝 Document Maintenance

**Last Updated:** 2026-06-09  
**Next Review:** 2026-09-09 (quarterly)  
**Owner:** Architecture Team  
**Status:** Active

### How to Update Documentation
1. Make changes to relevant `.md` file
2. Update "Last Updated" date
3. Add change summary to git commit message
4. PR review by tech lead before merge

### Versioning
- **V1.0** — Initial documentation (2026-06-09)
- **V1.1** — Planned for Phase 2 (100K+ records)
- **V2.0** — Major architecture changes (future)

---

## 🙌 Contributing to Documentation

This documentation is a **living document**. As the platform evolves:

- **Errors/Typos?** Submit a pull request with corrections
- **Outdated sections?** Flag for update in team retrospective
- **Missing information?** Add it and get reviewed by tech lead

---

## 📞 Contact & Support

- **Architect Questions?** → @tech-lead-slack
- **API Questions?** → #api-design channel
- **Database Questions?** → @database-team
- **Deployment Questions?** → #devops channel

---

**Happy reading! 📚**
