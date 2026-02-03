using backend.Dtos;
using backend.Models;
using backend.DTOs;

namespace backend.Services
{
    /// <summary>
    /// Service abstraction for exporting events to Google Calendar.
    /// </summary>
    public interface IGoogleCalendarService
    {
        /// <summary>Exports the specified calendar items to the Google Calendar of <paramref name="username"/>.</summary>
        /// <param name="username">Local username whose calendar to export to.</param>
        /// <param name="items">Set of items to export.</param>
        Task<ExportSummaryDto> ExportEventsAsync(string username, IEnumerable<CalendarItemDto> items);
    }
} 
