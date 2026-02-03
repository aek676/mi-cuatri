using backend.Dtos;
using backend.Models;
using backend.Repositories;
using Microsoft.Extensions.Logging;
using System.Linq;
using System.Text.Json;
using System.Globalization;

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

        public GoogleCalendarService(IUserRepository userRepository, IConfiguration configuration, ILogger<GoogleCalendarService> logger)
        {
            _userRepository = userRepository;
            _logger = logger;
            _clientId = configuration["Google:ClientId"] ?? Environment.GetEnvironmentVariable("Google__ClientId") ?? string.Empty;
            _clientSecret = configuration["Google:ClientSecret"] ?? Environment.GetEnvironmentVariable("Google__ClientSecret") ?? string.Empty;
        }

        public async Task<ExportSummaryDto> ExportEventsAsync(string username, IEnumerable<CalendarItemDto> items)
        {
            var summary = new ExportSummaryDto { Created = 0, Updated = 0, Failed = 0, Errors = new List<string>() };

            var user = await _userRepository.GetByUsernameAsync(username);
            if (user == null || user.GoogleAccount == null)
            {
                throw new InvalidOperationException("User is not connected to Google Calendar.");
            }

            string accessToken = await EnsureAccessTokenAsync(user);

            // 2. Inicializar el cliente HTTP
            using var client = new HttpClient();
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
            client.BaseAddress = new Uri("https://www.googleapis.com/");

            foreach (var item in items)
            {
                try
                {
                    // 3. Crear el payload del evento (UTC)
                    var payload = new Dictionary<string, object>
                    {
                        ["summary"] = string.IsNullOrWhiteSpace(item.Subject) ? item.Title : $"{item.Subject} - {item.Title}",
                        ["description"] = item.Description ?? "",
                        ["location"] = item.Location ?? "",
                        ["start"] = new Dictionary<string, string>
                        {
                            ["dateTime"] = item.Start.ToUniversalTime().ToString("yyyy-MM-dd'T'HH:mm:ssZ"),
                            ["timeZone"] = "UTC"
                        },
                        ["end"] = new Dictionary<string, string>
                        {
                            ["dateTime"] = item.End.ToUniversalTime().ToString("yyyy-MM-dd'T'HH:mm:ssZ"),
                            ["timeZone"] = "UTC"
                        },
                        ["extendedProperties"] = new Dictionary<string, object>
                        {
                            ["private"] = new Dictionary<string, string> { ["mi-cuatri-id"] = item.CalendarId }
                        }
                    };

                    // Map and attach colorId if user provided a color (hex)
                    var colorId = GetClosestGoogleColorId(item.Color);
                    if (colorId != null)
                    {
                        payload["colorId"] = colorId;
                    }
                    else if (!string.IsNullOrWhiteSpace(item.Color))
                    {
                        // Color provided but invalid or unsupported — log and proceed without color
                        _logger.LogWarning("Invalid or unsupported color '{Color}' for event {CalendarId}; skipping color assignment.", item.Color, item.CalendarId);
                    }

                    // 4. Buscar evento existente
                    string listUri = $"calendar/v3/calendars/primary/events?privateExtendedProperty={System.Net.WebUtility.UrlEncode($"mi-cuatri-id={item.CalendarId}")}&fields=items(id)";
                    var listResp = await client.GetAsync(listUri);
                    var listContent = await listResp.Content.ReadAsStringAsync();

                    if (!listResp.IsSuccessStatusCode)
                    {
                        throw new InvalidOperationException($"Failed to list events: {listResp.StatusCode}");
                    }

                    string eventId = null;
                    using (var listDoc = JsonDocument.Parse(listContent))
                    {
                        if (listDoc.RootElement.TryGetProperty("items", out var arr) &&
                            arr.ValueKind == JsonValueKind.Array && arr.GetArrayLength() > 0)
                        {
                            if (arr[0].TryGetProperty("id", out var idProp))
                                eventId = idProp.GetString();

                            if (arr.GetArrayLength() > 1)
                                _logger.LogWarning("Multiple events found for mi-cuatri-id {Id}, using first", item.CalendarId);
                        }
                    }

                    if (!string.IsNullOrEmpty(eventId))
                    {
                        // ACTUALIZAR (PATCH)
                        var content = new StringContent(JsonSerializer.Serialize(payload), System.Text.Encoding.UTF8, "application/json");
                        var patchReq = new HttpRequestMessage(new HttpMethod("PATCH"), $"calendar/v3/calendars/primary/events/{eventId}")
                        {
                            Content = content
                        };
                        var patchResp = await client.SendAsync(patchReq);
                        var patchContent = await patchResp.Content.ReadAsStringAsync();

                        if (!patchResp.IsSuccessStatusCode)
                        {
                            throw new InvalidOperationException($"Failed to update event: {patchResp.StatusCode}");
                        }

                        summary.Updated++;
                        _logger.LogInformation("Updated Google event {EventId} for mi-cuatri-id {Id}", eventId, item.CalendarId);
                    }
                    else
                    {
                        // CREAR (INSERT)
                        var content = new StringContent(JsonSerializer.Serialize(payload), System.Text.Encoding.UTF8, "application/json");
                        var insertResp = await client.PostAsync("calendar/v3/calendars/primary/events", content);
                        var insertContent = await insertResp.Content.ReadAsStringAsync();

                        if (!insertResp.IsSuccessStatusCode)
                        {
                            throw new InvalidOperationException($"Failed to create event: {insertResp.StatusCode}");
                        }

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

        /// <summary>
        /// Ensures a valid access token is available, refreshing if necessary.
        /// </summary>
        private async Task<string> EnsureAccessTokenAsync(User user)
        {
            var account = user.GoogleAccount ?? throw new InvalidOperationException("No Google account available");

            if (!string.IsNullOrEmpty(account.AccessToken) &&
                account.AccessTokenExpiry.HasValue &&
                account.AccessTokenExpiry.Value > DateTime.UtcNow.AddMinutes(1))
            {
                return account.AccessToken;
            }

            if (string.IsNullOrEmpty(account.RefreshToken))
            {
                throw new InvalidOperationException("Refresh token is not available for the user.");
            }

            using var client = new HttpClient();
            var body = new Dictionary<string, string>
            {
                { "client_id", _clientId },
                { "client_secret", _clientSecret },
                { "refresh_token", account.RefreshToken },
                { "grant_type", "refresh_token" }
            };

            var resp = await client.PostAsync(TokenEndpoint, new FormUrlEncodedContent(body));
            if (!resp.IsSuccessStatusCode)
            {
                var content = await resp.Content.ReadAsStringAsync();
                _logger.LogError("Failed to refresh Google access token: {Status}", resp.StatusCode);
                throw new InvalidOperationException("Failed to refresh Google access token");
            }

            var json = await resp.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            if (!root.TryGetProperty("access_token", out var tokenProp))
            {
                _logger.LogError("Token refresh response missing access_token");
                throw new InvalidOperationException("No access token received from Google");
            }

            var newAccessToken = tokenProp.GetString();
            var expiresIn = root.TryGetProperty("expires_in", out var exProp) ? exProp.GetInt32() : 3600;

            account.AccessToken = newAccessToken;
            account.AccessTokenExpiry = DateTime.UtcNow.AddSeconds(expiresIn);

            await _userRepository.UpsertGoogleAccountAsync(user.Username, account);

            return account.AccessToken;
        }
    }
}
