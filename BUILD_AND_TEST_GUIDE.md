# Build & Test Automation Setup Guide

This document explains the new build and test automation infrastructure for the Chubb APAC Policy Management Platform.

## Overview

The project now includes:

1. **EF Core Rules** — Mandatory Entity Framework Core standards in `.github/instructions/ef-core-rules.instructions.md`
2. **VS Code Tasks** — Automated build and test tasks in `.vscode/tasks.json`
3. **Debug Configurations** — Debugging profiles in `.vscode/launch.json`
4. **GitHub Actions CI/CD** — Automated build & test pipeline in `.github/workflows/build-and-test.yml`

---

## VS Code Tasks

All build and test commands are available via VS Code's Task Runner. Open the Command Palette (`Ctrl+Shift+P` or `Cmd+Shift+P`) and type `Tasks: Run Task`.

### Available Tasks

#### Build Tasks

| Task | Purpose | Command |
|---|---|---|
| **Build Solution** (default) | Build entire solution in Debug mode | `dotnet build src/Chubb.PolicyManagement.sln` |
| Build Backend Only | Build backend projects only | `dotnet build src/backend` |
| Build Frontend | Build Angular frontend | `npm run build` (in frontend directory) |
| Clean Build Artifacts | Remove all bin/obj directories | `dotnet clean src/Chubb.PolicyManagement.sln` |
| Restore NuGet Packages | Restore all NuGet dependencies | `dotnet restore src/Chubb.PolicyManagement.sln` |
| Watch Build Backend | Watch for changes and rebuild automatically | `dotnet watch build` |
| Build & Test (Complete Pipeline) | Build + run tests in sequence | Builds then tests with coverage |

#### Test Tasks

| Task | Purpose | Command |
|---|---|---|
| **Run Unit Tests** (default) | Run all backend + frontend tests with coverage | `dotnet test src/Chubb.PolicyManagement.sln --collect:"XPlat Code Coverage"` |
| Run Backend Unit Tests | Run backend tests only with coverage | `dotnet test src/backend --collect:"XPlat Code Coverage"` |
| Run Frontend Tests | Run Angular tests with coverage | `npm test -- --watch=false --code-coverage` |
| Run Tests with Coverage Report | Run tests and generate OpenCover report | Tests with coverage output to `./coverage/` |

### Running Tasks via Command Line

```bash
# Build solution
cd "c:\Users\VishwanathaReddyKs\OneDrive - EPAM\Coding\AI\Chubb-PolicyManagement-CaseStudy-V3\Chubb-PolicyManagement"
dotnet build src/Chubb.PolicyManagement.sln

# Run tests with coverage
dotnet test src/Chubb.PolicyManagement.sln --collect:"XPlat Code Coverage"

# Run backend tests only
dotnet test src/backend --configuration Debug

# Frontend tests
cd src/frontend/policy-dashboard
npm test -- --watch=false --code-coverage
```

---

## Debug Configurations

Launch the debugger using the Debug sidebar or by pressing `F5`. Available configurations:

### Backend API
- **Name**: Backend API
- **Action**: Launches the ASP.NET Core API on `http://localhost:5000`
- **Pre-launch Task**: Builds backend first
- **Opens Browser**: Automatically opens the app in your browser on successful startup

### Backend Tests
- **Name**: Backend Tests
- **Action**: Runs unit tests with debugger attached
- **Pre-launch Task**: Builds backend first

### Attach to Backend
- **Name**: Attach to Backend
- **Action**: Attaches debugger to a running backend process
- **Use Case**: Debugging a process that's already running

---

## CI/CD Pipeline — GitHub Actions

### Trigger Events

The workflow runs automatically on:
- **Push** to `main` or `develop` branches
- **Pull requests** targeting `main` or `develop` branches

### Pipeline Stages

#### 1. **Build & Test Job** (`build-and-test`)

Steps:
1. Checkout code
2. Setup .NET 8.0
3. Restore NuGet packages
4. Build solution in Release mode
5. **Run backend unit tests** with code coverage (XPlat OpenCover format)
6. **Validate 90% coverage threshold** — fails if below 90% for lines or branches
7. Build Angular frontend
8. Run frontend tests with coverage
9. Upload coverage reports to Codecov
10. Archive build artifacts and coverage reports

**Coverage Threshold**: Code must maintain **≥ 90%** line and branch coverage.

#### 2. **Code Quality Job** (`code-quality`)

Steps:
1. Run code analysis
2. Build with `/p:TreatWarningsAsErrors=true` — treats all warnings as errors
3. Report status

#### 3. **Notify Job** (`notify`)

- Determines overall pipeline status
- Fails if any previous job failed
- Provides clear pass/fail feedback

### Accessing Results

- **Artifacts**: GitHub Actions stores coverage reports and build artifacts for 5–10 days
  - Access via: Actions tab → Workflow run → Artifacts
- **Codecov**: Coverage reports are uploaded to Codecov for trend tracking
- **Workflow Logs**: Full build and test logs available in GitHub Actions console

### Local Reproduction

To reproduce the CI/CD pipeline locally:

```bash
# Full build + test (as GitHub Actions would run)
dotnet build src/Chubb.PolicyManagement.sln --configuration Release
dotnet test src/backend --configuration Release --collect:"XPlat Code Coverage;Format=opencover"

# Check coverage threshold
# Look for coverage.opencover.xml in coverage folder
```

---

## Entity Framework Core — Mandatory Rules

All EF Core code **must** follow the standards in `.github/instructions/ef-core-rules.instructions.md`.

### Key Rules

| Rule | Details |
|---|---|
| **DbContext Configuration** | Entity mapping via `IEntityTypeConfiguration<T>` — **no data annotations** |
| **String Lengths** | Every string property must have explicit `HasMaxLength()` |
| **Migrations** | Use `dotnet ef migrations add` — apply via `Database.Migrate()` in development only |
| **Seeding** | Idempotent seeding via `SeedData` class — check for existing data before inserting |
| **Queries** | Use `.AsNoTracking()` for read-only queries; paginate at DB level |
| **Security** | Parameterized queries only — never string concatenation |
| **Repository Pattern** | Interfaces in `Domain`, implementations in `Infrastructure` |
| **Testing** | Mock `IPolicyRepository` in unit tests; use in-memory DbContext for integration tests |

### Creating a Migration

```bash
cd src/backend/Chubb.PolicyManagement.Infrastructure

# Create migration
dotnet ef migrations add InitialSchema \
  --context ChubbPolicyManagementContext \
  --project Chubb.PolicyManagement.Infrastructure \
  --startup-project ../Chubb.PolicyManagement.Api \
  --configuration Debug

# Apply migration (development only)
dotnet ef database update --context ChubbPolicyManagementContext
```

---

## Coverage Reporting

### Backend Coverage

Coverage reports are generated in `.opencover` format during test runs:

```bash
dotnet test src/backend --collect:"XPlat Code Coverage;Format=opencover" --results-directory ./coverage
```

Coverage files are stored in `./coverage/*/coverage.opencover.xml`.

### Frontend Coverage

Angular coverage is generated via Karma/Jest:

```bash
cd src/frontend/policy-dashboard
npm test -- --code-coverage
```

Coverage report: `src/frontend/policy-dashboard/coverage/index.html`

### Viewing Coverage Reports

- **Backend (Local)**: Open `./coverage/*/coverage.opencover.xml` in a coverage viewer
- **Frontend (Local)**: Open `src/frontend/policy-dashboard/coverage/index.html` in browser
- **Codecov (Remote)**: View trend reports at codecov.io for the repository

---

## Troubleshooting

### Build Fails with "EF Core Migration not found"

**Solution**: Ensure migrations are applied before running tests:
```bash
dotnet ef database update --context ChubbPolicyManagementContext
```

### Tests Fail with "Connection string not found"

**Solution**: Set environment variables or add to `appsettings.Development.json`:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=ChubbPolicyDb;Trusted_Connection=true;Encrypt=false;"
  }
}
```

### Coverage Below 90%

**Issue**: Tests don't cover all public code paths.

**Solution**:
1. Identify uncovered code via coverage report
2. Add unit tests for untested paths (see code-coverage.instructions.md)
3. Run coverage again: `dotnet test --collect:"XPlat Code Coverage"`

### GitHub Actions Workflow Fails

1. Check workflow logs: GitHub → Actions tab → Click failed run
2. Look for error messages in "Run backend unit tests" or "Check code coverage threshold" steps
3. Reproduce locally: `dotnet build --configuration Release && dotnet test --collect:"XPlat Code Coverage"`

---

## Quick Start Checklist

- [ ] Install .NET 8 SDK
- [ ] Install Node.js 18+ (for frontend)
- [ ] Install SQL Server LocalDB or Docker
- [ ] Run `dotnet restore src/Chubb.PolicyManagement.sln`
- [ ] Run `dotnet build src/Chubb.PolicyManagement.sln`
- [ ] Run `dotnet test src/Chubb.PolicyManagement.sln --collect:"XPlat Code Coverage"`
- [ ] Verify all tests pass and coverage ≥ 90%
- [ ] Open `src/frontend/policy-dashboard` and run `npm install && npm test`
- [ ] Use VS Code tasks: `Ctrl+Shift+P` → "Tasks: Run Task" → "Build Solution"

---

## File Reference

| File | Purpose |
|---|---|
| `.vscode/tasks.json` | Build and test tasks for VS Code |
| `.vscode/launch.json` | Debug configurations |
| `.github/workflows/build-and-test.yml` | GitHub Actions CI/CD pipeline |
| `.github/instructions/ef-core-rules.instructions.md` | EF Core coding standards |
| `.github/instructions/code-coverage.instructions.md` | 90% coverage requirement |
| `.github/instructions/exception-handling.instructions.md` | RFC 7807 error handling |

---

## Next Steps

1. **Review EF Core Rules**: Read `.github/instructions/ef-core-rules.instructions.md` for mandatory patterns
2. **Try a Build Task**: Run "Build Solution" from VS Code Task Runner
3. **Run Tests**: Execute "Run Unit Tests" task to verify everything works
4. **Debug**: Press `F5` to launch Backend API with debugger
5. **Create a Migration**: Follow the migration creation steps to practice EF Core setup

---

**Last Updated**: 2026-06-09
