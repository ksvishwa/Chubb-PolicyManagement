# Deployment & Infrastructure Design

**Version:** 1.0  
**Last Updated:** 2026-06-09  
**Status:** Active

---

## Table of Contents

1. [Deployment Architecture](#deployment-architecture)
2. [Local Development Setup](#local-development-setup)
3. [Docker & Containerization](#docker--containerization)
4. [CI/CD Pipeline](#cicd-pipeline)
5. [Environment Configuration](#environment-configuration)
6. [Monitoring & Observability](#monitoring--observability)
7. [Security Hardening](#security-hardening)
8. [Disaster Recovery](#disaster-recovery)

---

## Deployment Architecture

### Three-Tier Deployment Model

```
┌─────────────────────────────────────────────────────────┐
│ Development (Local)                                     │
├─────────────────────────────────────────────────────────┤
│ docker-compose up → db (SQL Server), api, frontend      │
│ Hot reload enabled; seed data loaded; debug endpoints   │
└─────────────────────────────────────────────────────────┘
                    ↓ git push
┌─────────────────────────────────────────────────────────┐
│ Staging (Azure)                                         │
├─────────────────────────────────────────────────────────┤
│ CI/CD: Build images, run tests, push to ACR             │
│ Deploy: 2x API instances, 1x frontend, Azure SQL        │
│ Purpose: Pre-production validation, performance testing │
└─────────────────────────────────────────────────────────┘
                    ↓ manual approval
┌─────────────────────────────────────────────────────────┐
│ Production (Azure)                                      │
├─────────────────────────────────────────────────────────┤
│ Deploy: 3x API instances (load balanced), frontend      │
│ Database: Azure SQL Managed Instance (HA, backups)      │
│ CDN: Azure Front Door for static assets                 │
│ Monitoring: Application Insights, Log Analytics         │
└─────────────────────────────────────────────────────────┘
```

---

## Local Development Setup

### Prerequisites

- **Docker Desktop** (latest)
- **.NET 8 SDK** (if running API outside container)
- **Node.js 20+** (if running frontend outside container)
- **Visual Studio Code** (recommended)

### Quick Start

```bash
# Clone repository
git clone https://github.com/ksvishwa/Chubb-PolicyManagement.git
cd Chubb-PolicyManagement

# Start services (docker-compose)
docker-compose up --build

# Services become available:
# - API: http://localhost:5000
# - Swagger UI: http://localhost:5000/swagger
# - Frontend: http://localhost:4200
# - SQL Server: localhost:1433 (sa / YourPassword123)

# Run tests
docker exec -it chubb-api dotnet test

# Seed database
docker exec -it chubb-api dotnet ef database update
```

### Manual Setup (without Docker)

```bash
# Backend
cd src/backend
dotnet build
dotnet ef database update  # Apply migrations to local SQL Server
dotnet run

# Frontend (in separate terminal)
cd src/frontend/policy-dashboard
npm install
ng serve  # Runs on http://localhost:4200
```

### Database Seeding

```bash
# Apply migrations + seed data (one command)
dotnet ef database update

# Or via SQL Server Management Studio:
# 1. Create database: CREATE DATABASE [Chubb.PolicyManagement];
# 2. Run migrations: dotnet ef database update
# 3. Seed data: Run PolicySeeder.cs
```

---

## Docker & Containerization

### Dockerfile — API

**File:** `src/backend/Chubb.PolicyManagement.Api/Dockerfile`

```dockerfile
# Multi-stage build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy project files
COPY ["src/backend/Chubb.PolicyManagement.Api/Chubb.PolicyManagement.Api.csproj", "api/"]
COPY ["src/backend/Chubb.PolicyManagement.Application/Chubb.PolicyManagement.Application.csproj", "app/"]
COPY ["src/backend/Chubb.PolicyManagement.Domain/Chubb.PolicyManagement.Domain.csproj", "domain/"]
COPY ["src/backend/Chubb.PolicyManagement.Infrastructure/Chubb.PolicyManagement.Infrastructure.csproj", "infra/"]

# Restore NuGet packages
RUN dotnet restore "api/Chubb.PolicyManagement.Api.csproj"

# Copy source code
COPY src/backend/ .

# Build
RUN dotnet build "api/Chubb.PolicyManagement.Api.csproj" -c Release -o /app/build

# Publish
FROM build AS publish
RUN dotnet publish "api/Chubb.PolicyManagement.Api.csproj" -c Release -o /app/publish

# Runtime image
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Install healthcheck tool
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*

# Copy published app
COPY --from=publish /app/publish .

# Health check
HEALTHCHECK --interval=30s --timeout=5s --start-period=10s --retries=3 \
    CMD curl -f http://localhost:5000/health || exit 1

EXPOSE 5000

ENTRYPOINT ["dotnet", "Chubb.PolicyManagement.Api.dll"]
```

### Dockerfile — Frontend

**File:** `src/frontend/policy-dashboard/Dockerfile`

```dockerfile
# Build stage
FROM node:20 AS build
WORKDIR /app

COPY package.json package-lock.json ./
RUN npm ci

COPY . .
RUN npm run build -- --configuration production

# Nginx runtime
FROM nginx:latest
COPY --from=build /app/dist/policy-dashboard /usr/share/nginx/html
COPY nginx.conf /etc/nginx/nginx.conf

EXPOSE 80

CMD ["nginx", "-g", "daemon off;"]
```

### docker-compose.yml

```yaml
version: '3.9'

services:
  db:
    image: mcr.microsoft.com/mssql/server:2022-latest
    environment:
      SA_PASSWORD: "YourPassword123"
      ACCEPT_EULA: "Y"
    ports:
      - "1433:1433"
    volumes:
      - db-data:/var/opt/mssql/data
    healthcheck:
      test: /opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P "YourPassword123" -Q "SELECT 1"
      interval: 10s
      timeout: 5s
      retries: 5

  api:
    build:
      context: .
      dockerfile: src/backend/Chubb.PolicyManagement.Api/Dockerfile
    environment:
      ASPNETCORE_ENVIRONMENT: "Development"
      ConnectionStrings__DefaultConnection: "Server=db,1433;Database=Chubb.PolicyManagement;User=sa;Password=YourPassword123;"
      JwtSecret: "YourSecretKeyAtLeast32CharactersLong123"
    ports:
      - "5000:5000"
    depends_on:
      db:
        condition: service_healthy
    healthcheck:
      test: ["CMD", "curl", "-f", "http://localhost:5000/health"]
      interval: 30s
      timeout: 5s
      retries: 3

  frontend:
    build:
      context: src/frontend/policy-dashboard
      dockerfile: Dockerfile
    ports:
      - "4200:80"
    depends_on:
      - api
    environment:
      API_BASE_URL: "http://localhost:5000"

volumes:
  db-data:
```

### Building & Running

```bash
# Build images
docker-compose build

# Run services (foreground)
docker-compose up

# Run services (background)
docker-compose up -d

# View logs
docker-compose logs -f api

# Stop services
docker-compose down

# Remove images
docker-compose down -v
```

---

## CI/CD Pipeline

### GitHub Actions Workflow

**File:** `.github/workflows/ci-cd.yml`

```yaml
name: CI/CD Pipeline

on:
  push:
    branches: [main, develop]
  pull_request:
    branches: [main]

jobs:
  build-test:
    runs-on: ubuntu-latest
    
    services:
      sqlserver:
        image: mcr.microsoft.com/mssql/server:2022-latest
        env:
          SA_PASSWORD: "YourPassword123"
          ACCEPT_EULA: "Y"
        ports:
          - 1433:1433
        options: >-
          --health-cmd="/opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P YourPassword123 -Q 'SELECT 1'"
          --health-interval 10s
          --health-timeout 5s
          --health-retries 5
    
    steps:
      - uses: actions/checkout@v3
      
      - name: Setup .NET
        uses: actions/setup-dotnet@v3
        with:
          dotnet-version: '8.0.x'
      
      - name: Restore NuGet packages
        run: dotnet restore src/backend/Chubb.PolicyManagement.sln
      
      - name: Build backend
        run: dotnet build src/backend/Chubb.PolicyManagement.sln --no-restore --configuration Release
      
      - name: Run backend unit tests
        run: dotnet test src/backend/tests/ --configuration Release --no-build --verbosity normal /p:CollectCoverage=true
      
      - name: Setup Node.js
        uses: actions/setup-node@v3
        with:
          node-version: '20'
          cache: npm
          cache-dependency-path: src/frontend/policy-dashboard/package-lock.json
      
      - name: Install frontend dependencies
        run: npm ci
        working-directory: src/frontend/policy-dashboard
      
      - name: Build frontend
        run: npm run build -- --configuration production
        working-directory: src/frontend/policy-dashboard
      
      - name: Run frontend tests
        run: npm run test:ci
        working-directory: src/frontend/policy-dashboard
      
      - name: Code coverage
        uses: codecov/codecov-action@v3
        with:
          files: ./coverage/cobertura-coverage.xml

  security-scan:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v3
      
      - name: Run Trivy vulnerability scanner
        uses: aquasecurity/trivy-action@master
        with:
          scan-type: 'config'
          scan-ref: '.'

  docker-push:
    runs-on: ubuntu-latest
    if: github.ref == 'refs/heads/main' && github.event_name == 'push'
    needs: [build-test, security-scan]
    
    steps:
      - uses: actions/checkout@v3
      
      - name: Set up Docker Buildx
        uses: docker/setup-buildx-action@v2
      
      - name: Login to Azure Container Registry
        uses: docker/login-action@v2
        with:
          registry: chubbacr.azurecr.io
          username: ${{ secrets.ACR_USERNAME }}
          password: ${{ secrets.ACR_PASSWORD }}
      
      - name: Build and push API image
        uses: docker/build-push-action@v4
        with:
          context: .
          file: src/backend/Chubb.PolicyManagement.Api/Dockerfile
          push: true
          tags: |
            chubbacr.azurecr.io/api:latest
            chubbacr.azurecr.io/api:${{ github.sha }}
      
      - name: Build and push Frontend image
        uses: docker/build-push-action@v4
        with:
          context: src/frontend/policy-dashboard
          file: src/frontend/policy-dashboard/Dockerfile
          push: true
          tags: |
            chubbacr.azurecr.io/frontend:latest
            chubbacr.azurecr.io/frontend:${{ github.sha }}

  deploy-staging:
    runs-on: ubuntu-latest
    if: github.ref == 'refs/heads/main'
    needs: docker-push
    
    steps:
      - uses: actions/checkout@v3
      
      - name: Deploy to Azure Container Instances
        run: |
          az login --service-principal -u ${{ secrets.AZURE_CLIENT_ID }} \
            -p ${{ secrets.AZURE_CLIENT_SECRET }} \
            --tenant ${{ secrets.AZURE_TENANT_ID }}
          
          az container create \
            --resource-group chubb-rg \
            --name policy-api-staging \
            --image chubbacr.azurecr.io/api:latest \
            --cpu 1 --memory 1
```

### Pipeline Stages

1. **Build & Test** (on every push)
   - Restore NuGet packages
   - Compile C# projects
   - Run unit tests (backend)
   - Run unit tests (frontend)
   - Collect code coverage

2. **Security Scan** (on every push)
   - Scan for vulnerable dependencies (Trivy)
   - SAST/code analysis

3. **Docker Build & Push** (main branch only)
   - Build Docker images
   - Push to Azure Container Registry (ACR)
   - Tag with commit SHA

4. **Deploy to Staging** (main branch only)
   - Pull images from ACR
   - Deploy to Azure Container Instances
   - Run integration tests
   - Smoke tests

5. **Deploy to Production** (manual approval)
   - Approved by release manager
   - Blue-green deployment
   - Health checks
   - Rollback if needed

---

## Environment Configuration

### Configuration by Environment

| Setting | Development | Staging | Production |
|---|---|---|---|
| **Database** | Local SQL Server | Azure SQL (test DB) | Azure SQL Managed Instance (HA) |
| **Logging Level** | Debug | Information | Warning |
| **Secrets Source** | `appsettings.Development.json` | Azure Key Vault | Azure Key Vault |
| **CORS Origin** | http://localhost:4200 | https://staging.example.com | https://api.example.com |
| **API Rate Limit** | None | 100 req/min | 1000 req/min |
| **Cache TTL** | 0 (disabled) | 5 min | 15 min |

### Environment Variables

```bash
# Database
ConnectionStrings__DefaultConnection=Server=localhost,1433;Database=Chubb.PolicyManagement;User=sa;Password=...;

# JWT
JwtSecret=YourSecretKeyAtLeast32CharactersLong...
JwtIssuer=https://api.example.com
JwtAudience=https://frontend.example.com

# CORS
AllowedOrigins=http://localhost:4200,https://staging.example.com,https://api.example.com

# Application
ASPNETCORE_ENVIRONMENT=Development
ASPNETCORE_URLS=http://+:5000

# Azure
AZURE_KEY_VAULT_URL=https://chubb-keyvault.vault.azure.net/
```

### appsettings.json Structure

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information"
    }
  },
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost,1433;Database=Chubb.PolicyManagement;..."
  },
  "Jwt": {
    "Secret": "${JWT_SECRET}",
    "Issuer": "https://api.example.com",
    "Audience": "https://frontend.example.com",
    "ExpirationMinutes": 15
  },
  "Cors": {
    "AllowedOrigins": ["http://localhost:4200"]
  },
  "RateLimit": {
    "Enabled": false,
    "RequestsPerMinute": 100
  }
}
```

---

## Monitoring & Observability

### Application Insights Integration

```csharp
// Program.cs
builder.Services.AddApplicationInsightsTelemetry();

var app = builder.Build();

// Custom telemetry
var telemetryClient = app.Services.GetRequiredService<TelemetryClient>();

// Track custom event
telemetryClient.TrackEvent("PolicyFlagged", new Dictionary<string, string>
{
    { "PolicyId", policyId.ToString() },
    { "Count", flagCount.ToString() }
});
```

### Key Metrics to Monitor

| Metric | Target | Alert Threshold |
|---|---|---|
| **API Response Time (P95)** | < 200ms | > 500ms |
| **Error Rate** | < 0.1% | > 1% |
| **Database Connection Pool Exhaustion** | < 50% used | > 80% used |
| **Disk Space (DB)** | > 20% free | < 10% free |
| **Memory Usage** | < 1GB | > 1.5GB |
| **Uptime** | > 99.9% | < 99.5% (1 pagerduty alert) |
| **Test Coverage** | > 90% | < 85% (blocking PR) |

### Logging Strategy

```csharp
// Structured logging with Serilog
var logger = LoggerFactory.CreateLogger<PolicyService>();

logger.LogInformation(
    "Fetching policies with filter: {@Filter} for user {UserId}",
    filter,
    userId);

logger.LogWarning(
    "Policy {PolicyId} not found after 3 retries",
    policyId);

logger.LogError(ex,
    "Failed to flag policies {PolicyIds} due to {ErrorMessage}",
    string.Join(",", policyIds),
    ex.Message);
```

### Log Aggregation

```
┌──────────────┐
│ Application  │ ─→ Serilog → 
│ (logs)       │              └─→ Application Insights → 
└──────────────┘              ┌─→ Log Analytics
                              │   ├─ Query: failed requests
                              │   ├─ Dashboard: Error rate
                              │   └─ Alert: Threshold exceeded
```

---

## Security Hardening

### Network Security

```yaml
# Azure Network Security Group (NSG)
- Allow inbound on port 443 (HTTPS) from CloudFlare IPs
- Allow inbound on port 80 (redirect to 443)
- Deny all other inbound traffic
- Allow outbound on port 443 (HTTPS) only
```

### SSL/TLS

```bash
# Use Let's Encrypt for free SSL certificates
# Auto-renewal via Azure App Service

# Enforce HTTPS redirect
app.UseHttpsRedirection();
app.UseHsts();  // HTTP Strict-Transport-Security header
```

### Secret Management

```bash
# Use Azure Key Vault for secrets (not .env files or config)
1. Create Key Vault: az keyvault create --name chubb-kv --resource-group chubb-rg
2. Store secrets: az keyvault secret set --vault-name chubb-kv --name JwtSecret --value "..."
3. Reference in code: 
   builder.Configuration.AddAzureKeyVault(
       new Uri("https://chubb-kv.vault.azure.net/"),
       new DefaultAzureCredential());
```

### API Security Headers

```csharp
// Middleware to add security headers
app.Use(async (context, next) =>
{
    context.Response.Headers.Add("X-Content-Type-Options", "nosniff");
    context.Response.Headers.Add("X-Frame-Options", "DENY");
    context.Response.Headers.Add("X-XSS-Protection", "1; mode=block");
    context.Response.Headers.Add("Strict-Transport-Security", "max-age=31536000; includeSubDomains");
    context.Response.Headers.Add("Content-Security-Policy", "default-src 'self'");
    
    await next();
});
```

---

## Disaster Recovery

### Backup Strategy

| Backup Type | Frequency | Retention | Purpose |
|---|---|---|---|
| **Full Backup** | Daily (2 AM UTC) | 7 days | Complete DB snapshot |
| **Transaction Log** | Hourly | 7 days | Point-in-time recovery |
| **Geo-Redundant** | Continuous | 30 days | Cross-region failover |

### Recovery Time & Point Objectives (RTO/RPO)

- **RTO (Recovery Time Objective):** < 1 hour
- **RPO (Recovery Point Objective):** < 15 minutes

### Failover Procedure

```bash
# 1. Detect failure (health check fails 3x)
# 2. Trigger failover (automatic via Azure)
# 3. Restore from backup:
az sql server restore \
  --resource-group chubb-rg \
  --name chubb-sql-primary \
  --time "2026-06-09T12:00:00Z" \
  --target chubb-sql-restored

# 4. Update connection strings to point to restored server
# 5. Verify data integrity (run smoke tests)
# 6. Switch traffic back to primary (once recovered)
```

---

## Summary

Deployment architecture spans:

1. **Local Development** — Docker Compose for parity with production
2. **CI/CD Pipeline** — Automated build, test, security scan, push to registry
3. **Multi-Environment** — Dev, staging, production with configuration management
4. **Containerization** — Multi-stage Dockerfiles for minimal image size
5. **Monitoring** — Application Insights, structured logging, key metrics
6. **Security** — HTTPS, API security headers, Key Vault secrets, NSG rules
7. **Disaster Recovery** — Automated backups, failover procedures, RTO/RPO targets

This design enables **reliable**, **secure**, **observable** deployments across environments.
