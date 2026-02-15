using System.Net;
using backend.Data;
using backend.Models;
using backend.Services;
using backend.Tests.Integration.Fixtures;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;

namespace backend.Tests.Integration.Controllers;

/// <summary>
/// Integration tests for ImageProxyController using WebApplicationFactory.
/// Tests image proxy functionality with mocked Blackboard service.
/// </summary>
public class ImageProxyControllerIntegrationTests
    : IClassFixture<CustomWebApplicationFactory>,
        IAsyncLifetime
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;
    private readonly MongoDbContext _context;
    private const string ValidSessionCookie = "test-session-image";

    public ImageProxyControllerIntegrationTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();

        var scope = factory.Services.CreateScope();
        _context = scope.ServiceProvider.GetRequiredService<MongoDbContext>();
    }

    public async ValueTask InitializeAsync()
    {
        await CleanupDatabase();
    }

    public async ValueTask DisposeAsync()
    {
        await CleanupDatabase();
    }

    private async Task CleanupDatabase()
    {
        await _context.Users.DeleteManyAsync(Builders<User>.Filter.Empty);
    }

    private HttpResponseMessage CreateImageResponse(
        byte[] imageBytes,
        string contentType,
        HttpStatusCode statusCode = HttpStatusCode.OK
    )
    {
        var response = new HttpResponseMessage(statusCode);
        response.Content = new ByteArrayContent(imageBytes);
        response.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(
            contentType
        );
        return response;
    }

    #region Get Image Tests

    [Fact]
    public async Task Get_WithValidSessionAndImage_ReturnsImageBytes()
    {
        var imageBytes = new byte[] { 0xFF, 0xD8, 0xFF, 0xE0 }; // JPEG magic bytes
        var response = CreateImageResponse(imageBytes, "image/jpeg");

        _factory.ResetMocks();
        _factory
            .MockBlackboardService.Setup(s =>
                s.GetProxiedImageResponseAsync(
                    It.Is<string>(c => c.Contains(ValidSessionCookie)),
                    "https://blackboard.ual.es/image.jpg",
                    It.IsAny<string>()
                )
            )
            .ReturnsAsync(response);

        _client.DefaultRequestHeaders.Add("X-Session-Cookie", ValidSessionCookie);
        var result = await _client.GetAsync(
            "/api/imageproxy?imageUrl=https://blackboard.ual.es/image.jpg"
        );

        result.StatusCode.Should().Be(HttpStatusCode.OK);
        result.Content.Headers.ContentType!.MediaType.Should().Be("image/jpeg");
        var resultBytes = await result.Content.ReadAsByteArrayAsync();
        resultBytes.Should().BeEquivalentTo(imageBytes);
    }

    [Fact]
    public async Task Get_WithoutImageUrl_ReturnsBadRequest()
    {
        _client.DefaultRequestHeaders.Add("X-Session-Cookie", ValidSessionCookie);

        var response = await _client.GetAsync("/api/imageproxy");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Get_WithoutSessionCookie_ReturnsUnauthorized()
    {
        var response = await _client.GetAsync(
            "/api/imageproxy?imageUrl=https://example.com/image.jpg"
        );

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Get_WithUnauthorizedResponse_ReturnsUnauthorized()
    {
        var errorResponse = new HttpResponseMessage(HttpStatusCode.Unauthorized);

        _factory.ResetMocks();
        _factory
            .MockBlackboardService.Setup(s =>
                s.GetProxiedImageResponseAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>()
                )
            )
            .ReturnsAsync(errorResponse);

        _client.DefaultRequestHeaders.Add("X-Session-Cookie", ValidSessionCookie);
        var response = await _client.GetAsync(
            "/api/imageproxy?imageUrl=https://example.com/image.jpg"
        );

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Get_WithForbiddenResponse_ReturnsUnauthorized()
    {
        var errorResponse = new HttpResponseMessage(HttpStatusCode.Forbidden);

        _factory.ResetMocks();
        _factory
            .MockBlackboardService.Setup(s =>
                s.GetProxiedImageResponseAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>()
                )
            )
            .ReturnsAsync(errorResponse);

        _client.DefaultRequestHeaders.Add("X-Session-Cookie", ValidSessionCookie);
        var response = await _client.GetAsync(
            "/api/imageproxy?imageUrl=https://example.com/image.jpg"
        );

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Get_WithNotAcceptableResponse_ReturnsNotAcceptable()
    {
        var errorResponse = new HttpResponseMessage(HttpStatusCode.NotAcceptable);

        _factory.ResetMocks();
        _factory
            .MockBlackboardService.Setup(s =>
                s.GetProxiedImageResponseAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>()
                )
            )
            .ReturnsAsync(errorResponse);

        _client.DefaultRequestHeaders.Add("X-Session-Cookie", ValidSessionCookie);
        var response = await _client.GetAsync(
            "/api/imageproxy?imageUrl=https://example.com/image.jpg"
        );

        response.StatusCode.Should().Be(HttpStatusCode.NotAcceptable);
    }

    [Fact]
    public async Task Get_WithNotFoundResponse_ReturnsNotFound()
    {
        var errorResponse = new HttpResponseMessage(HttpStatusCode.NotFound);

        _factory.ResetMocks();
        _factory
            .MockBlackboardService.Setup(s =>
                s.GetProxiedImageResponseAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>()
                )
            )
            .ReturnsAsync(errorResponse);

        _client.DefaultRequestHeaders.Add("X-Session-Cookie", ValidSessionCookie);
        var response = await _client.GetAsync(
            "/api/imageproxy?imageUrl=https://example.com/image.jpg"
        );

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Get_WithNullResponse_ReturnsNotFound()
    {
        _factory.ResetMocks();
        _factory
            .MockBlackboardService.Setup(s =>
                s.GetProxiedImageResponseAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>()
                )
            )
            .ReturnsAsync((HttpResponseMessage?)null);

        _client.DefaultRequestHeaders.Add("X-Session-Cookie", ValidSessionCookie);
        var response = await _client.GetAsync(
            "/api/imageproxy?imageUrl=https://example.com/image.jpg"
        );

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Get_WithSessionCookieOnly_NoEqualsSign_FormatsAsCookie()
    {
        var imageBytes = new byte[] { 0x89, 0x50, 0x4E, 0x47 };
        var response = CreateImageResponse(imageBytes, "image/png");

        _factory.ResetMocks();
        _factory
            .MockBlackboardService.Setup(s =>
                s.GetProxiedImageResponseAsync(
                    "bb_session=simple-token",
                    It.IsAny<string>(),
                    It.IsAny<string>()
                )
            )
            .ReturnsAsync(response);

        _client.DefaultRequestHeaders.Add("X-Session-Cookie", "simple-token");
        var result = await _client.GetAsync(
            "/api/imageproxy?imageUrl=https://example.com/image.png"
        );

        result.StatusCode.Should().Be(HttpStatusCode.OK);

        _factory.MockBlackboardService.Verify(
            s =>
                s.GetProxiedImageResponseAsync(
                    "bb_session=simple-token",
                    It.IsAny<string>(),
                    It.IsAny<string>()
                ),
            Times.Once
        );
    }

    [Fact]
    public async Task Get_WithCookieAlreadyFormatted_PassesAsIs()
    {
        var imageBytes = new byte[] { 0x47, 0x49, 0x46, 0x38 };
        var response = CreateImageResponse(imageBytes, "image/gif");

        _factory.ResetMocks();
        _factory
            .MockBlackboardService.Setup(s =>
                s.GetProxiedImageResponseAsync(
                    "bb_session=already-formatted",
                    It.IsAny<string>(),
                    It.IsAny<string>()
                )
            )
            .ReturnsAsync(response);

        _client.DefaultRequestHeaders.Add("X-Session-Cookie", "bb_session=already-formatted");
        var result = await _client.GetAsync(
            "/api/imageproxy?imageUrl=https://example.com/image.gif"
        );

        result.StatusCode.Should().Be(HttpStatusCode.OK);

        _factory.MockBlackboardService.Verify(
            s =>
                s.GetProxiedImageResponseAsync(
                    "bb_session=already-formatted",
                    It.IsAny<string>(),
                    It.IsAny<string>()
                ),
            Times.Once
        );
    }

    [Fact]
    public async Task Get_WithWebPImage_ReturnsWebPContentType()
    {
        var imageBytes = new byte[] { 0x52, 0x49, 0x46, 0x46 };
        var response = CreateImageResponse(imageBytes, "image/webp");

        _factory.ResetMocks();
        _factory
            .MockBlackboardService.Setup(s =>
                s.GetProxiedImageResponseAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>()
                )
            )
            .ReturnsAsync(response);

        _client.DefaultRequestHeaders.Add("X-Session-Cookie", ValidSessionCookie);
        var result = await _client.GetAsync(
            "/api/imageproxy?imageUrl=https://example.com/image.webp"
        );

        result.StatusCode.Should().Be(HttpStatusCode.OK);
        result.Content.Headers.ContentType!.MediaType.Should().Be("image/webp");
    }

    [Fact]
    public async Task Get_WithSVGImage_ReturnsSVGContentType()
    {
        var imageBytes = System.Text.Encoding.UTF8.GetBytes("<svg></svg>");
        var response = CreateImageResponse(imageBytes, "image/svg+xml");

        _factory.ResetMocks();
        _factory
            .MockBlackboardService.Setup(s =>
                s.GetProxiedImageResponseAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>()
                )
            )
            .ReturnsAsync(response);

        _client.DefaultRequestHeaders.Add("X-Session-Cookie", ValidSessionCookie);
        var result = await _client.GetAsync(
            "/api/imageproxy?imageUrl=https://example.com/image.svg"
        );

        result.StatusCode.Should().Be(HttpStatusCode.OK);
        result.Content.Headers.ContentType!.MediaType.Should().Be("image/svg+xml");
    }

    [Fact]
    public async Task Get_WithUnknownImageType_DefaultsToJpeg()
    {
        var imageBytes = new byte[] { 0x00, 0x00, 0x00, 0x00 };
        var response = CreateImageResponse(imageBytes, "application/octet-stream");

        _factory.ResetMocks();
        _factory
            .MockBlackboardService.Setup(s =>
                s.GetProxiedImageResponseAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>()
                )
            )
            .ReturnsAsync(response);

        _client.DefaultRequestHeaders.Add("X-Session-Cookie", ValidSessionCookie);
        var result = await _client.GetAsync(
            "/api/imageproxy?imageUrl=https://example.com/image.bin"
        );

        result.StatusCode.Should().Be(HttpStatusCode.OK);
        result.Content.Headers.ContentType!.MediaType.Should().Be("image/jpeg");
    }

    [Fact]
    public async Task Get_ForwardsAcceptHeader()
    {
        var imageBytes = new byte[] { 0xFF, 0xD8, 0xFF };
        var response = CreateImageResponse(imageBytes, "image/jpeg");
        string? capturedAcceptHeader = null;

        _factory.ResetMocks();
        _factory
            .MockBlackboardService.Setup(s =>
                s.GetProxiedImageResponseAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>()
                )
            )
            .Callback<string, string, string>((_, _, accept) => capturedAcceptHeader = accept)
            .ReturnsAsync(response);

        _client.DefaultRequestHeaders.Add("X-Session-Cookie", ValidSessionCookie);
        _client.DefaultRequestHeaders.Add(
            "Accept",
            "image/webp,image/apng,image/svg+xml,image/*,*/*;q=0.8"
        );

        var result = await _client.GetAsync(
            "/api/imageproxy?imageUrl=https://example.com/image.jpg"
        );

        result.StatusCode.Should().Be(HttpStatusCode.OK);
        capturedAcceptHeader.Should().NotBeNullOrEmpty();
        capturedAcceptHeader.Should().Contain("image/webp");
    }

    [Fact]
    public async Task Get_WithServiceException_ReturnsInternalServerError()
    {
        _factory.ResetMocks();
        _factory
            .MockBlackboardService.Setup(s =>
                s.GetProxiedImageResponseAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>()
                )
            )
            .ThrowsAsync(new Exception("Network error"));

        _client.DefaultRequestHeaders.Add("X-Session-Cookie", ValidSessionCookie);
        var response = await _client.GetAsync(
            "/api/imageproxy?imageUrl=https://example.com/image.jpg"
        );

        response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
    }

    #endregion
}
