using System.Text.Json.Serialization;

namespace backend.Dtos
{
    /// <summary>
    /// Clean calendar event item mapped from Blackboard raw calendar entries.
    /// </summary>
    public record CalendarItemDto
    {
        /// <summary>Gets the calendar item identifier mapped from the Blackboard 'id' field.</summary>
        [JsonPropertyName("calendarid")]
        public string CalendarId { get; init; } = string.Empty;

        /// <summary>Gets the event title.</summary>
        [JsonPropertyName("title")]
        public string Title { get; init; } = string.Empty;

        /// <summary>Gets the event start date/time in UTC.</summary>
        [JsonPropertyName("start")]
        public DateTime Start { get; init; }

        /// <summary>Gets the event end date/time in UTC.</summary>
        [JsonPropertyName("end")]
        public DateTime End { get; init; }

        /// <summary>Gets the physical or virtual location of the event.</summary>
        [JsonPropertyName("location")]
        public string Location { get; init; } = string.Empty;

        /// <summary>Gets the event category type mapped from Blackboard 'type' field. Values: Course, GradebookColumn, Institution, OfficeHours, Personal.</summary>
        [JsonPropertyName("category")]
        public string Category { get; init; } = string.Empty;

        /// <summary>Gets the cleaned subject/course name extracted from the calendar name using regex. Empty for Institution/Personal categories.</summary>
        [JsonPropertyName("subject")]
        public string Subject { get; init; } = string.Empty;

        /// <summary>Gets the hexadecimal color code for visual representation.</summary>
        [JsonPropertyName("color")]
        public string Color { get; init; } = string.Empty;

        /// <summary>Gets the optional event description.</summary>
        [JsonPropertyName("description")]
        public string? Description { get; init; }
    }
}
