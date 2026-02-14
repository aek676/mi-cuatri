using backend.Controllers;
using backend.Dtos;
using backend.Enums;
using backend.Models;
using backend.Repositories;
using backend.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace backend.Tests.Unit.Controllers;

/// <summary>
/// Unit tests for GoogleCalendarController.
/// Tests Google Calendar status and export operations.
/// </summary>
public class GoogleCalendarControllerTests
{
    private readonly Mock<IGoogleCalendarService> _mockGoogleCalendarService;
    private readonly Mock<IUserRepository> _mockUserRepository;
    private readonly Mock<IBlackboardService> _mockBlackboardService;
    private readonly GoogleCalendarController _controller;

    public GoogleCalendarControllerTests()
    {
        _mockGoogleCalendarService = new Mock<IGoogleCalendarService>();
        _mockUserRepository = new Mock<IUserRepository>();
        _mockBlackboardService = new Mock<IBlackboardService>();
        _controller = new GoogleCalendarController(
            _mockGoogleCalendarService.Object,
            _mockUserRepository.Object,
            _mockBlackboardService.Object
        );
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

    private void SetupValidUserSession(string email = "test@ual.es", bool hasGoogleAccount = true, string username = "testuser")
    {
        var userDataResponse = new UserResponseDto
        {
            IsSuccess = true,
            UserData = new UserDetailDto { Email = email }
        };

        _mockBlackboardService
            .Setup(s => s.GetUserDataAsync(It.IsAny<string>()))
            .ReturnsAsync(userDataResponse);

        var user = new User
        {
            Id = "123",
            Username = username,
            Email = email,
            GoogleAccount = hasGoogleAccount ? new GoogleAccount 
            { 
                Email = "test@gmail.com",
                RefreshToken = "refresh_token"
            } : null
        };

        _mockUserRepository
            .Setup(r => r.GetByEmailAsync(email))
            .ReturnsAsync(user);
    }

    #region GET api/calendar/google/status Tests

    [Fact]
    public async Task Status_ConnectedWithValidToken_ReturnsTrue()
    {
        // Arrange
        SetupControllerContext();
        SetupValidUserSession(hasGoogleAccount: true);
        
        _mockGoogleCalendarService
            .Setup(s => s.ValidateTokenAsync("testuser"))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.Status("bb_session=test123");

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(200);
        
        var status = okResult.Value.Should().BeOfType<GoogleStatusDto>().Subject;
        status.IsConnected.Should().BeTrue();
        status.Email.Should().Be("test@gmail.com");
    }

    [Fact]
    public async Task Status_NotConnected_ReturnsFalse()
    {
        // Arrange
        SetupControllerContext();
        SetupValidUserSession(hasGoogleAccount: false);

        // Act
        var result = await _controller.Status("bb_session=test123");

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(200);
        
        var status = okResult.Value.Should().BeOfType<GoogleStatusDto>().Subject;
        status.IsConnected.Should().BeFalse();
    }

    [Fact]
    public async Task Status_InvalidToken_RemovesAccountAndReturnsFalse()
    {
        // Arrange
        SetupControllerContext();
        SetupValidUserSession(hasGoogleAccount: true);
        
        _mockGoogleCalendarService
            .Setup(s => s.ValidateTokenAsync("testuser"))
            .ReturnsAsync(false);

        _mockUserRepository
            .Setup(r => r.RemoveGoogleAccountAsync("testuser"))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.Status("bb_session=test123");

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var status = okResult.Value.Should().BeOfType<GoogleStatusDto>().Subject;
        status.IsConnected.Should().BeFalse();
        
        _mockUserRepository.Verify(r => r.RemoveGoogleAccountAsync("testuser"), Times.Once);
    }

    [Fact]
    public async Task Status_InvalidSession_Returns400()
    {
        // Arrange
        SetupControllerContext();
        _mockBlackboardService
            .Setup(s => s.GetUserDataAsync(It.IsAny<string>()))
            .ReturnsAsync(new UserResponseDto { IsSuccess = false });

        // Act
        var result = await _controller.Status("bb_session=invalid");

        // Assert
        var badRequestResult = result.Result.Should().BeOfType<BadRequestObjectResult>().Subject;
        badRequestResult.StatusCode.Should().Be(400);
    }

    [Fact]
    public async Task Status_NoEmailInBlackboard_ReturnsNotConnected()
    {
        // Arrange
        SetupControllerContext();
        _mockBlackboardService
            .Setup(s => s.GetUserDataAsync(It.IsAny<string>()))
            .ReturnsAsync(new UserResponseDto 
            { 
                IsSuccess = true,
                UserData = new UserDetailDto { Email = null }
            });

        // Act
        var result = await _controller.Status("bb_session=test123");

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var status = okResult.Value.Should().BeOfType<GoogleStatusDto>().Subject;
        status.IsConnected.Should().BeFalse();
    }

    #endregion

    #region POST api/calendar/google/export Tests

    [Fact]
    public async Task Export_ValidRequest_ReturnsSummary()
    {
        // Arrange
        SetupControllerContext();
        SetupValidUserSession(hasGoogleAccount: true);
        
        var blackboardItems = new List<CalendarItemDto>
        {
            new CalendarItemDto 
            { 
                CalendarId = "bb1", 
                Title = "BB Event",
                Start = DateTime.UtcNow,
                End = DateTime.UtcNow.AddHours(1),
                Category = CalendarCategory.Course,
                Subject = "Math",
                Color = "#FF5733"
            }
        };
        
        var personalEvents = new List<Event>
        {
            new Event { Id = "p1", Title = "Personal Event", Start = DateTime.UtcNow, End = DateTime.UtcNow.AddHours(1), Color = "#FF5733", Category = CalendarCategory.Personal }
        };

        _mockBlackboardService
            .Setup(s => s.GetCalendarItemsAsync(It.IsAny<DateTime>(), "bb_session=test123"))
            .ReturnsAsync(blackboardItems);

        _mockUserRepository
            .Setup(r => r.GetUserEventsAsync("testuser"))
            .ReturnsAsync(personalEvents);

        var exportSummary = new ExportSummaryDto 
        { 
            Created = 1, 
            Updated = 1, 
            Failed = 0 
        };

        _mockGoogleCalendarService
            .Setup(s => s.ExportEventsAsync("testuser", It.IsAny<IEnumerable<CalendarItemDto>>()))
            .ReturnsAsync(exportSummary);

        // Act
        var result = await _controller.Export("bb_session=test123", null);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(200);
        
        var summary = okResult.Value.Should().BeOfType<ExportSummaryDto>().Subject;
        summary.Created.Should().Be(1);
        summary.Updated.Should().Be(1);
    }

    [Fact]
    public async Task Export_NotConnected_Returns400()
    {
        // Arrange
        SetupControllerContext();
        SetupValidUserSession(hasGoogleAccount: false);

        // Act
        var result = await _controller.Export("bb_session=test123", null);

        // Assert
        var badRequestResult = result.Result.Should().BeOfType<BadRequestObjectResult>().Subject;
        badRequestResult.StatusCode.Should().Be(400);
        
        var response = badRequestResult.Value.Should().BeAssignableTo<object>().Subject;
        response.ToString().Should().Contain("not connected to Google");
    }

    [Fact]
    public async Task Export_InvalidSession_Returns400()
    {
        // Arrange
        SetupControllerContext();
        _mockBlackboardService
            .Setup(s => s.GetUserDataAsync(It.IsAny<string>()))
            .ReturnsAsync(new UserResponseDto { IsSuccess = false });

        // Act
        var result = await _controller.Export("bb_session=invalid", null);

        // Assert
        var badRequestResult = result.Result.Should().BeOfType<BadRequestObjectResult>().Subject;
        badRequestResult.StatusCode.Should().Be(400);
    }

    #endregion
}
