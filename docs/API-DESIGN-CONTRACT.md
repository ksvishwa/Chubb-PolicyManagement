# API Design & Contract Documentation

**Version:** 1.0  
**Last Updated:** 2026-06-09  
**Status:** Active

---

## Table of Contents

1. [API Overview](#api-overview)
2. [Authentication & Authorization](#authentication--authorization)
3. [Request/Response Format](#requestresponse-format)
4. [Endpoints Specification](#endpoints-specification)
5. [Error Handling](#error-handling)
6. [Pagination](#pagination)
7. [Filtering & Sorting](#filtering--sorting)
8. [Rate Limiting](#rate-limiting)
9. [Versioning Strategy](#versioning-strategy)

---

## API Overview

### Base URL
```
Development:  http://localhost:5000/api/v1
Staging:      https://staging-api.example.com/api/v1
Production:   https://api.example.com/api/v1
```

### API Style
- **REST** — Stateless, resource-oriented
- **Format** — JSON (request/response)
- **Protocol** — HTTP/2 (HTTPS only in production)
- **Documentation** — OpenAPI 3.x (Swagger UI at `/swagger`)

### Supported Content Types
```
Content-Type: application/json
Accept: application/json
Accept-Encoding: gzip, deflate, br
```

---

## Authentication & Authorization

### JWT Bearer Token

All endpoints (except health) require JWT bearer token:

```http
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
```

### Token Structure

```json
{
  "sub": "user-123",
  "email": "user@example.com",
  "name": "John Doe",
  "roles": ["Admin", "PolicyManager"],
  "iat": 1623283200,
  "exp": 1623286800,
  "iss": "https://api.example.com",
  "aud": "https://frontend.example.com"
}
```

### Token Lifecycle

1. **Obtain Token** — POST `/auth/login` (not part of this API)
   ```http
   POST /auth/login
   Content-Type: application/json
   
   { "username": "user@example.com", "password": "..." }
   
   Response:
   {
     "accessToken": "eyJ...",
     "refreshToken": "eyJ...",
     "expiresIn": 900
   }
   ```

2. **Use Token** — Include in Authorization header
   ```http
   GET /api/v1/policies
   Authorization: Bearer eyJ...
   ```

3. **Refresh Token** — POST `/auth/refresh` (expires in 7 days)
   ```http
   POST /auth/refresh
   Content-Type: application/json
   
   { "refreshToken": "eyJ..." }
   
   Response:
   {
     "accessToken": "eyJ...",
     "expiresIn": 900
   }
   ```

### Authorization Levels

| Role | Permissions |
|---|---|
| **Admin** | Full access (view, flag, summary) |
| **PolicyManager** | View, flag policies |
| **Viewer** | View policies only |
| **Service** | Automated operations (system-to-system) |

---

## Request/Response Format

### Standard Request Headers

```http
Accept: application/json
Accept-Encoding: gzip, deflate, br
Authorization: Bearer <token>
Content-Type: application/json
User-Agent: PolicyDashboard/1.0
X-Correlation-ID: 550e8400-e29b-41d4-a716-446655440000
```

### Standard Response Headers

```http
Content-Type: application/json; charset=utf-8
Content-Encoding: gzip
Cache-Control: no-cache, no-store, must-revalidate
X-Total-Count: 250
X-Total-Pages: 13
X-Correlation-ID: 550e8400-e29b-41d4-a716-446655440000
X-RateLimit-Limit: 1000
X-RateLimit-Remaining: 999
X-RateLimit-Reset: 1623369600
```

### Response Envelope (Success)

```json
{
  "data": { ... },
  "meta": {
    "timestamp": "2026-06-09T10:30:45.123Z",
    "correlationId": "550e8400-e29b-41d4-a716-446655440000"
  }
}
```

### Response Envelope (Error)

See [Error Handling](#error-handling) section.

---

## Endpoints Specification

### 1. GET /api/v1/policies — List Policies

**Description:** Retrieve paginated, filtered, sorted list of policies.

**Method:** `GET`

**Authentication:** Required (Bearer token)

**Query Parameters:**

| Parameter | Type | Default | Required | Description |
|---|---|---|---|---|
| `page` | integer | 1 | No | Page number (1-based) |
| `size` | integer | 20 | No | Page size (max 100) |
| `sort` | string | createdAt,desc | No | Sort field and direction (e.g., `policyNumber,asc`) |
| `status` | string | — | No | Filter: `Active`, `Expired`, `Pending`, `Cancelled` |
| `lineOfBusiness` | string | — | No | Filter: `Property`, `Casualty`, `A&H`, `Marine` |
| `region` | string | — | No | Filter: APAC region name |
| `effectiveDateFrom` | date | — | No | Filter: ISO 8601 date (YYYY-MM-DD) |
| `effectiveDateTo` | date | — | No | Filter: ISO 8601 date (YYYY-MM-DD) |
| `search` | string | — | No | Free-text search (policy number, holder name, underwriter) |

**Example Request:**
```http
GET /api/v1/policies?page=1&size=20&status=Active&region=Singapore&sort=policyNumber,asc
Authorization: Bearer eyJ...
```

**Success Response (200 OK):**
```json
{
  "data": [
    {
      "id": "550e8400-e29b-41d4-a716-446655440000",
      "policyNumber": "APAC-POL-000001",
      "policyholderName": "Acme Corporation",
      "underwriter": "Chubb Insurance Singapore",
      "premiumAmount": 150000.00,
      "currency": "SGD",
      "status": "Active",
      "lineOfBusiness": "Property",
      "region": "Singapore",
      "effectiveDate": "2025-01-01T00:00:00Z",
      "expiryDate": "2026-01-01T00:00:00Z",
      "flaggedForReview": false,
      "reviewReason": null,
      "createdAt": "2026-06-09T10:30:45.123Z",
      "updatedAt": "2026-06-09T10:30:45.123Z"
    },
    ...
  ],
  "page": 1,
  "size": 20,
  "totalCount": 250,
  "totalPages": 13
}
```

**Response Headers:**
```
X-Total-Count: 250
X-Total-Pages: 13
X-RateLimit-Limit: 1000
X-RateLimit-Remaining: 999
```

**Error Response (400 Bad Request):**
```json
{
  "type": "https://api.example.com/errors/invalid-filter",
  "title": "Invalid Policy Filter",
  "status": 400,
  "detail": "Page must be >= 1",
  "instance": "/api/v1/policies?page=-1"
}
```

---

### 2. GET /api/v1/policies/{id} — Get Policy Detail

**Description:** Retrieve a single policy by ID.

**Method:** `GET`

**Authentication:** Required (Bearer token)

**Path Parameters:**

| Parameter | Type | Description |
|---|---|---|
| `id` | uuid | Policy ID (GUID) |

**Example Request:**
```http
GET /api/v1/policies/550e8400-e29b-41d4-a716-446655440000
Authorization: Bearer eyJ...
```

**Success Response (200 OK):**
```json
{
  "id": "550e8400-e29b-41d4-a716-446655440000",
  "policyNumber": "APAC-POL-000001",
  "policyholderName": "Acme Corporation",
  "underwriter": "Chubb Insurance Singapore",
  "premiumAmount": 150000.00,
  "currency": "SGD",
  "status": "Active",
  "lineOfBusiness": "Property",
  "region": "Singapore",
  "effectiveDate": "2025-01-01T00:00:00Z",
  "expiryDate": "2026-01-01T00:00:00Z",
  "flaggedForReview": false,
  "reviewReason": null,
  "createdAt": "2026-06-09T10:30:45.123Z",
  "updatedAt": "2026-06-09T10:30:45.123Z"
}
```

**Error Response (404 Not Found):**
```json
{
  "type": "https://api.example.com/errors/policy-not-found",
  "title": "Policy Not Found",
  "status": 404,
  "detail": "Policy with ID 550e8400-e29b-41d4-a716-446655440000 not found.",
  "instance": "/api/v1/policies/550e8400-e29b-41d4-a716-446655440000"
}
```

---

### 3. PATCH /api/v1/policies/flag — Bulk Flag Policies

**Description:** Flag multiple policies for review in a single atomic transaction.

**Method:** `PATCH`

**Authentication:** Required (Bearer token with `Admin` or `PolicyManager` role)

**Request Body:**
```json
{
  "policyIds": [
    "550e8400-e29b-41d4-a716-446655440000",
    "550e8400-e29b-41d4-a716-446655440001",
    "550e8400-e29b-41d4-a716-446655440002"
  ],
  "reason": "High premium review required"
}
```

**Schema Validation:**
- `policyIds`: Array of GUIDs; required; min 1, max 1000 elements
- `reason`: Optional string; max 500 characters

**Example Request:**
```http
PATCH /api/v1/policies/flag
Authorization: Bearer eyJ...
Content-Type: application/json

{
  "policyIds": [
    "550e8400-e29b-41d4-a716-446655440000",
    "550e8400-e29b-41d4-a716-446655440001"
  ],
  "reason": "Policy anniversary review"
}
```

**Success Response (204 No Content):**
```http
HTTP/1.1 204 No Content
X-RateLimit-Limit: 1000
X-RateLimit-Remaining: 999
```

**Error Response (400 Bad Request):**
```json
{
  "type": "https://api.example.com/errors/bulk-flag-validation",
  "title": "Bulk Flag Validation Failed",
  "status": 400,
  "detail": "At least one policy ID required",
  "instance": "/api/v1/policies/flag"
}
```

**Error Response (404 Not Found):**
```json
{
  "type": "https://api.example.com/errors/policy-not-found",
  "title": "Policy Not Found",
  "status": 404,
  "detail": "Policy with ID 550e8400-e29b-41d4-a716-446655440099 not found. Bulk operation rolled back.",
  "instance": "/api/v1/policies/flag"
}
```

---

### 4. GET /api/v1/policies/summary — Get Summary Statistics

**Description:** Retrieve aggregated statistics across all policies.

**Method:** `GET`

**Authentication:** Required (Bearer token)

**Example Request:**
```http
GET /api/v1/policies/summary
Authorization: Bearer eyJ...
```

**Success Response (200 OK):**
```json
{
  "totalPolicies": 250,
  "activeCount": 180,
  "expiredCount": 50,
  "pendingCount": 15,
  "cancelledCount": 5,
  "premiumByLOB": {
    "Property": 5000000.00,
    "Casualty": 3000000.00,
    "HealthAndAccident": 1500000.00,
    "Marine": 2500000.00
  },
  "uniqueRegionCount": 5,
  "flaggedCount": 12
}
```

**Cache Strategy:**
- Response cached for 5 minutes (development: 0, production: 5 min)
- Cache-Control header: `public, max-age=300`

---

### 5. GET /health — Health Check

**Description:** Verify API and database connectivity.

**Method:** `GET`

**Authentication:** None (public endpoint)

**Example Request:**
```http
GET /health
```

**Success Response (200 OK):**
```json
{
  "status": "Healthy",
  "checks": {
    "database": {
      "status": "Healthy",
      "duration": "12ms"
    },
    "api": {
      "status": "Healthy",
      "duration": "1ms"
    }
  },
  "timestamp": "2026-06-09T10:30:45.123Z"
}
```

**Error Response (503 Service Unavailable):**
```json
{
  "status": "Unhealthy",
  "checks": {
    "database": {
      "status": "Unhealthy",
      "error": "Connection timeout"
    }
  },
  "timestamp": "2026-06-09T10:30:45.123Z"
}
```

---

## Error Handling

### RFC 7807 Problem Details

All error responses follow RFC 7807 Problem Details standard:

```json
{
  "type": "https://api.example.com/errors/<error-code>",
  "title": "<HTTP Status Phrase>",
  "status": <HTTP Status Code>,
  "detail": "<human-readable error message>",
  "instance": "<request path>",
  "traceId": "<correlation ID>"
}
```

### Error Codes & Mappings

| HTTP Status | Type | Title | Description |
|---|---|---|---|
| `400` | `invalid-filter` | Bad Request | Query/body validation failed |
| `400` | `bulk-flag-validation` | Bad Request | Bulk flag payload invalid |
| `401` | `unauthorized` | Unauthorized | Missing or invalid JWT |
| `403` | `forbidden` | Forbidden | Insufficient permissions |
| `404` | `policy-not-found` | Not Found | Policy ID does not exist |
| `409` | `conflict` | Conflict | Optimistic concurrency violation |
| `422` | `unprocessable-entity` | Unprocessable Entity | Semantic validation failed |
| `429` | `too-many-requests` | Too Many Requests | Rate limit exceeded |
| `500` | `internal-server-error` | Internal Server Error | Unexpected error; see logs |

### Example Error Responses

**Validation Error (400):**
```json
{
  "type": "https://api.example.com/errors/invalid-filter",
  "title": "Bad Request",
  "status": 400,
  "detail": "Query parameter 'size' must be between 1 and 100.",
  "instance": "/api/v1/policies?size=500"
}
```

**Authentication Error (401):**
```json
{
  "type": "https://api.example.com/errors/unauthorized",
  "title": "Unauthorized",
  "status": 401,
  "detail": "Missing or invalid authorization header.",
  "instance": "/api/v1/policies"
}
```

**Not Found Error (404):**
```json
{
  "type": "https://api.example.com/errors/policy-not-found",
  "title": "Not Found",
  "status": 404,
  "detail": "Policy with ID 550e8400-e29b-41d4-a716-446655440000 not found.",
  "instance": "/api/v1/policies/550e8400-e29b-41d4-a716-446655440000"
}
```

**Rate Limit Error (429):**
```json
{
  "type": "https://api.example.com/errors/too-many-requests",
  "title": "Too Many Requests",
  "status": 429,
  "detail": "Rate limit exceeded. Max 1000 requests per minute. Wait 30 seconds.",
  "instance": "/api/v1/policies",
  "retryAfter": 30
}
```

---

## Pagination

### Pagination Model

**Limit-Offset Pagination** (suitable for MVP with 200-10K records):

```
page = 1  (1-based)
size = 20 (items per page)
offset = (page - 1) * size = 0
```

**Query:**
```sql
SELECT TOP 20 * FROM Policies
ORDER BY CreatedAt DESC
OFFSET 0 ROWS
```

**Response:**
```json
{
  "data": [...],
  "page": 1,
  "size": 20,
  "totalCount": 250,
  "totalPages": 13
}
```

### Pagination Headers

```http
X-Total-Count: 250
X-Total-Pages: 13
X-Current-Page: 1
X-Page-Size: 20
```

### Constraints

- Min page size: 1
- Max page size: 100
- Max page: `ceil(totalCount / size)`
- Requesting page > max page returns empty array

### Example Requests

```http
# Page 1 (default)
GET /api/v1/policies

# Page 2, size 50
GET /api/v1/policies?page=2&size=50

# Last page (12 with 20 items)
GET /api/v1/policies?page=12&size=20
```

---

## Filtering & Sorting

### Supported Filters

| Filter | Type | Format | Example |
|---|---|---|---|
| `status` | enum | `Active`, `Expired`, `Pending`, `Cancelled` | `?status=Active` |
| `lineOfBusiness` | enum | `Property`, `Casualty`, `A&H`, `Marine` | `?lineOfBusiness=Property` |
| `region` | string | Region name | `?region=Singapore` |
| `effectiveDateFrom` | date | ISO 8601 (YYYY-MM-DD) | `?effectiveDateFrom=2025-01-01` |
| `effectiveDateTo` | date | ISO 8601 (YYYY-MM-DD) | `?effectiveDateTo=2025-12-31` |
| `search` | string | Free-text | `?search=Acme` |

### Filter Combinations

Filters are combined with AND logic:
```http
# Active policies in Singapore
GET /api/v1/policies?status=Active&region=Singapore

# Policies with coverage in 2025
GET /api/v1/policies?effectiveDateFrom=2025-01-01&effectiveDateTo=2025-12-31
```

### Supported Sort Fields

| Sort Field | Order | Direction |
|---|---|---|
| `policyNumber` | Alphabetical | `asc` (A→Z), `desc` (Z→A) |
| `createdAt` | Chronological | `asc` (oldest first), `desc` (newest first) |
| `status` | Enum | `asc`, `desc` |
| `premiumAmount` | Numerical | `asc` (low to high), `desc` (high to low) |
| `region` | Alphabetical | `asc`, `desc` |

### Sort Format

```
sort=<field>,<direction>
```

### Sort Examples

```http
# Default: created date, newest first
GET /api/v1/policies

# Sort by policy number, ascending
GET /api/v1/policies?sort=policyNumber,asc

# Sort by premium, highest first
GET /api/v1/policies?sort=premiumAmount,desc
```

---

## Rate Limiting

### Rate Limit Policy

| Tier | Requests/Min | Requests/Hour | Burst |
|---|---|---|---|
| **Anonymous** | 10 | 100 | 5 |
| **Authenticated** | 100 | 1000 | 20 |
| **Admin** | 500 | 5000 | 100 |
| **Service (API-Key)** | 1000 | 10000 | 500 |

### Rate Limit Headers

```http
X-RateLimit-Limit: 1000
X-RateLimit-Remaining: 999
X-RateLimit-Reset: 1623369600
```

### Rate Limit Response (429)

```http
HTTP/1.1 429 Too Many Requests
X-RateLimit-Limit: 100
X-RateLimit-Remaining: 0
X-RateLimit-Reset: 1623369645
Retry-After: 45

{
  "type": "https://api.example.com/errors/too-many-requests",
  "title": "Too Many Requests",
  "status": 429,
  "detail": "Rate limit exceeded. Retry after 45 seconds.",
  "instance": "/api/v1/policies"
}
```

---

## Versioning Strategy

### URL-Based Versioning

All endpoints prefixed with `/api/v1/`:

```
/api/v1/policies      (current, stable)
/api/v2/policies      (future, backward incompatible changes)
```

### Version Support

| Version | Status | Sunset |
|---|---|---|
| `v1` | Active | — |
| `v2` | Planned | — |

### Backward Compatibility Policy

**v1 will support:**
- Adding new optional fields (old clients ignore)
- Adding new endpoints
- Extending error details

**v1 will NOT support:**
- Removing fields
- Changing field types
- Changing HTTP status codes for same scenario

**Migration path:**
- 6 months notice before deprecating API version
- v1 clients continue to work; v2 available alongside v1

---

## Summary

The API design follows:

1. **REST principles** — Resource-oriented URLs, HTTP verbs
2. **OpenAPI 3.x contract** — Auto-documented, Swagger UI available
3. **RFC 7807 errors** — Standardized, machine-readable error responses
4. **JWT authentication** — Stateless, scalable, secure
5. **Pagination** — Limit-offset, suitable for MVP scale
6. **Filtering & sorting** — Flexible, standard query parameters
7. **Rate limiting** — Tiered, protects API from abuse
8. **Versioning** — URL-based, backward compatible

This API is designed to support **fast client development**, **clear error handling**, and **operational clarity**.
