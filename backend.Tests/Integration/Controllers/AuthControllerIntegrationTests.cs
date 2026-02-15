using System.Net;
using System.Net.Http.Json;
using backend.Data;
using backend.Dtos;
using backend.Models;
using backend.Repositories;
using backend.Services;
using backend.Tests.Integration.Fixtures;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;

namespace backend.Tests.Integration.Controllers;

/// <summary>
/// Integration tests for AuthController using WebApplicationFactory.
/// Tests authentication endpoints with real MongoDB via Testcontainers.
/// </summary>
public class AuthControllerIntegrationTests
    : IClassFixture<CustomWebApplicationFactory>,
        IAsyncLifetime
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;
    private readonly MongoDbContext _context;
    private readonly IUserRepository _userRepository;

    public AuthControllerIntegrationTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();

        var scope = factory.Services.CreateScope();
        _context = scope.ServiceProvider.GetRequiredService<MongoDbContext>();
        _userRepository = scope.ServiceProvider.GetRequiredService<IUserRepository>();
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

    #region Login Tests

    [Fact]
    public async Task Login_WithValidCredentials_ReturnsSuccessAndCreatesUser()
    {
        _factory.ResetMocks();
        _factory
            .MockBlackboardService.Setup(s => s.AuthenticateAsync("testuser", "testpass"))
            .ReturnsAsync(
                new LoginResponseDto
                {
                    IsSuccess = true,
                    Message = "Login successful",
                    SessionCookie = "test-session-123",
                }
            );

        _factory
            .MockBlackboardService.Setup(s => s.GetUserDataAsync("test-session-123"))
            .ReturnsAsync(
                new UserResponseDto
                {
                    IsSuccess = true,
                    Message = "User data retrieved",
                    UserData = new UserDetailDto
                    {
                        Given = "Test",
                        Family = "User",
                        Email = "testuser@ual.es",
                        Avatar = "https://example.com/avatar.png",
                    },
                }
            );

        var request = new LoginRequestDto { Username = "testuser", Password = "testpass" };

        var response = await _client.PostAsJsonAsync("/api/auth/login-ual", request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<LoginResponseDto>();
        result.Should().NotBeNull();
        result!.IsSuccess.Should().BeTrue();
        result.SessionCookie.Should().Be("test-session-123");

        var user = await _userRepository.GetByUsernameAsync("testuser");
        user.Should().NotBeNull();
        user!.Username.Should().Be("testuser");
    }

    [Fact]
    public async Task Login_WithInvalidCredentials_ReturnsUnauthorized()
    {
        _factory.ResetMocks();
        _factory
            .MockBlackboardService.Setup(s => s.AuthenticateAsync("testuser", "wrongpass"))
            .ReturnsAsync(
                new LoginResponseDto
                {
                    IsSuccess = false,
                    Message = "Invalid credentials",
                    SessionCookie = string.Empty,
                }
            );

        var request = new LoginRequestDto { Username = "testuser", Password = "wrongpass" };

        var response = await _client.PostAsJsonAsync("/api/auth/login-ual", request);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Login_UpdatesExistingUserEmail_WhenUserAlreadyExists()
    {
        await _userRepository.UpsertByUsernameAsync("existinguser");

        _factory.ResetMocks();
        _factory
            .MockBlackboardService.Setup(s => s.AuthenticateAsync("existinguser", "testpass"))
            .ReturnsAsync(
                new LoginResponseDto
                {
                    IsSuccess = true,
                    Message = "Login successful",
                    SessionCookie = "test-session-456",
                }
            );

        _factory
            .MockBlackboardService.Setup(s => s.GetUserDataAsync("test-session-456"))
            .ReturnsAsync(
                new UserResponseDto
                {
                    IsSuccess = true,
                    Message = "User data retrieved",
                    UserData = new UserDetailDto
                    {
                        Given = "Existing",
                        Family = "User",
                        Email = "existing@ual.es",
                        Avatar = "",
                    },
                }
            );

        var request = new LoginRequestDto { Username = "existinguser", Password = "testpass" };

        var response = await _client.PostAsJsonAsync("/api/auth/login-ual", request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var user = await _userRepository.GetByUsernameAsync("existinguser");
        user.Should().NotBeNull();
        user!.Email.Should().Be("existing@ual.es");
    }

    [Fact]
    public async Task Login_Succeeds_WhenGetUserDataFails()
    {
        _factory.ResetMocks();
        _factory
            .MockBlackboardService.Setup(s => s.AuthenticateAsync("testuser", "testpass"))
            .ReturnsAsync(
                new LoginResponseDto
                {
                    IsSuccess = true,
                    Message = "Login successful",
                    SessionCookie = "test-session-789",
                }
            );

        _factory
            .MockBlackboardService.Setup(s => s.GetUserDataAsync("test-session-789"))
            .ThrowsAsync(new Exception("Network error"));

        var request = new LoginRequestDto { Username = "testuser", Password = "testpass" };

        var response = await _client.PostAsJsonAsync("/api/auth/login-ual", request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var user = await _userRepository.GetByUsernameAsync("testuser");
        user.Should().NotBeNull();
    }

    #endregion

    #region Me Tests

    [Fact]
    public async Task Me_WithValidSession_ReturnsUserData()
    {
        _factory.ResetMocks();
        _factory
            .MockBlackboardService.Setup(s => s.GetUserDataAsync("valid-session"))
            .ReturnsAsync(
                new UserResponseDto
                {
                    IsSuccess = true,
                    Message = "User data retrieved",
                    UserData = new UserDetailDto
                    {
                        Given = "John",
                        Family = "Doe",
                        Email = "john.doe@ual.es",
                        Avatar = "https://example.com/avatar.jpg",
                    },
                }
            );

        _client.DefaultRequestHeaders.Add("X-Session-Cookie", "valid-session");
        var response = await _client.GetAsync("/api/auth/me");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<UserResponseDto>();
        result.Should().NotBeNull();
        result!.IsSuccess.Should().BeTrue();
        result.UserData!.Email.Should().Be("john.doe@ual.es");
    }

    [Fact]
    public async Task Me_WithoutSessionCookie_ReturnsBadRequest()
    {
        _client.DefaultRequestHeaders.Remove("X-Session-Cookie");

        var response = await _client.GetAsync("/api/auth/me");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Me_WithInvalidSession_ReturnsBadRequest()
    {
        _factory.ResetMocks();
        _factory
            .MockBlackboardService.Setup(s => s.GetUserDataAsync("invalid-session"))
            .ReturnsAsync(
                new UserResponseDto
                {
                    IsSuccess = false,
                    Message = "Session expired",
                    UserData = null,
                }
            );

        _client.DefaultRequestHeaders.Add("X-Session-Cookie", "invalid-session");
        var response = await _client.GetAsync("/api/auth/me");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Me_WithCookieHeader_ReturnsUserData()
    {
        _factory.ResetMocks();
        _factory
            .MockBlackboardService.Setup(s => s.GetUserDataAsync("cookie-session"))
            .ReturnsAsync(
                new UserResponseDto
                {
                    IsSuccess = true,
                    Message = "User data retrieved",
                    UserData = new UserDetailDto
                    {
                        Given = "Cookie",
                        Family = "User",
                        Email = "cookie@ual.es",
                        Avatar = "",
                    },
                }
            );

        _client.DefaultRequestHeaders.Remove("X-Session-Cookie");
        _client.DefaultRequestHeaders.Add("Cookie", "cookie-session");

        var response = await _client.GetAsync("/api/auth/me");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    #endregion

    #region User Persistence Tests

    [Fact]
    public async Task Login_PersistsUserInDatabase()
    {
        _factory.ResetMocks();
        _factory
            .MockBlackboardService.Setup(s => s.AuthenticateAsync("newuser", "password"))
            .ReturnsAsync(
                new LoginResponseDto
                {
                    IsSuccess = true,
                    Message = "Login successful",
                    SessionCookie = "session-abc",
                }
            );

        _factory
            .MockBlackboardService.Setup(s => s.GetUserDataAsync("session-abc"))
            .ReturnsAsync(
                new UserResponseDto
                {
                    IsSuccess = true,
                    Message = "User data retrieved",
                    UserData = new UserDetailDto
                    {
                        Given = "New",
                        Family = "User",
                        Email = "newuser@ual.es",
                        Avatar = "",
                    },
                }
            );

        var request = new LoginRequestDto { Username = "newuser", Password = "password" };

        await _client.PostAsJsonAsync("/api/auth/login-ual", request);

        // Direct database verification
        var user = await _context.Users.Find(u => u.Username == "newuser").FirstOrDefaultAsync();

        user.Should().NotBeNull();
        user.Username.Should().Be("newuser");
        user.Email.Should().Be("newuser@ual.es");
    }

    [Fact]
    public async Task Login_WithGetUserDataSuccessButNoEmail_PersistsUserWithoutEmail()
    {
        _factory.ResetMocks();
        _factory
            .MockBlackboardService.Setup(s => s.AuthenticateAsync("userwithoutemail", "password"))
            .ReturnsAsync(
                new LoginResponseDto
                {
                    IsSuccess = true,
                    Message = "Login successful",
                    SessionCookie = "session-def",
                }
            );

        _factory
            .MockBlackboardService.Setup(s => s.GetUserDataAsync("session-def"))
            .ReturnsAsync(
                new UserResponseDto
                {
                    IsSuccess = true,
                    Message = "User data retrieved",
                    UserData = new UserDetailDto
                    {
                        Given = "No",
                        Family = "Email",
                        Email = string.Empty, // No email
                        Avatar = "",
                    },
                }
            );

        var request = new LoginRequestDto { Username = "userwithoutemail", Password = "password" };

        await _client.PostAsJsonAsync("/api/auth/login-ual", request);

        var user = await _userRepository.GetByUsernameAsync("userwithoutemail");
        user.Should().NotBeNull();
        user!.Email.Should().BeNullOrEmpty();
    }

    #endregion
}
