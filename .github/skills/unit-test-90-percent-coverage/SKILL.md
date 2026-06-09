---
name: "unit-test-90-percent-coverage"
description: >
  Write unit tests for the Chubb APAC Policy Management Platform achieving 90%+ code coverage.
  Covers xUnit + Moq + FluentAssertions (backend) and Jasmine/Jest (Angular frontend) patterns,
  AAA structure, test naming conventions, mocking strategies, and coverage enforcement.
---

# Unit Test — 90% Coverage

## When to Use

Invoke this skill when asked to:
- Write unit tests for services, controllers, or repositories
- Improve code coverage on existing code
- Test edge cases, null paths, and error scenarios
- Set up mocks, stubs, or test fixtures
- Debug failing tests or coverage gaps
- Validate coverage thresholds in CI

**Trigger phrases**: "write tests", "unit test", "90 percent coverage", "mock", "test coverage", "failing test", "AAA pattern", "coverage log", "log coverage"

---

## Coverage Logging Hook

Every test run **automatically** writes a timestamped coverage entry to `coverage-logs/`.

### How it works

| Component | File | Role |
|---|---|---|
| MSBuild target | `Tests.Unit.csproj` — `LogCoverageAfterTest` | Fires after `VSTest`, calls the script |
| Log script | `scripts/log-coverage.ps1` | Parses OpenCover XML, writes log files |
| History log | `coverage-logs/coverage-history.md` | Appended on every run (git-tracked) |
| Latest summary | `coverage-logs/latest-summary.md` | Overwritten each run with current result |
| Raw XML | `coverage-logs/coverage.opencover.xml` | Git-ignored (generated artifact) |

### Output files

```
coverage-logs/
├── coverage-history.md      ← Timestamped history of every run (committed to git)
├── latest-summary.md        ← Always reflects the most recent run
└── coverage.opencover.xml   ← Raw XML (git-ignored)
```

### Running the log script manually

```powershell
# After dotnet test has produced coverage XML:
pwsh -File scripts/log-coverage.ps1 -CoverageDir coverage-logs
```

### VS Code task

Use **"Log Coverage"** task (`Ctrl+Shift+P` → Tasks: Run Task → Log Coverage) to re-run the
log script against the last coverage output without re-running all tests.

### Example `coverage-history.md` entry

```markdown
## ✅ 2026-06-09 14:30:15 — PASS

| Metric | Covered | Total | Percentage | Threshold |
|--------|---------|-------|------------|-----------|
| Lines  | 1842    | 1950  | 94.46%     | 90%       |
| Branches | —     | —     | 91.20%     | 90%       |
| Methods | 312    | 320   | 97.50%     | 90%       |
| Classes | 48     | 50    | —          | —         |
```

---

## Prerequisites

- xUnit, Moq, FluentAssertions NuGet packages installed
- Test project references Domain, Application, Infrastructure
- `Threshold=90` set in test `.csproj`
- Read `.github/instructions/code-coverage.instructions.md`

---

## Test Project Configuration

**File**: `src/backend/Chubb.PolicyManagement.Tests.Unit/Chubb.PolicyManagement.Tests.Unit.csproj`

```xml
<PropertyGroup>
  <TargetFramework>net8.0</TargetFramework>
  <IsTestProject>true</IsTestProject>
  <Nullable>enable</Nullable>
  <ImplicitUsings>enable</ImplicitUsings>
  <CollectCoverage>true</CollectCoverage>
  <CoverletOutputFormat>cobertura</CoverletOutputFormat>
  <Threshold>90</Threshold>
  <ThresholdType>line,branch,method</ThresholdType>
  <ThresholdStat>total</ThresholdStat>
</PropertyGroup>

<ItemGroup>
  <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.8.0" />
  <PackageReference Include="xunit" Version="2.6.0" />
  <PackageReference Include="xunit.runner.visualstudio" Version="2.5.0" />
  <PackageReference Include="Moq" Version="4.20.0" />
  <PackageReference Include="FluentAssertions" Version="6.12.0" />
  <PackageReference Include="coverlet.collector" Version="6.0.0" />
</ItemGroup>
```

---

## Naming Convention

Pattern: `MethodName_StateUnderTest_ExpectedBehaviour`

| Good | Bad |
|---|---|
| `GetByIdAsync_WithValidId_ReturnsPolicy` | `Test1` |
| `GetByIdAsync_WithInvalidId_ThrowsNotFoundException` | `GetById_Test` |
| `BulkFlagAsync_WithMixedResults_ReturnsCorrectCounts` | `ShouldFlag` |

---

## AAA Pattern (Required in Every Test)

```csharp
[Fact]
public async Task MethodName_Scenario_ExpectedResult()
{
    // Arrange
    var input = ...;
    var mock = new Mock<IDependency>();
    mock.Setup(...).Returns(...);

    // Act
    var result = await _sut.MethodAsync(input);

    // Assert
    result.Should().NotBeNull();
    result.Property.Should().Be(expected);
    mock.Verify(..., Times.Once);
}
```

---

## Service Layer Tests

**File**: `src/backend/Chubb.PolicyManagement.Tests.Unit/Services/PolicyServiceTests.cs`

```csharp
public class PolicyServiceTests
{
    private readonly Mock<IPolicyRepository> _mockRepo;
    private readonly PolicyService _sut;

    public PolicyServiceTests()
    {
        _mockRepo = new Mock<IPolicyRepository>();
        _sut = new PolicyService(_mockRepo.Object);
    }

    [Fact]
    public async Task GetPolicyByIdAsync_WithValidId_ReturnsDto()
    {
        // Arrange
        var id = Guid.NewGuid();
        var policy = new Policy { Id = id, PolicyNumber = "POL-001", Status = PolicyStatus.Active };
        _mockRepo.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(policy);

        // Act
        var result = await _sut.GetPolicyByIdAsync(id, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.PolicyNumber.Should().Be("POL-001");
        _mockRepo.Verify(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetPolicyByIdAsync_WithInvalidId_ThrowsNotFoundException()
    {
        // Arrange
        var id = Guid.NewGuid();
        _mockRepo.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()))
                 .ReturnsAsync((Policy?)null);

        // Act
        var act = () => _sut.GetPolicyByIdAsync(id, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<PolicyNotFoundException>();
    }

    [Fact]
    public async Task GetPoliciesAsync_WithStatusFilter_ReturnsFilteredResults()
    {
        // Arrange
        var filter = new PolicyFilterQuery { Page = 1, Size = 20, Status = "Active" };
        var policies = new List<Policy>
        {
            new() { Id = Guid.NewGuid(), PolicyNumber = "POL-001", Status = PolicyStatus.Active },
            new() { Id = Guid.NewGuid(), PolicyNumber = "POL-002", Status = PolicyStatus.Active }
        };
        _mockRepo.Setup(r => r.ListAsync(It.IsAny<PolicyFilterQuery>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync(policies);
        _mockRepo.Setup(r => r.CountAsync(It.IsAny<PolicyFilterQuery>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync(2);

        // Act
        var result = await _sut.GetPoliciesAsync(filter, CancellationToken.None);

        // Assert
        result.Data.Should().HaveCount(2);
        result.TotalCount.Should().Be(2);
        result.TotalPages.Should().Be(1);
        result.Page.Should().Be(1);
    }

    [Fact]
    public async Task GetPoliciesAsync_WithNoResults_ReturnsEmptyPage()
    {
        // Arrange
        _mockRepo.Setup(r => r.ListAsync(It.IsAny<PolicyFilterQuery>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync(new List<Policy>());
        _mockRepo.Setup(r => r.CountAsync(It.IsAny<PolicyFilterQuery>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync(0);

        // Act
        var result = await _sut.GetPoliciesAsync(new PolicyFilterQuery(), CancellationToken.None);

        // Assert
        result.Data.Should().BeEmpty();
        result.TotalCount.Should().Be(0);
    }

    [Fact]
    public async Task BulkFlagForReviewAsync_WithValidIds_FlagsAllPolicies()
    {
        // Arrange
        var ids = new[] { Guid.NewGuid(), Guid.NewGuid() };
        foreach (var id in ids)
        {
            _mockRepo.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()))
                     .ReturnsAsync(new Policy { Id = id, FlaggedForReview = false });
            _mockRepo.Setup(r => r.UpdateAsync(It.IsAny<Policy>(), It.IsAny<CancellationToken>()))
                     .Returns(Task.CompletedTask);
        }

        // Act
        var result = await _sut.BulkFlagForReviewAsync(ids, CancellationToken.None);

        // Assert
        result.FlaggedCount.Should().Be(2);
        result.FailedCount.Should().Be(0);
        _mockRepo.Verify(r => r.UpdateAsync(It.IsAny<Policy>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact]
    public async Task BulkFlagForReviewAsync_WhenPolicyNotFound_CountsAsFailed()
    {
        // Arrange
        var validId = Guid.NewGuid();
        var missingId = Guid.NewGuid();
        _mockRepo.Setup(r => r.GetByIdAsync(validId, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(new Policy { Id = validId });
        _mockRepo.Setup(r => r.GetByIdAsync(missingId, It.IsAny<CancellationToken>()))
                 .ReturnsAsync((Policy?)null);
        _mockRepo.Setup(r => r.UpdateAsync(It.IsAny<Policy>(), It.IsAny<CancellationToken>()))
                 .Returns(Task.CompletedTask);

        // Act
        var result = await _sut.BulkFlagForReviewAsync(new[] { validId, missingId }, CancellationToken.None);

        // Assert
        result.FlaggedCount.Should().Be(1);
        result.FailedCount.Should().Be(1);
    }
}
```

---

## Controller Layer Tests

```csharp
public class PoliciesControllerTests
{
    private readonly Mock<IPolicyService> _mockService;
    private readonly PoliciesController _sut;

    public PoliciesControllerTests()
    {
        _mockService = new Mock<IPolicyService>();
        _sut = new PoliciesController(_mockService.Object);
    }

    [Fact]
    public async Task GetPolicies_WithValidQuery_Returns200WithPagedResult()
    {
        // Arrange
        var paged = new PagedResult<PolicyDto>(new List<PolicyDto>(), 1, 20, 0, 0);
        _mockService.Setup(s => s.GetPoliciesAsync(It.IsAny<PolicyFilterQuery>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(paged);

        // Act
        var result = await _sut.GetPolicies(new PolicyFilterQuery(), CancellationToken.None);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>()
              .Which.Value.Should().Be(paged);
    }

    [Fact]
    public async Task GetPolicyById_WithValidId_Returns200WithDto()
    {
        // Arrange
        var id = Guid.NewGuid();
        var dto = new PolicyDto(id, "POL-001", "Active", "Property", "APAC",
            DateTime.Today, null, "Corp", "Agent", 1000m, false, DateTime.UtcNow, null);
        _mockService.Setup(s => s.GetPolicyByIdAsync(id, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(dto);

        // Act
        var result = await _sut.GetPolicyById(id, CancellationToken.None);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
    }
}
```

---

## Frontend Angular Tests

```typescript
// policy-api.service.spec.ts
describe('PolicyApiService', () => {
  let service: PolicyApiService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [HttpClientTestingModule],
      providers: [PolicyApiService]
    });
    service = TestBed.inject(PolicyApiService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  it('should fetch paginated policies', () => {
    const mockResponse = { data: [], page: 1, size: 20, totalCount: 0, totalPages: 0 };

    service.getPolicies({ page: 1, size: 20 }).subscribe(res => {
      expect(res.page).toBe(1);
      expect(res.data).toEqual([]);
    });

    const req = httpMock.expectOne(r => r.url.includes('/api/v1/policies'));
    expect(req.request.method).toBe('GET');
    req.flush(mockResponse);
  });

  it('should handle 404 error gracefully', () => {
    service.getPolicyById('bad-id').subscribe({
      error: err => expect(err.status).toBe(404)
    });

    const req = httpMock.expectOne('/api/v1/policies/bad-id');
    req.flush('Not Found', { status: 404, statusText: 'Not Found' });
  });
});
```

---

## Run Coverage

```bash
# Backend
dotnet test src/Chubb.PolicyManagement.sln --collect:"XPlat Code Coverage"

# Frontend
cd src/frontend/policy-dashboard
npm test -- --code-coverage --watch=false
```

---

## Coverage Failure Checklist

If coverage drops below 90%:
1. Run tests to get the report
2. Open `coverage/*/coverage.opencover.xml` — find uncovered lines
3. Add tests for each uncovered branch (null check, empty collection, exception path)
4. Re-run until all three metrics (line, branch, method) reach 90%

---

## Checklist

- [ ] Test project has `<Threshold>90</Threshold>` in `.csproj`
- [ ] All public service methods have happy-path test
- [ ] All public service methods have error/null-path test
- [ ] All controller actions tested via mock service
- [ ] Test names follow `MethodName_State_Behaviour` convention
- [ ] AAA comments present in every test
- [ ] `mock.Verify(...)` used to confirm interactions
- [ ] No production code modified in test file
- [ ] Coverage >= 90% for line, branch, and method
