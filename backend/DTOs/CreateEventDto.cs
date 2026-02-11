using System.ComponentModel.DataAnnotations;

namespace backend.Dtos;

/// <summary>
/// Data transfer object for creating a new event.
/// </summary>
/// <param name="Title">The title of the event.</param>
/// <param name="Subject">Optional subject or course name.</param>
/// <param name="Start">The start date and time in UTC.</param>
/// <param name="End">The end date and time in UTC.</param>
/// <param name="Location">Optional location of the event.</param>
/// <param name="Color">The color code in hexadecimal format (e.g., #FF5733).</param>
public record CreateEventDto(
    [property: Required] string Title,
    string? Subject,
    [property: Required] DateTime Start,
    [property: Required] DateTime End,
    string? Location,
    [property: Required] string Color = "#000000"
);
