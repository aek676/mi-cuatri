using System.ComponentModel.DataAnnotations;
using backend.Enums;

namespace backend.Models
{
    /// <summary>
    /// Represents a calendar event belonging to a user.
    /// </summary>
    public class Event
    {
        /// <summary>
        /// The unique identifier of the event (GUID as string).
        /// </summary>
        [Required]
        public string Id { get; set; } = Guid.NewGuid().ToString();

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

        /// <summary>
        /// The category of the event.
        /// </summary>
        [Required]
        public CalendarCategory Category { get; set; }
    }
}
