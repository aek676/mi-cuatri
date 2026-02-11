namespace backend.Dtos
{
    /// <summary>
    /// Data transfer object for updating an existing event.
    /// All fields are optional to allow partial updates.
    /// </summary>
    public class UpdateEventDto
    {
        /// <summary>
        /// The title of the event.
        /// </summary>
        public string? Title { get; set; }

        /// <summary>
        /// Optional subject or course name.
        /// </summary>
        public string? Subject { get; set; }

        /// <summary>
        /// The start date and time in UTC.
        /// </summary>
        public DateTime? Start { get; set; }

        /// <summary>
        /// The end date and time in UTC.
        /// </summary>
        public DateTime? End { get; set; }

        /// <summary>
        /// Optional location of the event.
        /// </summary>
        public string? Location { get; set; }

        /// <summary>
        /// The color code in hexadecimal format (e.g., #FF5733).
        /// </summary>
        public string? Color { get; set; }
    }
}
