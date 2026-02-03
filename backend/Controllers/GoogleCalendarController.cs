using backend.Dtos;
using backend.Services;
using backend.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace backend.Controllers
{
    [ApiController]
    [Route("api/calendar/google")]
    public class GoogleCalendarController : ControllerBase
    {
        private readonly IGoogleCalendarService _googleCalendarService;
        private readonly IUserRepository _userRepository;
        private readonly IBlackboardService _blackboardService;

        public GoogleCalendarController(IGoogleCalendarService googleCalendarService, IUserRepository userRepository, IBlackboardService blackboardService)
        {
            _googleCalendarService = googleCalendarService;
            _userRepository = userRepository;
            _blackboardService = blackboardService;
        }

        /// <summary>
        /// Returns whether the current Blackboard-authenticated user has a Google account linked.
        /// </summary>
        [HttpGet("status")]
        [ProducesResponseType(typeof(GoogleStatusDto), StatusCodes.Status200OK)]
        public async Task<ActionResult<GoogleStatusDto>> Status([FromHeader(Name = "X-Session-Cookie")] string? sessionCookieHeader)
        {
            var cookie = sessionCookieHeader;
            if (string.IsNullOrEmpty(cookie) && Request.Headers.TryGetValue("Cookie", out var c)) cookie = c.ToString();
            if (string.IsNullOrEmpty(cookie)) return BadRequest("Session cookie required.");

            var bb = await _blackboardService.GetUserDataAsync(cookie);
            if (!bb.IsSuccess) return BadRequest("Unable to resolve Blackboard user from session cookie.");
            var email = bb.UserData?.Email;
            if (string.IsNullOrEmpty(email)) return Ok(new GoogleStatusDto { IsConnected = false });

            var user = await _userRepository.GetByEmailAsync(email);
            if (user == null) return Ok(new GoogleStatusDto { IsConnected = false });

            return Ok(new GoogleStatusDto { IsConnected = user.GoogleAccount != null, Email = user.GoogleAccount?.Email });
        }

        /// <summary>
        /// Exports calendar items from Blackboard to the user's Google Calendar synchronously.
        /// Returns a summary with counts of created, updated and failed events.
        /// </summary>
        /// <param name="from">Optional reference date to export the 16-week window starting at the first day of the month for this date. Defaults to now.</param>
        [HttpPost("export")]
        [ProducesResponseType(typeof(ExportSummaryDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<ExportSummaryDto>> Export([FromHeader(Name = "X-Session-Cookie")] string? sessionCookieHeader, [FromQuery] DateTime? from = null)
        {
            var cookie = sessionCookieHeader;
            if (string.IsNullOrEmpty(cookie) && Request.Headers.TryGetValue("Cookie", out var c)) cookie = c.ToString();
            if (string.IsNullOrEmpty(cookie)) return BadRequest("Session cookie required.");

            var bb = await _blackboardService.GetUserDataAsync(cookie);
            if (!bb.IsSuccess) return BadRequest("Unable to resolve Blackboard user from session cookie.");
            var email = bb.UserData?.Email;
            if (string.IsNullOrEmpty(email)) return BadRequest("Blackboard user has no email to identify local user.");

            var user = await _userRepository.GetByEmailAsync(email);
            if (user == null || user.GoogleAccount == null)
            {
                return BadRequest(new { message = "User is not connected to Google. Use /api/auth/google/connect to link an account." });
            }

            var date = from ?? DateTime.UtcNow;
            var items = await _blackboardService.GetCalendarItemsAsync(date, cookie);

            var summary = await _googleCalendarService.ExportEventsAsync(user.Username, items);

            return Ok(summary);
        }
    }
}
