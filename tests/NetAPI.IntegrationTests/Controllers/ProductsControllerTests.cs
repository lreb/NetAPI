using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using FluentAssertions;
using Microsoft.IdentityModel.Tokens;
using NetAPI.Application.Commands;
using NetAPI.Application.DTOs;

namespace NetAPI.IntegrationTests.Controllers;

public class ProductsControllerTests : IClassFixture<ApiWebApplicationFactory>
{
    private readonly HttpClient _client;

    private const string JwtKey = "integration-test-super-secret-key-32chars!!";
    private const string JwtIssuer = "NetAPI";
    private const string JwtAudience = "NetAPI.Client";

    public ProductsControllerTests(ApiWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetAll_ShouldReturn200()
    {
        var response = await _client.GetAsync("/api/v1/products");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetById_NonExistent_ShouldReturn404()
    {
        var response = await _client.GetAsync($"/api/v1/products/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Create_WithoutAuth_ShouldReturn401()
    {
        var command = new CreateProductCommand("Test", "desc", 10m, "USD", 5);
        var response = await _client.PostAsJsonAsync("/api/v1/products", command);
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Create_WithAdminToken_ShouldReturn201()
    {
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", GenerateJwtToken("Admin"));

        var command = new CreateProductCommand("Integration Product", "From test", 49.99m, "USD", 10);
        var response = await _client.PostAsJsonAsync("/api/v1/products", command);

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var dto = await response.Content.ReadFromJsonAsync<ProductDto>();
        dto.Should().NotBeNull();
        dto!.Name.Should().Be("Integration Product");
        dto.Price.Should().Be(49.99m);
    }

    /// <summary>Generates a valid JWT for test purposes using the known test signing key.</summary>
    private static string GenerateJwtToken(string role)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(JwtKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()),
            new Claim(ClaimTypes.Name, "test-user"),
            new Claim(ClaimTypes.Role, role)
        };

        var token = new JwtSecurityToken(
            issuer: JwtIssuer,
            audience: JwtAudience,
            claims: claims,
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
