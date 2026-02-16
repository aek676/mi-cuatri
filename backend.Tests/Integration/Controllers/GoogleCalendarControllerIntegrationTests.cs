using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using backend.Data;
using backend.Dtos;
using backend.Enums;
using backend.Models;
using backend.Repositories;
using backend.Services;
using backend.Tests.Helpers;
using backend.Tests.Integration.Fixtures;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;

namespace backend.Tests.Integration.Controllers;

/// <summary>
/// Integration tests for GoogleCalendarController using WebApplicationFactory.
/// Tests Google Calendar integration endpoints with real MongoDB.
/// </summary>
public class GoogleCalendarControllerIntegrationTests
    : IClassFixture<CustomWebApplicationFactory>,
        IAsyncLifetime
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;
    private readonly MongoDbContext _context;
    private readonly IUserRepository _userRepository;
    private readonly TestDataBuilder _testDataBuilder;
    private const string ValidSessionCookie = "test-session-google";
    private const string ValidEmail = "testuser@ual.es";
    private readonly JsonSerializerOptions _jsonOptions;

    public GoogleCalendarControllerIntegrationTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
        _testDataBuilder = new TestDataBuilder();
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            Converters = { new JsonStringEnumConverter() }
        };

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

    private void SetupValidAuthenticationMocks()
    {
        _factory.ResetMocks();

        _factory
            .MockBlackboardService.Setup(s =>
                s.GetUserDataAsync(It.Is<string>(c => c.Contains(ValidSessionCookie)))
            )
            .ReturnsAsync(
                new UserResponseDto
                {
                    IsSuccess = true,
                    Message = "User data retrieved",
                    UserData = new UserDetailDto
                    {
                        Given = "Test",
                        Family = "User",
                        Email = ValidEmail,
                        Avatar = "",
                    },
                }
            );
    }

    private async Task CreateTestUser(bool withGoogleAccount = false)
    {
        await _userRepository.UpsertByUsernameAsync("testuser", ValidEmail);

        if (withGoogleAccount)
        {
            var googleAccount = new GoogleAccount
            {
                GoogleId = "google-123",
                Email = "testuser@gmail.com",
                RefreshToken = "refresh-token",
                AccessToken = "access-token",
                AccessTokenExpiry = DateTime.UtcNow.AddHours(1),
                Scopes = new[] { "calendar" },
            };
            await _userRepository.UpsertGoogleAccountAsync("testuser", googleAccount);
        }
    }

    #region Status Tests

    [Fact]
    public async Task Status_WithConnectedAccount_ReturnsConnectedTrue()
    {
        SetupValidAuthenticationMocks();
        await CreateTestUser(withGoogleAccount: true);

        _factory
            .MockGoogleCalendarService.Setup(s => s.ValidateTokenAsync("testuser"))
            .ReturnsAsync(true);

        _client.DefaultRequestHeaders.Add("X-Session-Cookie", ValidSessionCookie);
        var response = await _client.GetAsync("/api/calendar/google/status");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var status = await response.Content.ReadFromJsonAsync<GoogleStatusDto>(_jsonOptions);
        status.Should().NotBeNull();
        status!.IsConnected.Should().BeTrue();
        status.Email.Should().Be("testuser@gmail.com");
    }

    [Fact]
    public async Task Status_WithNoAccount_ReturnsConnectedFalse()
    {
        SetupValidAuthenticationMocks();
        await CreateTestUser(withGoogleAccount: false);

        _client.DefaultRequestHeaders.Add("X-Session-Cookie", ValidSessionCookie);
        var response = await _client.GetAsync("/api/calendar/google/status");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var status = await response.Content.ReadFromJsonAsync<GoogleStatusDto>(_jsonOptions);
        status.Should().NotBeNull();
        status!.IsConnected.Should().BeFalse();
    }

    [Fact]
    public async Task Status_WithInvalidToken_RemovesAccountAndReturnsDisconnected()
    {
        SetupValidAuthenticationMocks();
        await CreateTestUser(withGoogleAccount: true);

        _factory
            .MockGoogleCalendarService.Setup(s => s.ValidateTokenAsync("testuser"))
            .ReturnsAsync(false);

        _client.DefaultRequestHeaders.Add("X-Session-Cookie", ValidSessionCookie);
        var response = await _client.GetAsync("/api/calendar/google/status");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var status = await response.Content.ReadFromJsonAsync<GoogleStatusDto>(_jsonOptions);
        status!.IsConnected.Should().BeFalse();

        var user = await _userRepository.GetByEmailAsync(ValidEmail);
        user!.GoogleAccount.Should().BeNull();
    }

    [Fact]
    public async Task Status_WithoutSessionCookie_ReturnsBadRequest()
    {
        var response = await _client.GetAsync("/api/calendar/google/status");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Status_WithInvalidBlackboardSession_ReturnsBadRequest()
    {
        _factory.ResetMocks();
        _factory
            .MockBlackboardService.Setup(s => s.GetUserDataAsync(It.IsAny<string>()))
            .ReturnsAsync(
                new UserResponseDto
                {
                    IsSuccess = false,
                    Message = "Invalid session",
                    UserData = null,
                }
            );

        _client.DefaultRequestHeaders.Add("X-Session-Cookie", "invalid");
        var response = await _client.GetAsync("/api/calendar/google/status");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Status_WithUserWithoutEmail_ReturnsDisconnected()
    {
        SetupValidAuthenticationMocks();
        _factory
            .MockBlackboardService.Setup(s => s.GetUserDataAsync(ValidSessionCookie))
            .ReturnsAsync(
                new UserResponseDto
                {
                    IsSuccess = true,
                    Message = "User data retrieved",
                    UserData = new UserDetailDto
                    {
                        Given = "Test",
                        Family = "User",
                        Email = string.Empty,
                        Avatar = "",
                    },
                }
            );

        _client.DefaultRequestHeaders.Add("X-Session-Cookie", ValidSessionCookie);
        var response = await _client.GetAsync("/api/calendar/google/status");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var status = await response.Content.ReadFromJsonAsync<GoogleStatusDto>(_jsonOptions);
        status!.IsConnected.Should().BeFalse();
    }

    #endregion

    #region Export Tests

    [Fact]
    public async Task Export_WithConnectedAccount_ReturnsExportSummary()
    {
        SetupValidAuthenticationMocks();
        await CreateTestUser(withGoogleAccount: true);

        var blackboardItems = new List<CalendarItemDto>
        {
            new()
            {
                CalendarId = "bb-1",
                Title = "Blackboard Event",
                Start = DateTime.UtcNow.AddDays(1),
                End = DateTime.UtcNow.AddDays(1).AddHours(1),
                Category = CalendarCategory.Course,
                Color = "#FF5733",
                Subject = "Math",
            },
        };

        var personalEvent = _testDataBuilder.BuildEvent("Personal Event", DateTime.UtcNow.AddDays(2), DateTime.UtcNow.AddDays(2).AddHours(1));
        await _userRepository.AddEventAsync("testuser", personalEvent);

        _factory
            .MockBlackboardService.Setup(s =>
                s.GetCalendarItemsAsync(It.IsAny<DateTime>(), ValidSessionCookie)
            )
            .ReturnsAsync(blackboardItems);

        _factory
            .MockGoogleCalendarService.Setup(s =>
                s.ExportEventsAsync("testuser", It.IsAny<IEnumerable<CalendarItemDto>>())
            )
            .ReturnsAsync(
                new ExportSummaryDto
                {
                    Created = 2,
                    Updated = 0,
                    Failed = 0,
                    Errors = null,
                }
            );

        _client.DefaultRequestHeaders.Add("X-Session-Cookie", ValidSessionCookie);
        var response = await _client.PostAsync("/api/calendar/google/export", null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var summary = await response.Content.ReadFromJsonAsync<ExportSummaryDto>(_jsonOptions);
        summary.Should().NotBeNull();
        summary!.Created.Should().Be(2);
    }

    [Fact]
    public async Task Export_WithoutSessionCookie_ReturnsBadRequest()
    {
        var response = await _client.PostAsync("/api/calendar/google/export", null);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Export_WithInvalidBlackboardSession_ReturnsBadRequest()
    {
        _factory.ResetMocks();
        _factory
            .MockBlackboardService.Setup(s => s.GetUserDataAsync(It.IsAny<string>()))
            .ReturnsAsync(
                new UserResponseDto
                {
                    IsSuccess = false,
                    Message = "Invalid session",
                    UserData = null,
                }
            );

        _client.DefaultRequestHeaders.Add("X-Session-Cookie", "invalid");
        var response = await _client.PostAsync("/api/calendar/google/export", null);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Export_WithUserNotInDatabase_ReturnsBadRequest()
    {
        SetupValidAuthenticationMocks();

        _client.DefaultRequestHeaders.Add("X-Session-Cookie", ValidSessionCookie);
        var response = await _client.PostAsync("/api/calendar/google/export", null);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Export_WithUserWithoutGoogleAccount_ReturnsBadRequest()
    {
        SetupValidAuthenticationMocks();
        await CreateTestUser(withGoogleAccount: false);

        _client.DefaultRequestHeaders.Add("X-Session-Cookie", ValidSessionCookie);
        var response = await _client.PostAsync("/api/calendar/google/export", null);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Export_WithDateParameter_UsesProvidedDate()
    {
        SetupValidAuthenticationMocks();
        await CreateTestUser(withGoogleAccount: true);

        var fromDate = new DateTime(2024, 1, 15, 0, 0, 0, DateTimeKind.Utc);

        _factory
            .MockBlackboardService.Setup(s => s.GetCalendarItemsAsync(fromDate, ValidSessionCookie))
            .ReturnsAsync(new List<CalendarItemDto>());

        _factory
            .MockGoogleCalendarService.Setup(s =>
                s.ExportEventsAsync("testuser", It.IsAny<IEnumerable<CalendarItemDto>>())
            )
            .ReturnsAsync(
                new ExportSummaryDto
                {
                    Created = 0,
                    Updated = 0,
                    Failed = 0,
                }
            );

        _client.DefaultRequestHeaders.Add("X-Session-Cookie", ValidSessionCookie);
        var response = await _client.PostAsync(
            $"/api/calendar/google/export?from={fromDate:O}",
            null
        );

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        _factory.MockBlackboardService.Verify(
            s => s.GetCalendarItemsAsync(fromDate, ValidSessionCookie),
            Times.Once
        );
    }

    [Fact]
    public async Task Export_CombinesBlackboardAndPersonalEvents()
    {
        SetupValidAuthenticationMocks();
        await CreateTestUser(withGoogleAccount: true);

        var blackboardItems = new List<CalendarItemDto>
        {
            new()
            {
                CalendarId = "bb-1",
                Title = "BB Event",
                Start = DateTime.UtcNow,
                End = DateTime.UtcNow.AddHours(1),
                Category = CalendarCategory.Course,
                Color = "#FF0000",
                Subject = "Test Subject",
            },
        };

        var personalEvent = _testDataBuilder.BuildEvent("Personal Event", DateTime.UtcNow.AddDays(1), DateTime.UtcNow.AddDays(1).AddHours(1));
        await _userRepository.AddEventAsync("testuser", personalEvent);

        _factory
            .MockBlackboardService.Setup(s =>
                s.GetCalendarItemsAsync(It.IsAny<DateTime>(), ValidSessionCookie)
            )
            .ReturnsAsync(blackboardItems);

        IEnumerable<CalendarItemDto>? capturedItems = null;
        _factory
            .MockGoogleCalendarService.Setup(s =>
                s.ExportEventsAsync("testuser", It.IsAny<IEnumerable<CalendarItemDto>>())
            )
            .Callback<string, IEnumerable<CalendarItemDto>>((_, items) => capturedItems = items)
            .ReturnsAsync(
                new ExportSummaryDto
                {
                    Created = 2,
                    Updated = 0,
                    Failed = 0,
                }
            );

        _client.DefaultRequestHeaders.Add("X-Session-Cookie", ValidSessionCookie);
        await _client.PostAsync("/api/calendar/google/export", null);

        capturedItems.Should().NotBeNull();
        capturedItems!.Should().HaveCount(2);
        capturedItems.Should().Contain(i => i.Title == "BB Event");
        capturedItems.Should().Contain(i => i.Title == "Personal Event");
        capturedItems.Should().Contain(i => i.CalendarId.StartsWith("personal-"));
    }

    #endregion
}
