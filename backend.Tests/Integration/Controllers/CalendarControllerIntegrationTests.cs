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
using backend.Tests.Integration.Fixtures;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;

namespace backend.Tests.Integration.Controllers;

/// <summary>
/// Integration tests for CalendarController using WebApplicationFactory.
/// Tests Blackboard calendar retrieval endpoints.
/// </summary>
public class CalendarControllerIntegrationTests
    : IClassFixture<CustomWebApplicationFactory>,
        IAsyncLifetime
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;
    private readonly MongoDbContext _context;
    private readonly IUserRepository _userRepository;
    private const string ValidSessionCookie = "test-session-calendar";
    private readonly JsonSerializerOptions _jsonOptions;

    public CalendarControllerIntegrationTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
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

    #region Get Calendar Tests

    [Fact]
    public async Task Get_WithValidSessionAndDate_ReturnsCalendarItems()
    {
        var currentDate = DateTime.UtcNow;
        var expectedItems = new List<CalendarItemDto>
        {
            new()
            {
                CalendarId = "cal-1",
                Title = "Math Class",
                Start = currentDate.AddDays(1),
                End = currentDate.AddDays(1).AddHours(1),
                Location = "Room A",
                Category = CalendarCategory.Course,
                Subject = "Mathematics",
                Color = "#FF5733",
                Description = "Algebra lecture",
            },
            new()
            {
                CalendarId = "cal-2",
                Title = "Physics Lab",
                Start = currentDate.AddDays(2),
                End = currentDate.AddDays(2).AddHours(2),
                Location = "Lab B",
                Category = CalendarCategory.Course,
                Subject = "Physics",
                Color = "#33FF57",
                Description = "Experiment session",
            },
        };

        _factory.ResetMocks();
        _factory
            .MockBlackboardService.Setup(s =>
                s.GetCalendarItemsAsync(
                    It.Is<DateTime>(d => d.Date == currentDate.Date),
                    ValidSessionCookie
                )
            )
            .ReturnsAsync(expectedItems);

        _client.DefaultRequestHeaders.Add("X-Session-Cookie", ValidSessionCookie);
        var response = await _client.GetAsync($"/api/calendar?currentDate={currentDate:O}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var items = await response.Content.ReadFromJsonAsync<IEnumerable<CalendarItemDto>>(_jsonOptions);
        items.Should().HaveCount(2);
        items!.First().Title.Should().Be("Math Class");
        items.Last().Title.Should().Be("Physics Lab");
    }

    [Fact]
    public async Task Get_WithoutCurrentDate_ReturnsBadRequest()
    {
        _client.DefaultRequestHeaders.Add("X-Session-Cookie", ValidSessionCookie);

        var response = await _client.GetAsync("/api/calendar");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Get_WithoutSessionCookie_ReturnsBadRequest()
    {
        var currentDate = DateTime.UtcNow;

        var response = await _client.GetAsync($"/api/calendar?currentDate={currentDate:O}");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Get_WithInvalidSession_ReturnsUnauthorized()
    {
        var currentDate = DateTime.UtcNow;

        _factory.ResetMocks();
        _factory
            .MockBlackboardService.Setup(s =>
                s.GetCalendarItemsAsync(It.IsAny<DateTime>(), "invalid-session")
            )
            .ThrowsAsync(
                new HttpRequestException("Unauthorized", null, HttpStatusCode.Unauthorized)
            );

        _client.DefaultRequestHeaders.Add("X-Session-Cookie", "invalid-session");
        var response = await _client.GetAsync($"/api/calendar?currentDate={currentDate:O}");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Get_WithBlackboardError_ReturnsBadGateway()
    {
        var currentDate = DateTime.UtcNow;

        _factory.ResetMocks();
        _factory
            .MockBlackboardService.Setup(s =>
                s.GetCalendarItemsAsync(It.IsAny<DateTime>(), ValidSessionCookie)
            )
            .ThrowsAsync(
                new HttpRequestException(
                    "Service unavailable",
                    null,
                    HttpStatusCode.ServiceUnavailable
                )
            );

        _client.DefaultRequestHeaders.Add("X-Session-Cookie", ValidSessionCookie);
        var response = await _client.GetAsync($"/api/calendar?currentDate={currentDate:O}");

        response.StatusCode.Should().Be(HttpStatusCode.BadGateway);
    }

    [Fact]
    public async Task Get_WithForbiddenSession_ReturnsUnauthorized()
    {
        var currentDate = DateTime.UtcNow;

        _factory.ResetMocks();
        _factory
            .MockBlackboardService.Setup(s =>
                s.GetCalendarItemsAsync(It.IsAny<DateTime>(), ValidSessionCookie)
            )
            .ThrowsAsync(new HttpRequestException("Forbidden", null, HttpStatusCode.Forbidden));

        _client.DefaultRequestHeaders.Add("X-Session-Cookie", ValidSessionCookie);
        var response = await _client.GetAsync($"/api/calendar?currentDate={currentDate:O}");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Get_WithArgumentException_ReturnsBadRequest()
    {
        var currentDate = DateTime.UtcNow;

        _factory.ResetMocks();
        _factory
            .MockBlackboardService.Setup(s =>
                s.GetCalendarItemsAsync(It.IsAny<DateTime>(), ValidSessionCookie)
            )
            .ThrowsAsync(new ArgumentException("Invalid date range"));

        _client.DefaultRequestHeaders.Add("X-Session-Cookie", ValidSessionCookie);
        var response = await _client.GetAsync($"/api/calendar?currentDate={currentDate:O}");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Get_ReturnsEmptyList_WhenNoCalendarItems()
    {
        var currentDate = DateTime.UtcNow;

        _factory.ResetMocks();
        _factory
            .MockBlackboardService.Setup(s =>
                s.GetCalendarItemsAsync(It.IsAny<DateTime>(), ValidSessionCookie)
            )
            .ReturnsAsync(new List<CalendarItemDto>());

        _client.DefaultRequestHeaders.Add("X-Session-Cookie", ValidSessionCookie);
        var response = await _client.GetAsync($"/api/calendar?currentDate={currentDate:O}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var items = await response.Content.ReadFromJsonAsync<IEnumerable<CalendarItemDto>>(_jsonOptions);
        items.Should().BeEmpty();
    }

    [Fact]
    public async Task Get_WithCookieHeader_ReturnsCalendarItems()
    {
        var currentDate = DateTime.UtcNow;

        _factory.ResetMocks();
        _factory
            .MockBlackboardService.Setup(s =>
                s.GetCalendarItemsAsync(It.IsAny<DateTime>(), "cookie-session")
            )
            .ReturnsAsync(new List<CalendarItemDto>());

        _client.DefaultRequestHeaders.Add("Cookie", "cookie-session");
        var response = await _client.GetAsync($"/api/calendar?currentDate={currentDate:O}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Get_PreservesCalendarItemProperties()
    {
        var currentDate = DateTime.UtcNow;
        var expectedItem = new CalendarItemDto
        {
            CalendarId = "test-cal-id",
            Title = "Test Event",
            Start = currentDate.AddHours(1),
            End = currentDate.AddHours(2),
            Location = "Test Room",
            Category = CalendarCategory.OfficeHours,
            Subject = "Test Subject",
            Color = "#123456",
            Description = "Test Description",
        };

        _factory.ResetMocks();
        _factory
            .MockBlackboardService.Setup(s =>
                s.GetCalendarItemsAsync(It.IsAny<DateTime>(), ValidSessionCookie)
            )
            .ReturnsAsync(new List<CalendarItemDto> { expectedItem });

        _client.DefaultRequestHeaders.Add("X-Session-Cookie", ValidSessionCookie);
        var response = await _client.GetAsync($"/api/calendar?currentDate={currentDate:O}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var items = await response.Content.ReadFromJsonAsync<IEnumerable<CalendarItemDto>>(_jsonOptions);
        items.Should().HaveCount(1);

        var item = items!.First();
        item.CalendarId.Should().Be("test-cal-id");
        item.Title.Should().Be("Test Event");
        item.Location.Should().Be("Test Room");
        item.Category.Should().Be(CalendarCategory.OfficeHours);
        item.Subject.Should().Be("Test Subject");
        item.Color.Should().Be("#123456");
        item.Description.Should().Be("Test Description");
    }

    #endregion
}
