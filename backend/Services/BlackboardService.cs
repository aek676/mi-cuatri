using HtmlAgilityPack;
using backend.Dtos;
using backend.Enums;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Text.Json.Nodes;
using System.Globalization;

namespace backend.Services
{
    /// <summary>
    /// Service for Blackboard authentication and data retrieval operations.
    /// </summary>
    public class BlackboardService : IBlackboardService
    {
        private const string BASE_URL = "https://aulavirtual.ual.es";
        private const string LOGIN_PATH = "/webapps/login/";
        private const string API_ME_URL = "/learn/api/public/v1/users/me";
        private const string API_CALENDAR_ITEMS_URL = "/learn/api/public/v1/calendars/items";

        /// <summary>
        /// Authenticates a user with Blackboard credentials.
        /// </summary>
        /// <param name="username">The user's username.</param>
        /// <param name="password">The user's password.</param>
        /// <returns>A login response containing authentication status and session cookie if successful.</returns>
        public async Task<LoginResponseDto> AuthenticateAsync(string username, string password)
        {
            var handler = new HttpClientHandler
            {
                UseCookies = false,
                AllowAutoRedirect = false,
                SslProtocols = System.Security.Authentication.SslProtocols.Tls12
            };

            using (var client = new HttpClient(handler))
            {
                client.BaseAddress = new Uri(BASE_URL);

                client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
                client.DefaultRequestHeaders.Add("Accept", "*/*");
                client.DefaultRequestHeaders.ExpectContinue = false;

                var requestGet = new HttpRequestMessage(HttpMethod.Get, LOGIN_PATH);
                var responseGet = await client.SendAsync(requestGet);
                var htmlGet = await responseGet.Content.ReadAsStringAsync();

                if (!responseGet.Headers.TryGetValues("Set-Cookie", out var cookieHeaders))
                    return new LoginResponseDto { IsSuccess = false, Message = "Error: Sin cookies iniciales." };

                var cookieList = new List<string>();
                foreach (var header in cookieHeaders) cookieList.Add(header.Split(';')[0]);
                string manualCookieHeader = string.Join("; ", cookieList);

                var doc = new HtmlDocument();
                doc.LoadHtml(htmlGet);
                var nonceNode = doc.DocumentNode.SelectSingleNode("//input[@name='blackboard.platform.security.NonceUtil.nonce.ajax']");
                var nonceValue = nonceNode?.GetAttributeValue("value", "");

                if (string.IsNullOrEmpty(nonceValue))
                    return new LoginResponseDto { IsSuccess = false, Message = "Error: Sin Nonce." };

                string encodedUser = Uri.EscapeDataString(username);
                string encodedPass = Uri.EscapeDataString(password);
                string encodedNonce = Uri.EscapeDataString(nonceValue);

                string payload = $"user_id={encodedUser}&password={encodedPass}&action=login&new_loc=&blackboard.platform.security.NonceUtil.nonce.ajax={encodedNonce}";
                var content = new StringContent(payload, Encoding.UTF8, "application/x-www-form-urlencoded");

                var requestPost = new HttpRequestMessage(HttpMethod.Post, LOGIN_PATH)
                {
                    Content = content
                };

                requestPost.Headers.Add("Cookie", manualCookieHeader);
                requestPost.Headers.Referrer = new Uri(BASE_URL + LOGIN_PATH);
                requestPost.Headers.Add("Origin", BASE_URL);

                var responsePost = await client.SendAsync(requestPost);

                if (responsePost.StatusCode == HttpStatusCode.Found || responsePost.StatusCode == HttpStatusCode.Redirect || responsePost.StatusCode == HttpStatusCode.SeeOther)
                {
                    string finalCookies = manualCookieHeader;
                    if (responsePost.Headers.TryGetValues("Set-Cookie", out var newCookieHeaders))
                    {
                        var newCookies = new List<string>();
                        foreach (var h in newCookieHeaders) newCookies.Add(h.Split(';')[0]);
                        finalCookies = finalCookies + "; " + string.Join("; ", newCookies);
                    }

                    return new LoginResponseDto
                    {
                        IsSuccess = true,
                        Message = "Login Exitoso",
                        SessionCookie = finalCookies // Save the session cookies at the frontend
                    };
                }
                else
                {
                    var errorHtml = await responsePost.Content.ReadAsStringAsync();
                    string cleanHtml = Regex.Replace(errorHtml, "<noscript>.*?</noscript>", "", RegexOptions.Singleline);
                    return new LoginResponseDto { IsSuccess = false, Message = "Fallo en login. Revise contraseña." };
                }
            }
        }

        /// <summary>
        /// Retrieves user data from Blackboard using a session cookie.
        /// </summary>
        /// <param name="sessionCookie">The session cookie from a successful authentication.</param>
        /// <returns>A user response containing user information if successful.</returns>
        public async Task<UserResponseDto> GetUserDataAsync(string sessionCookie)
        {
            if (string.IsNullOrEmpty(sessionCookie))
                return new UserResponseDto { IsSuccess = false, Message = "Session cookie faltante." };

            var handler = new HttpClientHandler
            {
                UseCookies = false,
                AllowAutoRedirect = false,
                SslProtocols = System.Security.Authentication.SslProtocols.Tls12
            };

            using (var client = new HttpClient(handler))
            {
                client.BaseAddress = new Uri(BASE_URL);
                client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
                client.DefaultRequestHeaders.Add("Accept", "*/*");
                client.DefaultRequestHeaders.ExpectContinue = false;

                var requestApi = new HttpRequestMessage(HttpMethod.Get, API_ME_URL);
                requestApi.Headers.Add("Cookie", sessionCookie);

                var responseApi = await client.SendAsync(requestApi);

                if (responseApi.IsSuccessStatusCode)
                {
                    var jsonApi = await responseApi.Content.ReadAsStringAsync();

                    var root = JsonNode.Parse(jsonApi);

                    var flatUser = new UserDetailDto
                    {
                        Given = root?["name"]?["given"]?.ToString() ?? string.Empty,
                        Family = root?["name"]?["family"]?.ToString() ?? string.Empty,
                        Email = root?["contact"]?["email"]?.ToString() ?? string.Empty,
                        Avatar = root?["avatar"]?["viewUrl"]?.ToString() ?? string.Empty
                    };

                    return new UserResponseDto
                    {
                        IsSuccess = true,
                        Message = "OK",
                        UserData = flatUser
                    };
                }
                else
                {
                    return new UserResponseDto { IsSuccess = false, Message = $"API returned {(int)responseApi.StatusCode}" };
                }
            }
        }

        /// <summary>
        /// Retrieves a proxied image response from Blackboard.
        /// </summary>
        /// <param name="sessionCookie">The session cookie for authentication.</param>
        /// <param name="imageUrl">The URL of the image to proxy.</param>
        /// <param name="acceptHeader">The optional Accept header for the image request.</param>
        /// <returns>The proxied HTTP response message or null if unsuccessful.</returns>
        public async Task<HttpResponseMessage?> GetProxiedImageResponseAsync(string sessionCookie, string imageUrl, string? acceptHeader = null)
        {
            if (string.IsNullOrEmpty(imageUrl)) return null;
            if (!Uri.TryCreate(imageUrl, UriKind.Absolute, out var uri)) return null;

            // Allow only the expected host to prevent SSRF
            if (!uri.Host.Contains("aulavirtual.ual.es")) return null;

            // Create a fresh HttpClient with explicit cookie handling (UseCookies = false)
            // to avoid accumulated state from connection pooling that causes 401 on second call
            var handler = new HttpClientHandler
            {
                UseCookies = false,
                AllowAutoRedirect = true, // Follow redirects (302, 301, etc.) for avatar URLs
                SslProtocols = System.Security.Authentication.SslProtocols.Tls12
            };

            using (var client = new HttpClient(handler))
            {
                client.Timeout = TimeSpan.FromSeconds(30); // Timeout for image downloads
                client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
                client.DefaultRequestHeaders.ExpectContinue = false;

                var request = new HttpRequestMessage(HttpMethod.Get, uri);

                var accept = string.IsNullOrWhiteSpace(acceptHeader)
                    ? "image/avif,image/webp,image/*,*/*;q=0.8"
                    : acceptHeader;
                request.Headers.TryAddWithoutValidation("Accept", accept);

                if (!string.IsNullOrEmpty(sessionCookie))
                {
                    var cookieHeader = sessionCookie.Contains("=") ? sessionCookie : $"bb_session={sessionCookie}";
                    request.Headers.Add("Cookie", cookieHeader);
                }

                var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
                return response;
            }
        }

        /// <summary>
        /// Retrieves and maps calendar items from Blackboard within a 16-week window starting at the first day of the month for the provided date.
        /// </summary>
        /// <param name="currentDate">Reference date to compute the window.</param>
        /// <param name="sessionCookie">Blackboard session cookie.</param>
        /// <returns>Mapped calendar items.</returns>
        public async Task<IEnumerable<CalendarItemDto>> GetCalendarItemsAsync(DateTime currentDate, string sessionCookie)
        {
            if (string.IsNullOrWhiteSpace(sessionCookie))
            {
                throw new ArgumentException("Session cookie is required", nameof(sessionCookie));
            }

            var startDate = new DateTime(currentDate.Year, currentDate.Month, 1, 0, 0, 0, DateTimeKind.Utc);
            var endDate = startDate.AddDays(7 * 16 - 1);

            var since = FormatUtcDate(startDate);
            var until = FormatUtcDate(endDate);

            var handler = new HttpClientHandler
            {
                UseCookies = false,
                AllowAutoRedirect = false,
                SslProtocols = System.Security.Authentication.SslProtocols.Tls12
            };

            using (var client = new HttpClient(handler))
            {
                client.BaseAddress = new Uri(BASE_URL);
                client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
                client.DefaultRequestHeaders.Add("Accept", "*/*");
                client.DefaultRequestHeaders.ExpectContinue = false;

                var request = new HttpRequestMessage(HttpMethod.Get, $"{API_CALENDAR_ITEMS_URL}?since={since}&until={until}&sort=start");
                request.Headers.Add("Cookie", NormalizeCookie(sessionCookie));

                var response = await client.SendAsync(request);

                if (!response.IsSuccessStatusCode)
                {
                    throw new HttpRequestException($"Blackboard calendar API returned {(int)response.StatusCode}", null, response.StatusCode);
                }

                var json = await response.Content.ReadAsStringAsync();
                var root = JsonNode.Parse(json);
                var results = root?["results"] as JsonArray;
                if (results is null)
                {
                    return Enumerable.Empty<CalendarItemDto>();
                }

                var mapped = new List<CalendarItemDto>();
                foreach (var item in results)
                {
                    if (item is not JsonObject obj)
                    {
                        continue;
                    }

                    var type = obj["type"]?.ToString() ?? string.Empty;
                    var startString = obj["start"]?.ToString();
                    var endString = obj["end"]?.ToString();

                    if (!TryParseDate(startString, out var start) || !TryParseDate(endString, out var end))
                    {
                        continue;
                    }

                    var subject = string.Empty;
                    if (!IsCategoryWithoutSubject(type))
                    {
                        var calendarName = obj["calendarName"]?.ToString() ?? string.Empty;
                        subject = ExtractSubject(calendarName);
                    }

                    mapped.Add(new CalendarItemDto
                    {
                        CalendarId = obj["id"]?.ToString() ?? string.Empty,
                        Title = obj["title"]?.ToString() ?? string.Empty,
                        Start = start,
                        End = end,
                        Location = obj["location"]?.ToString() ?? string.Empty,
                        Category = ParseCalendarCategory(type),
                        Subject = subject,
                        Color = obj["color"]?.ToString() ?? string.Empty,
                        Description = obj["description"]?.ToString()
                    });
                }

                return mapped;
            }
        }

        private static string NormalizeCookie(string sessionCookie)
        {
            return sessionCookie.Contains("=") ? sessionCookie : $"bb_session={sessionCookie}";
        }

        private static string FormatUtcDate(DateTime date)
        {
            return date.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ss.fffZ", CultureInfo.InvariantCulture);
        }

        private static bool TryParseDate(string? raw, out DateTime value)
        {
            return DateTime.TryParse(raw, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out value);
        }

        private static bool IsCategoryWithoutSubject(string category)
        {
            return category.Equals("Institution", StringComparison.OrdinalIgnoreCase)
                   || category.Equals("Personal", StringComparison.OrdinalIgnoreCase);
        }

        private static string ExtractSubject(string calendarName)
        {
            var match = Regex.Match(calendarName, " - (?<subject>[^-]+?) - ", RegexOptions.Compiled);
            return match.Success ? match.Groups["subject"].Value.Trim() : string.Empty;
        }

        private static CalendarCategory ParseCalendarCategory(string categoryString)
        {
            if (string.IsNullOrWhiteSpace(categoryString))
            {
                return CalendarCategory.Course; // Default fallback
            }

            // Try case-insensitive parsing
            if (Enum.TryParse<CalendarCategory>(categoryString, ignoreCase: true, out var result))
            {
                return result;
            }

            // Log warning for unrecognized category and default to Course
            System.Diagnostics.Debug.WriteLine($"Warning: Unrecognized calendar category '{categoryString}', defaulting to Course");
            return CalendarCategory.Course;
        }
    }
}