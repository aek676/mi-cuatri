using backend.Dtos;
using backend.Services;
using Microsoft.AspNetCore.Mvc;
using System.Net;

namespace backend.Controllers
{
    /// <summary>
    /// Controller to expose Blackboard calendar items without persistence.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class CalendarController : ControllerBase
    {
        private readonly IBlackboardService _blackboardService;

        /// <summary>
        /// Initializes the controller with Blackboard service dependency.
        /// </summary>
        /// <param name="blackboardService">Blackboard service instance.</param>
        public CalendarController(IBlackboardService blackboardService)
        {
            _blackboardService = blackboardService;
        }

        /// <summary>
        /// Gets calendar items from Blackboard in a 16-week window starting at the first day of the month for the provided date.
        /// </summary>
        /// <param name="currentDate">Reference date used to calculate the window.</param>
        /// <param name="sessionCookieHeader">Session cookie from X-Session-Cookie header.</param>
        /// <returns>List of mapped calendar items.</returns>
        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<CalendarItemDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status502BadGateway)]
        public async Task<IActionResult> Get(
            [FromQuery] DateTime? currentDate,
            [FromHeader(Name = "X-Session-Cookie")] string? sessionCookieHeader)
        {
            if (currentDate is null)
            {
                return BadRequest("currentDate query parameter is required.");
            }

            var cookie = sessionCookieHeader;
            if (string.IsNullOrEmpty(cookie) && Request.Headers.TryGetValue("Cookie", out var cookieHeaderValue))
            {
                cookie = cookieHeaderValue.ToString();
            }

            if (string.IsNullOrWhiteSpace(cookie))
            {
                return BadRequest("Session cookie is required in 'X-Session-Cookie' or 'Cookie' header.");
            }

            try
            {
                var items = await _blackboardService.GetCalendarItemsAsync(currentDate.Value, cookie);
                return Ok(items);
            }
            catch (HttpRequestException ex)
            {
                if (ex.StatusCode == HttpStatusCode.Unauthorized || ex.StatusCode == HttpStatusCode.Forbidden)
                {
                    return Unauthorized(new { error = "Invalid or expired session cookie" });
                }

                return StatusCode(StatusCodes.Status502BadGateway, new { error = $"Upstream calendar API error: {ex.Message}" });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}
