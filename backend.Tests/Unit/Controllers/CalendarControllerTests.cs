using backend.Controllers;
using backend.Dtos;
using backend.Enums;
using backend.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Net;

namespace backend.Tests.Unit.Controllers;

/// <summary>
/// Unit tests for CalendarController.
/// Tests retrieving calendar items from Blackboard.
/// </summary>
public class CalendarControllerTests
{
    private readonly Mock<IBlackboardService> _mockBlackboardService;
    private readonly CalendarController _controller;

    public CalendarControllerTests()
    {
        _mockBlackboardService = new Mock<IBlackboardService>();
        _controller = new CalendarController(_mockBlackboardService.Object);
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

    #region GET api/Calendar Tests

    [Fact]
    public async Task Get_ValidSession_ReturnsCalendarItems()
    {
        // Arrange
        SetupControllerContext();
        var currentDate = DateTime.Now;
        
        var calendarItems = new List<CalendarItemDto>
        {
            new CalendarItemDto 
            { 
                CalendarId = "event1", 
                Title = "Course Event", 
                Start = DateTime.UtcNow, 
                End = DateTime.UtcNow.AddHours(1),
                Category = CalendarCategory.Course,
                Subject = "Mathematics",
                Color = "#FF5733"
            },
            new CalendarItemDto 
            { 
                CalendarId = "event2", 
                Title = "Personal Event", 
                Start = DateTime.UtcNow.AddDays(1), 
                End = DateTime.UtcNow.AddDays(1).AddHours(1),
                Category = CalendarCategory.Personal,
                Subject = "",
                Color = "#00FF00"
            }
        };

        _mockBlackboardService
            .Setup(s => s.GetCalendarItemsAsync(currentDate, "bb_session=test123"))
            .ReturnsAsync(calendarItems);

        // Act
        var result = await _controller.Get(currentDate, "bb_session=test123");

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(200);
        
        var returnedItems = okResult.Value.Should().BeAssignableTo<IEnumerable<CalendarItemDto>>().Subject;
        returnedItems.Should().HaveCount(2);
    }

    [Fact]
    public async Task Get_MissingCurrentDate_ReturnsBadRequest()
    {
        // Arrange
        SetupControllerContext();

        // Act
        var result = await _controller.Get(null, "bb_session=test123");

        // Assert
        var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        badRequestResult.StatusCode.Should().Be(400);
        
        var message = badRequestResult.Value.Should().BeOfType<string>().Subject;
        message.Should().Contain("currentDate query parameter is required");
    }

    [Fact]
    public async Task Get_MissingSession_ReturnsBadRequest()
    {
        // Arrange
        SetupControllerContext(null);
        var currentDate = DateTime.Now;

        // Act
        var result = await _controller.Get(currentDate, null);

        // Assert
        var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        badRequestResult.StatusCode.Should().Be(400);
        
        var message = badRequestResult.Value.Should().BeOfType<string>().Subject;
        message.Should().Contain("Session cookie is required");
    }

    [Fact]
    public async Task Get_InvalidSession_Returns401()
    {
        // Arrange
        SetupControllerContext();
        var currentDate = DateTime.Now;
        
        _mockBlackboardService
            .Setup(s => s.GetCalendarItemsAsync(currentDate, "bb_session=invalid"))
            .ThrowsAsync(new HttpRequestException("Unauthorized", null, HttpStatusCode.Unauthorized));

        // Act
        var result = await _controller.Get(currentDate, "bb_session=invalid");

        // Assert
        var unauthorizedResult = result.Should().BeOfType<UnauthorizedObjectResult>().Subject;
        unauthorizedResult.StatusCode.Should().Be(401);
    }

    [Fact]
    public async Task Get_BlackboardServerError_Returns502()
    {
        // Arrange
        SetupControllerContext();
        var currentDate = DateTime.Now;
        
        _mockBlackboardService
            .Setup(s => s.GetCalendarItemsAsync(currentDate, "bb_session=test123"))
            .ThrowsAsync(new HttpRequestException("Server Error", null, HttpStatusCode.InternalServerError));

        // Act
        var result = await _controller.Get(currentDate, "bb_session=test123");

        // Assert
        var badGatewayResult = result.Should().BeOfType<ObjectResult>().Subject;
        badGatewayResult.StatusCode.Should().Be(502);
    }

    #endregion
}
