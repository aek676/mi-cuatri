using backend.Dtos;
using backend.Models;
using backend.Repositories;
using System.Text.Json;
using System.Globalization;
using System.Linq;
using System.Threading;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Auth.OAuth2.Responses;
using Google.Apis.Services;
using Google.Apis.Calendar.v3;
using Google.Apis.Calendar.v3.Data;

namespace backend.Services
{
    /// <summary>
    /// Service responsible for exporting events to Google Calendar and handling token refresh.
    /// Uses pure HttpClient approach for compatibility with net10.0 and Google Apis 1.8.1.
    /// </summary>
    public class GoogleCalendarService : IGoogleCalendarService
    {
        private readonly IUserRepository _userRepository;
        private readonly ILogger<GoogleCalendarService> _logger;
        private readonly string _clientId;
        private readonly string _clientSecret;
        private const string TokenEndpoint = "https://oauth2.googleapis.com/token";

        /// <summary>
        /// Initializes a new instance of the <see cref="GoogleCalendarService"/> class.
        /// </summary>
        /// <param name="userRepository">Repository to read/update users and Google account tokens.</param>
        /// <param name="configuration">Configuration for Google client ID and secret.</param>
        /// <param name="logger">Logger for diagnostic messages.</param>
        public GoogleCalendarService(IUserRepository userRepository, IConfiguration configuration, ILogger<GoogleCalendarService> logger)
        {
            _userRepository = userRepository;
            _logger = logger;
            _clientId = configuration["Google:ClientId"] ?? Environment.GetEnvironmentVariable("Google__ClientId") ?? string.Empty;
            _clientSecret = configuration["Google:ClientSecret"] ?? Environment.GetEnvironmentVariable("Google__ClientSecret") ?? string.Empty;
        }

        /// <summary>
        /// Exports the provided calendar items to the user's Google Calendar and returns a summary.
        /// </summary>
        /// <param name="username">Local username of the Google account owner.</param>
        /// <param name="items">Collection of calendar items to export.</param>
        /// <returns>ExportSummaryDto summarizing created, updated, and failed counts and errors.</returns>
        public async Task<ExportSummaryDto> ExportEventsAsync(string username, IEnumerable<CalendarItemDto> items)
        {
            var summary = new ExportSummaryDto { Created = 0, Updated = 0, Failed = 0, Errors = new List<string>() };

            var user = await _userRepository.GetByUsernameAsync(username);
            if (user == null || user.GoogleAccount == null)
            {
                throw new InvalidOperationException("User is not connected to Google Calendar.");
            }

            var account = user.GoogleAccount;

            // Build token response and credential/flow for refresh and authorized requests
            var token = new TokenResponse
            {
                AccessToken = account.AccessToken,
                RefreshToken = account.RefreshToken,
                ExpiresInSeconds = account.AccessTokenExpiry.HasValue ? (long?)(account.AccessTokenExpiry.Value - DateTime.UtcNow).TotalSeconds : null
            };

            var flow = new GoogleAuthorizationCodeFlow(new GoogleAuthorizationCodeFlow.Initializer
            {
                ClientSecrets = new ClientSecrets { ClientId = _clientId, ClientSecret = _clientSecret }
            });

            var credential = new UserCredential(flow, user.Username, token);

            // Refresh if needed
            if (string.IsNullOrEmpty(account.AccessToken) || !account.AccessTokenExpiry.HasValue || account.AccessTokenExpiry.Value <= DateTime.UtcNow.AddMinutes(1))
            {
                if (string.IsNullOrEmpty(account.RefreshToken))
                {
                    throw new InvalidOperationException("Refresh token is not available for the user.");
                }

                var refreshed = await credential.RefreshTokenAsync(CancellationToken.None);
                if (!refreshed)
                {
                    throw new InvalidOperationException("Failed to refresh Google access token");
                }

                account.AccessToken = credential.Token.AccessToken;
                account.AccessTokenExpiry = DateTime.UtcNow.AddSeconds(credential.Token.ExpiresInSeconds ?? 3600);
                await _userRepository.UpsertGoogleAccountAsync(user.Username, account);
            }

            var service = new CalendarService(new BaseClientService.Initializer
            {
                HttpClientInitializer = credential,
                ApplicationName = "mi-cuatri"
            });

            foreach (var item in items)
            {
                try
                {
                    var eventResource = new Event
                    {
                        Summary = string.IsNullOrWhiteSpace(item.Subject) ? item.Title : $"{item.Subject} - {item.Title}",
                        Description = item.Description ?? string.Empty,
                        Location = item.Location ?? string.Empty,
                        Start = new EventDateTime { DateTimeDateTimeOffset = new DateTimeOffset(item.Start.ToUniversalTime()), TimeZone = "UTC" },
                        End = new EventDateTime { DateTimeDateTimeOffset = new DateTimeOffset(item.End.ToUniversalTime()), TimeZone = "UTC" }
                    };

                    // Set private extended properties via reflection to remain compatible across client versions
                    var ext = new Event.ExtendedPropertiesData();
                    var privateProp = ext.GetType().GetProperty("Private");
                    if (privateProp != null && privateProp.PropertyType == typeof(IDictionary<string, string>))
                    {
                        privateProp.SetValue(ext, new Dictionary<string, string> { ["mi-cuatri-id"] = item.CalendarId });
                    }
                    else
                    {
                        // Fallback: try to find any IDictionary<string,string> property and set it
                        var dictProp = ext.GetType().GetProperties().FirstOrDefault(pr => pr.PropertyType == typeof(IDictionary<string, string>));
                        if (dictProp != null)
                        {
                            dictProp.SetValue(ext, new Dictionary<string, string> { ["mi-cuatri-id"] = item.CalendarId });
                        }
                        else
                        {
                            _logger.LogWarning("Unable to set extended properties for event {CalendarId}", item.CalendarId);
                        }
                    }

                    eventResource.ExtendedProperties = ext;

                    var colorId = GetClosestGoogleColorId(item.Color);
                    if (colorId != null)
                    {
                        eventResource.ColorId = colorId;
                    }
                    else if (!string.IsNullOrWhiteSpace(item.Color))
                    {
                        _logger.LogWarning("Invalid or unsupported color '{Color}' for event {CalendarId}; skipping color assignment.", item.Color, item.CalendarId);
                    }

                    // Find existing event by private extended property
                    var listReq = service.Events.List("primary");
                    listReq.PrivateExtendedProperty = $"mi-cuatri-id={item.CalendarId}";
                    listReq.Fields = "items(id)";
                    var list = await listReq.ExecuteAsync();

                    string? eventId = list.Items?.FirstOrDefault()?.Id;

                    if (!string.IsNullOrEmpty(eventId))
                    {
                        var patchReq = service.Events.Patch(eventResource, "primary", eventId);
                        await patchReq.ExecuteAsync();
                        summary.Updated++;
                        _logger.LogInformation("Updated Google event {EventId} for mi-cuatri-id {Id}", eventId, item.CalendarId);
                    }
                    else
                    {
                        var insertReq = service.Events.Insert(eventResource, "primary");
                        await insertReq.ExecuteAsync();
                        summary.Created++;
                        _logger.LogInformation("Created Google event for mi-cuatri-id {Id}", item.CalendarId);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error exporting event {CalendarId} for user {User}", item.CalendarId, username);
                    summary.Failed++;
                    summary.Errors.Add($"Error exporting event {item.CalendarId}: {ex.Message}");
                }
            }

            return summary;
        }

        /// <summary>
        /// Map a hex color string to the closest Google Calendar event color id ("1".."11"). Returns null if invalid/unsupported.
        /// </summary>
        private static string? GetClosestGoogleColorId(string? hex)
        {
            if (string.IsNullOrWhiteSpace(hex)) return null;

            var h = hex.Trim().TrimStart('#');
            if (h.Length == 3)
            {
                // expand shorthand e.g. 'f00' -> 'ff0000'
                h = string.Concat(h.Select(c => $"{c}{c}"));
            }

            if (h.Length != 6) return null;

            if (!int.TryParse(h, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var rgb)) return null;

            var r = (rgb >> 16) & 0xFF;
            var g = (rgb >> 8) & 0xFF;
            var b = rgb & 0xFF;

            // Palette approximating Google Calendar event colors (id => rgb)
            var palette = new Dictionary<string, (int r, int g, int b)>
            {
                ["1"] = (199, 0, 0),
                ["2"] = (255, 115, 0),
                ["3"] = (255, 213, 0),
                ["4"] = (0, 136, 51),
                ["5"] = (0, 176, 255),
                ["6"] = (26, 115, 232),
                ["7"] = (106, 27, 154),
                ["8"] = (233, 30, 99),
                ["9"] = (141, 110, 99),
                ["10"] = (121, 85, 72),
                ["11"] = (3, 155, 0)
            };

            string? bestId = null;
            double bestDist = double.MaxValue;
            foreach (var kv in palette)
            {
                var dr = r - kv.Value.r;
                var dg = g - kv.Value.g;
                var db = b - kv.Value.b;
                var dist = dr * dr + dg * dg + db * db;
                if (dist < bestDist)
                {
                    bestDist = dist;
                    bestId = kv.Key;
                }
            }

            return bestId;
        }


    }
}
