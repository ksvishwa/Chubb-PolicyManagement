# Copilot Instructions 

## Stack
- Backend: C# / .NET 8 (Clean Architecture)
- Frontend: Angular 17+ (standalone components)
- Database: SQL Server (EF Core migrations)
- API: Contract-first OpenAPI 3.x

## Architecture Rules
- Clean Architecture: Domain → Application → Infrastructure → API
- No infrastructure concerns in Domain or Application layers
- Use Result<T> pattern for error handling — no raw exceptions in service layer
- All endpoints must match the OpenAPI spec exactly

## Code Style
- Use record types for DTOs
- Async/await throughout — no .Result or .Wait()
- Dependency injection for all services
- No magic strings — use constants or enums

## Testing
- xUnit for unit tests, integration tests use WebApplicationFactory
- Test method naming: MethodName_Scenario_ExpectedResult
- Arrange/Act/Assert comments in every test

## Agent Authoring Rules
- Agents must be **generic and purely technical** — no project-specific names, domain terms, or business context in the agent body
- Do NOT hardcode entity names (e.g. `Policy`, `Chubb`, `APAC`), endpoint paths, or schema fields inside agent definitions
- Describe capabilities in terms of technology and patterns only (e.g. "EF Core migrations", "ASP.NET Core controllers", "Angular standalone components")
- Project-specific context belongs in `.instructions.md` files (loaded via `applyTo`) — not in `.agent.md` files
- Agent `description` and `argument-hint` may reference the project name for discoverability, but the agent body must remain reusable across projects