using backend.Controllers;
using backend.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Net;

namespace backend.Tests.Unit.Controllers;

/// <summary>
/// Unit tests for ImageProxyController.
/// Tests image proxying functionality from Blackboard.
/// </summary>
public class ImageProxyControllerTests
{
    private readonly Mock<IBlackboardService> _mockBlackboardService;
    private readonly Mock<ILogger<ImageProxyController>> _mockLogger;
    private readonly ImageProxyController _controller;

    public ImageProxyControllerTests()
    {
        _mockBlackboardService = new Mock<IBlackboardService>();
        _mockLogger = new Mock<ILogger<ImageProxyController>>();
        _controller = new ImageProxyController(
            _mockBlackboardService.Object,
            _mockLogger.Object
        );
    }

    private void SetupControllerContext(string? acceptHeader = "image/webp,image/*")
    {
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };
        if (!string.IsNullOrEmpty(acceptHeader))
        {
            _controller.Request.Headers["Accept"] = acceptHeader;
        }
    }

    private HttpResponseMessage CreateImageResponse(byte[] imageBytes, string contentType = "image/jpeg")
    {
        var response = new HttpResponseMessage(HttpStatusCode.OK);
        response.Content = new ByteArrayContent(imageBytes);
        response.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(contentType);
        return response;
    }

    #region GET api/ImageProxy Tests

    [Fact]
    public async Task GetImage_ValidRequest_ReturnsImage()
    {
        // Arrange
        SetupControllerContext();
        var imageBytes = new byte[] { 0xFF, 0xD8, 0xFF }; // JPEG magic bytes
        
        _mockBlackboardService
            .Setup(s => s.GetProxiedImageResponseAsync("bb_session=test123", "https://aulavirtual.ual.es/image.jpg", "image/webp,image/*"))
            .ReturnsAsync(CreateImageResponse(imageBytes, "image/jpeg"));

        // Act
        var result = await _controller.Get("https://aulavirtual.ual.es/image.jpg", "test123");

        // Assert
        var fileResult = result.Should().BeOfType<FileContentResult>().Subject;
        fileResult.ContentType.Should().Be("image/jpeg");
        fileResult.FileContents.Should().Equal(imageBytes);
    }

    [Fact]
    public async Task GetImage_MissingUrl_Returns400()
    {
        // Arrange
        SetupControllerContext();

        // Act
        var result = await _controller.Get(null, "test123");

        // Assert
        var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        badRequestResult.StatusCode.Should().Be(400);
        
        var error = badRequestResult.Value.Should().BeAssignableTo<object>().Subject;
        error.ToString().Should().Contain("imageUrl parameter is required");
    }

    [Fact]
    public async Task GetImage_MissingSession_Returns401()
    {
        // Arrange
        SetupControllerContext();

        // Act
        var result = await _controller.Get("https://aulavirtual.ual.es/image.jpg", null);

        // Assert
        var unauthorizedResult = result.Should().BeOfType<UnauthorizedObjectResult>().Subject;
        unauthorizedResult.StatusCode.Should().Be(401);
        
        var error = unauthorizedResult.Value.Should().BeAssignableTo<object>().Subject;
        error.ToString().Should().Contain("Authentication token required");
    }

    [Fact]
    public async Task GetImage_NullResponse_Returns404()
    {
        // Arrange
        SetupControllerContext();
        
        _mockBlackboardService
            .Setup(s => s.GetProxiedImageResponseAsync("bb_session=test123", "https://aulavirtual.ual.es/image.jpg", It.IsAny<string>()))
            .ReturnsAsync((HttpResponseMessage?)null);

        // Act
        var result = await _controller.Get("https://aulavirtual.ual.es/image.jpg", "test123");

        // Assert
        var notFoundResult = result.Should().BeOfType<NotFoundObjectResult>().Subject;
        notFoundResult.StatusCode.Should().Be(404);
    }

    [Fact]
    public async Task GetImage_UnauthorizedFromBlackboard_Returns401()
    {
        // Arrange
        SetupControllerContext();
        
        var response = new HttpResponseMessage(HttpStatusCode.Unauthorized);
        _mockBlackboardService
            .Setup(s => s.GetProxiedImageResponseAsync("bb_session=test123", "https://aulavirtual.ual.es/image.jpg", It.IsAny<string>()))
            .ReturnsAsync(response);

        // Act
        var result = await _controller.Get("https://aulavirtual.ual.es/image.jpg", "test123");

        // Assert
        var unauthorizedResult = result.Should().BeOfType<UnauthorizedObjectResult>().Subject;
        unauthorizedResult.StatusCode.Should().Be(401);
        
        var error = unauthorizedResult.Value.Should().BeAssignableTo<object>().Subject;
        error.ToString().Should().Contain("Invalid or expired session token");
    }

    [Fact]
    public async Task GetImage_NotFound_Returns404()
    {
        // Arrange
        SetupControllerContext();
        
        var response = new HttpResponseMessage(HttpStatusCode.NotFound);
        _mockBlackboardService
            .Setup(s => s.GetProxiedImageResponseAsync("bb_session=test123", "https://aulavirtual.ual.es/image.jpg", It.IsAny<string>()))
            .ReturnsAsync(response);

        // Act
        var result = await _controller.Get("https://aulavirtual.ual.es/image.jpg", "test123");

        // Assert
        var notFoundResult = result.Should().BeOfType<NotFoundObjectResult>().Subject;
        notFoundResult.StatusCode.Should().Be(404);
        
        var error = notFoundResult.Value.Should().BeAssignableTo<object>().Subject;
        error.ToString().Should().Contain("Image not found");
    }

    [Fact]
    public async Task GetImage_ServerError_Returns500()
    {
        // Arrange
        SetupControllerContext();
        
        _mockBlackboardService
            .Setup(s => s.GetProxiedImageResponseAsync("bb_session=test123", "https://aulavirtual.ual.es/image.jpg", It.IsAny<string>()))
            .ThrowsAsync(new Exception("Network error"));

        // Act
        var result = await _controller.Get("https://aulavirtual.ual.es/image.jpg", "test123");

        // Assert
        var serverErrorResult = result.Should().BeOfType<ObjectResult>().Subject;
        serverErrorResult.StatusCode.Should().Be(500);
        
        var error = serverErrorResult.Value.Should().BeAssignableTo<object>().Subject;
        error.ToString().Should().Contain("Failed to fetch image");
    }

    [Fact]
    public async Task GetImage_FormatsBareTokenAsCookie()
    {
        // Arrange
        SetupControllerContext();
        var imageBytes = new byte[] { 0xFF, 0xD8 };
        
        // When session cookie doesn't contain "=", it should be formatted as "bb_session={token}"
        _mockBlackboardService
            .Setup(s => s.GetProxiedImageResponseAsync("bb_session=bare_token_value", "https://aulavirtual.ual.es/image.jpg", It.IsAny<string>()))
            .ReturnsAsync(CreateImageResponse(imageBytes));

        // Act - Pass bare token without "="
        var result = await _controller.Get("https://aulavirtual.ual.es/image.jpg", "bare_token_value");

        // Assert
        result.Should().BeOfType<FileContentResult>();
        _mockBlackboardService.Verify(s => s.GetProxiedImageResponseAsync(
            "bb_session=bare_token_value", 
            It.IsAny<string>(), 
            It.IsAny<string>()
        ), Times.Once);
    }

    [Fact]
    public async Task GetImage_UsesProvidedAcceptHeader()
    {
        // Arrange
        SetupControllerContext("image/avif,image/webp,image/*");
        var imageBytes = new byte[] { 0xFF, 0xD8 };
        
        _mockBlackboardService
            .Setup(s => s.GetProxiedImageResponseAsync("bb_session=test123", "https://aulavirtual.ual.es/image.jpg", "image/avif,image/webp,image/*"))
            .ReturnsAsync(CreateImageResponse(imageBytes));

        // Act
        var result = await _controller.Get("https://aulavirtual.ual.es/image.jpg", "test123");

        // Assert
        result.Should().BeOfType<FileContentResult>();
        _mockBlackboardService.Verify(s => s.GetProxiedImageResponseAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            "image/avif,image/webp,image/*"
        ), Times.Once);
    }

    #endregion
}
