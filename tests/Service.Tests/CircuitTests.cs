using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Service.Tests;

public sealed class CircuitTests : IClassFixture<WebApplicationFactory<Program>> {
    private readonly WebApplicationFactory<Program> _factory;

    public CircuitTests(WebApplicationFactory<Program> factory) {
        _factory = factory;
    }

    [Fact]
    public async Task Status_can_change() {
        var client = _factory.CreateClient();
        var response = await client.PostAsJsonAsync("/circuits/CKT-0001/status", new { status = "Planned" });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
