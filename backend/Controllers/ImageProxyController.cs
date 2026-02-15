using backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace backend.Controllers
{
    /// <summary>
    /// Proxy controller to fetch images from Blackboard, forwarding session token when provided.
    /// Uses token from X-Session-Cookie header
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [AllowAnonymous] // Allows token validation via custom logic
    public class ImageProxyController : ControllerBase
    {
        private readonly IBlackboardService _blackboardService;
        private readonly ILogger<ImageProxyController> _logger;

        /// <summary>
        /// Initializes a new instance of the ImageProxyController.
        /// </summary>
        /// <param name="blackboardService">The Blackboard service for proxying image requests.</param>
        /// <param name="logger">The logger instance.</param>
        public ImageProxyController(IBlackboardService blackboardService, ILogger<ImageProxyController> logger)
        {
            _blackboardService = blackboardService;
            _logger = logger;
        }

        /// <summary>
        /// Proxies an image request from Blackboard and returns the image stream as a blob.
        /// Accepts token only via X-Session-Cookie header
        /// </summary>
        /// <param name="imageUrl">The URL of the image to proxy.</param>
        /// <param name="sessionCookieHeader">Session token from X-Session-Cookie header.</param>
        /// <returns>Image stream (200), 400 (bad request), 401 (unauthorized), or 404 (not found).</returns>
        [HttpGet]
        [Produces("image/png", "image/jpeg", "image/gif", "image/webp", "image/svg+xml", "application/json")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(byte[]))]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Get(
            [FromQuery] string? imageUrl,
            [FromHeader(Name = "X-Session-Cookie")] string? sessionCookieHeader)
        {
            // Validate imageUrl parameter
            if (string.IsNullOrEmpty(imageUrl))
            {
                _logger.LogWarning("ImageProxy called without imageUrl parameter");
                return BadRequest(new { error = "imageUrl parameter is required" });
            }

            // Get token from header only
            string? sessionCookie = sessionCookieHeader;

            // Validate token is provided
            if (string.IsNullOrEmpty(sessionCookie))
            {
                _logger.LogWarning("ImageProxy called without authentication token for URL: {ImageUrl}", imageUrl);
                return Unauthorized(new { error = "Authentication token required. Provide token via X-Session-Cookie header" });
            }

            // Format token as cookie if needed
            if (!sessionCookie.Contains("="))
            {
                sessionCookie = $"bb_session={sessionCookie}";
            }

            try
            {
                var acceptHeader = Request.Headers.Accept.ToString();

                // Fetch image through Blackboard service
                var response = await _blackboardService.GetProxiedImageResponseAsync(sessionCookie, imageUrl, acceptHeader);
                
                if (response == null)
                {
                    _logger.LogWarning("Failed to fetch image - null response for URL: {ImageUrl}", imageUrl);
                    return NotFound(new { error = "Image not found or domain not allowed" });
                }

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Failed to fetch image - status {StatusCode} for URL: {ImageUrl}", response.StatusCode, imageUrl);
                    
                    if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized || 
                        response.StatusCode == System.Net.HttpStatusCode.Forbidden)
                    {
                        return Unauthorized(new { error = "Invalid or expired session token" });
                    }

                    if (response.StatusCode == System.Net.HttpStatusCode.NotAcceptable)
                    {
                        return StatusCode(StatusCodes.Status406NotAcceptable, new { error = "Upstream rejected the requested Accept header" });
                    }
                    
                    return NotFound(new { error = "Image not found" });
                }

                // Read image as byte array to preserve binary integrity
                var imageBytes = await response.Content.ReadAsByteArrayAsync();
                
                // Use MediaType only (without charset) for binary image content
                var rawContentType = response.Content.Headers.ContentType?.MediaType ?? "image/jpeg";
                
                // Ensure content-type is one of the declared [Produces] types
                var allowedImageTypes = new[] { "image/png", "image/jpeg", "image/gif", "image/webp", "image/svg+xml" };
                var contentType = allowedImageTypes.Contains(rawContentType, StringComparer.OrdinalIgnoreCase) 
                    ? rawContentType 
                    : "image/jpeg"; // Default to image/jpeg for unknown image types
                
                _logger.LogInformation("Successfully proxied image: {ImageUrl}, ContentType: {ContentType}, Size: {Size} bytes", imageUrl, contentType, imageBytes.Length);
                
                return File(imageBytes, contentType);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error proxying image from URL: {ImageUrl}", imageUrl);
                return StatusCode(StatusCodes.Status500InternalServerError, new { error = "Failed to fetch image" });
            }
        }
    }
}
