using Microsoft.AspNetCore.Mvc;
using backend.Dtos;
using backend.Services;
using backend.Repositories;

namespace backend.Controllers
{
    /// <summary>
    /// Controller for handling authentication operations.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IBlackboardService _blackboardService;
        private readonly IUserRepository _userRepository;

        /// <summary>
        /// Initializes a new instance of the AuthController class.
        /// </summary>
        /// <param name="blackboardService">The blackboard service for authentication.</param>
        /// <param name="userRepository">The user repository for persisting username on login.</param>
        public AuthController(IBlackboardService blackboardService, IUserRepository userRepository)
        {
            _blackboardService = blackboardService;
            _userRepository = userRepository;
        }

        /// <summary>
        /// Authenticates a user with the provided credentials.
        /// </summary>
        /// <param name="request">The login request containing username and password.</param>
        /// <returns>A login response with authentication result and session cookie if successful.</returns>
        [HttpPost("login-ual")]
        [ProducesResponseType(typeof(LoginResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(LoginResponseDto), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<LoginResponseDto>> Login([FromBody] LoginRequestDto request)
        {
            var result = await _blackboardService.AuthenticateAsync(request.Username, request.Password);

            if (result.IsSuccess)
            {
                await _userRepository.UpsertByUsernameAsync(request.Username);
                return Ok(result);
            }
            else
            {
                return Unauthorized(result);
            }
        }

        /// <summary>
        /// Retrieves the current authenticated user's information.
        /// </summary>
        /// <param name="sessionCookieHeader">The session cookie from the X-Session-Cookie header.</param>
        /// <returns>User response containing user details or error message.</returns>
        [HttpGet("me")]
        [ProducesResponseType(typeof(UserResponseDto), StatusCodes.Status200OK)]
        public async Task<ActionResult<UserResponseDto>> Me([FromHeader(Name = "X-Session-Cookie")] string? sessionCookieHeader)
        {
            var cookie = sessionCookieHeader;
            if (string.IsNullOrEmpty(cookie) && Request.Headers.TryGetValue("Cookie", out var cookieHeaderValue))
            {
                cookie = cookieHeaderValue.ToString();
            }

            if (string.IsNullOrEmpty(cookie))
            {
                return BadRequest("Session cookie is required in 'X-Session-Cookie' or 'Cookie' header.");
            }

            var result = await _blackboardService.GetUserDataAsync(cookie);

            if (result.IsSuccess)
            {
                return Ok(result);
            }
            else
            {
                return BadRequest(result);
            }
        }
    }
}