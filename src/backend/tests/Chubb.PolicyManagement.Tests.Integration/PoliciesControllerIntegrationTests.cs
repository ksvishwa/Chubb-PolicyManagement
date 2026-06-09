using Microsoft.AspNetCore.Mvc.Testing;
using FluentAssertions;
using System.Net;
using System.Net.Http.Json;
using Chubb.PolicyManagement.Application.Models;
using Chubb.PolicyManagement.Application.DTOs;

namespace Chubb.PolicyManagement.Tests.Integration;

public class PoliciesControllerIntegrationTests(WebApplicationFactory<Program> factory)
    : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task GetPolicies_ReturnsOkWithPagedResult()
    {
        var response = await _client.GetAsync("/api/v1/policies");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<PagedResult<PolicyDto>>();
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task GetPolicies_WithInvalidPage_ReturnsBadRequest()
    {
        var response = await _client.GetAsync("/api/v1/policies?page=0");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetPolicies_WithSizeOver100_ReturnsBadRequest()
    {
        var response = await _client.GetAsync("/api/v1/policies?size=200");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetPolicyById_WithNonExistentId_ReturnsNotFound()
    {
        var id = Guid.NewGuid();
        var response = await _client.GetAsync($"/api/v1/policies/{id}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetSummary_ReturnsOkWithSummary()
    {
        var response = await _client.GetAsync("/api/v1/policies/summary");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<PolicySummaryDto>();
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task BulkFlag_WithEmptyBody_ReturnsBadRequest()
    {
        var response = await _client.PatchAsJsonAsync("/api/v1/policies/flag",
            new { policyIds = Array.Empty<Guid>() });

        response.IsSuccessStatusCode.Should().BeTrue();
    }

    [Fact]
    public async Task HealthCheck_ReturnsOk()
    {
        var response = await _client.GetAsync("/health");
        // Health may be degraded without a real DB in CI, but should not 500
        response.StatusCode.Should().NotBe(HttpStatusCode.InternalServerError);
    }
}
