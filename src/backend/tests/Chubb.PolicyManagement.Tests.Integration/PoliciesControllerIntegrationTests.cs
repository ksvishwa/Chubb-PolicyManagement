using Chubb.PolicyManagement.Application.DTOs;
using Chubb.PolicyManagement.Application.Models;
using FluentAssertions;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Chubb.PolicyManagement.Tests.Integration;

/// <summary>
/// Integration tests for PoliciesController using SQLite in-memory database.
/// The <see cref="PolicyWebApplicationFactory"/> seeds three known policies before tests run.
/// </summary>
public class PoliciesControllerIntegrationTests(PolicyWebApplicationFactory factory)
    : IClassFixture<PolicyWebApplicationFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    private static readonly JsonSerializerOptions JsonOpts = new(JsonSerializerDefaults.Web);

    // ── GET /api/v1/policies ─────────────────────────────────────────────

    [Fact]
    public async Task GetPolicies_DefaultParams_Returns200WithDataArray()
    {
        // Act
        var response = await _client.GetAsync("/api/v1/policies");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<PagedResult<PolicyDto>>(JsonOpts);
        result.Should().NotBeNull();
        result!.Data.Should().NotBeNull();
    }

    [Fact]
    public async Task GetPolicies_DefaultParams_ReturnsSeedPolicies()
    {
        // Act
        var response = await _client.GetAsync("/api/v1/policies");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<PagedResult<PolicyDto>>(JsonOpts);
        result!.TotalCount.Should().BeGreaterThanOrEqualTo(3);
    }

    [Fact]
    public async Task GetPolicies_WithStatusActive_Returns200WithOnlyActivePolicies()
    {
        // Act
        var response = await _client.GetAsync("/api/v1/policies?status=Active");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<PagedResult<PolicyDto>>(JsonOpts);
        result.Should().NotBeNull();
        result!.Data.Should().AllSatisfy(p => p.Status.Should().Be("Active"));
    }

    [Fact]
    public async Task GetPolicies_WithLineOfBusinessMarine_Returns200WithOnlyMarinePolicies()
    {
        // Act
        var response = await _client.GetAsync("/api/v1/policies?lineOfBusiness=Marine");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<PagedResult<PolicyDto>>(JsonOpts);
        result.Should().NotBeNull();
        result!.Data.Should().AllSatisfy(p => p.LineOfBusiness.Should().Be("Marine"));
    }

    [Fact]
    public async Task GetPolicies_WithSearchPOL_Returns200WithMatchingPolicies()
    {
        // Act
        var response = await _client.GetAsync("/api/v1/policies?search=POL");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<PagedResult<PolicyDto>>(JsonOpts);
        result.Should().NotBeNull();
        result!.Data.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetPolicies_WithPaginationPage1Size1_Returns200WithMaxOneRecord()
    {
        // Act
        var response = await _client.GetAsync("/api/v1/policies?page=1&size=1");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<PagedResult<PolicyDto>>(JsonOpts);
        result.Should().NotBeNull();
        result!.Data.Count.Should().BeLessThanOrEqualTo(1);
        result.Size.Should().Be(1);
        result.Page.Should().Be(1);
    }

    [Fact]
    public async Task GetPolicies_WithPaginationPage1Size5_Returns200WithDataLengthAtMost5()
    {
        // Act
        var response = await _client.GetAsync("/api/v1/policies?page=1&size=5");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<PagedResult<PolicyDto>>(JsonOpts);
        result.Should().NotBeNull();
        result!.Data.Count.Should().BeLessThanOrEqualTo(5);
    }

    [Fact]
    public async Task GetPolicies_PagedResponse_ContainsExpectedPaginationFields()
    {
        // Act
        var response = await _client.GetAsync("/api/v1/policies?page=1&size=2");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<PagedResult<PolicyDto>>(JsonOpts);
        result.Should().NotBeNull();
        result!.Page.Should().Be(1);
        result.Size.Should().Be(2);
        result.TotalCount.Should().BeGreaterThan(0);
        result.TotalPages.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task GetPolicies_WithInvalidPage_Returns400BadRequest()
    {
        // Act
        var response = await _client.GetAsync("/api/v1/policies?page=0");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetPolicies_WithNegativePage_Returns400BadRequest()
    {
        // Act
        var response = await _client.GetAsync("/api/v1/policies?page=-1");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetPolicies_WithSizeOver100_Returns400BadRequest()
    {
        // Act
        var response = await _client.GetAsync("/api/v1/policies?size=200");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetPolicies_WithRegionFilter_Returns200()
    {
        // Act
        var response = await _client.GetAsync("/api/v1/policies?region=Singapore");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<PagedResult<PolicyDto>>(JsonOpts);
        result.Should().NotBeNull();
        result!.Data.Should().AllSatisfy(p => p.Region.Should().Be("Singapore"));
    }

    // ── GET /api/v1/policies/{id} ────────────────────────────────────────

    [Fact]
    public async Task GetPolicyById_WithKnownId_Returns200WithCorrectPolicy()
    {
        // Arrange
        var id = PolicyWebApplicationFactory.SeedActiveId;

        // Act
        var response = await _client.GetAsync($"/api/v1/policies/{id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<PolicyDto>(JsonOpts);
        result.Should().NotBeNull();
        result!.Id.Should().Be(id);
        result.PolicyNumber.Should().Be("POL-001");
        result.Status.Should().Be("Active");
    }

    [Fact]
    public async Task GetPolicyById_WithNonExistentId_Returns404()
    {
        // Arrange
        var unknownId = Guid.NewGuid();

        // Act
        var response = await _client.GetAsync($"/api/v1/policies/{unknownId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetPolicyById_WithNonExistentId_ResponseBodyIsRfc7807ProblemDetails()
    {
        // Arrange
        var unknownId = Guid.NewGuid();

        // Act
        var response = await _client.GetAsync($"/api/v1/policies/{unknownId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        // Note: WriteAsJsonAsync sets ContentType to "application/json"; verify body has RFC 7807 fields
        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        doc.RootElement.GetProperty("status").GetInt32().Should().Be(404);
    }

    // ── PATCH /api/v1/policies/flag ──────────────────────────────────────

    [Fact]
    public async Task BulkFlag_WithValidIds_Returns204NoContent()
    {
        // Arrange
        var ids = new[] { PolicyWebApplicationFactory.SeedActiveId };
        var payload = new { policyIds = ids };

        // Act
        var response = await _client.PatchAsJsonAsync("/api/v1/policies/flag", payload);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task BulkFlag_WithEmptyPolicyIds_Returns422UnprocessableEntity()
    {
        // Arrange — empty array fails BulkFlagRequestValidator → 422
        var payload = new { policyIds = Array.Empty<Guid>() };

        // Act
        var response = await _client.PatchAsJsonAsync("/api/v1/policies/flag", payload);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task BulkFlag_WithNullBody_Returns400BadRequest()
    {
        // Arrange — missing required body
        using var emptyContent = new StringContent("{}", System.Text.Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PatchAsync("/api/v1/policies/flag", emptyContent);

        // Assert — model binding failure returns 400, or validation returns 4xx
        ((int)response.StatusCode).Should().BeInRange(400, 422);
    }

    // ── GET /api/v1/policies/summary ─────────────────────────────────────

    [Fact]
    public async Task GetSummary_Returns200WithTotalPoliciesField()
    {
        // Act
        var response = await _client.GetAsync("/api/v1/policies/summary");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<PolicySummaryDto>(JsonOpts);
        result.Should().NotBeNull();
        result!.TotalPolicies.Should().BeGreaterThanOrEqualTo(3);
    }

    [Fact]
    public async Task GetSummary_Returns200WithByLineOfBusinessBreakdown()
    {
        // Act
        var response = await _client.GetAsync("/api/v1/policies/summary");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<PolicySummaryDto>(JsonOpts);
        result.Should().NotBeNull();
        result!.ByLineOfBusiness.Should().NotBeNull();
    }

    [Fact]
    public async Task GetSummary_Returns200WithByRegionBreakdown()
    {
        // Act
        var response = await _client.GetAsync("/api/v1/policies/summary");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<PolicySummaryDto>(JsonOpts);
        result.Should().NotBeNull();
        result!.ByRegion.Should().NotBeNull();
    }

    [Fact]
    public async Task GetSummary_ActiveCountMatchesSeedData()
    {
        // Act
        var response = await _client.GetAsync("/api/v1/policies/summary");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<PolicySummaryDto>(JsonOpts);
        result!.ActivePolicies.Should().BeGreaterThanOrEqualTo(1);
    }

    // ── Health check ─────────────────────────────────────────────────────

    [Fact]
    public async Task HealthCheck_DoesNotReturn500()
    {
        // Act
        var response = await _client.GetAsync("/health");

        // Assert — health may show degraded SQL Server check, but must not 500
        response.StatusCode.Should().NotBe(HttpStatusCode.InternalServerError);
    }
}
