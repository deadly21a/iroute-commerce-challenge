using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Commerce.Api.Models;
using Commerce.Api.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace Commerce.Tests;

public sealed class HttpContractTests : IDisposable
{
    private readonly WebApplicationFactory<Program> factory = new WebApplicationFactory<Program>()
        .WithWebHostBuilder(builder => builder.UseEnvironment("Development"));

    private HttpClient AuthenticatedClient()
    {
        var client = factory.CreateClient();
        var token = factory.Services.GetRequiredService<TokenService>().Create(new AppUser(1, "test@example.com", "Test", ""));
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token.Token);
        return client;
    }

    [Fact]
    public async Task CommerceRequiresAuthentication()
    {
        using var client = factory.CreateClient();
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/commerce/overview")).StatusCode);
    }

    [Fact]
    public async Task InvalidLoginIsBadRequestBeforeDatabaseAccess()
    {
        using var client = factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/auth/login", new { email = "not-an-email", password = "" });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Theory]
    [InlineData("{}")]
    [InlineData("{\"processDate\":null}")]
    [InlineData("{\"processDate\":\"2026-02-30\"}")]
    public async Task MissingOrInvalidProcessDateIsBadRequest(string body)
    {
        using var client = AuthenticatedClient();
        using var content = new StringContent(body, System.Text.Encoding.UTF8, "application/json");
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsync("/api/commerce/process", content)).StatusCode);
    }

    [Fact]
    public async Task EmptyUploadIsBadRequest()
    {
        using var client = AuthenticatedClient();
        using var multipart = new MultipartFormDataContent();
        multipart.Add(new ByteArrayContent([]), "file", "commerce_07102026.csv");
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsync("/api/commerce/import", multipart)).StatusCode);
    }

    [Fact]
    public async Task SwaggerCanDescribeMultipartUpload()
    {
        using var client = factory.CreateClient();
        var response = await client.GetAsync("/swagger/v1/swagger.json");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var document = await response.Content.ReadAsStringAsync();
        Assert.Contains("multipart/form-data", document);
        Assert.Contains("securitySchemes", document);
    }

    public void Dispose() => factory.Dispose();
}
