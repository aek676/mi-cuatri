using backend.Controllers;
using backend.Dtos;
using backend.Repositories;
using backend.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

namespace backend.Tests.Unit.Controllers;

/// <summary>
/// Unit tests for GoogleAuthController.
/// Tests Google OAuth2 connect/callback flow.
/// </summary>
public class GoogleAuthControllerTests
{
    private readonly Mock<IUserRepository> _mockUserRepository;
    private readonly Mock<IBlackboardService> _mockBlackboardService;
    private readonly Mock<IConfiguration> _mockConfiguration;
    private readonly GoogleAuthController _controller;

    public GoogleAuthControllerTests()
    {
        _mockUserRepository = new Mock<IUserRepository>();
        _mockBlackboardService = new Mock<IBlackboardService>();
        _mockConfiguration = new Mock<IConfiguration>();
        _controller = new GoogleAuthController(
            _mockUserRepository.Object,
            _mockBlackboardService.Object,
            _mockConfiguration.Object
        );
    }

    private void SetupValidConfiguration()
    {
        _mockConfiguration.Setup(c => c["Google:ClientId"]).Returns("test-client-id");
        _mockConfiguration.Setup(c => c["Google:RedirectUri"]).Returns("http://localhost:5000/api/auth/google/callback");
        _mockConfiguration.Setup(c => c["Google:ClientSecret"]).Returns("test-client-secret");
    }

    private void SetupControllerContext(string? sessionCookie = "bb_session=test123")
    {
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };
        if (!string.IsNullOrEmpty(sessionCookie))
        {
            _controller.Request.Headers["X-Session-Cookie"] = sessionCookie;
        }
    }

    #region GET api/auth/google/connect Tests

    [Fact]
    public void Connect_ValidSession_ReturnsAuthUrl()
    {
        // Arrange
        SetupControllerContext();
        SetupValidConfiguration();

        // Act
        var result = _controller.Connect("bb_session=test123");

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(200);
        
        var response = okResult.Value.Should().BeOfType<GoogleConnectResponse>().Subject;
        response.Url.Should().NotBeNull();
        response.Url.ToString().Should().Contain("accounts.google.com");
        response.StateToken.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void Connect_MissingSession_Returns400()
    {
        // Arrange
        SetupControllerContext(null);
        SetupValidConfiguration();

        // Act
        var result = _controller.Connect(null);

        // Assert
        var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        badRequestResult.StatusCode.Should().Be(400);
    }

    [Fact]
    public void Connect_MissingConfiguration_Returns500()
    {
        // Arrange
        SetupControllerContext();
        _mockConfiguration.Setup(c => c["Google:ClientId"]).Returns((string?)null);
        _mockConfiguration.Setup(c => c["Google:RedirectUri"]).Returns((string?)null);

        // Act
        var result = _controller.Connect("bb_session=test123");

        // Assert
        var problemResult = result.Should().BeOfType<ObjectResult>().Subject;
        problemResult.StatusCode.Should().Be(500);
    }

    #endregion

    #region GET api/auth/google/callback Tests

    [Fact]
    public async Task Callback_MissingCode_Returns400()
    {
        // Arrange
        SetupValidConfiguration();

        // Act
        var result = await _controller.Callback(null, "state123");

        // Assert
        var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        badRequestResult.StatusCode.Should().Be(400);
        
        var response = badRequestResult.Value.Should().BeAssignableTo<object>().Subject;
        response.ToString().Should().Contain("Missing code");
    }

    [Fact]
    public async Task Callback_MissingState_Returns400()
    {
        // Arrange
        SetupValidConfiguration();

        // Act
        var result = await _controller.Callback("authcode123", null);

        // Assert
        var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        badRequestResult.StatusCode.Should().Be(400);
        
        var response = badRequestResult.Value.Should().BeAssignableTo<object>().Subject;
        response.ToString().Should().Contain("Missing state");
    }

    [Fact]
    public async Task Callback_InvalidState_Returns400()
    {
        // Arrange
        SetupValidConfiguration();

        // Act
        var result = await _controller.Callback("authcode123", "invalid_state");

        // Assert
        var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        badRequestResult.StatusCode.Should().Be(400);
        
        var response = badRequestResult.Value.Should().BeAssignableTo<object>().Subject;
        response.ToString().Should().Contain("Invalid or expired state token");
    }

    #endregion
}
