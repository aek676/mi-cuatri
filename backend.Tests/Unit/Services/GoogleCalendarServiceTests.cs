using System.Net;
using System.Text;
using System.Text.Json;
using backend.Dtos;
using backend.Enums;
using backend.Models;
using backend.Repositories;
using backend.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq.Protected;

namespace backend.Tests.Unit.Services;

/// <summary>
/// Unit tests for GoogleCalendarService.
/// Tests event export, token validation, and token refresh operations.
/// </summary>
public class GoogleCalendarServiceTests
{
    private GoogleCalendarService CreateServiceWithMockHttpClient(
        Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> sendAsyncFunc,
        Mock<IUserRepository>? userRepositoryMock = null,
        Dictionary<string, string?>? configValues = null
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

        var httpClientFactory = () => new HttpClient(mockHandler.Object);

        var mockUserRepo = userRepositoryMock ?? new Mock<IUserRepository>();
        var mockLogger = new Mock<ILogger<GoogleCalendarService>>();

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(
                configValues
                    ?? new Dictionary<string, string?>
                    {
                        ["Google:ClientId"] = "test-client-id",
                        ["Google:ClientSecret"] = "test-client-secret",
                    }
            )
            .Build();

        return new GoogleCalendarService(
            mockUserRepo.Object,
            configuration,
            mockLogger.Object,
            httpClientFactory
        );
    }

    private User CreateTestUser(
        string username,
        bool hasGoogleAccount = true,
        bool hasValidToken = true
    )
    {
        var user = new User
        {
            Id = Guid.NewGuid().ToString(),
            Username = username,
            Email = $"{username}@test.com",
            Events = new List<Event>(),
        };

        if (hasGoogleAccount)
        {
            user.GoogleAccount = new GoogleAccount
            {
                GoogleId = "google-id-123",
                Email = $"{username}@gmail.com",
                RefreshToken = "refresh-token-xyz",
                AccessToken = hasValidToken ? "valid-access-token" : "expired-access-token",
                AccessTokenExpiry = hasValidToken
                    ? DateTime.UtcNow.AddHours(1)
                    : DateTime.UtcNow.AddHours(-1),
                Scopes = new[] { "https://www.googleapis.com/auth/calendar.events" },
            };
        }

        return user;
    }

    private CalendarItemDto CreateCalendarItem(
        string id = "event-1",
        string title = "Test Event",
        string? subject = null,
        string color = "#FF5733",
        CalendarCategory category = CalendarCategory.Course,
        DateTime? start = null,
        DateTime? end = null
    )
    {
        return new CalendarItemDto
        {
            CalendarId = id,
            Title = title,
            Subject = subject,
            Start = start ?? DateTime.UtcNow.AddHours(1),
            End = end ?? DateTime.UtcNow.AddHours(2),
            Location = "Test Location",
            Color = color,
            Category = category,
            Description = "Test Description",
        };
    }

    #region ExportEventsAsync Tests

    [Fact]
    public async Task ExportEventsAsync_CreatesNewEvent_WhenEventDoesNotExist()
    {
        var user = CreateTestUser("testuser");
        var mockUserRepo = new Mock<IUserRepository>();
        mockUserRepo.Setup(r => r.GetByUsernameAsync("testuser")).ReturnsAsync(user);

        var requestCount = 0;
        var service = CreateServiceWithMockHttpClient(
            (request, cancellationToken) =>
            {
                requestCount++;
                var url = request.RequestUri?.ToString() ?? "";

                if (url.Contains("/events?privateExtendedProperty="))
                {
                    return Task.FromResult(
                        new HttpResponseMessage(HttpStatusCode.OK)
                        {
                            Content = new StringContent(
                                "{\"items\":[]}",
                                Encoding.UTF8,
                                "application/json"
                            ),
                        }
                    );
                }
                else if (request.Method == HttpMethod.Post)
                {
                    return Task.FromResult(
                        new HttpResponseMessage(HttpStatusCode.OK)
                        {
                            Content = new StringContent(
                                "{\"id\":\"google-event-id\"}",
                                Encoding.UTF8,
                                "application/json"
                            ),
                        }
                    );
                }

                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
            },
            mockUserRepo
        );

        var items = new[] { CreateCalendarItem() };

        var result = await service.ExportEventsAsync("testuser", items);

        result.Created.Should().Be(1);
        result.Updated.Should().Be(0);
        result.Failed.Should().Be(0);
    }

    [Fact]
    public async Task ExportEventsAsync_UpdatesEvent_WhenEventExists()
    {
        var user = CreateTestUser("testuser");
        var mockUserRepo = new Mock<IUserRepository>();
        mockUserRepo.Setup(r => r.GetByUsernameAsync("testuser")).ReturnsAsync(user);

        var service = CreateServiceWithMockHttpClient(
            (request, cancellationToken) =>
            {
                var url = request.RequestUri?.ToString() ?? "";

                if (url.Contains("/events?privateExtendedProperty="))
                {
                    return Task.FromResult(
                        new HttpResponseMessage(HttpStatusCode.OK)
                        {
                            Content = new StringContent(
                                "{\"items\":[{\"id\":\"existing-google-id\"}]}",
                                Encoding.UTF8,
                                "application/json"
                            ),
                        }
                    );
                }
                else if (request.Method == new HttpMethod("PATCH"))
                {
                    return Task.FromResult(
                        new HttpResponseMessage(HttpStatusCode.OK)
                        {
                            Content = new StringContent(
                                "{\"id\":\"existing-google-id\"}",
                                Encoding.UTF8,
                                "application/json"
                            ),
                        }
                    );
                }

                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
            },
            mockUserRepo
        );

        var items = new[] { CreateCalendarItem() };

        var result = await service.ExportEventsAsync("testuser", items);

        result.Created.Should().Be(0);
        result.Updated.Should().Be(1);
        result.Failed.Should().Be(0);
    }

    [Fact]
    public async Task ExportEventsAsync_HandlesMultipleEvents_MixedResults()
    {
        var user = CreateTestUser("testuser");
        var mockUserRepo = new Mock<IUserRepository>();
        mockUserRepo.Setup(r => r.GetByUsernameAsync("testuser")).ReturnsAsync(user);

        var eventCounter = 0;
        var service = CreateServiceWithMockHttpClient(
            (request, cancellationToken) =>
            {
                var url = request.RequestUri?.ToString() ?? "";

                if (url.Contains("/events?privateExtendedProperty="))
                {
                    eventCounter++;
                    var hasEvent = eventCounter == 1;
                    var items = hasEvent ? "[{\"id\":\"existing-id\"}]" : "[]";
                    return Task.FromResult(
                        new HttpResponseMessage(HttpStatusCode.OK)
                        {
                            Content = new StringContent(
                                $"{{\"items\":{items}}}",
                                Encoding.UTF8,
                                "application/json"
                            ),
                        }
                    );
                }
                else if (request.Method == new HttpMethod("PATCH"))
                {
                    return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
                }
                else if (request.Method == HttpMethod.Post)
                {
                    if (eventCounter == 3)
                    {
                        return Task.FromResult(new HttpResponseMessage(HttpStatusCode.BadRequest));
                    }
                    return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
                }

                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
            },
            mockUserRepo
        );

        var items = new[]
        {
            CreateCalendarItem("event-1"),
            CreateCalendarItem("event-2"),
            CreateCalendarItem("event-3"),
        };

        var result = await service.ExportEventsAsync("testuser", items);

        result.Created.Should().Be(1);
        result.Updated.Should().Be(1);
        result.Failed.Should().Be(1);
        result.Errors.Should().HaveCount(1);
    }

    [Fact]
    public async Task ExportEventsAsync_Throws_WhenUserNotFound()
    {
        var mockUserRepo = new Mock<IUserRepository>();
        mockUserRepo.Setup(r => r.GetByUsernameAsync("testuser")).ReturnsAsync((User?)null);

        var service = CreateServiceWithMockHttpClient(
            (request, cancellationToken) =>
                Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)),
            mockUserRepo
        );

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.ExportEventsAsync("testuser", new[] { CreateCalendarItem() })
        );
    }

    [Fact]
    public async Task ExportEventsAsync_Throws_WhenUserHasNoGoogleAccount()
    {
        var user = CreateTestUser("testuser", hasGoogleAccount: false);
        var mockUserRepo = new Mock<IUserRepository>();
        mockUserRepo.Setup(r => r.GetByUsernameAsync("testuser")).ReturnsAsync(user);

        var service = CreateServiceWithMockHttpClient(
            (request, cancellationToken) =>
                Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)),
            mockUserRepo
        );

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.ExportEventsAsync("testuser", new[] { CreateCalendarItem() })
        );
        exception.Message.Should().Be("User is not connected to Google Calendar.");
    }

    [Fact]
    public async Task ExportEventsAsync_Continues_WhenOneEventFails()
    {
        var user = CreateTestUser("testuser");
        var mockUserRepo = new Mock<IUserRepository>();
        mockUserRepo.Setup(r => r.GetByUsernameAsync("testuser")).ReturnsAsync(user);

        var callCount = 0;
        var service = CreateServiceWithMockHttpClient(
            (request, cancellationToken) =>
            {
                var url = request.RequestUri?.ToString() ?? "";

                if (url.Contains("/events?privateExtendedProperty="))
                {
                    return Task.FromResult(
                        new HttpResponseMessage(HttpStatusCode.OK)
                        {
                            Content = new StringContent(
                                "{\"items\":[]}",
                                Encoding.UTF8,
                                "application/json"
                            ),
                        }
                    );
                }
                else if (request.Method == HttpMethod.Post)
                {
                    callCount++;
                    if (callCount == 2)
                    {
                        return Task.FromResult(
                            new HttpResponseMessage(HttpStatusCode.InternalServerError)
                        );
                    }
                    return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
                }

                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
            },
            mockUserRepo
        );

        var items = new[]
        {
            CreateCalendarItem("event-1"),
            CreateCalendarItem("event-2"),
            CreateCalendarItem("event-3"),
        };

        var result = await service.ExportEventsAsync("testuser", items);

        result.Created.Should().Be(2);
        result.Failed.Should().Be(1);
        result.Errors.Should().HaveCount(1);
    }

    [Fact]
    public async Task ExportEventsAsync_UsesTitleOnly_WhenSubjectIsNull()
    {
        var user = CreateTestUser("testuser");
        var mockUserRepo = new Mock<IUserRepository>();
        mockUserRepo.Setup(r => r.GetByUsernameAsync("testuser")).ReturnsAsync(user);

        string? capturedSummary = null;
        var service = CreateServiceWithMockHttpClient(
            (request, cancellationToken) =>
            {
                var url = request.RequestUri?.ToString() ?? "";

                if (url.Contains("/events?privateExtendedProperty="))
                {
                    return Task.FromResult(
                        new HttpResponseMessage(HttpStatusCode.OK)
                        {
                            Content = new StringContent(
                                "{\"items\":[]}",
                                Encoding.UTF8,
                                "application/json"
                            ),
                        }
                    );
                }
                else if (request.Method == HttpMethod.Post)
                {
                    request
                        .Content?.ReadAsStringAsync()
                        .ContinueWith(t =>
                        {
                            var json = JsonDocument.Parse(t.Result);
                            capturedSummary = json.RootElement.GetProperty("summary").GetString();
                        })
                        .Wait();

                    return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
                }

                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
            },
            mockUserRepo
        );

        var item = CreateCalendarItem(subject: null);

        await service.ExportEventsAsync("testuser", new[] { item });

        capturedSummary.Should().Be(item.Title);
    }

    [Fact]
    public async Task ExportEventsAsync_UsesSubjectDashTitle_WhenSubjectProvided()
    {
        var user = CreateTestUser("testuser");
        var mockUserRepo = new Mock<IUserRepository>();
        mockUserRepo.Setup(r => r.GetByUsernameAsync("testuser")).ReturnsAsync(user);

        string? capturedSummary = null;
        var service = CreateServiceWithMockHttpClient(
            (request, cancellationToken) =>
            {
                var url = request.RequestUri?.ToString() ?? "";

                if (url.Contains("/events?privateExtendedProperty="))
                {
                    return Task.FromResult(
                        new HttpResponseMessage(HttpStatusCode.OK)
                        {
                            Content = new StringContent(
                                "{\"items\":[]}",
                                Encoding.UTF8,
                                "application/json"
                            ),
                        }
                    );
                }
                else if (request.Method == HttpMethod.Post)
                {
                    request
                        .Content?.ReadAsStringAsync()
                        .ContinueWith(t =>
                        {
                            var json = JsonDocument.Parse(t.Result);
                            capturedSummary = json.RootElement.GetProperty("summary").GetString();
                        })
                        .Wait();

                    return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
                }

                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
            },
            mockUserRepo
        );

        var item = CreateCalendarItem(subject: "Mathematics");

        await service.ExportEventsAsync("testuser", new[] { item });

        capturedSummary.Should().Be("Mathematics - Test Event");
    }

    [Fact]
    public async Task ExportEventsAsync_AssignsColorId_WhenValidHexColor()
    {
        var user = CreateTestUser("testuser");
        var mockUserRepo = new Mock<IUserRepository>();
        mockUserRepo.Setup(r => r.GetByUsernameAsync("testuser")).ReturnsAsync(user);

        string? capturedColorId = null;
        var service = CreateServiceWithMockHttpClient(
            (request, cancellationToken) =>
            {
                var url = request.RequestUri?.ToString() ?? "";

                if (url.Contains("/events?privateExtendedProperty="))
                {
                    return Task.FromResult(
                        new HttpResponseMessage(HttpStatusCode.OK)
                        {
                            Content = new StringContent(
                                "{\"items\":[]}",
                                Encoding.UTF8,
                                "application/json"
                            ),
                        }
                    );
                }
                else if (request.Method == HttpMethod.Post)
                {
                    request
                        .Content?.ReadAsStringAsync()
                        .ContinueWith(t =>
                        {
                            var json = JsonDocument.Parse(t.Result);
                            if (json.RootElement.TryGetProperty("colorId", out var colorId))
                            {
                                capturedColorId = colorId.GetString();
                            }
                        })
                        .Wait();

                    return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
                }

                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
            },
            mockUserRepo
        );

        var item = CreateCalendarItem(color: "#FF0000"); // Red

        await service.ExportEventsAsync("testuser", new[] { item });

        capturedColorId.Should().NotBeNull();
    }

    [Fact]
    public async Task ExportEventsAsync_OmitsColorId_WhenInvalidHexColor()
    {
        var user = CreateTestUser("testuser");
        var mockUserRepo = new Mock<IUserRepository>();
        mockUserRepo.Setup(r => r.GetByUsernameAsync("testuser")).ReturnsAsync(user);

        bool hasColorId = false;
        var service = CreateServiceWithMockHttpClient(
            (request, cancellationToken) =>
            {
                var url = request.RequestUri?.ToString() ?? "";

                if (url.Contains("/events?privateExtendedProperty="))
                {
                    return Task.FromResult(
                        new HttpResponseMessage(HttpStatusCode.OK)
                        {
                            Content = new StringContent(
                                "{\"items\":[]}",
                                Encoding.UTF8,
                                "application/json"
                            ),
                        }
                    );
                }
                else if (request.Method == HttpMethod.Post)
                {
                    request
                        .Content?.ReadAsStringAsync()
                        .ContinueWith(t =>
                        {
                            var json = JsonDocument.Parse(t.Result);
                            hasColorId = json.RootElement.TryGetProperty("colorId", out _);
                        })
                        .Wait();

                    return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
                }

                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
            },
            mockUserRepo
        );

        var item = CreateCalendarItem(color: "INVALID");

        await service.ExportEventsAsync("testuser", new[] { item });

        hasColorId.Should().BeFalse();
    }

    [Fact]
    public async Task ExportEventsAsync_NormalizesDatesToUtc()
    {
        var user = CreateTestUser("testuser");
        var mockUserRepo = new Mock<IUserRepository>();
        mockUserRepo.Setup(r => r.GetByUsernameAsync("testuser")).ReturnsAsync(user);

        string? capturedStart = null;
        string? capturedEnd = null;
        var service = CreateServiceWithMockHttpClient(
            (request, cancellationToken) =>
            {
                var url = request.RequestUri?.ToString() ?? "";

                if (url.Contains("/events?privateExtendedProperty="))
                {
                    return Task.FromResult(
                        new HttpResponseMessage(HttpStatusCode.OK)
                        {
                            Content = new StringContent(
                                "{\"items\":[]}",
                                Encoding.UTF8,
                                "application/json"
                            ),
                        }
                    );
                }
                else if (request.Method == HttpMethod.Post)
                {
                    request
                        .Content?.ReadAsStringAsync()
                        .ContinueWith(t =>
                        {
                            var json = JsonDocument.Parse(t.Result);
                            capturedStart = json
                                .RootElement.GetProperty("start")
                                .GetProperty("dateTime")
                                .GetString();
                            capturedEnd = json
                                .RootElement.GetProperty("end")
                                .GetProperty("dateTime")
                                .GetString();
                        })
                        .Wait();

                    return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
                }

                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
            },
            mockUserRepo
        );

        var localDate = new DateTime(2024, 3, 15, 10, 0, 0, DateTimeKind.Local);
        var item = CreateCalendarItem(start: localDate, end: localDate.AddHours(1));

        await service.ExportEventsAsync("testuser", new[] { item });

        capturedStart.Should().EndWith("Z");
        capturedEnd.Should().EndWith("Z");
    }

    #endregion

    #region GetClosestGoogleColorId Tests (Private method tested indirectly)

    [Theory]
    [InlineData("#FF0000", "1")]
    [InlineData("#00FF00", "11")]
    [InlineData("#0000FF", "6")]
    [InlineData("#000000", "4")]
    [InlineData("#FFFFFF", "9")]
    public async Task ExportEventsAsync_MapsColorsCorrectly(string hexColor, string expectedColorId)
    {
        var user = CreateTestUser("testuser");
        var mockUserRepo = new Mock<IUserRepository>();
        mockUserRepo.Setup(r => r.GetByUsernameAsync("testuser")).ReturnsAsync(user);

        string? capturedColorId = null;
        var service = CreateServiceWithMockHttpClient(
            (request, cancellationToken) =>
            {
                var url = request.RequestUri?.ToString() ?? "";

                if (url.Contains("/events?privateExtendedProperty="))
                {
                    return Task.FromResult(
                        new HttpResponseMessage(HttpStatusCode.OK)
                        {
                            Content = new StringContent(
                                "{\"items\":[]}",
                                Encoding.UTF8,
                                "application/json"
                            ),
                        }
                    );
                }
                else if (request.Method == HttpMethod.Post)
                {
                    request
                        .Content?.ReadAsStringAsync()
                        .ContinueWith(t =>
                        {
                            var json = JsonDocument.Parse(t.Result);
                            if (json.RootElement.TryGetProperty("colorId", out var colorId))
                            {
                                capturedColorId = colorId.GetString();
                            }
                        })
                        .Wait();

                    return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
                }

                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
            },
            mockUserRepo
        );

        var item = CreateCalendarItem(color: hexColor);

        await service.ExportEventsAsync("testuser", new[] { item });

        capturedColorId.Should().Be(expectedColorId);
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    [InlineData("   ")]
    public async Task ExportEventsAsync_NoColorId_WhenEmptyOrNullColor(string? hexColor)
    {
        var user = CreateTestUser("testuser");
        var mockUserRepo = new Mock<IUserRepository>();
        mockUserRepo.Setup(r => r.GetByUsernameAsync("testuser")).ReturnsAsync(user);

        bool hasColorId = false;
        var service = CreateServiceWithMockHttpClient(
            (request, cancellationToken) =>
            {
                var url = request.RequestUri?.ToString() ?? "";

                if (url.Contains("/events?privateExtendedProperty="))
                {
                    return Task.FromResult(
                        new HttpResponseMessage(HttpStatusCode.OK)
                        {
                            Content = new StringContent(
                                "{\"items\":[]}",
                                Encoding.UTF8,
                                "application/json"
                            ),
                        }
                    );
                }
                else if (request.Method == HttpMethod.Post)
                {
                    request
                        .Content?.ReadAsStringAsync()
                        .ContinueWith(t =>
                        {
                            var json = JsonDocument.Parse(t.Result);
                            hasColorId = json.RootElement.TryGetProperty("colorId", out _);
                        })
                        .Wait();

                    return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
                }

                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
            },
            mockUserRepo
        );

        var item = CreateCalendarItem(color: hexColor!);

        await service.ExportEventsAsync("testuser", new[] { item });

        hasColorId.Should().BeFalse();
    }

    [Fact]
    public async Task ExportEventsAsync_HandlesShorthandHexColors()
    {
        var user = CreateTestUser("testuser");
        var mockUserRepo = new Mock<IUserRepository>();
        mockUserRepo.Setup(r => r.GetByUsernameAsync("testuser")).ReturnsAsync(user);

        string? capturedColorId = null;
        var service = CreateServiceWithMockHttpClient(
            (request, cancellationToken) =>
            {
                var url = request.RequestUri?.ToString() ?? "";

                if (url.Contains("/events?privateExtendedProperty="))
                {
                    return Task.FromResult(
                        new HttpResponseMessage(HttpStatusCode.OK)
                        {
                            Content = new StringContent(
                                "{\"items\":[]}",
                                Encoding.UTF8,
                                "application/json"
                            ),
                        }
                    );
                }
                else if (request.Method == HttpMethod.Post)
                {
                    request
                        .Content?.ReadAsStringAsync()
                        .ContinueWith(t =>
                        {
                            var json = JsonDocument.Parse(t.Result);
                            if (json.RootElement.TryGetProperty("colorId", out var colorId))
                            {
                                capturedColorId = colorId.GetString();
                            }
                        })
                        .Wait();

                    return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
                }

                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
            },
            mockUserRepo
        );

        var item = CreateCalendarItem(color: "#F00");

        await service.ExportEventsAsync("testuser", new[] { item });

        capturedColorId.Should().Be("1");
    }

    #endregion

    #region ValidateTokenAsync Tests

    [Fact]
    public async Task ValidateTokenAsync_ReturnsTrue_WhenCachedTokenValid()
    {
        var user = CreateTestUser("testuser", hasValidToken: true);
        var mockUserRepo = new Mock<IUserRepository>();
        mockUserRepo.Setup(r => r.GetByUsernameAsync("testuser")).ReturnsAsync(user);

        var service = CreateServiceWithMockHttpClient(
            (request, cancellationToken) =>
                Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)),
            mockUserRepo
        );

        var result = await service.ValidateTokenAsync("testuser");

        result.Should().BeTrue();
    }

    [Fact]
    public async Task ValidateTokenAsync_RefreshesToken_WhenTokenExpired()
    {
        var user = CreateTestUser("testuser", hasValidToken: false);
        var mockUserRepo = new Mock<IUserRepository>();
        mockUserRepo.Setup(r => r.GetByUsernameAsync("testuser")).ReturnsAsync(user);
        mockUserRepo
            .Setup(r => r.UpsertGoogleAccountAsync(It.IsAny<string>(), It.IsAny<GoogleAccount>()))
            .Returns(Task.CompletedTask);

        var service = CreateServiceWithMockHttpClient(
            (request, cancellationToken) =>
            {
                var json = JsonSerializer.Serialize(
                    new { access_token = "new-access-token", expires_in = 3600 }
                );
                return Task.FromResult(
                    new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent(json, Encoding.UTF8, "application/json"),
                    }
                );
            },
            mockUserRepo
        );

        var result = await service.ValidateTokenAsync("testuser");

        result.Should().BeTrue();
        mockUserRepo.Verify(
            r =>
                r.UpsertGoogleAccountAsync(
                    "testuser",
                    It.Is<GoogleAccount>(a => a.AccessToken == "new-access-token")
                ),
            Times.Once
        );
    }

    [Fact]
    public async Task ValidateTokenAsync_ReturnsFalse_WhenNoRefreshToken()
    {
        var user = CreateTestUser("testuser", hasValidToken: false);
        user.GoogleAccount!.RefreshToken = null!;

        var mockUserRepo = new Mock<IUserRepository>();
        mockUserRepo.Setup(r => r.GetByUsernameAsync("testuser")).ReturnsAsync(user);

        var service = CreateServiceWithMockHttpClient(
            (request, cancellationToken) =>
                Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)),
            mockUserRepo
        );

        var result = await service.ValidateTokenAsync("testuser");

        result.Should().BeFalse();
    }

    [Fact]
    public async Task ValidateTokenAsync_ReturnsFalse_WhenUserNotFound()
    {
        var mockUserRepo = new Mock<IUserRepository>();
        mockUserRepo.Setup(r => r.GetByUsernameAsync("testuser")).ReturnsAsync((User?)null);

        var service = CreateServiceWithMockHttpClient(
            (request, cancellationToken) =>
                Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)),
            mockUserRepo
        );

        var result = await service.ValidateTokenAsync("testuser");

        result.Should().BeFalse();
    }

    [Fact]
    public async Task ValidateTokenAsync_ReturnsFalse_WhenNoGoogleAccount()
    {
        var user = CreateTestUser("testuser", hasGoogleAccount: false);
        var mockUserRepo = new Mock<IUserRepository>();
        mockUserRepo.Setup(r => r.GetByUsernameAsync("testuser")).ReturnsAsync(user);

        var service = CreateServiceWithMockHttpClient(
            (request, cancellationToken) =>
                Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)),
            mockUserRepo
        );

        var result = await service.ValidateTokenAsync("testuser");

        result.Should().BeFalse();
    }

    [Fact]
    public async Task ValidateTokenAsync_ReturnsFalse_WhenRefreshFailsWithInvalidGrant()
    {
        var user = CreateTestUser("testuser", hasValidToken: false);
        var mockUserRepo = new Mock<IUserRepository>();
        mockUserRepo.Setup(r => r.GetByUsernameAsync("testuser")).ReturnsAsync(user);

        var service = CreateServiceWithMockHttpClient(
            (request, cancellationToken) =>
            {
                return Task.FromResult(
                    new HttpResponseMessage(HttpStatusCode.BadRequest)
                    {
                        Content = new StringContent(
                            "{\"error\":\"invalid_grant\"}",
                            Encoding.UTF8,
                            "application/json"
                        ),
                    }
                );
            },
            mockUserRepo
        );

        var result = await service.ValidateTokenAsync("testuser");

        result.Should().BeFalse();
    }

    [Fact]
    public async Task ValidateTokenAsync_ReturnsFalse_WhenTokenResponseMissingAccessToken()
    {
        var user = CreateTestUser("testuser", hasValidToken: false);
        var mockUserRepo = new Mock<IUserRepository>();
        mockUserRepo.Setup(r => r.GetByUsernameAsync("testuser")).ReturnsAsync(user);

        var service = CreateServiceWithMockHttpClient(
            (request, cancellationToken) =>
            {
                return Task.FromResult(
                    new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent(
                            "{\"expires_in\":3600}",
                            Encoding.UTF8,
                            "application/json"
                        ),
                    }
                );
            },
            mockUserRepo
        );

        var result = await service.ValidateTokenAsync("testuser");

        result.Should().BeFalse();
    }

    #endregion

    #region EnsureAccessTokenAsync Tests (Private method tested indirectly via ExportEventsAsync)

    [Fact]
    public async Task ExportEventsAsync_UsesCachedToken_WhenNotExpired()
    {
        var user = CreateTestUser("testuser", hasValidToken: true);
        var mockUserRepo = new Mock<IUserRepository>();
        mockUserRepo.Setup(r => r.GetByUsernameAsync("testuser")).ReturnsAsync(user);

        int tokenRefreshCalls = 0;
        var service = CreateServiceWithMockHttpClient(
            (request, cancellationToken) =>
            {
                var url = request.RequestUri?.ToString() ?? "";

                if (url.Contains("oauth2.googleapis.com"))
                {
                    tokenRefreshCalls++;
                }

                if (url.Contains("/events?privateExtendedProperty="))
                {
                    return Task.FromResult(
                        new HttpResponseMessage(HttpStatusCode.OK)
                        {
                            Content = new StringContent(
                                "{\"items\":[]}",
                                Encoding.UTF8,
                                "application/json"
                            ),
                        }
                    );
                }

                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
            },
            mockUserRepo
        );

        var item = CreateCalendarItem();

        await service.ExportEventsAsync("testuser", new[] { item });

        tokenRefreshCalls.Should().Be(0);
    }

    [Fact]
    public async Task ExportEventsAsync_RefreshesToken_WhenExpired()
    {
        var user = CreateTestUser("testuser", hasValidToken: false);
        var mockUserRepo = new Mock<IUserRepository>();
        mockUserRepo.Setup(r => r.GetByUsernameAsync("testuser")).ReturnsAsync(user);
        mockUserRepo
            .Setup(r => r.UpsertGoogleAccountAsync(It.IsAny<string>(), It.IsAny<GoogleAccount>()))
            .Returns(Task.CompletedTask);

        int tokenRefreshCalls = 0;
        var service = CreateServiceWithMockHttpClient(
            (request, cancellationToken) =>
            {
                var url = request.RequestUri?.ToString() ?? "";

                if (url.Contains("oauth2.googleapis.com"))
                {
                    tokenRefreshCalls++;
                    var json = JsonSerializer.Serialize(
                        new { access_token = "new-access-token", expires_in = 3600 }
                    );
                    return Task.FromResult(
                        new HttpResponseMessage(HttpStatusCode.OK)
                        {
                            Content = new StringContent(json, Encoding.UTF8, "application/json"),
                        }
                    );
                }

                if (url.Contains("/events?privateExtendedProperty="))
                {
                    return Task.FromResult(
                        new HttpResponseMessage(HttpStatusCode.OK)
                        {
                            Content = new StringContent(
                                "{\"items\":[]}",
                                Encoding.UTF8,
                                "application/json"
                            ),
                        }
                    );
                }

                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
            },
            mockUserRepo
        );

        var item = CreateCalendarItem();

        await service.ExportEventsAsync("testuser", new[] { item });

        tokenRefreshCalls.Should().Be(1);
        mockUserRepo.Verify(
            r =>
                r.UpsertGoogleAccountAsync(
                    "testuser",
                    It.Is<GoogleAccount>(a => a.AccessToken == "new-access-token")
                ),
            Times.Once
        );
    }

    [Fact]
    public async Task ExportEventsAsync_Throws_WhenNoRefreshTokenAvailable()
    {
        var user = CreateTestUser("testuser", hasValidToken: false);
        user.GoogleAccount!.RefreshToken = null!;

        var mockUserRepo = new Mock<IUserRepository>();
        mockUserRepo.Setup(r => r.GetByUsernameAsync("testuser")).ReturnsAsync(user);

        var service = CreateServiceWithMockHttpClient(
            (request, cancellationToken) =>
                Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)),
            mockUserRepo
        );

        var item = CreateCalendarItem();

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.ExportEventsAsync("testuser", new[] { item })
        );
        exception.Message.Should().Contain("Please reconnect your Google account");
    }

    [Fact]
    public async Task ExportEventsAsync_Throws_WhenRefreshTokenExpired()
    {
        var user = CreateTestUser("testuser", hasValidToken: false);
        var mockUserRepo = new Mock<IUserRepository>();
        mockUserRepo.Setup(r => r.GetByUsernameAsync("testuser")).ReturnsAsync(user);

        var service = CreateServiceWithMockHttpClient(
            (request, cancellationToken) =>
            {
                return Task.FromResult(
                    new HttpResponseMessage(HttpStatusCode.BadRequest)
                    {
                        Content = new StringContent(
                            "{\"error\":\"invalid_grant\"}",
                            Encoding.UTF8,
                            "application/json"
                        ),
                    }
                );
            },
            mockUserRepo
        );

        var item = CreateCalendarItem();

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.ExportEventsAsync("testuser", new[] { item })
        );
        exception.Message.Should().Contain("Google refresh token has expired");
    }

    [Fact]
    public async Task ExportEventsAsync_Throws_WhenTokenResponseMissingAccessToken()
    {
        var user = CreateTestUser("testuser", hasValidToken: false);
        var mockUserRepo = new Mock<IUserRepository>();
        mockUserRepo.Setup(r => r.GetByUsernameAsync("testuser")).ReturnsAsync(user);

        var service = CreateServiceWithMockHttpClient(
            (request, cancellationToken) =>
            {
                return Task.FromResult(
                    new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent(
                            "{\"expires_in\":3600}",
                            Encoding.UTF8,
                            "application/json"
                        ),
                    }
                );
            },
            mockUserRepo
        );

        var item = CreateCalendarItem();

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.ExportEventsAsync("testuser", new[] { item })
        );
        exception.Message.Should().Contain("no access token received");
    }

    #endregion
}
