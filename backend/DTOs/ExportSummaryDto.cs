namespace backend.Dtos
{
    /// <summary>
    /// Summary of an export operation to Google Calendar.
    /// Contains counts of created, updated and failed events and any error messages.
    /// </summary>
    public class ExportSummaryDto
    {
        /// <summary>Number of events created in Google Calendar.</summary>
        public int Created { get; set; }
        /// <summary>Number of events updated in Google Calendar.</summary>
        public int Updated { get; set; }
        /// <summary>Number of events that failed to be exported.</summary>
        public int Failed { get; set; }
        /// <summary>List of error messages for failed items, if any.</summary>
        public List<string>? Errors { get; set; }
    }
} 
