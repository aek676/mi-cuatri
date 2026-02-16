using backend.Data;
using backend.Dtos;
using backend.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;
using Testcontainers.MongoDb;

namespace backend.Tests.Integration.Fixtures;

/// <summary>
/// Custom WebApplicationFactory for integration tests with Testcontainers MongoDB.
/// Configures the application with a real MongoDB container and mocked external services.
/// </summary>
public class CustomWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly MongoDbContainer _mongoContainer;
    private readonly Mock<IBlackboardService> _mockBlackboardService;
    private readonly Mock<IGoogleCalendarService> _mockGoogleCalendarService;

    /// <summary>
    /// Gets the mock Blackboard service for configuring test scenarios.
    /// </summary>
    public Mock<IBlackboardService> MockBlackboardService => _mockBlackboardService;

    /// <summary>
    /// Gets the mock Google Calendar service for configuring test scenarios.
    /// </summary>
    public Mock<IGoogleCalendarService> MockGoogleCalendarService => _mockGoogleCalendarService;

    /// <summary>
    /// Gets the MongoDB connection string for direct database operations in tests.
    /// </summary>
    public string MongoConnectionString => _mongoContainer.GetConnectionString();

    /// <summary>
    /// Initializes a new instance of the CustomWebApplicationFactory.
    /// </summary>
    public CustomWebApplicationFactory()
    {
        _mongoContainer = new MongoDbBuilder().WithImage("mongo:8.2.3").Build();

        _mockBlackboardService = new Mock<IBlackboardService>();
        _mockGoogleCalendarService = new Mock<IGoogleCalendarService>();
    }

    /// <summary>
    /// Configures the web host for integration testing.
    /// </summary>
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Test");

        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<MongoDbContext>();

            services.AddScoped<MongoDbContext>(provider => new MongoDbContext(
                _mongoContainer.GetConnectionString()
            ));

            services.RemoveAll<IBlackboardService>();
            services.AddSingleton(_mockBlackboardService.Object);

            services.RemoveAll<IGoogleCalendarService>();
            services.AddSingleton(_mockGoogleCalendarService.Object);
        });
    }

    /// <summary>
    /// Starts the MongoDB container before tests run.
    /// </summary>
    public async ValueTask InitializeAsync()
    {
        await _mongoContainer.StartAsync();
    }

    /// <summary>
    /// Stops and disposes the MongoDB container after tests complete.
    /// </summary>
    public override async ValueTask DisposeAsync()
    {
        await _mongoContainer.DisposeAsync();
        await base.DisposeAsync();
    }

    /// <summary>
    /// Resets the mock configurations to default state.
    /// Call this between tests to ensure isolation.
    /// </summary>
    public void ResetMocks()
    {
        _mockBlackboardService.Reset();
        _mockGoogleCalendarService.Reset();

        SetupDefaultBlackboardMocks();
        SetupDefaultGoogleCalendarMocks();
    }

    /// <summary>
    /// Sets up default mock behavior for BlackboardService.
    /// </summary>
    private void SetupDefaultBlackboardMocks()
    {
        _mockBlackboardService
            .Setup(s => s.AuthenticateAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(
                (string username, string password) =>
                    new LoginResponseDto
                    {
                        IsSuccess = true,
                        Message = "Login successful",
                        SessionCookie = $"test-session-{username}",
                    }
            );

        _mockBlackboardService
            .Setup(s => s.GetUserDataAsync(It.IsAny<string>()))
            .ReturnsAsync(
                (string cookie) =>
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

        _mockBlackboardService
            .Setup(s => s.GetCalendarItemsAsync(It.IsAny<DateTime>(), It.IsAny<string>()))
            .ReturnsAsync(new List<CalendarItemDto>());
    }

    /// <summary>
    /// Sets up default mock behavior for GoogleCalendarService.
    /// </summary>
    private void SetupDefaultGoogleCalendarMocks()
    {
        _mockGoogleCalendarService
            .Setup(s => s.ValidateTokenAsync(It.IsAny<string>()))
            .ReturnsAsync(true);

        _mockGoogleCalendarService
            .Setup(s =>
                s.ExportEventsAsync(It.IsAny<string>(), It.IsAny<IEnumerable<CalendarItemDto>>())
            )
            .ReturnsAsync(
                new ExportSummaryDto
                {
                    Created = 0,
                    Updated = 0,
                    Failed = 0,
                    Errors = null,
                }
            );
    }
}
