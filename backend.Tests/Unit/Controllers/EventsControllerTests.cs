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
/// Unit tests for EventsController.
/// Tests CRUD operations for user calendar events.
/// </summary>
public class EventsControllerTests
{
    private readonly Mock<IUserRepository> _mockUserRepository;
    private readonly Mock<IBlackboardService> _mockBlackboardService;
    private readonly EventsController _controller;

    public EventsControllerTests()
    {
        _mockUserRepository = new Mock<IUserRepository>();
        _mockBlackboardService = new Mock<IBlackboardService>();
        _controller = new EventsController(
            _mockUserRepository.Object,
            _mockBlackboardService.Object
        );
    }

    private void SetupValidSession(string email = "test@ual.es", string username = "testuser")
    {
        var userDataResponse = new UserResponseDto
        {
            IsSuccess = true,
            UserData = new UserDetailDto
            {
                Email = email,
                Given = "Test",
                Family = "User"
            }
        };

        var user = new User
        {
            Id = "123",
            Username = username,
            Email = email
        };

        _mockBlackboardService
            .Setup(s => s.GetUserDataAsync(It.IsAny<string>()))
            .ReturnsAsync(userDataResponse);

        _mockUserRepository
            .Setup(r => r.GetByEmailAsync(email))
            .ReturnsAsync(user);
    }

    private void SetupInvalidSession()
    {
        _mockBlackboardService
            .Setup(s => s.GetUserDataAsync(It.IsAny<string>()))
            .ReturnsAsync(new UserResponseDto { IsSuccess = false });
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

    #region GET api/Events Tests

    [Fact]
    public async Task GetAll_ValidSession_ReturnsEvents()
    {
        // Arrange
        SetupControllerContext();
        SetupValidSession();
        
        var events = new List<Event>
        {
            new Event { Id = "1", Title = "Event 1", Start = DateTime.UtcNow, End = DateTime.UtcNow.AddHours(1), Color = "#FF5733", Category = CalendarCategory.Course },
            new Event { Id = "2", Title = "Event 2", Start = DateTime.UtcNow.AddDays(1), End = DateTime.UtcNow.AddDays(1).AddHours(1), Color = "#00FF00", Category = CalendarCategory.Personal }
        };

        _mockUserRepository
            .Setup(r => r.GetUserEventsAsync("testuser"))
            .ReturnsAsync(events);

        // Act
        var result = await _controller.GetAll("bb_session=test123", null, null);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(200);
        
        var returnedEvents = okResult.Value.Should().BeAssignableTo<IEnumerable<EventDto>>().Subject;
        returnedEvents.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetAll_WithDateFilters_ReturnsFilteredEvents()
    {
        // Arrange
        SetupControllerContext();
        SetupValidSession();
        
        var startDate = DateTime.UtcNow;
        var endDate = DateTime.UtcNow.AddDays(7);
        
        var events = new List<Event>
        {
            new Event { Id = "1", Title = "Inside Range", Start = startDate.AddDays(1), End = startDate.AddDays(1).AddHours(1), Color = "#FF5733", Category = CalendarCategory.Course },
            new Event { Id = "2", Title = "Outside Range", Start = startDate.AddDays(10), End = startDate.AddDays(10).AddHours(1), Color = "#00FF00", Category = CalendarCategory.Personal }
        };

        _mockUserRepository
            .Setup(r => r.GetUserEventsAsync("testuser"))
            .ReturnsAsync(events);

        // Act
        var result = await _controller.GetAll("bb_session=test123", startDate, endDate);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedEvents = okResult.Value.Should().BeAssignableTo<IEnumerable<EventDto>>().Subject;
        returnedEvents.Should().HaveCount(1);
        returnedEvents.First().Title.Should().Be("Inside Range");
    }

    [Fact]
    public async Task GetAll_InvalidSession_ReturnsUnauthorized()
    {
        // Arrange
        SetupControllerContext();
        SetupInvalidSession();

        // Act
        var result = await _controller.GetAll("bb_session=invalid", null, null);

        // Assert
        var unauthorizedResult = result.Should().BeOfType<UnauthorizedObjectResult>().Subject;
        unauthorizedResult.StatusCode.Should().Be(401);
    }

    [Fact]
    public async Task GetAll_NoSession_ReturnsBadRequest()
    {
        // Arrange
        SetupControllerContext(null);

        // Act
        var result = await _controller.GetAll(null, null, null);

        // Assert
        var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        badRequestResult.StatusCode.Should().Be(400);
    }

    #endregion

    #region GET api/Events/{id} Tests

    [Fact]
    public async Task GetById_ValidId_ReturnsEvent()
    {
        // Arrange
        SetupControllerContext();
        SetupValidSession();
        
        var evt = new Event 
        { 
            Id = "event123", 
            Title = "Test Event", 
            Start = DateTime.UtcNow, 
            End = DateTime.UtcNow.AddHours(1), 
            Color = "#FF5733",
            Category = CalendarCategory.Course,
            Subject = "Math"
        };

        _mockUserRepository
            .Setup(r => r.GetEventByIdAsync("testuser", "event123"))
            .ReturnsAsync(evt);

        // Act
        var result = await _controller.GetById("event123", "bb_session=test123");

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(200);
        
        var returnedEvent = okResult.Value.Should().BeOfType<EventDto>().Subject;
        returnedEvent.Id.Should().Be("event123");
        returnedEvent.Title.Should().Be("Test Event");
    }

    [Fact]
    public async Task GetById_NotFound_Returns404()
    {
        // Arrange
        SetupControllerContext();
        SetupValidSession();

        _mockUserRepository
            .Setup(r => r.GetEventByIdAsync("testuser", "nonexistent"))
            .ReturnsAsync((Event?)null);

        // Act
        var result = await _controller.GetById("nonexistent", "bb_session=test123");

        // Assert
        var notFoundResult = result.Should().BeOfType<NotFoundObjectResult>().Subject;
        notFoundResult.StatusCode.Should().Be(404);
    }

    [Fact]
    public async Task GetById_InvalidSession_ReturnsUnauthorized()
    {
        // Arrange
        SetupControllerContext();
        SetupInvalidSession();

        // Act
        var result = await _controller.GetById("event123", "bb_session=invalid");

        // Assert
        var unauthorizedResult = result.Should().BeOfType<UnauthorizedObjectResult>().Subject;
        unauthorizedResult.StatusCode.Should().Be(401);
    }

    #endregion

    #region POST api/Events Tests

    [Fact]
    public async Task Create_ValidEvent_Returns201()
    {
        // Arrange
        SetupControllerContext();
        SetupValidSession();
        
        var dto = new CreateEventDto
        {
            Title = "New Event",
            Start = DateTime.UtcNow.AddHours(1),
            End = DateTime.UtcNow.AddHours(2),
            Color = "#FF5733",
            Category = CalendarCategory.Course,
            Subject = "Math",
            Location = "Room 101"
        };

        _mockUserRepository
            .Setup(r => r.AddEventAsync("testuser", It.IsAny<Event>()))
            .ReturnsAsync((string username, Event evt) => evt);

        // Act
        var result = await _controller.Create(dto, "bb_session=test123");

        // Assert
        var createdResult = result.Should().BeOfType<CreatedAtActionResult>().Subject;
        createdResult.StatusCode.Should().Be(201);
        
        var returnedEvent = createdResult.Value.Should().BeOfType<EventDto>().Subject;
        returnedEvent.Title.Should().Be("New Event");
        returnedEvent.Color.Should().Be("#FF5733");
    }

    [Fact]
    public async Task Create_InvalidHexColor_Returns400()
    {
        // Arrange
        SetupControllerContext();
        SetupValidSession();
        
        var dto = new CreateEventDto
        {
            Title = "New Event",
            Start = DateTime.UtcNow.AddHours(1),
            End = DateTime.UtcNow.AddHours(2),
            Color = "INVALID", // Invalid color
            Category = CalendarCategory.Course
        };

        // Act
        var result = await _controller.Create(dto, "bb_session=test123");

        // Assert
        var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        badRequestResult.StatusCode.Should().Be(400);
        
        var error = badRequestResult.Value.Should().BeAssignableTo<object>().Subject;
        error.ToString().Should().Contain("Color must be a valid hexadecimal");
    }

    [Fact]
    public async Task Create_InvalidSession_ReturnsUnauthorized()
    {
        // Arrange
        SetupControllerContext();
        SetupInvalidSession();
        
        var dto = new CreateEventDto
        {
            Title = "New Event",
            Start = DateTime.UtcNow.AddHours(1),
            End = DateTime.UtcNow.AddHours(2),
            Color = "#FF5733",
            Category = CalendarCategory.Course
        };

        // Act
        var result = await _controller.Create(dto, "bb_session=invalid");

        // Assert
        var unauthorizedResult = result.Should().BeOfType<UnauthorizedObjectResult>().Subject;
        unauthorizedResult.StatusCode.Should().Be(401);
    }

    [Fact]
    public async Task Create_StartDateAfterEndDate_Returns400()
    {
        // Arrange
        SetupControllerContext();
        SetupValidSession();
        
        var dto = new CreateEventDto
        {
            Title = "New Event",
            Start = DateTime.UtcNow.AddHours(2), // Start after end
            End = DateTime.UtcNow.AddHours(1),
            Color = "#FF5733",
            Category = CalendarCategory.Course
        };

        // Act
        var result = await _controller.Create(dto, "bb_session=test123");

        // Assert
        var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        badRequestResult.StatusCode.Should().Be(400);
        
        var error = badRequestResult.Value.Should().BeAssignableTo<object>().Subject;
        error.ToString().Should().Contain("Start date must be before end date");
    }

    #endregion

    #region PUT api/Events/{id} Tests

    [Fact]
    public async Task Update_ValidEvent_Returns200()
    {
        // Arrange
        SetupControllerContext();
        SetupValidSession();
        
        var existingEvent = new Event 
        { 
            Id = "event123", 
            Title = "Old Title", 
            Start = DateTime.UtcNow, 
            End = DateTime.UtcNow.AddHours(1), 
            Color = "#FF5733",
            Category = CalendarCategory.Course
        };

        _mockUserRepository
            .Setup(r => r.GetEventByIdAsync("testuser", "event123"))
            .ReturnsAsync(existingEvent);

        _mockUserRepository
            .Setup(r => r.UpdateEventAsync("testuser", It.IsAny<Event>()))
            .ReturnsAsync(true);

        var dto = new UpdateEventDto
        {
            Title = "Updated Title",
            Color = "#00FF00"
        };

        // Act
        var result = await _controller.Update("event123", dto, "bb_session=test123");

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(200);
        
        var returnedEvent = okResult.Value.Should().BeOfType<EventDto>().Subject;
        returnedEvent.Title.Should().Be("Updated Title");
        returnedEvent.Color.Should().Be("#00FF00");
    }

    [Fact]
    public async Task Update_NotFound_Returns404()
    {
        // Arrange
        SetupControllerContext();
        SetupValidSession();

        _mockUserRepository
            .Setup(r => r.GetEventByIdAsync("testuser", "nonexistent"))
            .ReturnsAsync((Event?)null);

        var dto = new UpdateEventDto { Title = "Updated Title" };

        // Act
        var result = await _controller.Update("nonexistent", dto, "bb_session=test123");

        // Assert
        var notFoundResult = result.Should().BeOfType<NotFoundObjectResult>().Subject;
        notFoundResult.StatusCode.Should().Be(404);
    }

    [Fact]
    public async Task Update_InvalidSession_ReturnsUnauthorized()
    {
        // Arrange
        SetupControllerContext();
        SetupInvalidSession();

        var dto = new UpdateEventDto { Title = "Updated Title" };

        // Act
        var result = await _controller.Update("event123", dto, "bb_session=invalid");

        // Assert
        var unauthorizedResult = result.Should().BeOfType<UnauthorizedObjectResult>().Subject;
        unauthorizedResult.StatusCode.Should().Be(401);
    }

    #endregion

    #region DELETE api/Events/{id} Tests

    [Fact]
    public async Task Delete_ValidId_Returns204()
    {
        // Arrange
        SetupControllerContext();
        SetupValidSession();
        
        var existingEvent = new Event 
        { 
            Id = "event123", 
            Title = "Event to Delete", 
            Start = DateTime.UtcNow, 
            End = DateTime.UtcNow.AddHours(1), 
            Color = "#FF5733",
            Category = CalendarCategory.Course
        };

        _mockUserRepository
            .Setup(r => r.GetEventByIdAsync("testuser", "event123"))
            .ReturnsAsync(existingEvent);

        _mockUserRepository
            .Setup(r => r.DeleteEventAsync("testuser", "event123"))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.Delete("event123", "bb_session=test123");

        // Assert
        var noContentResult = result.Should().BeOfType<NoContentResult>().Subject;
        noContentResult.StatusCode.Should().Be(204);
    }

    [Fact]
    public async Task Delete_NotFound_Returns404()
    {
        // Arrange
        SetupControllerContext();
        SetupValidSession();

        _mockUserRepository
            .Setup(r => r.GetEventByIdAsync("testuser", "nonexistent"))
            .ReturnsAsync((Event?)null);

        // Act
        var result = await _controller.Delete("nonexistent", "bb_session=test123");

        // Assert
        var notFoundResult = result.Should().BeOfType<NotFoundObjectResult>().Subject;
        notFoundResult.StatusCode.Should().Be(404);
    }

    [Fact]
    public async Task Delete_InvalidSession_ReturnsUnauthorized()
    {
        // Arrange
        SetupControllerContext();
        SetupInvalidSession();

        // Act
        var result = await _controller.Delete("event123", "bb_session=invalid");

        // Assert
        var unauthorizedResult = result.Should().BeOfType<UnauthorizedObjectResult>().Subject;
        unauthorizedResult.StatusCode.Should().Be(401);
    }

    #endregion
}
