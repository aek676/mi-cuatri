using System.ComponentModel.DataAnnotations;

namespace backend.Dtos;

/// <summary>
/// Data transfer object for event responses.
/// </summary>
/// <param name="Id">Gets the unique identifier of the event.</param>
/// <param name="Title">Gets the title of the event.</param>
/// <param name="Subject">Gets the optional subject or course name associated with the event.</param>
/// <param name="Start">Gets the start date and time of the event in UTC.</param>
/// <param name="End">Gets the end date and time of the event in UTC.</param>
/// <param name="Location">Gets the optional physical or virtual location of the event.</param>
/// <param name="Color">Gets the hexadecimal color code for the event.</param>
public record EventDto(
    [property: Required] string Id,
    [property: Required] string Title,
    string? Subject,
    [property: Required] DateTime Start,
    [property: Required] DateTime End,
    string? Location,
    [property: Required] string Color
);
