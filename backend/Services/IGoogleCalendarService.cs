using backend.Dtos;
using backend.Models;
using backend.DTOs;

namespace backend.Services
{
    public interface IGoogleCalendarService
    {
        Task<ExportSummaryDto> ExportEventsAsync(string username, IEnumerable<CalendarItemDto> items);
    }
}
