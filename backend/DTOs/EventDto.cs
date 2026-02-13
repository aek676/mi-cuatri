using System.ComponentModel.DataAnnotations;
using backend.Enums;

namespace backend.Dtos;

/// <summary>
/// Data transfer object for event responses.
/// </summary>
public record EventDto
{
    /// <summary>Gets the unique identifier of the event.</summary>
    [Required]
    public required string Id { get; init; }

    /// <summary>Gets the title of the event.</summary>
    [Required]
    public required string Title { get; init; }

    /// <summary>Gets the optional subject or course name associated with the event.</summary>
    public string? Subject { get; init; }

    /// <summary>Gets the start date and time of the event in UTC.</summary>
    [Required]
    public DateTime Start { get; init; }

    /// <summary>Gets the end date and time of the event in UTC.</summary>
    [Required]
    public DateTime End { get; init; }

    /// <summary>Gets the optional physical or virtual location of the event.</summary>
    public string? Location { get; init; }

    /// <summary>Gets the hexadecimal color code for the event.</summary>
    [Required]
    public required string Color { get; init; }

    /// <summary>Gets the category of the event.</summary>
    [Required]
    public CalendarCategory Category { get; init; }
}
