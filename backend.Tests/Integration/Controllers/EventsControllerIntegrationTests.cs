using System.Net;
using System.Net.Http.Headers;
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
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Bson;
using MongoDB.Driver;

namespace backend.Tests.Integration.Controllers;

/// <summary>
/// Integration tests for EventsController using WebApplicationFactory.
/// Tests the complete HTTP pipeline with real MongoDB via Testcontainers.
/// </summary>
public class EventsControllerIntegrationTests
    : IClassFixture<CustomWebApplicationFactory>,
        IAsyncLifetime
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;
    private readonly MongoDbContext _context;
    private readonly IUserRepository _userRepository;
    private readonly TestDataBuilder _testDataBuilder;
    private const string ValidSessionCookie = "test-session-cookie";
    private const string ValidEmail = "testuser@ual.es";
    private readonly JsonSerializerOptions _jsonOptions;

    public EventsControllerIntegrationTests(CustomWebApplicationFactory factory)
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
        await CreateTestUser();
        SetupValidAuthenticationMocks();
    }

    public async ValueTask DisposeAsync()
    {
        await CleanupDatabase();
    }

    private async Task CleanupDatabase()
    {
        await _context.Users.DeleteManyAsync(Builders<User>.Filter.Empty);
    }

    private async Task CreateTestUser()
    {
        await _userRepository.UpsertByUsernameAsync("testuser", ValidEmail);
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
                        Avatar = "https://example.com/avatar.png",
                    },
                }
            );
    }

    private void SetupInvalidAuthenticationMocks()
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
    }

    private CreateEventDto CreateValidEventDto(string? title = null)
    {
        return title != null 
            ? _testDataBuilder.BuildCreateEventDto(title)
            : _testDataBuilder.BuildCreateEventDto();
    }

    #region GetAll Tests

    [Fact]
    public async Task GetAll_WithValidSession_ReturnsEmptyList_WhenNoEvents()
    {
        SetupValidAuthenticationMocks();
        _client.DefaultRequestHeaders.Add("X-Session-Cookie", ValidSessionCookie);

        var response = await _client.GetAsync("/api/events");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var events = await response.Content.ReadFromJsonAsync<IEnumerable<EventDto>>(_jsonOptions);
        events.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAll_WithValidSession_ReturnsEvents_WhenEventsExist()
    {
        SetupValidAuthenticationMocks();
        var testEvent = _testDataBuilder.BuildEvent("My Event");
        await _userRepository.AddEventAsync("testuser", testEvent);

        _client.DefaultRequestHeaders.Add("X-Session-Cookie", ValidSessionCookie);
        var response = await _client.GetAsync("/api/events");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var events = await response.Content.ReadFromJsonAsync<IEnumerable<EventDto>>(_jsonOptions);
        events.Should().HaveCount(1);
        events!.First().Title.Should().Be("My Event");
    }

    [Fact]
    public async Task GetAll_WithoutSessionCookie_ReturnsBadRequest()
    {
        _client.DefaultRequestHeaders.Remove("X-Session-Cookie");

        var response = await _client.GetAsync("/api/events");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetAll_WithInvalidSession_ReturnsUnauthorized()
    {
        SetupInvalidAuthenticationMocks();
        _client.DefaultRequestHeaders.Add("X-Session-Cookie", "invalid-cookie");

        var response = await _client.GetAsync("/api/events");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetAll_WithDateFilter_ReturnsFilteredEvents()
    {
        SetupValidAuthenticationMocks();
        var now = DateTime.UtcNow;

        var pastEvent = _testDataBuilder.BuildEvent("Past Event", now.AddDays(-2), now.AddDays(-1));
        var futureEvent = _testDataBuilder.BuildEvent("Future Event", now.AddDays(1), now.AddDays(2));

        await _userRepository.AddEventAsync("testuser", pastEvent);
        await _userRepository.AddEventAsync("testuser", futureEvent);

        _client.DefaultRequestHeaders.Add("X-Session-Cookie", ValidSessionCookie);
        var response = await _client.GetAsync($"/api/events?start={now:O}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var events = await response.Content.ReadFromJsonAsync<IEnumerable<EventDto>>(_jsonOptions);
        events.Should().HaveCount(1);
        events!.First().Title.Should().Be("Future Event");
    }

    #endregion

    #region GetById Tests

    [Fact]
    public async Task GetById_WithValidId_ReturnsEvent()
    {
        SetupValidAuthenticationMocks();
        var testEvent = _testDataBuilder.BuildEvent("Specific Event");
        await _userRepository.AddEventAsync("testuser", testEvent);

        _client.DefaultRequestHeaders.Add("X-Session-Cookie", ValidSessionCookie);
        var response = await _client.GetAsync($"/api/events/{testEvent.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var eventDto = await response.Content.ReadFromJsonAsync<EventDto>(_jsonOptions);
        eventDto.Should().NotBeNull();
        eventDto!.Id.Should().Be(testEvent.Id);
        eventDto.Title.Should().Be("Specific Event");
    }

    [Fact]
    public async Task GetById_WithInvalidId_ReturnsNotFound()
    {
        SetupValidAuthenticationMocks();
        _client.DefaultRequestHeaders.Add("X-Session-Cookie", ValidSessionCookie);

        var response = await _client.GetAsync("/api/events/nonexistent-id");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    #endregion

    #region Create Tests

    [Fact]
    public async Task Create_WithValidData_ReturnsCreatedEvent()
    {
        SetupValidAuthenticationMocks();
        var dto = CreateValidEventDto("New Event");

        _client.DefaultRequestHeaders.Add("X-Session-Cookie", ValidSessionCookie);
        var response = await _client.PostAsJsonAsync("/api/events", dto);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var eventDto = await response.Content.ReadFromJsonAsync<EventDto>(_jsonOptions);
        eventDto.Should().NotBeNull();
        eventDto!.Title.Should().Be("New Event");
        eventDto.Color.Should().MatchRegex("^#[0-9A-Fa-f]{6}$");

        response.Headers.Location.Should().NotBeNull();
        response.Headers.Location!.ToString().ToLowerInvariant().Should().Contain($"/api/events/{eventDto.Id}");
    }

    [Fact]
    public async Task Create_WithEmptyTitle_ReturnsBadRequest()
    {
        SetupValidAuthenticationMocks();
        var dto = new CreateEventDto
        {
            Title = "",
            Start = DateTime.UtcNow.AddHours(1),
            End = DateTime.UtcNow.AddHours(2),
            Color = "#FF5733",
            Category = CalendarCategory.Course,
        };

        _client.DefaultRequestHeaders.Add("X-Session-Cookie", ValidSessionCookie);
        var response = await _client.PostAsJsonAsync("/api/events", dto);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Create_WithEndBeforeStart_ReturnsBadRequest()
    {
        SetupValidAuthenticationMocks();
        var dto = new CreateEventDto
        {
            Title = "Invalid Event",
            Start = DateTime.UtcNow.AddHours(2),
            End = DateTime.UtcNow.AddHours(1),
            Color = "#FF5733",
            Category = CalendarCategory.Course,
        };

        _client.DefaultRequestHeaders.Add("X-Session-Cookie", ValidSessionCookie);
        var response = await _client.PostAsJsonAsync("/api/events", dto);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Create_WithInvalidColor_ReturnsBadRequest()
    {
        SetupValidAuthenticationMocks();
        var dto = new CreateEventDto
        {
            Title = "Invalid Color Event",
            Start = DateTime.UtcNow.AddHours(1),
            End = DateTime.UtcNow.AddHours(2),
            Color = "invalid-color",
            Category = CalendarCategory.Course,
        };

        _client.DefaultRequestHeaders.Add("X-Session-Cookie", ValidSessionCookie);
        var response = await _client.PostAsJsonAsync("/api/events", dto);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Create_WithoutAuthentication_ReturnsBadRequest()
    {
        var dto = CreateValidEventDto();
        _client.DefaultRequestHeaders.Remove("X-Session-Cookie");

        var response = await _client.PostAsJsonAsync("/api/events", dto);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    #endregion

    #region Update Tests

    [Fact]
    public async Task Update_WithValidData_ReturnsUpdatedEvent()
    {
        SetupValidAuthenticationMocks();
        var testEvent = _testDataBuilder.BuildEvent("Original Title");
        await _userRepository.AddEventAsync("testuser", testEvent);

        var updateDto = new UpdateEventDto(Title: "Updated Title", Color: "#00FF00");

        _client.DefaultRequestHeaders.Add("X-Session-Cookie", ValidSessionCookie);
        var response = await _client.PutAsJsonAsync($"/api/events/{testEvent.Id}", updateDto);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var eventDto = await response.Content.ReadFromJsonAsync<EventDto>(_jsonOptions);
        eventDto.Should().NotBeNull();
        eventDto!.Title.Should().Be("Updated Title");
        eventDto.Color.Should().Be("#00FF00");
    }

    [Fact]
    public async Task Update_WithInvalidId_ReturnsNotFound()
    {
        SetupValidAuthenticationMocks();
        var updateDto = new UpdateEventDto(Title: "Updated Title");

        _client.DefaultRequestHeaders.Add("X-Session-Cookie", ValidSessionCookie);
        var response = await _client.PutAsJsonAsync("/api/events/nonexistent-id", updateDto);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Update_WithInvalidColor_ReturnsBadRequest()
    {
        SetupValidAuthenticationMocks();
        var testEvent = _testDataBuilder.BuildEvent();
        await _userRepository.AddEventAsync("testuser", testEvent);

        var updateDto = new UpdateEventDto(Color: "invalid");

        _client.DefaultRequestHeaders.Add("X-Session-Cookie", ValidSessionCookie);
        var response = await _client.PutAsJsonAsync($"/api/events/{testEvent.Id}", updateDto);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Update_WithEndBeforeStart_ReturnsBadRequest()
    {
        SetupValidAuthenticationMocks();
        var testEvent = _testDataBuilder.BuildEvent();
        await _userRepository.AddEventAsync("testuser", testEvent);

        var updateDto = new UpdateEventDto(
            Start: DateTime.UtcNow.AddHours(3),
            End: DateTime.UtcNow.AddHours(1)
        );

        _client.DefaultRequestHeaders.Add("X-Session-Cookie", ValidSessionCookie);
        var response = await _client.PutAsJsonAsync($"/api/events/{testEvent.Id}", updateDto);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    #endregion

    #region Delete Tests

    [Fact]
    public async Task Delete_WithValidId_ReturnsNoContent()
    {
        SetupValidAuthenticationMocks();
        var testEvent = _testDataBuilder.BuildEvent("To Delete");
        await _userRepository.AddEventAsync("testuser", testEvent);

        _client.DefaultRequestHeaders.Add("X-Session-Cookie", ValidSessionCookie);
        var response = await _client.DeleteAsync($"/api/events/{testEvent.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var getResponse = await _client.GetAsync($"/api/events/{testEvent.Id}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Delete_WithInvalidId_ReturnsNotFound()
    {
        SetupValidAuthenticationMocks();
        _client.DefaultRequestHeaders.Add("X-Session-Cookie", ValidSessionCookie);

        var response = await _client.DeleteAsync("/api/events/nonexistent-id");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    #endregion

    #region Cookie Header Tests

    [Fact]
    public async Task GetAll_WithCookieHeader_ReturnsEvents()
    {
        SetupValidAuthenticationMocks();
        _client.DefaultRequestHeaders.Remove("X-Session-Cookie");
        _client.DefaultRequestHeaders.Add("Cookie", ValidSessionCookie);

        var response = await _client.GetAsync("/api/events");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    #endregion
}
