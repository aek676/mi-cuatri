using Bogus;
using backend.Models;
using backend.Enums;
using backend.Dtos;

namespace backend.Tests.Helpers;

/// <summary>
/// Builder class for creating test data using Bogus library.
/// Provides fluent API for generating realistic test objects.
/// </summary>
public class TestDataBuilder
{
    private readonly Faker<User> _userFaker;
    private readonly Faker<Event> _eventFaker;

    public TestDataBuilder()
    {
        _userFaker = new Faker<User>()
            .RuleFor(u => u.Id, f => Guid.NewGuid().ToString())
            .RuleFor(u => u.Username, f => f.Internet.UserName())
            .RuleFor(u => u.Email, f => f.Internet.Email())
            .RuleFor(u => u.GoogleAccount, f => null)
            .RuleFor(u => u.Events, f => new List<Event>());

        _eventFaker = new Faker<Event>()
            .RuleFor(e => e.Id, f => Guid.NewGuid().ToString())
            .RuleFor(e => e.Title, f => string.Join(" ", f.Lorem.Words(2)))
            .RuleFor(e => e.Subject, f => f.Lorem.Word())
            .RuleFor(e => e.Start, f => f.Date.Future().ToUniversalTime())
            .RuleFor(e => e.End, (f, evt) => evt.Start.AddHours(1))
            .RuleFor(e => e.Location, f => f.Address.City())
            .RuleFor(e => e.Color, f => GenerateRandomColor())
            .RuleFor(e => e.Category, f => f.PickRandom<CalendarCategory>());
    }

    /// <summary>
    /// Helper to generate a random hex color.
    /// </summary>
    private static string GenerateRandomColor()
    {
        var random = new Random();
        return $"#{random.Next(0x1000000):X6}";
    }

    /// <summary>
    /// Build a User with default fake data.
    /// </summary>
    public User BuildUser()
    {
        return _userFaker.Generate();
    }

    /// <summary>
    /// Build a User with specific username.
    /// </summary>
    public User BuildUser(string username)
    {
        return _userFaker
            .RuleFor(u => u.Username, username)
            .Generate();
    }

    /// <summary>
    /// Build a User with specific username and email.
    /// </summary>
    public User BuildUser(string username, string email)
    {
        return _userFaker
            .RuleFor(u => u.Username, username)
            .RuleFor(u => u.Email, email)
            .Generate();
    }

    /// <summary>
    /// Build an Event with default fake data.
    /// </summary>
    public Event BuildEvent()
    {
        return _eventFaker.Generate();
    }

    /// <summary>
    /// Build an Event with specific title.
    /// </summary>
    public Event BuildEvent(string title)
    {
        return _eventFaker
            .RuleFor(e => e.Title, title)
            .Generate();
    }

    /// <summary>
    /// Build an Event with specific title and date range.
    /// </summary>
    public Event BuildEvent(string title, DateTime start, DateTime end)
    {
        return _eventFaker
            .RuleFor(e => e.Title, title)
            .RuleFor(e => e.Start, start)
            .RuleFor(e => e.End, end)
            .Generate();
    }

    /// <summary>
    /// Build a list of Events with default fake data.
    /// </summary>
    public List<Event> BuildEvents(int count)
    {
        return _eventFaker.Generate(count);
    }

    /// <summary>
    /// Build a CreateEventDto with default fake data.
    /// </summary>
    public CreateEventDto BuildCreateEventDto()
    {
        var fakeEvent = BuildEvent();
        return new CreateEventDto
        {
            Title = fakeEvent.Title,
            Start = fakeEvent.Start,
            End = fakeEvent.End,
            Color = fakeEvent.Color,
            Category = fakeEvent.Category
        };
    }

    /// <summary>
    /// Build a CreateEventDto with specific title.
    /// </summary>
    public CreateEventDto BuildCreateEventDto(string title)
    {
        var fakeEvent = BuildEvent(title);
        return new CreateEventDto
        {
            Title = fakeEvent.Title,
            Start = fakeEvent.Start,
            End = fakeEvent.End,
            Color = fakeEvent.Color,
            Category = fakeEvent.Category
        };
    }

    /// <summary>
    /// Build an UpdateEventDto with default fake data.
    /// </summary>
    public UpdateEventDto BuildUpdateEventDto()
    {
        var fakeEvent = BuildEvent();
        return new UpdateEventDto
        {
            Title = fakeEvent.Title,
            Start = fakeEvent.Start,
            End = fakeEvent.End,
            Color = fakeEvent.Color
        };
    }

    /// <summary>
    /// Build valid hexadecimal color codes for testing.
    /// </summary>
    /// <param name="count">Number of colors to generate</param>
    public List<string> BuildValidColors(int count = 5)
    {
        return Enumerable.Range(0, count)
            .Select(_ => GenerateRandomColor())
            .ToList();
    }

    /// <summary>
    /// Build invalid color codes for testing validation.
    /// </summary>
    /// <param name="count">Number of invalid colors to generate</param>
    public List<string> BuildInvalidColors(int count = 5)
    {
        return new List<string>
        {
            "INVALID",
            "#12G45",
            "#GGGGGG",
            "not-a-color",
            "#FF",
            "",
        };
    }
}
