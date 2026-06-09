---
description: >
  Use when Copilot generates, adds, or modifies any code — backend C#, frontend Angular/TypeScript,
  or configuration. Enforces a minimum 90% code coverage requirement for all Copilot-authored code.
  Applies to unit tests, integration tests, and any new feature or fix.
applyTo: "**"
---

# Code Coverage Requirement — 90% Minimum

## Rule

**Every unit of code added or modified by Copilot must be accompanied by tests that achieve at least 90% code coverage.**

This applies to:
- New classes, methods, and functions
- Modified logic in existing classes
- Angular components, services, pipes, and guards
- C# services, repositories, controllers, and domain entities

---

## Backend (.NET / xUnit)

- Write xUnit tests for **every public method** introduced or changed.
- Use `Moq` for mocking dependencies and `FluentAssertions` for assertions.
- Test naming convention: `MethodName_StateUnderTest_ExpectedBehaviour`
  - Example: `GetPoliciesAsync_WithStatusFilter_ReturnsFilteredResults`
- Cover all branches: happy path, edge cases, and exception paths.
- Run coverage with:
  ```bash
  dotnet test --collect:"XPlat Code Coverage"
  ```
- Minimum threshold — add or maintain in each test `.csproj`:
  ```xml
  <PropertyGroup>
    <CollectCoverage>true</CollectCoverage>
    <CoverletOutputFormat>cobertura</CoverletOutputFormat>
    <Threshold>90</Threshold>
    <ThresholdType>line,branch,method</ThresholdType>
    <ThresholdStat>total</ThresholdStat>
  </PropertyGroup>
  ```

### What to test (backend)
| Layer | What to Cover |
|---|---|
| `Application/Services` | All public service methods, filter logic, mapping |
| `Api/Controllers` | All HTTP actions via `WebApplicationFactory` |
| `Domain/Entities` | Domain logic, validation, exceptions |
| `Infrastructure` | Repository logic (use in-memory or integration DB) |

---

## Frontend (Angular / Jasmine / Jest)

- Write component, service, and pipe tests for **every file introduced or changed**.
- Use `HttpClientTestingModule` to mock HTTP calls in service tests.
- Handle `loading`, `error`, and `empty` states in every component test.
- Run coverage with:
  ```bash
  ng test --code-coverage --watch=false
  ```
- Enforce in `angular.json` (under the project's `test` target):
  ```json
  "codeCoverageExclude": [],
  "coverageReporter": ["html", "lcov", "text-summary"],
  "karmaConfig": "karma.conf.js"
  ```
- Add thresholds to `karma.conf.js`:
  ```js
  coverageReporter: {
    check: {
      global: {
        statements: 90,
        branches: 90,
        functions: 90,
        lines: 90
      }
    }
  }
  ```

### What to test (frontend)
| Type | What to Cover |
|---|---|
| Components | Render, input bindings, output events, loading/error/empty states |
| Services | All public methods, HTTP calls mocked |
| Pipes & Directives | Transformation logic, edge inputs |
| Guards & Interceptors | Auth, error-handling, loading indicator |

---

## General Rules

1. **No code without tests.** Copilot must generate or update the corresponding test file alongside any code change.
2. **Tests must actually assert behaviour** — not just call the method and expect no errors.
3. **Coverage is measured on lines, branches, and methods** — all three must hit 90%.
4. **Do not skip or exclude code from coverage** unless it is a generated file (e.g., EF Core migration files, `*.Designer.cs`).
5. **Integration tests count** toward coverage — but unit tests must still exist for service layer logic.
6. If coverage drops below 90% after a change, **fix the gap before considering the task complete**.
