# Data Model & Database Design

**Version:** 1.0  
**Last Updated:** 2026-06-09  
**Status:** Active

---

## Table of Contents

1. [Data Model Overview](#data-model-overview)
2. [Entity Relationship Diagram](#entity-relationship-diagram)
3. [Policy Entity Details](#policy-entity-details)
4. [Indexes & Query Performance](#indexes--query-performance)
5. [Seed Data](#seed-data)
6. [Constraints & Validation](#constraints--validation)
7. [Audit & Compliance](#audit--compliance)
8. [Scalability & Growth](#scalability--growth)

---

## Data Model Overview

The data model is intentionally simple for this MVP:

| Entity | Purpose | Rows | Status |
|---|---|---|---|
| `Policies` | Core policy records across APAC regions | 200+ initial | Active |

### Design Principles

1. **Normalization** — Single-table design; minimal joins; all data denormalized into `Policies`
2. **Audit Trail** — `CreatedAt` and `UpdatedAt` timestamps immutable
3. **Soft Deletes** — No hard deletes; logical deletion via status transition (Cancelled)
4. **Immutability** — Core fields (`PolicyNumber`) never change; only status/flags updated
5. **Scalability** — Schema designed for millions of rows with proper indexing

---

## Entity Relationship Diagram

```
┌────────────────────────────────────────────────────────────────┐
│  Policies (Core Table)                                         │
├────────────────────────────────────────────────────────────────┤
│ PK  Id: Guid                                                   │
│ UX  PolicyNumber: string(50)                                   │
│     PolicyholderName: string(100)                              │
│     Underwriter: string(100)                                   │
│     PremiumAmount: decimal(18,2)                               │
│     Currency: string(3) default='SGD'                          │
│ IX  Status: enum (Pending, Active, Expired, Cancelled)        │
│ IX  LineOfBusiness: enum (Property, Casualty, A&H, Marine)    │
│ IX  Region: string(50)                                         │
│ IX  EffectiveDate: DateTime                                    │
│ IX  ExpiryDate: DateTime                                       │
│ IX  FlaggedForReview: bool                                     │
│     ReviewReason: string(500) nullable                         │
│     CreatedAt: DateTime default=GETUTCDATE()                   │
│     UpdatedAt: DateTime default=GETUTCDATE()                   │
└────────────────────────────────────────────────────────────────┘
```

**Notes:**
- Single table design — all policy attributes in one row
- Audit fields (`CreatedAt`, `UpdatedAt`) track data changes
- `FlaggedForReview` flag enables bulk operations

---

## Policy Entity Details

### Columns Specification

| Column | Data Type | Constraints | Notes |
|---|---|---|---|
| `Id` | `UNIQUEIDENTIFIER` | PK, NOT NULL | UUID primary key |
| `PolicyNumber` | `VARCHAR(50)` | UNIQUE, NOT NULL, INDEXED | Unique identifier, must not change |
| `PolicyholderName` | `VARCHAR(100)` | NOT NULL | Name of policy holder |
| `Underwriter` | `VARCHAR(100)` | NOT NULL | Underwriting company |
| `PremiumAmount` | `DECIMAL(18,2)` | NOT NULL, CHECK ≥ 0 | Currency amount |
| `Currency` | `CHAR(3)` | DEFAULT 'SGD' | ISO 4217 code |
| `Status` | `VARCHAR(20)` | NOT NULL, INDEXED | Pending, Active, Expired, Cancelled |
| `LineOfBusiness` | `VARCHAR(20)` | NOT NULL, INDEXED | Property, Casualty, A&H, Marine |
| `Region` | `VARCHAR(50)` | NOT NULL, INDEXED | APAC region name |
| `EffectiveDate` | `DATETIME2` | NOT NULL, INDEXED | Coverage start date |
| `ExpiryDate` | `DATETIME2` | NOT NULL, INDEXED | Coverage end date |
| `FlaggedForReview` | `BIT` | DEFAULT 0, INDEXED | Admin review flag |
| `ReviewReason` | `VARCHAR(500)` | NULLABLE | Reason for review |
| `CreatedAt` | `DATETIME2` | DEFAULT GETUTCDATE() | Record creation time (immutable) |
| `UpdatedAt` | `DATETIME2` | DEFAULT GETUTCDATE() | Last modification time |

### Sample Data

```sql
INSERT INTO Policies (
    Id, PolicyNumber, PolicyholderName, Underwriter, 
    PremiumAmount, Currency, Status, LineOfBusiness, Region,
    EffectiveDate, ExpiryDate, FlaggedForReview, CreatedAt, UpdatedAt
) VALUES (
    '550e8400-e29b-41d4-a716-446655440000',
    'APAC-POL-001',
    'Acme Corporation',
    'Chubb Insurance Singapore',
    150000.00,
    'SGD',
    'Active',
    'Property',
    'Singapore',
    '2025-01-01 00:00:00',
    '2026-01-01 00:00:00',
    0,
    '2025-06-09 10:30:45.123',
    '2025-06-09 10:30:45.123'
);
```

---

## Indexes & Query Performance

### Index Strategy

| Index Name | Columns | Type | Selectivity | Purpose |
|---|---|---|---|---|
| `PK_Policies_Id` | `Id` | Clustered | Unique | Primary key; every query filtered by Id |
| `UX_Policies_PolicyNumber` | `PolicyNumber` | Unique | Unique | Enforce uniqueness; enable fast lookup by policy number |
| `IX_Policies_Status` | `Status` | Non-clustered | ~4 values | Filter by Active/Expired/Pending/Cancelled |
| `IX_Policies_Region` | `Region` | Non-clustered | ~10-20 values | Regional filtering |
| `IX_Policies_LOB` | `LineOfBusiness` | Non-clustered | ~4 values | Filter by line of business |
| `IX_Policies_FlaggedForReview` | `FlaggedForReview` | Non-clustered | ~2 values (0,1) | Show flagged policies |
| `IX_Policies_EffectiveDate` | `EffectiveDate` | Non-clustered | Continuous | Date range filtering |
| `IX_Policies_ExpiryDate` | `ExpiryDate` | Non-clustered | Continuous | Date range filtering |
| `IX_Policies_CreatedAt` | `CreatedAt` | Non-clustered | Continuous | Sorting by creation date |

### Composite Indexes (for common queries)

If performance analysis shows need:

```sql
-- Filter by Status + Region (common query)
CREATE INDEX IX_Policies_Status_Region 
ON Policies(Status, Region) 
INCLUDE (PolicyNumber, PolicyholderName, PremiumAmount);

-- Filter by Status + Date Range (activity analysis)
CREATE INDEX IX_Policies_Status_EffectiveDate 
ON Policies(Status, EffectiveDate DESC) 
INCLUDE (PremiumAmount);
```

### Query Performance Examples

#### Example 1: List policies by status (indexed)
```sql
SELECT * FROM Policies 
WHERE Status = 'Active' 
ORDER BY CreatedAt DESC 
OFFSET 0 ROWS FETCH NEXT 20 ROWS ONLY;
```
**Performance:** < 5ms (indexed on Status + CreatedAt)

#### Example 2: Filter by date range (indexed)
```sql
SELECT * FROM Policies 
WHERE EffectiveDate >= '2025-01-01' 
  AND ExpiryDate <= '2025-12-31' 
ORDER BY CreatedAt DESC;
```
**Performance:** < 10ms (indexed on EffectiveDate, ExpiryDate)

#### Example 3: Free-text search (not indexed, acceptable for MVP)
```sql
SELECT * FROM Policies 
WHERE PolicyNumber LIKE '%APAC%' 
   OR PolicyholderName LIKE '%Acme%' 
   OR Underwriter LIKE '%Chubb%';
```
**Performance:** ~50-100ms for 1000 rows (full table scan, acceptable for MVP; could add FULLTEXT index if needed)

#### Example 4: Summary aggregation (not indexed, materialized view optional)
```sql
SELECT 
  COUNT(*) AS TotalPolicies,
  COUNT(CASE WHEN Status = 'Active' THEN 1 END) AS ActiveCount,
  SUM(PremiumAmount) AS TotalPremium,
  COUNT(DISTINCT Region) AS RegionCount
FROM Policies;
```
**Performance:** ~10-20ms for 1000 rows (full table scan, acceptable; could cache result 5 min)

---

## Seed Data

### Seed Data Strategy

200+ realistic APAC policy records seeded on database creation for:
- Development/testing (consistent data)
- Demos (realistic-looking policies)
- Performance testing (verify indexes work with large dataset)

### Seed Data Script

Created by `PolicySeeder` class in Infrastructure layer:

```csharp
public static class PolicySeeder
{
    public static Policy[] GetSeedPolicies()
    {
        var policies = new List<Policy>();
        
        var regions = new[] { "Singapore", "Australia", "Japan", "Hong Kong", "Malaysia" };
        var lobs = new[] { 
            LineOfBusiness.Property, 
            LineOfBusiness.Casualty, 
            LineOfBusiness.HealthAndAccident, 
            LineOfBusiness.Marine 
        };
        var statuses = new[] { 
            PolicyStatus.Pending, 
            PolicyStatus.Active, 
            PolicyStatus.Expired 
        };
        
        for (int i = 1; i <= 200; i++)
        {
            policies.Add(new Policy
            {
                Id = Guid.NewGuid(),
                PolicyNumber = $"APAC-POL-{i:D6}",
                PolicyholderName = $"Company {i}",
                Underwriter = "Chubb Insurance APAC",
                PremiumAmount = 100000 + (i * 5000),
                Currency = "SGD",
                Status = statuses[i % statuses.Length],
                LineOfBusiness = lobs[i % lobs.Length],
                Region = regions[i % regions.Length],
                EffectiveDate = DateTime.UtcNow.AddMonths(-i),
                ExpiryDate = DateTime.UtcNow.AddMonths(24 - i),
                FlaggedForReview = i % 20 == 0,  // ~5% flagged
                CreatedAt = DateTime.UtcNow.AddDays(-i),
                UpdatedAt = DateTime.UtcNow.AddDays(-i)
            });
        }
        
        return policies.ToArray();
    }
}
```

---

## Constraints & Validation

### Database Constraints

```sql
-- Primary Key
ALTER TABLE Policies
ADD CONSTRAINT PK_Policies_Id PRIMARY KEY CLUSTERED (Id);

-- Unique Constraint
ALTER TABLE Policies
ADD CONSTRAINT UX_Policies_PolicyNumber UNIQUE (PolicyNumber);

-- Check Constraints
ALTER TABLE Policies
ADD CONSTRAINT CK_Policies_EffectiveDate_LE_ExpiryDate
    CHECK (EffectiveDate <= ExpiryDate);

ALTER TABLE Policies
ADD CONSTRAINT CK_Policies_PremiumAmount_GE_Zero
    CHECK (PremiumAmount >= 0);

-- Enum-like constraints (via check or domain)
ALTER TABLE Policies
ADD CONSTRAINT CK_Policies_Status
    CHECK (Status IN ('Pending', 'Active', 'Expired', 'Cancelled'));

ALTER TABLE Policies
ADD CONSTRAINT CK_Policies_LineOfBusiness
    CHECK (LineOfBusiness IN ('Property', 'Casualty', 'HealthAndAccident', 'Marine'));
```

### Application-Level Validation (FluentValidation)

```csharp
public class PolicyFilterQueryValidator : AbstractValidator<PolicyFilterQuery>
{
    public PolicyFilterQueryValidator()
    {
        RuleFor(x => x.Page)
            .GreaterThanOrEqualTo(1)
            .LessThanOrEqualTo(1000)  // Max 1000 pages to prevent DoS
            .WithMessage("Page must be between 1 and 1000");
        
        RuleFor(x => x.Size)
            .GreaterThanOrEqualTo(1)
            .LessThanOrEqualTo(100)
            .WithMessage("Size must be between 1 and 100");
        
        RuleFor(x => x.EffectiveDateFrom)
            .LessThanOrEqualTo(x => x.EffectiveDateTo)
            .When(x => x.EffectiveDateFrom.HasValue && x.EffectiveDateTo.HasValue)
            .WithMessage("EffectiveDateFrom must be <= EffectiveDateTo");
    }
}
```

---

## Audit & Compliance

### Audit Fields

Every record has immutable audit fields:

| Field | Purpose | Set By | Frequency |
|---|---|---|---|
| `CreatedAt` | Record creation timestamp | DB default (GETUTCDATE) | Once (immutable) |
| `UpdatedAt` | Last modification timestamp | Application (SaveChangesAsync override) | Every update |

### Audit Query Example

```sql
-- Show policy update history (via UpdatedAt)
SELECT 
  PolicyNumber,
  Status,
  FlaggedForReview,
  UpdatedAt
FROM Policies
WHERE PolicyNumber = 'APAC-POL-000001'
ORDER BY UpdatedAt DESC;

-- Find recently modified policies (last 24h)
SELECT *
FROM Policies
WHERE UpdatedAt >= DATEADD(DAY, -1, GETUTCDATE())
ORDER BY UpdatedAt DESC;
```

### Compliance Notes

- **PII Protection:** Policyholder names are stored in plain text (acceptable for internal APAC operations; could be encrypted at rest if needed)
- **Data Retention:** No explicit retention policy implemented (but soft deletes via status transition preserve historical data)
- **Audit Trail:** UpdatedAt timestamp enables tracking changes; no change log table (acceptable for MVP; could add version history table if needed)

---

## Scalability & Growth

### Growth Plan: 200 → 1M+ Policies

| Phase | Size | Strategy | Notes |
|---|---|---|---|
| **Phase 1 (Current)** | 200-10K policies | Indexes on filter columns; single SQL Server instance | MVP; sufficient |
| **Phase 2** | 10K-100K policies | Add composite indexes; partition tables by region (optional); implement caching | Quarterly review; monitor query performance |
| **Phase 3** | 100K-1M policies | Read replicas; separate OLTP/OLAP databases; data warehouse for analytics | Estimated 1-2 years out |
| **Phase 4** | 1M+ policies | Sharding by region; distributed database; cloud scaling | Future roadmap |

### Index Maintenance

```sql
-- Fragmentation check
SELECT 
  OBJECT_NAME(ips.object_id) AS TableName,
  i.name AS IndexName,
  ips.avg_fragmentation_in_percent
FROM sys.dm_db_index_physical_stats(DB_ID(), NULL, NULL, NULL, 'LIMITED') ips
JOIN sys.indexes i ON ips.object_id = i.object_id AND ips.index_id = i.index_id
WHERE ips.avg_fragmentation_in_percent > 10
  AND ips.page_count > 1000;

-- Rebuild fragmented indexes (> 30%)
ALTER INDEX IX_Policies_Status ON Policies REBUILD;

-- Reorganize moderately fragmented indexes (10-30%)
ALTER INDEX IX_Policies_Status ON Policies REORGANIZE;
```

### Backup & Disaster Recovery

```sql
-- Full backup
BACKUP DATABASE [Chubb.PolicyManagement] 
TO DISK = '/var/opt/mssql/backup/policydb_full.bak'
WITH INIT, COMPRESSION, STATS = 10;

-- Transaction log backup (hourly)
BACKUP LOG [Chubb.PolicyManagement] 
TO DISK = '/var/opt/mssql/backup/policydb_log.trn'
WITH COMPRESSION;

-- Recovery point objective (RPO): 1 hour (daily full + hourly transaction logs)
-- Recovery time objective (RTO): < 15 minutes (restore full + apply transaction logs)
```

---

## Summary

The data model is designed to:

1. **Support the current MVP** — 200+ policies in APAC regions
2. **Scale to millions** — Proper indexing, pagination, no N+1 queries
3. **Maintain audit trails** — CreatedAt/UpdatedAt for compliance
4. **Enable compliance** — Unique policy numbers, soft deletes (Cancelled status)
5. **Optimize performance** — Strategic indexes on filter/sort columns

This single-table schema is intentionally simple for the MVP. If the platform evolves (multi-company support, claims management, etc.), the data model can be normalized with additional tables.
