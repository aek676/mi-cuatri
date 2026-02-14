using System.Net;
using System.Text;
using System.Text.Json;
using backend.Dtos;
using backend.Enums;
using backend.Services;
using Moq.Protected;

namespace backend.Tests.Unit.Services;

/// <summary>
/// Unit tests for BlackboardService.
/// Tests authentication, user data retrieval, image proxying, and calendar items.
/// </summary>
public class BlackboardServiceTests
{
    private BlackboardService CreateServiceWithMockHandler(
        Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> sendAsyncFunc
    )
    {
        var mockHandler = new Mock<HttpMessageHandler>();
        mockHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .Returns(sendAsyncFunc);

        return new BlackboardService(handler => new HttpClient(mockHandler.Object));
    }

    #region AuthenticateAsync Tests

    [Fact]
    public async Task AuthenticateAsync_WithValidCredentials_ReturnsSuccessWithSessionCookie()
    {
        var requestCount = 0;
        var service = CreateServiceWithMockHandler(
            (request, cancellationToken) =>
            {
                requestCount++;
                if (requestCount == 1)
                {
                    var response = new HttpResponseMessage(HttpStatusCode.OK);
                    response.Headers.Add("Set-Cookie", "session1=value1; Path=/");
                    response.Content = new StringContent(
                        @"
                    <html>
                        <body>
                            <input name='blackboard.platform.security.NonceUtil.nonce.ajax' value='test-nonce-123' />
                        </body>
                    </html>",
                        Encoding.UTF8,
                        "text/html"
                    );
                    return Task.FromResult(response);
                }
                else
                {
                    var response = new HttpResponseMessage(HttpStatusCode.Found);
                    response.Headers.Add("Set-Cookie", "session2=value2; Path=/");
                    return Task.FromResult(response);
                }
            }
        );

        var result = await service.AuthenticateAsync("testuser", "testpass");

        result.IsSuccess.Should().BeTrue();
        result.SessionCookie.Should().Be("session1=value1; session2=value2");
        result.Message.Should().Be("Login Exitoso");
    }

    [Fact]
    public async Task AuthenticateAsync_WithoutInitialSetCookieHeader_ReturnsFailure()
    {
        var service = CreateServiceWithMockHandler(
            (request, cancellationToken) =>
            {
                var response = new HttpResponseMessage(HttpStatusCode.OK);
                response.Content = new StringContent(
                    @"
                <html>
                    <body>
                        <input name='blackboard.platform.security.NonceUtil.nonce.ajax' value='test-nonce-123' />
                    </body>
                </html>",
                    Encoding.UTF8,
                    "text/html"
                );
                return Task.FromResult(response);
            }
        );

        var result = await service.AuthenticateAsync("testuser", "testpass");

        result.IsSuccess.Should().BeFalse();
        result.Message.Should().Be("Error: Sin cookies iniciales.");
    }

    [Fact]
    public async Task AuthenticateAsync_WithoutNonceInHtml_ReturnsFailure()
    {
        var service = CreateServiceWithMockHandler(
            (request, cancellationToken) =>
            {
                var response = new HttpResponseMessage(HttpStatusCode.OK);
                response.Headers.Add("Set-Cookie", "session1=value1; Path=/");
                response.Content = new StringContent(
                    "<html><body>No nonce here</body></html>",
                    Encoding.UTF8,
                    "text/html"
                );
                return Task.FromResult(response);
            }
        );

        var result = await service.AuthenticateAsync("testuser", "testpass");

        result.IsSuccess.Should().BeFalse();
        result.Message.Should().Be("Error: Sin Nonce.");
    }

    [Fact]
    public async Task AuthenticateAsync_WithInvalidCredentials_ReturnsFailure()
    {
        var requestCount = 0;
        var service = CreateServiceWithMockHandler(
            (request, cancellationToken) =>
            {
                requestCount++;
                if (requestCount == 1)
                {
                    var response = new HttpResponseMessage(HttpStatusCode.OK);
                    response.Headers.Add("Set-Cookie", "session1=value1; Path=/");
                    response.Content = new StringContent(
                        @"
                    <html>
                        <body>
                            <input name='blackboard.platform.security.NonceUtil.nonce.ajax' value='test-nonce-123' />
                        </body>
                    </html>",
                        Encoding.UTF8,
                        "text/html"
                    );
                    return Task.FromResult(response);
                }
                else
                {
                    var response = new HttpResponseMessage(HttpStatusCode.OK);
                    return Task.FromResult(response);
                }
            }
        );

        var result = await service.AuthenticateAsync("testuser", "wrongpassword");

        result.IsSuccess.Should().BeFalse();
        result.Message.Should().Be("Fallo en login. Revise contraseña.");
    }

    [Theory]
    [InlineData(HttpStatusCode.Redirect)]
    [InlineData(HttpStatusCode.SeeOther)]
    public async Task AuthenticateAsync_WithRedirectStatus_ReturnsSuccess(HttpStatusCode statusCode)
    {
        var requestCount = 0;
        var service = CreateServiceWithMockHandler(
            (request, cancellationToken) =>
            {
                requestCount++;
                if (requestCount == 1)
                {
                    var response = new HttpResponseMessage(HttpStatusCode.OK);
                    response.Headers.Add("Set-Cookie", "session1=value1; Path=/");
                    response.Content = new StringContent(
                        @"
                    <html>
                        <body>
                            <input name='blackboard.platform.security.NonceUtil.nonce.ajax' value='test-nonce-123' />
                        </body>
                    </html>",
                        Encoding.UTF8,
                        "text/html"
                    );
                    return Task.FromResult(response);
                }
                else
                {
                    var response = new HttpResponseMessage(statusCode);
                    return Task.FromResult(response);
                }
            }
        );

        var result = await service.AuthenticateAsync("testuser", "testpass");

        result.IsSuccess.Should().BeTrue();
    }

    #endregion

    #region GetUserDataAsync Tests

    [Fact]
    public async Task GetUserDataAsync_WithValidSession_ReturnsUserData()
    {
        var service = CreateServiceWithMockHandler(
            (request, cancellationToken) =>
            {
                var jsonResponse = JsonSerializer.Serialize(
                    new
                    {
                        name = new { given = "John", family = "Doe" },
                        contact = new { email = "john.doe@ual.es" },
                        avatar = new { viewUrl = "https://example.com/avatar.jpg" },
                    }
                );
                var response = new HttpResponseMessage(HttpStatusCode.OK);
                response.Content = new StringContent(
                    jsonResponse,
                    Encoding.UTF8,
                    "application/json"
                );
                return Task.FromResult(response);
            }
        );

        var result = await service.GetUserDataAsync("valid-session-cookie");

        result.IsSuccess.Should().BeTrue();
        result.UserData.Should().NotBeNull();
        result.UserData!.Given.Should().Be("John");
        result.UserData.Family.Should().Be("Doe");
        result.UserData.Email.Should().Be("john.doe@ual.es");
        result.UserData.Avatar.Should().Be("https://example.com/avatar.jpg");
    }

    [Fact]
    public async Task GetUserDataAsync_WithNullSession_ReturnsFailure()
    {
        var service = new BlackboardService();

        var result = await service.GetUserDataAsync(null!);

        result.IsSuccess.Should().BeFalse();
        result.Message.Should().Be("Session cookie faltante.");
    }

    [Fact]
    public async Task GetUserDataAsync_WithEmptySession_ReturnsFailure()
    {
        var service = new BlackboardService();

        var result = await service.GetUserDataAsync(string.Empty);

        result.IsSuccess.Should().BeFalse();
        result.Message.Should().Be("Session cookie faltante.");
    }

    [Fact]
    public async Task GetUserDataAsync_WithUnauthorizedResponse_ReturnsFailure()
    {
        var service = CreateServiceWithMockHandler(
            (request, cancellationToken) =>
            {
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.Unauthorized));
            }
        );

        var result = await service.GetUserDataAsync("invalid-session");

        result.IsSuccess.Should().BeFalse();
        result.Message.Should().Be("API returned 401");
    }

    [Fact]
    public async Task GetUserDataAsync_WithMissingEmail_ReturnsEmptyEmail()
    {
        var service = CreateServiceWithMockHandler(
            (request, cancellationToken) =>
            {
                var jsonResponse = JsonSerializer.Serialize(
                    new
                    {
                        name = new { given = "John", family = "Doe" },
                        contact = new { email = (string?)null },
                        avatar = new { viewUrl = "https://example.com/avatar.jpg" },
                    }
                );
                var response = new HttpResponseMessage(HttpStatusCode.OK);
                response.Content = new StringContent(
                    jsonResponse,
                    Encoding.UTF8,
                    "application/json"
                );
                return Task.FromResult(response);
            }
        );

        var result = await service.GetUserDataAsync("valid-session");

        result.IsSuccess.Should().BeTrue();
        result.UserData!.Email.Should().BeEmpty();
    }

    #endregion

    #region GetProxiedImageResponseAsync Tests

    [Fact]
    public async Task GetProxiedImageResponseAsync_WithValidImageUrl_ReturnsResponse()
    {
        var service = CreateServiceWithMockHandler(
            (request, cancellationToken) =>
            {
                var response = new HttpResponseMessage(HttpStatusCode.OK);
                response.Content = new ByteArrayContent([0x89, 0x50, 0x4E, 0x47]);
                return Task.FromResult(response);
            }
        );

        var result = await service.GetProxiedImageResponseAsync(
            "session-cookie",
            "https://aulavirtual.ual.es/image.png",
            "image/png"
        );

        result.Should().NotBeNull();
        result!.IsSuccessStatusCode.Should().BeTrue();
    }

    [Fact]
    public async Task GetProxiedImageResponseAsync_WithNullUrl_ReturnsNull()
    {
        var service = new BlackboardService();

        var result = await service.GetProxiedImageResponseAsync("session", null!);

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetProxiedImageResponseAsync_WithEmptyUrl_ReturnsNull()
    {
        var service = new BlackboardService();

        var result = await service.GetProxiedImageResponseAsync("session", string.Empty);

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetProxiedImageResponseAsync_WithInvalidUrlFormat_ReturnsNull()
    {
        var service = new BlackboardService();

        var result = await service.GetProxiedImageResponseAsync("session", "not-a-valid-url");

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetProxiedImageResponseAsync_WithWrongHost_ReturnsNull()
    {
        var service = new BlackboardService();

        var result = await service.GetProxiedImageResponseAsync(
            "session",
            "https://evil.com/image.png"
        );

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetProxiedImageResponseAsync_WithNonAulavirtualHost_ReturnsNull()
    {
        var service = new BlackboardService();

        var result = await service.GetProxiedImageResponseAsync(
            "session",
            "https://internal-server.local/image.png"
        );

        result.Should().BeNull();
    }

    #endregion

    #region GetCalendarItemsAsync Tests

    [Fact]
    public async Task GetCalendarItemsAsync_WithValidSession_ReturnsMappedItems()
    {
        var currentDate = new DateTime(2024, 1, 15);
        var service = CreateServiceWithMockHandler(
            (request, cancellationToken) =>
            {
                var jsonResponse = JsonSerializer.Serialize(
                    new
                    {
                        results = new[]
                        {
                            new
                            {
                                id = "event-1",
                                title = "Test Event",
                                start = "2024-01-15T10:00:00Z",
                                end = "2024-01-15T11:00:00Z",
                                location = "Room 101",
                                type = "Course",
                                calendarName = "2024 - Mathematics - Group A",
                                color = "#FF5733",
                                description = "Test description",
                            },
                        },
                    }
                );
                var response = new HttpResponseMessage(HttpStatusCode.OK);
                response.Content = new StringContent(
                    jsonResponse,
                    Encoding.UTF8,
                    "application/json"
                );
                return Task.FromResult(response);
            }
        );

        var result = await service.GetCalendarItemsAsync(currentDate, "valid-session");

        result.Should().HaveCount(1);
        var item = result.First();
        item.CalendarId.Should().Be("event-1");
        item.Title.Should().Be("Test Event");
        item.Location.Should().Be("Room 101");
        item.Category.Should().Be(CalendarCategory.Course);
        item.Subject.Should().Be("Mathematics");
        item.Color.Should().Be("#FF5733");
    }

    [Fact]
    public async Task GetCalendarItemsAsync_WithNullSession_ThrowsArgumentException()
    {
        var service = new BlackboardService();

        await Assert.ThrowsAsync<ArgumentException>(() =>
            service.GetCalendarItemsAsync(DateTime.Now, null!)
        );
    }

    [Fact]
    public async Task GetCalendarItemsAsync_WithEmptySession_ThrowsArgumentException()
    {
        var service = new BlackboardService();

        await Assert.ThrowsAsync<ArgumentException>(() =>
            service.GetCalendarItemsAsync(DateTime.Now, string.Empty)
        );
    }

    [Fact]
    public async Task GetCalendarItemsAsync_WithWhitespaceSession_ThrowsArgumentException()
    {
        var service = new BlackboardService();

        await Assert.ThrowsAsync<ArgumentException>(() =>
            service.GetCalendarItemsAsync(DateTime.Now, "   ")
        );
    }

    [Fact]
    public async Task GetCalendarItemsAsync_WithUnauthorizedResponse_ThrowsHttpRequestException()
    {
        var service = CreateServiceWithMockHandler(
            (request, cancellationToken) =>
            {
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.Unauthorized));
            }
        );

        var exception = await Assert.ThrowsAsync<HttpRequestException>(() =>
            service.GetCalendarItemsAsync(DateTime.Now, "invalid-session")
        );
        exception.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetCalendarItemsAsync_WithEmptyResults_ReturnsEmptyList()
    {
        var service = CreateServiceWithMockHandler(
            (request, cancellationToken) =>
            {
                var jsonResponse = JsonSerializer.Serialize(
                    new { results = Array.Empty<object>() }
                );
                var response = new HttpResponseMessage(HttpStatusCode.OK);
                response.Content = new StringContent(
                    jsonResponse,
                    Encoding.UTF8,
                    "application/json"
                );
                return Task.FromResult(response);
            }
        );

        var result = await service.GetCalendarItemsAsync(DateTime.Now, "valid-session");

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetCalendarItemsAsync_WithNullResults_ReturnsEmptyList()
    {
        var service = CreateServiceWithMockHandler(
            (request, cancellationToken) =>
            {
                var jsonResponse = JsonSerializer.Serialize(new { results = (object?)null });
                var response = new HttpResponseMessage(HttpStatusCode.OK);
                response.Content = new StringContent(
                    jsonResponse,
                    Encoding.UTF8,
                    "application/json"
                );
                return Task.FromResult(response);
            }
        );

        var result = await service.GetCalendarItemsAsync(DateTime.Now, "valid-session");

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetCalendarItemsAsync_WithInvalidDates_SkipsItems()
    {
        var service = CreateServiceWithMockHandler(
            (request, cancellationToken) =>
            {
                var jsonResponse = JsonSerializer.Serialize(
                    new
                    {
                        results = new[]
                        {
                            new
                            {
                                id = "event-1",
                                title = "Valid Event",
                                start = "2024-01-15T10:00:00Z",
                                end = "2024-01-15T11:00:00Z",
                                type = "Course",
                            },
                            new
                            {
                                id = "event-2",
                                title = "Invalid Event",
                                start = "invalid-date",
                                end = "2024-01-15T11:00:00Z",
                                type = "Course",
                            },
                        },
                    }
                );
                var response = new HttpResponseMessage(HttpStatusCode.OK);
                response.Content = new StringContent(
                    jsonResponse,
                    Encoding.UTF8,
                    "application/json"
                );
                return Task.FromResult(response);
            }
        );

        var result = await service.GetCalendarItemsAsync(DateTime.Now, "valid-session");

        result.Should().HaveCount(1);
        result.First().Title.Should().Be("Valid Event");
    }

    [Theory]
    [InlineData("Course", CalendarCategory.Course)]
    [InlineData("course", CalendarCategory.Course)]
    [InlineData("COURSE", CalendarCategory.Course)]
    [InlineData("GradebookColumn", CalendarCategory.GradebookColumn)]
    [InlineData("Institution", CalendarCategory.Institution)]
    [InlineData("OfficeHours", CalendarCategory.OfficeHours)]
    [InlineData("Personal", CalendarCategory.Personal)]
    [InlineData("UnknownType", CalendarCategory.Course)]
    public async Task GetCalendarItemsAsync_ParsesCategoryCorrectly(
        string typeInput,
        CalendarCategory expectedCategory
    )
    {
        var service = CreateServiceWithMockHandler(
            (request, cancellationToken) =>
            {
                var jsonResponse = JsonSerializer.Serialize(
                    new
                    {
                        results = new[]
                        {
                            new
                            {
                                id = "event-1",
                                title = "Test",
                                start = "2024-01-15T10:00:00Z",
                                end = "2024-01-15T11:00:00Z",
                                type = typeInput,
                                calendarName = "2024 - Test - Group",
                            },
                        },
                    }
                );
                var response = new HttpResponseMessage(HttpStatusCode.OK);
                response.Content = new StringContent(
                    jsonResponse,
                    Encoding.UTF8,
                    "application/json"
                );
                return Task.FromResult(response);
            }
        );

        var result = await service.GetCalendarItemsAsync(DateTime.Now, "valid-session");

        result.First().Category.Should().Be(expectedCategory);
    }

    [Fact]
    public async Task GetCalendarItemsAsync_InstitutionCategory_HasEmptySubject()
    {
        var service = CreateServiceWithMockHandler(
            (request, cancellationToken) =>
            {
                var jsonResponse = JsonSerializer.Serialize(
                    new
                    {
                        results = new[]
                        {
                            new
                            {
                                id = "event-1",
                                title = "Institution Event",
                                start = "2024-01-15T10:00:00Z",
                                end = "2024-01-15T11:00:00Z",
                                type = "Institution",
                                calendarName = "Some Institution Calendar",
                            },
                        },
                    }
                );
                var response = new HttpResponseMessage(HttpStatusCode.OK);
                response.Content = new StringContent(
                    jsonResponse,
                    Encoding.UTF8,
                    "application/json"
                );
                return Task.FromResult(response);
            }
        );

        var result = await service.GetCalendarItemsAsync(DateTime.Now, "valid-session");

        result.First().Subject.Should().BeEmpty();
    }

    [Fact]
    public async Task GetCalendarItemsAsync_PersonalCategory_HasEmptySubject()
    {
        var service = CreateServiceWithMockHandler(
            (request, cancellationToken) =>
            {
                var jsonResponse = JsonSerializer.Serialize(
                    new
                    {
                        results = new[]
                        {
                            new
                            {
                                id = "event-1",
                                title = "Personal Event",
                                start = "2024-01-15T10:00:00Z",
                                end = "2024-01-15T11:00:00Z",
                                type = "Personal",
                                calendarName = "My Personal Calendar",
                            },
                        },
                    }
                );
                var response = new HttpResponseMessage(HttpStatusCode.OK);
                response.Content = new StringContent(
                    jsonResponse,
                    Encoding.UTF8,
                    "application/json"
                );
                return Task.FromResult(response);
            }
        );

        var result = await service.GetCalendarItemsAsync(DateTime.Now, "valid-session");

        result.First().Subject.Should().BeEmpty();
    }

    [Fact]
    public async Task GetCalendarItemsAsync_CalculatesDateRangeCorrectly()
    {
        var currentDate = new DateTime(2024, 3, 15); // March 15, 2024
        DateTime? capturedSince = null;
        DateTime? capturedUntil = null;

        var service = CreateServiceWithMockHandler(
            (request, cancellationToken) =>
            {
                var uri = request.RequestUri!.ToString();
                var query = System.Web.HttpUtility.ParseQueryString(new Uri(uri).Query);
                capturedSince = DateTime.Parse(
                    query["since"]!,
                    null,
                    System.Globalization.DateTimeStyles.RoundtripKind
                );
                capturedUntil = DateTime.Parse(
                    query["until"]!,
                    null,
                    System.Globalization.DateTimeStyles.RoundtripKind
                );

                var jsonResponse = JsonSerializer.Serialize(
                    new { results = Array.Empty<object>() }
                );
                var response = new HttpResponseMessage(HttpStatusCode.OK);
                response.Content = new StringContent(
                    jsonResponse,
                    Encoding.UTF8,
                    "application/json"
                );
                return Task.FromResult(response);
            }
        );

        await service.GetCalendarItemsAsync(currentDate, "valid-session");

        var expectedStart = new DateTime(2024, 3, 1, 0, 0, 0, DateTimeKind.Utc);
        var expectedEnd = new DateTime(2024, 6, 20, 0, 0, 0, DateTimeKind.Utc);

        capturedSince.Should().Be(expectedStart);
        capturedUntil.Should().Be(expectedEnd);
    }

    #endregion
}
