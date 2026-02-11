using System.ComponentModel.DataAnnotations;

namespace backend.Dtos
{
    /// <summary>
    /// Data transfer object for event responses.
    /// </summary>
    public record EventDto
    {
        /// <summary>Gets the unique identifier of the event.</summary>
        [Required]
        public string Id { get; init; } = string.Empty;

        /// <summary>Gets the title of the event.</summary>
        [Required]
        public string Title { get; init; } = string.Empty;

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
        public string Color { get; init; } = string.Empty;
    }
}
