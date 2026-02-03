using backend.Models;
using backend.Repositories;
using backend.Services;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using Google.Apis.Auth;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Auth.OAuth2.Requests;
using Google.Apis.Auth.OAuth2.Responses;
using System.Threading;

namespace backend.Controllers
{
    /// <summary>
    /// Controller for Google OAuth2 connect/callback flow to link Google accounts to user accounts.
    /// </summary>
    [ApiController]
    [Route("api/auth/google")]
    public class GoogleAuthController : ControllerBase
    {
        private readonly IUserRepository _userRepository;
        private readonly IBlackboardService _blackboardService;
        private readonly IConfiguration _configuration;

        // Temporary storage for session cookies during OAuth flow (state -> cookie)
        // In production, use distributed cache (Redis) instead of in-memory
        private static readonly Dictionary<string, (string cookie, DateTime expiry)> _sessionCache = new();

        /// <summary>
        /// Initializes a new instance of the <see cref="GoogleAuthController"/> class.
        /// </summary>
        /// <param name="userRepository">User repository used to fetch and persist user accounts.</param>
        /// <param name="blackboardService">Service for validating Blackboard sessions and retrieving user info.</param>
        /// <param name="configuration">Application configuration containing Google OAuth settings.</param>
        public GoogleAuthController(IUserRepository userRepository, IBlackboardService blackboardService, IConfiguration configuration)
        {
            _userRepository = userRepository;
            _blackboardService = blackboardService;
            _configuration = configuration;
        }

        /// <summary>
        /// Returns the Google OAuth2 authorization URL for the frontend to redirect the user and obtain consent.
        /// Uses the minimal calendar events scope and requests offline access (refresh token).
        /// Stores the Blackboard session temporarily to retrieve it in the callback.
        /// </summary>
        /// <param name="sessionCookieHeader">Optional session cookie extracted from the 'X-Session-Cookie' header; falls back to 'Cookie' header.</param>
        /// <returns>200 OK with the authorization URL and state token, or 400/500 on error.</returns>
        [HttpGet("connect")]
        [ProducesResponseType(typeof(GoogleConnectResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public ActionResult Connect([FromHeader(Name = "X-Session-Cookie")] string? sessionCookieHeader)
        {
            var clientId = _configuration["Google:ClientId"] ?? Environment.GetEnvironmentVariable("Google__ClientId");
            var redirect = _configuration["Google:RedirectUri"] ?? Environment.GetEnvironmentVariable("Google__RedirectUri");
            var scope = "https://www.googleapis.com/auth/calendar.events openid email profile";

            if (string.IsNullOrWhiteSpace(clientId) || string.IsNullOrWhiteSpace(redirect))
            {
                return Problem("Missing Google OAuth configuration. Ensure Google__ClientId and Google__RedirectUri are set.", statusCode: StatusCodes.Status500InternalServerError);
            }

            // Get session cookie from header or Cookie
            var cookie = sessionCookieHeader;
            if (string.IsNullOrEmpty(cookie) && Request.Headers.TryGetValue("Cookie", out var c))
                cookie = c.ToString();
            if (string.IsNullOrEmpty(cookie))
                return BadRequest(new { message = "Session cookie is required in 'X-Session-Cookie' or 'Cookie' header." });

            // Generate a state token to identify this session
            var stateToken = Guid.NewGuid().ToString();

            // Store session cookie in cache (expires in 10 minutes)
            _sessionCache[stateToken] = (cookie, DateTime.UtcNow.AddMinutes(10));

            var requestUrl = new GoogleAuthorizationCodeRequestUrl(new Uri("https://accounts.google.com/o/oauth2/v2/auth"))
            {
                ClientId = clientId,
                RedirectUri = redirect,
                Scope = scope,
                AccessType = "offline",
                Prompt = "consent",
                ResponseType = "code",
                State = stateToken
            };

            var url = requestUrl.Build();

            return Ok(new GoogleConnectResponse { Url = url, StateToken = stateToken });
        }

        /// <summary>
        /// OAuth2 callback endpoint that exchanges the authorization <paramref name="code"/> for tokens
        /// and links the Google account (stores refresh token) to the current authenticated user (identified via state token).
        /// </summary>
        /// <param name="code">Authorization code returned by Google.</param>
        /// <param name="state">State token used to correlate the OAuth flow with the originating session.</param>
        /// <returns>200 OK on success (Google account linked), or 400/500 with error details.</returns>
        [HttpGet("callback")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult> Callback([FromQuery] string? code, [FromQuery] string? state)
        {
            if (string.IsNullOrEmpty(code)) return BadRequest(new { message = "Missing code query parameter." });
            if (string.IsNullOrEmpty(state)) return BadRequest(new { message = "Missing state query parameter." });

            // Retrieve session cookie from cache using state token
            if (!_sessionCache.TryGetValue(state, out var cachedSession))
            {
                return BadRequest(new { message = "Invalid or expired state token. Please start the OAuth flow again by calling /connect." });
            }

            // Check if cache entry expired
            if (DateTime.UtcNow > cachedSession.expiry)
            {
                _sessionCache.Remove(state);
                return BadRequest(new { message = "Session expired. Please start the OAuth flow again by calling /connect." });
            }

            var cookie = cachedSession.cookie;
            _sessionCache.Remove(state); // Clean up cache entry

            var clientId = _configuration["Google:ClientId"] ?? Environment.GetEnvironmentVariable("Google__ClientId");
            var clientSecret = _configuration["Google:ClientSecret"] ?? Environment.GetEnvironmentVariable("Google__ClientSecret");
            var redirect = _configuration["Google:RedirectUri"] ?? Environment.GetEnvironmentVariable("Google__RedirectUri");

            if (string.IsNullOrWhiteSpace(clientId) || string.IsNullOrWhiteSpace(clientSecret) || string.IsNullOrWhiteSpace(redirect))
            {
                return Problem("Missing Google OAuth configuration. Ensure Google__ClientId, Google__ClientSecret and Google__RedirectUri are set.", statusCode: StatusCodes.Status500InternalServerError);
            }

            var flow = new GoogleAuthorizationCodeFlow(new GoogleAuthorizationCodeFlow.Initializer
            {
                ClientSecrets = new Google.Apis.Auth.OAuth2.ClientSecrets { ClientId = clientId, ClientSecret = clientSecret }
            });

            TokenResponse token;
            try
            {
                token = await flow.ExchangeCodeForTokenAsync(state, code, redirect, CancellationToken.None);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = "Failed to exchange code for tokens.", details = ex.Message });
            }

            var accessToken = token.AccessToken;
            if (string.IsNullOrEmpty(accessToken))
            {
                return BadRequest(new { message = "Failed to get access token from Google." });
            }
            var refreshToken = token.RefreshToken;
            var expiresIn = token.ExpiresInSeconds.HasValue ? (int)token.ExpiresInSeconds.Value : 3600;

            // Validate ID token using Google library (verifies signature, issuer, expiry and audience)
            var idToken = token.IdToken;
            string? googleId = null;
            string? googleEmail = null;

            if (!string.IsNullOrEmpty(idToken))
            {
                try
                {
                    var settings = new GoogleJsonWebSignature.ValidationSettings
                    {
                        Audience = new[] { clientId },
                        IssuedAtClockTolerance = TimeSpan.FromMinutes(5),
                        ExpirationTimeClockTolerance = TimeSpan.FromMinutes(5)
                    };

                    var payload = await GoogleJsonWebSignature.ValidateAsync(idToken, settings);
                    googleId = payload.Subject;
                    googleEmail = payload.Email;
                }
                catch (Exception ex)
                {
                    return BadRequest(new { message = "Failed to validate ID token.", details = ex.Message });
                }
            }

            // Fallback: if no ID token, try userinfo endpoint
            if (string.IsNullOrEmpty(googleEmail) || string.IsNullOrEmpty(googleId))
            {
                using var http = new HttpClient();
                var infoReq = new HttpRequestMessage(HttpMethod.Get, "https://www.googleapis.com/oauth2/v1/userinfo");
                infoReq.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
                var infoResp = await http.SendAsync(infoReq);
                if (infoResp.IsSuccessStatusCode)
                {
                    var infoJson = await infoResp.Content.ReadAsStringAsync();
                    using var infoDoc = JsonDocument.Parse(infoJson);
                    var infoRoot = infoDoc.RootElement;
                    if (string.IsNullOrEmpty(googleId)) googleId = infoRoot.TryGetProperty("id", out var idProp) ? idProp.GetString() : null;
                    if (string.IsNullOrEmpty(googleEmail)) googleEmail = infoRoot.TryGetProperty("email", out var emailProp) ? emailProp.GetString() : null;
                }
            }

            // Get Blackboard user from cached session cookie - REQUIRED to link Google account
            var bbResult = await _blackboardService.GetUserDataAsync(cookie);
            if (bbResult?.IsSuccess != true)
            {
                return BadRequest(new { message = "Failed to validate Blackboard session.", details = bbResult?.Message });
            }

            var bbEmail = bbResult.UserData?.Email;
            if (string.IsNullOrEmpty(bbEmail))
            {
                return BadRequest(new { message = "Unable to retrieve your email from Blackboard session." });
            }

            // Find or create user by Blackboard email (primary identity)
            backend.Models.User? user = await _userRepository.GetByEmailAsync(bbEmail);
            if (user == null)
            {
                // Create user linked to Blackboard account
                await _userRepository.UpsertByUsernameAsync(bbEmail, bbEmail);
                user = await _userRepository.GetByUsernameAsync(bbEmail);
            }

            if (user == null)
            {
                return BadRequest(new { message = "Failed to create or find user for linking Google account." });
            }

            var account = new GoogleAccount
            {
                GoogleId = googleId,
                Email = googleEmail,
                RefreshToken = refreshToken,
                AccessToken = accessToken,
                AccessTokenExpiry = DateTime.UtcNow.AddSeconds(expiresIn),
                Scopes = new[] { "https://www.googleapis.com/auth/calendar.events" }
            };

            await _userRepository.UpsertGoogleAccountAsync(user.Username, account);

            return Ok(new { message = "Google account linked successfully", userEmail = bbEmail, googleEmail = googleEmail });
        }
    }
}
