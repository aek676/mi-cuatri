using backend.Data;
using backend.Enums;
using backend.Models;
using backend.Repositories;
using backend.Tests.Integration.Fixtures;
using MongoDB.Bson;
using MongoDB.Driver;

namespace backend.Tests.Integration.Repositories;

/// <summary>
/// Integration tests for UserRepository using Testcontainers MongoDB.
/// Uses IClassFixture to share a single MongoDB container across all tests.
/// </summary>
public class UserRepositoryTests : IClassFixture<MongoDbFixture>
{
    private readonly MongoDbFixture _fixture;

    public UserRepositoryTests(MongoDbFixture fixture)
    {
        _fixture = fixture;
    }

    private async Task CleanupDatabase()
    {
        await _fixture.CleanupDatabase();
    }

    private User CreateTestUser(string username, string? email = null)
    {
        return new User
        {
            Id = ObjectId.GenerateNewId().ToString(),
            Username = username,
            Email = email ?? $"{username}@test.com",
            Events = new List<Event>(),
        };
    }

    private GoogleAccount CreateTestGoogleAccount()
    {
        return new GoogleAccount
        {
            GoogleId = "google-123",
            Email = "user@gmail.com",
            RefreshToken = "refresh-token-secret",
            AccessToken = "access-token-secret",
            AccessTokenExpiry = DateTime.UtcNow.AddHours(1),
            Scopes = new[] { "calendar" },
        };
    }

    private Event CreateTestEvent(string title = "Test Event")
    {
        return new Event
        {
            Id = Guid.NewGuid().ToString(),
            Title = title,
            Subject = "Test Subject",
            Start = DateTime.UtcNow.AddHours(1),
            End = DateTime.UtcNow.AddHours(2),
            Location = "Test Location",
            Color = "#FF5733",
            Category = CalendarCategory.Course,
        };
    }

    #region UpsertByUsernameAsync Tests

    [Fact]
    public async Task UpsertByUsernameAsync_CreatesNewUser_WhenUserDoesNotExist()
    {
        await CleanupDatabase();
        var username = "newuser";

        await _fixture.Repository.UpsertByUsernameAsync(username);

        var user = await _fixture.Repository.GetByUsernameAsync(username);
        user.Should().NotBeNull();
        user!.Username.Should().Be(username);
    }

    [Fact]
    public async Task UpsertByUsernameAsync_UpdatesEmail_WhenUserExists()
    {
        await CleanupDatabase();
        var username = "existinguser";
        var initialEmail = "old@email.com";
        var newEmail = "new@email.com";

        await _fixture.Repository.UpsertByUsernameAsync(username, initialEmail);

        await _fixture.Repository.UpsertByUsernameAsync(username, newEmail);

        var user = await _fixture.Repository.GetByUsernameAsync(username);
        user.Should().NotBeNull();
        user!.Email.Should().Be(newEmail);
        user.Username.Should().Be(username);
    }

    [Fact]
    public async Task UpsertByUsernameAsync_KeepsUsername_WhenUpdatingOnlyEmail()
    {
        await CleanupDatabase();
        var username = "testuser";
        var email = "user@test.com";

        await _fixture.Repository.UpsertByUsernameAsync(username);

        await _fixture.Repository.UpsertByUsernameAsync(username, email);

        var user = await _fixture.Repository.GetByUsernameAsync(username);
        user.Should().NotBeNull();
        user!.Username.Should().Be(username);
        user.Email.Should().Be(email);
    }

    #endregion

    #region GetByUsernameAsync Tests

    [Fact]
    public async Task GetByUsernameAsync_ReturnsUser_WhenUserExists()
    {
        await CleanupDatabase();
        var username = "existentuser";
        await _fixture.Repository.UpsertByUsernameAsync(username, "existent@test.com");

        var result = await _fixture.Repository.GetByUsernameAsync(username);

        result.Should().NotBeNull();
        result!.Username.Should().Be(username);
        result.Email.Should().Be("existent@test.com");
    }

    [Fact]
    public async Task GetByUsernameAsync_ReturnsNull_WhenUserDoesNotExist()
    {
        await CleanupDatabase();

        var result = await _fixture.Repository.GetByUsernameAsync("nonexistent");

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByUsernameAsync_DecryptsGoogleAccountTokens()
    {
        await CleanupDatabase();
        var username = "userwithgoogle";
        await _fixture.Repository.UpsertByUsernameAsync(username);

        var googleAccount = CreateTestGoogleAccount();
        await _fixture.Repository.UpsertGoogleAccountAsync(username, googleAccount);

        var user = await _fixture.Repository.GetByUsernameAsync(username);

        user.Should().NotBeNull();
        user!.GoogleAccount.Should().NotBeNull();
        user.GoogleAccount!.RefreshToken.Should().Be("refresh-token-secret");
        user.GoogleAccount.AccessToken.Should().Be("access-token-secret");
    }

    #endregion

    #region GetByEmailAsync Tests

    [Fact]
    public async Task GetByEmailAsync_ReturnsUser_WhenEmailExists()
    {
        await CleanupDatabase();
        var username = "emailuser";
        var email = "findme@test.com";
        await _fixture.Repository.UpsertByUsernameAsync(username, email);

        var result = await _fixture.Repository.GetByEmailAsync(email);

        result.Should().NotBeNull();
        result!.Username.Should().Be(username);
        result.Email.Should().Be(email);
    }

    [Fact]
    public async Task GetByEmailAsync_ReturnsNull_WhenEmailDoesNotExist()
    {
        await CleanupDatabase();

        var result = await _fixture.Repository.GetByEmailAsync("nonexistent@test.com");

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByEmailAsync_DecryptsGoogleAccountTokens()
    {
        await CleanupDatabase();
        var username = "useremail";
        var email = "decrypt@test.com";
        await _fixture.Repository.UpsertByUsernameAsync(username, email);

        var googleAccount = CreateTestGoogleAccount();
        await _fixture.Repository.UpsertGoogleAccountAsync(username, googleAccount);

        var user = await _fixture.Repository.GetByEmailAsync(email);

        user.Should().NotBeNull();
        user!.GoogleAccount.Should().NotBeNull();
        user.GoogleAccount!.RefreshToken.Should().Be("refresh-token-secret");
        user.GoogleAccount.AccessToken.Should().Be("access-token-secret");
    }

    #endregion

    #region UpsertGoogleAccountAsync Tests

    [Fact]
    public async Task UpsertGoogleAccountAsync_AddsGoogleAccountToExistingUser()
    {
        await CleanupDatabase();
        var username = "addgoogle";
        await _fixture.Repository.UpsertByUsernameAsync(username);
        var googleAccount = CreateTestGoogleAccount();

        await _fixture.Repository.UpsertGoogleAccountAsync(username, googleAccount);

        var user = await _fixture.Repository.GetByUsernameAsync(username);
        user.Should().NotBeNull();
        user!.GoogleAccount.Should().NotBeNull();
        user.GoogleAccount!.Email.Should().Be("user@gmail.com");
    }

    [Fact]
    public async Task UpsertGoogleAccountAsync_UpdatesExistingGoogleAccount()
    {
        await CleanupDatabase();
        var username = "updategoogle";
        await _fixture.Repository.UpsertByUsernameAsync(username);

        var initialAccount = CreateTestGoogleAccount();
        await _fixture.Repository.UpsertGoogleAccountAsync(username, initialAccount);

        var updatedAccount = new GoogleAccount
        {
            GoogleId = "google-456",
            Email = "newemail@gmail.com",
            RefreshToken = "new-refresh-token",
            AccessToken = "new-access-token",
            AccessTokenExpiry = DateTime.UtcNow.AddHours(2),
            Scopes = new[] { "calendar", "email" },
        };

        await _fixture.Repository.UpsertGoogleAccountAsync(username, updatedAccount);

        var user = await _fixture.Repository.GetByUsernameAsync(username);
        user.Should().NotBeNull();
        user!.GoogleAccount.Should().NotBeNull();
        user.GoogleAccount!.Email.Should().Be("newemail@gmail.com");
        user.GoogleAccount.RefreshToken.Should().Be("new-refresh-token");
    }

    [Fact]
    public async Task UpsertGoogleAccountAsync_EncryptsTokensBeforeSaving()
    {
        await CleanupDatabase();
        var username = "encrypttest";
        await _fixture.Repository.UpsertByUsernameAsync(username);
        var googleAccount = CreateTestGoogleAccount();

        await _fixture.Repository.UpsertGoogleAccountAsync(username, googleAccount);

        var filter = Builders<User>.Filter.Eq(u => u.Username, username);
        var rawUser = await _fixture.Context.Users.Find(filter).FirstOrDefaultAsync();
        rawUser.Should().NotBeNull();
        rawUser!.GoogleAccount.Should().NotBeNull();

        rawUser.GoogleAccount!.RefreshToken.Should().NotBe("refresh-token-secret");
        rawUser.GoogleAccount.AccessToken.Should().NotBe("access-token-secret");
    }

    #endregion

    #region RemoveGoogleAccountAsync Tests

    [Fact]
    public async Task RemoveGoogleAccountAsync_RemovesGoogleAccountFromUser()
    {
        await CleanupDatabase();
        var username = "removegoogle";
        await _fixture.Repository.UpsertByUsernameAsync(username);
        var googleAccount = CreateTestGoogleAccount();
        await _fixture.Repository.UpsertGoogleAccountAsync(username, googleAccount);

        var userBefore = await _fixture.Repository.GetByUsernameAsync(username);
        userBefore!.GoogleAccount.Should().NotBeNull();

        await _fixture.Repository.RemoveGoogleAccountAsync(username);

        var userAfter = await _fixture.Repository.GetByUsernameAsync(username);
        userAfter!.GoogleAccount.Should().BeNull();
    }

    [Fact]
    public async Task RemoveGoogleAccountAsync_DoesNotFail_WhenUserHasNoGoogleAccount()
    {
        await CleanupDatabase();
        var username = "nogoogle";
        await _fixture.Repository.UpsertByUsernameAsync(username);

        await _fixture
            .Repository.Invoking(r => r.RemoveGoogleAccountAsync(username))
            .Should()
            .NotThrowAsync();

        var user = await _fixture.Repository.GetByUsernameAsync(username);
        user.Should().NotBeNull();
    }

    #endregion

    #region GetUserEventsAsync Tests

    [Fact]
    public async Task GetUserEventsAsync_ReturnsEmptyList_WhenUserHasNoEvents()
    {
        await CleanupDatabase();
        var username = "noevents";
        await _fixture.Repository.UpsertByUsernameAsync(username);

        var events = await _fixture.Repository.GetUserEventsAsync(username);

        events.Should().BeEmpty();
    }

    [Fact]
    public async Task GetUserEventsAsync_ReturnsEvents_WhenUserHasEvents()
    {
        await CleanupDatabase();
        var username = "withevents";
        await _fixture.Repository.UpsertByUsernameAsync(username);

        var event1 = CreateTestEvent("Event 1");
        var event2 = CreateTestEvent("Event 2");
        await _fixture.Repository.AddEventAsync(username, event1);
        await _fixture.Repository.AddEventAsync(username, event2);

        var events = await _fixture.Repository.GetUserEventsAsync(username);

        events.Should().HaveCount(2);
        events.Select(e => e.Title).Should().Contain("Event 1", "Event 2");
    }

    #endregion

    #region GetEventByIdAsync Tests

    [Fact]
    public async Task GetEventByIdAsync_ReturnsEvent_WhenEventExists()
    {
        await CleanupDatabase();
        var username = "eventowner";
        await _fixture.Repository.UpsertByUsernameAsync(username);

        var testEvent = CreateTestEvent("Specific Event");
        await _fixture.Repository.AddEventAsync(username, testEvent);

        var result = await _fixture.Repository.GetEventByIdAsync(username, testEvent.Id);

        result.Should().NotBeNull();
        result!.Id.Should().Be(testEvent.Id);
        result.Title.Should().Be("Specific Event");
    }

    [Fact]
    public async Task GetEventByIdAsync_ReturnsNull_WhenEventDoesNotExist()
    {
        await CleanupDatabase();
        var username = "eventowner";
        await _fixture.Repository.UpsertByUsernameAsync(username);

        var result = await _fixture.Repository.GetEventByIdAsync(username, "nonexistent-id");

        result.Should().BeNull();
    }

    #endregion

    #region AddEventAsync Tests

    [Fact]
    public async Task AddEventAsync_AddsEventToUser()
    {
        await CleanupDatabase();
        var username = "addeventuser";
        await _fixture.Repository.UpsertByUsernameAsync(username);
        var testEvent = CreateTestEvent("New Event");

        var addedEvent = await _fixture.Repository.AddEventAsync(username, testEvent);

        addedEvent.Should().NotBeNull();
        addedEvent.Id.Should().Be(testEvent.Id);

        var events = await _fixture.Repository.GetUserEventsAsync(username);
        events.Should().HaveCount(1);
        events.First().Title.Should().Be("New Event");
    }

    #endregion

    #region UpdateEventAsync Tests

    [Fact]
    public async Task UpdateEventAsync_UpdatesExistingEvent()
    {
        await CleanupDatabase();
        var username = "updateeventuser";
        await _fixture.Repository.UpsertByUsernameAsync(username);

        var originalEvent = CreateTestEvent("Original Title");
        await _fixture.Repository.AddEventAsync(username, originalEvent);

        var updatedEvent = new Event
        {
            Id = originalEvent.Id,
            Title = "Updated Title",
            Subject = "Updated Subject",
            Start = originalEvent.Start.AddHours(1),
            End = originalEvent.End.AddHours(1),
            Location = "Updated Location",
            Color = "#000000",
            Category = CalendarCategory.Personal,
        };

        var result = await _fixture.Repository.UpdateEventAsync(username, updatedEvent);

        result.Should().BeTrue();

        var retrievedEvent = await _fixture.Repository.GetEventByIdAsync(
            username,
            originalEvent.Id
        );
        retrievedEvent.Should().NotBeNull();
        retrievedEvent!.Title.Should().Be("Updated Title");
        retrievedEvent.Location.Should().Be("Updated Location");
        retrievedEvent.Category.Should().Be(CalendarCategory.Personal);
    }

    [Fact]
    public async Task UpdateEventAsync_ReturnsFalse_WhenEventDoesNotExist()
    {
        await CleanupDatabase();
        var username = "updateeventuser";
        await _fixture.Repository.UpsertByUsernameAsync(username);

        var nonExistentEvent = CreateTestEvent("Non Existent");

        var result = await _fixture.Repository.UpdateEventAsync(username, nonExistentEvent);

        result.Should().BeFalse();
    }

    #endregion

    #region DeleteEventAsync Tests

    [Fact]
    public async Task DeleteEventAsync_RemovesEventFromUser()
    {
        await CleanupDatabase();
        var username = "deleteeventuser";
        await _fixture.Repository.UpsertByUsernameAsync(username);

        var testEvent = CreateTestEvent("To Delete");
        await _fixture.Repository.AddEventAsync(username, testEvent);

        var beforeDelete = await _fixture.Repository.GetEventByIdAsync(username, testEvent.Id);
        beforeDelete.Should().NotBeNull();

        await _fixture.Repository.DeleteEventAsync(username, testEvent.Id);

        var afterDelete = await _fixture.Repository.GetEventByIdAsync(username, testEvent.Id);
        afterDelete.Should().BeNull();
    }

    [Fact]
    public async Task DeleteEventAsync_DoesNotFail_WhenEventDoesNotExist()
    {
        await CleanupDatabase();
        var username = "deleteeventuser";
        await _fixture.Repository.UpsertByUsernameAsync(username);

        await _fixture
            .Repository.Invoking(r => r.DeleteEventAsync(username, "nonexistent-id"))
            .Should()
            .NotThrowAsync();
    }

    #endregion

    #region Index Tests

    [Fact]
    public async Task UsernameIndex_IsUnique()
    {
        await CleanupDatabase();
        var username = "uniqueuser";
        await _fixture.Repository.UpsertByUsernameAsync(username);

        var duplicateUser = new User
        {
            Id = ObjectId.GenerateNewId().ToString(),
            Username = username,
            Email = "other@test.com",
        };

        await _fixture
            .Context.Invoking(c => c.Users.InsertOneAsync(duplicateUser))
            .Should()
            .ThrowAsync<MongoDB.Driver.MongoWriteException>();
    }

    #endregion
}
