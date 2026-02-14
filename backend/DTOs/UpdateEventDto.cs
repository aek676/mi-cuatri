using backend.Enums;

namespace backend.Dtos;

/// <summary>
/// Data transfer object for updating an existing event.
/// All fields are optional to allow partial updates.
/// </summary>
/// <param name="Title">The title of the event.</param>
/// <param name="Subject">Optional subject or course name.</param>
/// <param name="Start">The start date and time in UTC.</param>
/// <param name="End">The end date and time in UTC.</param>
/// <param name="Location">Optional location of the event.</param>
/// <param name="Color">The color code in hexadecimal format (e.g., #FF5733).</param>
/// <param name="Category">The category of the event.</param>
public record UpdateEventDto(
    string? Title = null,
    string? Subject = null,
    DateTime? Start = null,
    DateTime? End = null,
    string? Location = null,
    string? Color = null,
    CalendarCategory? Category = null
);
