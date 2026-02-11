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

        /// <summary>
        /// Validates the Google refresh token by attempting to refresh the access token.
        /// Returns true if the token is valid, false if it has expired or is invalid.
        /// </summary>
        /// <param name="username">Local username to validate.</param>
        Task<bool> ValidateTokenAsync(string username);
    }
} 
