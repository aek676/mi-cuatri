using System.Text.RegularExpressions;
using backend.Dtos;
using backend.Models;
using backend.Repositories;
using backend.Services;
using Microsoft.AspNetCore.Mvc;

namespace backend.Controllers
{
    /// <summary>
    /// Controller for managing user calendar events.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class EventsController : ControllerBase
    {
        private readonly IUserRepository _userRepository;
        private readonly IBlackboardService _blackboardService;
        private static readonly Regex HexColorRegex = new Regex(
            "^#[A-Fa-f0-9]{6}$",
            RegexOptions.Compiled
        );

        /// <summary>
        /// Initializes the controller with dependencies.
        /// </summary>
        /// <param name="userRepository">The user repository for event operations.</param>
        /// <param name="blackboardService">The blackboard service for authentication.</param>
        public EventsController(
            IUserRepository userRepository,
            IBlackboardService blackboardService
        )
        {
            _userRepository = userRepository;
            _blackboardService = blackboardService;
        }

        /// <summary>
        /// Gets the session cookie from the request headers.
        /// </summary>
        private string? GetSessionCookie()
        {
            if (Request.Headers.TryGetValue("X-Session-Cookie", out var sessionCookieHeader))
            {
                var cookie = sessionCookieHeader.ToString();
                if (!string.IsNullOrEmpty(cookie))
                    return cookie;
            }

            if (Request.Headers.TryGetValue("Cookie", out var cookieHeaderValue))
            {
                return cookieHeaderValue.ToString();
            }

            return null;
        }

        /// <summary>
        /// Validates the session cookie and returns the username.
        /// </summary>
        private async Task<(
            bool IsValid,
            string? Username,
            IActionResult? ErrorResult
        )> ValidateSessionAsync()
        {
            var cookie = GetSessionCookie();
            if (string.IsNullOrEmpty(cookie))
            {
                return (
                    false,
                    null,
                    BadRequest(
                        new
                        {
                            error = "Session cookie is required in 'X-Session-Cookie' or 'Cookie' header.",
                        }
                    )
                );
            }

            var userData = await _blackboardService.GetUserDataAsync(cookie);
            if (!userData.IsSuccess)
            {
                return (false, null, Unauthorized(new { error = "Invalid or expired session." }));
            }

            var email = userData.UserData?.Email;
            if (string.IsNullOrEmpty(email))
            {
                return (
                    false,
                    null,
                    Unauthorized(new { error = "Unable to identify user from session." })
                );
            }

            var user = await _userRepository.GetByEmailAsync(email);
            if (user == null)
            {
                return (
                    false,
                    null,
                    Unauthorized(new { error = "User not found in system. Please login again." })
                );
            }

            return (true, user.Username, null);
        }

        /// <summary>
        /// Validates event data.
        /// </summary>
        private IActionResult? ValidateEventData(
            string title,
            DateTime start,
            DateTime end,
            string color
        )
        {
            if (string.IsNullOrWhiteSpace(title))
            {
                return BadRequest(new { error = "Title is required." });
            }

            if (start >= end)
            {
                return BadRequest(new { error = "Start date must be before end date." });
            }

            if (!HexColorRegex.IsMatch(color))
            {
                return BadRequest(
                    new { error = "Color must be a valid hexadecimal color code (e.g., #FF5733)." }
                );
            }

            return null;
        }

        /// <summary>
        /// Maps an Event model to EventDto.
        /// </summary>
        private static EventDto MapToDto(Event evt) =>
            new EventDto
            {
                Id = evt.Id,
                Title = evt.Title,
                Subject = evt.Subject,
                Start = evt.Start,
                End = evt.End,
                Location = evt.Location,
                Color = evt.Color,
                Category = evt.Category
            };

        /// <summary>
        /// Gets all events for the authenticated user.
        /// </summary>
        /// <param name="sessionCookieHeader">Session cookie from X-Session-Cookie header.</param>
        /// <param name="start">Optional filter for events starting after this date.</param>
        /// <param name="end">Optional filter for events ending before this date.</param>
        /// <returns>List of events.</returns>
        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<EventDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GetAll(
            [FromHeader(Name = "X-Session-Cookie")] string? sessionCookieHeader,
            [FromQuery] DateTime? start,
            [FromQuery] DateTime? end
        )
        {
            var validation = await ValidateSessionAsync();
            if (!validation.IsValid)
            {
                return validation.ErrorResult!;
            }

            var events = await _userRepository.GetUserEventsAsync(validation.Username!);

            if (start.HasValue)
            {
                events = events.Where(e => e.End >= start.Value).ToList();
            }

            if (end.HasValue)
            {
                events = events.Where(e => e.Start <= end.Value).ToList();
            }

            var dtos = events.Select(MapToDto);
            return Ok(dtos);
        }

        /// <summary>
        /// Gets a specific event by ID.
        /// </summary>
        /// <param name="id">The event ID.</param>
        /// <param name="sessionCookieHeader">Session cookie from X-Session-Cookie header.</param>
        /// <returns>The event details.</returns>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(EventDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetById(
            string id,
            [FromHeader(Name = "X-Session-Cookie")] string? sessionCookieHeader
        )
        {
            var validation = await ValidateSessionAsync();
            if (!validation.IsValid)
            {
                return validation.ErrorResult!;
            }

            var evt = await _userRepository.GetEventByIdAsync(validation.Username!, id);
            if (evt == null)
            {
                return NotFound(new { error = "Event not found." });
            }

            return Ok(MapToDto(evt));
        }

        /// <summary>
        /// Creates a new event.
        /// </summary>
        /// <param name="dto">The event data to create.</param>
        /// <param name="sessionCookieHeader">Session cookie from X-Session-Cookie header.</param>
        /// <returns>The created event with 201 status.</returns>
        [HttpPost]
        [ProducesResponseType(typeof(EventDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> Create(
            [FromBody] CreateEventDto dto,
            [FromHeader(Name = "X-Session-Cookie")] string? sessionCookieHeader
        )
        {
            var validation = await ValidateSessionAsync();
            if (!validation.IsValid)
            {
                return validation.ErrorResult!;
            }

            var validationError = ValidateEventData(dto.Title, dto.Start, dto.End, dto.Color);
            if (validationError != null)
            {
                return validationError;
            }

            var evt = new Event
            {
                Title = dto.Title.Trim(),
                Subject = dto.Subject?.Trim(),
                Start = dto.Start.ToUniversalTime(),
                End = dto.End.ToUniversalTime(),
                Location = dto.Location?.Trim(),
                Color = dto.Color.ToUpperInvariant(),
                Category = dto.Category,
            };

            var createdEvent = await _userRepository.AddEventAsync(validation.Username!, evt);

            return CreatedAtAction(
                nameof(GetById),
                new { id = createdEvent.Id },
                MapToDto(createdEvent)
            );
        }

        /// <summary>
        /// Updates an existing event.
        /// </summary>
        /// <param name="id">The event ID.</param>
        /// <param name="dto">The updated event data.</param>
        /// <param name="sessionCookieHeader">Session cookie from X-Session-Cookie header.</param>
        /// <returns>204 No Content on success.</returns>
        [HttpPut("{id}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Update(
            string id,
            [FromBody] UpdateEventDto dto,
            [FromHeader(Name = "X-Session-Cookie")] string? sessionCookieHeader
        )
        {
            var validation = await ValidateSessionAsync();
            if (!validation.IsValid)
            {
                return validation.ErrorResult!;
            }

            var existingEvent = await _userRepository.GetEventByIdAsync(validation.Username!, id);
            if (existingEvent == null)
            {
                return NotFound(new { error = "Event not found." });
            }

            if (dto.Title != null)
                existingEvent.Title = dto.Title.Trim();

            if (dto.Subject != null)
                existingEvent.Subject = string.IsNullOrWhiteSpace(dto.Subject)
                    ? null
                    : dto.Subject.Trim();

            if (dto.Start.HasValue)
                existingEvent.Start = dto.Start.Value.ToUniversalTime();

            if (dto.End.HasValue)
                existingEvent.End = dto.End.Value.ToUniversalTime();

            if (dto.Location != null)
                existingEvent.Location = string.IsNullOrWhiteSpace(dto.Location)
                    ? null
                    : dto.Location.Trim();

            if (dto.Color != null)
                existingEvent.Color = dto.Color.ToUpperInvariant();

            if (dto.Category.HasValue)
                existingEvent.Category = dto.Category.Value;

            var validationError = ValidateEventData(
                existingEvent.Title,
                existingEvent.Start,
                existingEvent.End,
                existingEvent.Color
            );

            if (validationError != null)
            {
                return validationError;
            }

            var updated = await _userRepository.UpdateEventAsync(
                validation.Username!,
                existingEvent
            );
            if (!updated)
            {
                return NotFound(new { error = "Event not found or could not be updated." });
            }
            return NoContent();
        }

        /// <summary>
        /// Deletes an event.
        /// </summary>
        /// <param name="id">The event ID.</param>
        /// <param name="sessionCookieHeader">Session cookie from X-Session-Cookie header.</param>
        /// <returns>204 No Content on success.</returns>
        [HttpDelete("{id}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Delete(
            string id,
            [FromHeader(Name = "X-Session-Cookie")] string? sessionCookieHeader
        )
        {
            var validation = await ValidateSessionAsync();
            if (!validation.IsValid)
            {
                return validation.ErrorResult!;
            }

            var existingEvent = await _userRepository.GetEventByIdAsync(validation.Username!, id);
            if (existingEvent == null)
            {
                return NotFound(new { error = "Event not found." });
            }

            await _userRepository.DeleteEventAsync(validation.Username!, id);
            return NoContent();
        }
    }
}
