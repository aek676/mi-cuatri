using backend.Controllers;
using backend.Dtos;
using backend.Repositories;
using backend.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace backend.Tests.Unit.Controllers;

/// <summary>
/// Unit tests for AuthController.
/// Tests authentication endpoints including login and user data retrieval.
/// </summary>
public class AuthControllerTests
{
    private readonly Mock<IBlackboardService> _mockBlackboardService;
    private readonly Mock<IUserRepository> _mockUserRepository;
    private readonly AuthController _controller;

    public AuthControllerTests()
    {
        _mockBlackboardService = new Mock<IBlackboardService>();
        _mockUserRepository = new Mock<IUserRepository>();
        _controller = new AuthController(_mockBlackboardService.Object, _mockUserRepository.Object);
    }

    #region POST api/Auth/login-ual Tests

    [Fact]
    public async Task Login_ValidCredentials_ReturnsSuccess()
    {
        var request = new LoginRequestDto { Username = "testuser", Password = "testpass123" };

        var loginResponse = new LoginResponseDto
        {
            IsSuccess = true,
            Message = "Login Exitoso",
            SessionCookie = "bb_session=abc123def456",
        };

        var userDataResponse = new UserResponseDto
        {
            IsSuccess = true,
            Message = "OK",
            UserData = new UserDetailDto
            {
                Given = "Test",
                Family = "User",
                Email = "test@ual.es",
                Avatar = "https://example.com/avatar.jpg",
            },
        };

        _mockBlackboardService
            .Setup(s => s.AuthenticateAsync(request.Username, request.Password))
            .ReturnsAsync(loginResponse);

        _mockBlackboardService
            .Setup(s => s.GetUserDataAsync(loginResponse.SessionCookie))
            .ReturnsAsync(userDataResponse);

        var result = await _controller.Login(request);

        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(200);

        var response = okResult.Value.Should().BeOfType<LoginResponseDto>().Subject;
        response.IsSuccess.Should().BeTrue();
        response.Message.Should().Be("Login Exitoso");
        response.SessionCookie.Should().Be("bb_session=abc123def456");

        _mockUserRepository.Verify(
            r => r.UpsertByUsernameAsync(request.Username, "test@ual.es"),
            Times.Once
        );
    }

    [Fact]
    public async Task Login_ValidCredentials_GetUserDataFails_StillPersistsUsername()
    {
        var request = new LoginRequestDto { Username = "testuser", Password = "testpass123" };

        var loginResponse = new LoginResponseDto
        {
            IsSuccess = true,
            Message = "Login Exitoso",
            SessionCookie = "bb_session=abc123def456",
        };

        _mockBlackboardService
            .Setup(s => s.AuthenticateAsync(request.Username, request.Password))
            .ReturnsAsync(loginResponse);

        _mockBlackboardService
            .Setup(s => s.GetUserDataAsync(loginResponse.SessionCookie))
            .ThrowsAsync(new Exception("Network error"));

        var result = await _controller.Login(request);

        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(200);

        var response = okResult.Value.Should().BeOfType<LoginResponseDto>().Subject;
        response.IsSuccess.Should().BeTrue();

        // Should still persist username even if GetUserData fails (with null email)
        _mockUserRepository.Verify(
            r => r.UpsertByUsernameAsync(request.Username, null),
            Times.Once
        );
    }

    [Fact]
    public async Task Login_ValidCredentials_UserDataReturnsNoEmail_PersistsUsernameOnly()
    {
        // Arrange
        var request = new LoginRequestDto { Username = "testuser", Password = "testpass123" };

        var loginResponse = new LoginResponseDto
        {
            IsSuccess = true,
            Message = "Login Exitoso",
            SessionCookie = "bb_session=abc123def456",
        };

        var userDataResponse = new UserResponseDto
        {
            IsSuccess = true,
            Message = "OK",
            UserData = new UserDetailDto
            {
                Given = "Test",
                Family = "User",
                Email = null,
                Avatar = "",
            },
        };

        _mockBlackboardService
            .Setup(s => s.AuthenticateAsync(request.Username, request.Password))
            .ReturnsAsync(loginResponse);

        _mockBlackboardService
            .Setup(s => s.GetUserDataAsync(loginResponse.SessionCookie))
            .ReturnsAsync(userDataResponse);

        // Act
        var result = await _controller.Login(request);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(200);

        _mockUserRepository.Verify(
            r => r.UpsertByUsernameAsync(request.Username, null),
            Times.Once
        );
    }

    [Fact]
    public async Task Login_InvalidCredentials_ReturnsUnauthorized()
    {
        // Arrange
        var request = new LoginRequestDto { Username = "testuser", Password = "wrongpassword" };

        var loginResponse = new LoginResponseDto
        {
            IsSuccess = false,
            Message = "Fallo en login. Revise contraseña.",
        };

        _mockBlackboardService
            .Setup(s => s.AuthenticateAsync(request.Username, request.Password))
            .ReturnsAsync(loginResponse);

        // Act
        var result = await _controller.Login(request);

        // Assert
        var unauthorizedResult = result
            .Result.Should()
            .BeOfType<UnauthorizedObjectResult>()
            .Subject;
        unauthorizedResult.StatusCode.Should().Be(401);

        var response = unauthorizedResult.Value.Should().BeOfType<LoginResponseDto>().Subject;
        response.IsSuccess.Should().BeFalse();
        response.Message.Should().Be("Fallo en login. Revise contraseña.");

        // Should NOT persist user when login fails
        _mockUserRepository.Verify(
            r => r.UpsertByUsernameAsync(It.IsAny<string>(), It.IsAny<string?>()),
            Times.Never
        );
    }

    #endregion

    #region GET api/Auth/me Tests

    [Fact]
    public async Task GetMe_ValidSession_ReturnsUserData()
    {
        // Arrange
        var sessionCookie = "bb_session=validcookie123";

        var userDataResponse = new UserResponseDto
        {
            IsSuccess = true,
            Message = "OK",
            UserData = new UserDetailDto
            {
                Given = "John",
                Family = "Doe",
                Email = "john.doe@ual.es",
                Avatar = "https://example.com/avatar.jpg",
            },
        };

        _mockBlackboardService
            .Setup(s => s.GetUserDataAsync(sessionCookie))
            .ReturnsAsync(userDataResponse);

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext(),
        };
        _controller.Request.Headers["X-Session-Cookie"] = sessionCookie;

        // Act
        var result = await _controller.Me(sessionCookie);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(200);

        var response = okResult.Value.Should().BeOfType<UserResponseDto>().Subject;
        response.IsSuccess.Should().BeTrue();
        response.UserData.Should().NotBeNull();
        response.UserData!.Given.Should().Be("John");
        response.UserData.Family.Should().Be("Doe");
        response.UserData.Email.Should().Be("john.doe@ual.es");
    }

    [Fact]
    public async Task GetMe_MissingSession_ReturnsBadRequest()
    {
        // Arrange
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext(),
        };

        // Act
        var result = await _controller.Me(null);

        // Assert
        var badRequestResult = result.Result.Should().BeOfType<BadRequestObjectResult>().Subject;
        badRequestResult.StatusCode.Should().Be(400);

        var message = badRequestResult.Value.Should().BeOfType<string>().Subject;
        message.Should().Contain("Session cookie is required");
    }

    [Fact]
    public async Task GetMe_EmptySession_ReturnsBadRequest()
    {
        // Arrange
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext(),
        };
        _controller.Request.Headers["X-Session-Cookie"] = "";

        // Act
        var result = await _controller.Me("");

        // Assert
        var badRequestResult = result.Result.Should().BeOfType<BadRequestObjectResult>().Subject;
        badRequestResult.StatusCode.Should().Be(400);

        var message = badRequestResult.Value.Should().BeOfType<string>().Subject;
        message.Should().Contain("Session cookie is required");
    }

    [Fact]
    public async Task GetMe_InvalidSession_ReturnsBadRequest()
    {
        // Arrange
        var sessionCookie = "bb_session=invalidcookie";

        var userDataResponse = new UserResponseDto
        {
            IsSuccess = false,
            Message = "API returned 401",
        };

        _mockBlackboardService
            .Setup(s => s.GetUserDataAsync(sessionCookie))
            .ReturnsAsync(userDataResponse);

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext(),
        };
        _controller.Request.Headers["X-Session-Cookie"] = sessionCookie;

        // Act
        var result = await _controller.Me(sessionCookie);

        // Assert
        var badRequestResult = result.Result.Should().BeOfType<BadRequestObjectResult>().Subject;
        badRequestResult.StatusCode.Should().Be(400);

        var response = badRequestResult.Value.Should().BeOfType<UserResponseDto>().Subject;
        response.IsSuccess.Should().BeFalse();
        response.Message.Should().Be("API returned 401");
    }

    [Fact]
    public async Task GetMe_ValidSession_FromStandardCookieHeader_ReturnsUserData()
    {
        // Arrange
        var sessionCookie = "bb_session=cookiefromstandardheader";

        var userDataResponse = new UserResponseDto
        {
            IsSuccess = true,
            Message = "OK",
            UserData = new UserDetailDto
            {
                Given = "Jane",
                Family = "Smith",
                Email = "jane.smith@ual.es",
                Avatar = "",
            },
        };

        _mockBlackboardService
            .Setup(s => s.GetUserDataAsync(sessionCookie))
            .ReturnsAsync(userDataResponse);

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext(),
        };
        // Set in standard Cookie header instead of X-Session-Cookie
        _controller.Request.Headers["Cookie"] = sessionCookie;

        // Act - pass null for sessionCookieHeader, controller should fallback to Cookie header
        var result = await _controller.Me(null);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(200);

        var response = okResult.Value.Should().BeOfType<UserResponseDto>().Subject;
        response.IsSuccess.Should().BeTrue();
        response.UserData!.Given.Should().Be("Jane");
    }

    #endregion
}
