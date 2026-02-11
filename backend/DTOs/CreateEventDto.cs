using System.ComponentModel.DataAnnotations;

namespace backend.Dtos
{
    /// <summary>
    /// Data transfer object for creating a new event.
    /// </summary>
    public class CreateEventDto
    {
        /// <summary>
        /// The title of the event.
        /// </summary>
        [Required]
        public string Title { get; set; } = default!;

        /// <summary>
        /// Optional subject or course name.
        /// </summary>
        public string? Subject { get; set; }

        /// <summary>
        /// The start date and time in UTC.
        /// </summary>
        [Required]
        public DateTime Start { get; set; }

        /// <summary>
        /// The end date and time in UTC.
        /// </summary>
        [Required]
        public DateTime End { get; set; }

        /// <summary>
        /// Optional location of the event.
        /// </summary>
        public string? Location { get; set; }

        /// <summary>
        /// The color code in hexadecimal format (e.g., #FF5733).
        /// </summary>
        [Required]
        public string Color { get; set; } = "#000000";
    }
}
