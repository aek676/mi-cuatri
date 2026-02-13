using System.ComponentModel.DataAnnotations;
using backend.Enums;

namespace backend.Dtos;

/// <summary>
/// Data transfer object for creating a new event.
/// </summary>
public record CreateEventDto
{
    /// <summary>The title of the event.</summary>
    [Required]
    public required string Title { get; init; }

    /// <summary>Optional subject or course name.</summary>
    public string? Subject { get; init; }

    /// <summary>The start date and time in UTC.</summary>
    [Required]
    public DateTime Start { get; init; }

    /// <summary>The end date and time in UTC.</summary>
    [Required]
    public DateTime End { get; init; }

    /// <summary>Optional location of the event.</summary>
    public string? Location { get; init; }

    /// <summary>The color code in hexadecimal format (e.g., #FF5733).</summary>
    [Required]
    public required string Color { get; init; }

    /// <summary>The category of the event.</summary>
    [Required]
    public required CalendarCategory Category { get; init; }
}
