using System.Text.Json.Serialization;
using System.ComponentModel.DataAnnotations;
using backend.Enums;

namespace backend.Dtos
{
    /// <summary>
    /// Clean calendar event item mapped from Blackboard raw calendar entries.
    /// </summary>
    public record CalendarItemDto
    {
        /// <summary>Gets the calendar item identifier mapped from the Blackboard 'id' field.</summary>
        [Required]
        [JsonPropertyName("calendarid")]
        public required string CalendarId { get; init; }

        /// <summary>Gets the event title.</summary>
        [Required]
        [JsonPropertyName("title")]
        public required string Title { get; init; }

        /// <summary>Gets the event start date/time in UTC.</summary>
        [Required]
        [JsonPropertyName("start")]
        public required DateTime Start { get; init; }

        /// <summary>Gets the event end date/time in UTC.</summary>
        [Required]
        [JsonPropertyName("end")]
        public required DateTime End { get; init; }
        /// <summary>Gets the physical or virtual location of the event.</summary>
        [JsonPropertyName("location")]
        public string Location { get; init; } = string.Empty;

        /// <summary>Gets the event category type mapped from Blackboard 'type' field. Values: Course, GradebookColumn, Institution, OfficeHours, Personal.</summary>
        [Required]
        [JsonPropertyName("category")]
        public required CalendarCategory Category { get; init; }

        /// <summary>Gets the cleaned subject/course name extracted from the calendar name using regex. Empty for Institution/Personal categories.</summary>
        [Required]
        [JsonPropertyName("subject")]
        public required string Subject { get; init; }

        /// <summary>Gets the hexadecimal color code for visual representation.</summary>
        [Required]
        [JsonPropertyName("color")]
        public required string Color { get; init; }
        /// <summary>Gets the optional event description.</summary>
        [JsonPropertyName("description")]
        public string? Description { get; init; }
    }
}
