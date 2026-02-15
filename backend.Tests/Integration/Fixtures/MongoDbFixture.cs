using backend.Data;
using backend.Models;
using backend.Repositories;
using backend.Services;
using Microsoft.AspNetCore.DataProtection;
using MongoDB.Driver;
using Testcontainers.MongoDb;

namespace backend.Tests.Integration.Fixtures;

/// <summary>
/// Fixture that provides a shared MongoDB container for integration tests.
/// The container is created once per test class and reused across all tests.
/// </summary>
public class MongoDbFixture : IAsyncLifetime
{
    private readonly MongoDbContainer _mongoContainer;

    /// <summary>
    /// Gets the MongoDB context for database operations.
    /// </summary>
    public MongoDbContext Context { get; private set; } = null!;

    /// <summary>
    /// Gets the UserRepository instance configured with the test container.
    /// </summary>
    public UserRepository Repository { get; private set; } = null!;

    /// <summary>
    /// Initializes a new instance of the <see cref="MongoDbFixture"/> class.
    /// </summary>
    public MongoDbFixture()
    {
        _mongoContainer = new MongoDbBuilder().WithImage("mongo:8.2.3").Build();
    }

    /// <summary>
    /// Starts the MongoDB container and initializes the repository.
    /// Called once before all tests in the class.
    /// </summary>
    public async ValueTask InitializeAsync()
    {
        await _mongoContainer.StartAsync();

        var connectionString = _mongoContainer.GetConnectionString();
        Context = new MongoDbContext(connectionString);

        var dataProtectionProvider = new EphemeralDataProtectionProvider();
        var tokenProtector = new TokenProtector(dataProtectionProvider);

        Repository = new UserRepository(Context, tokenProtector);
    }

    /// <summary>
    /// Disposes the MongoDB container.
    /// Called once after all tests in the class.
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        await _mongoContainer.DisposeAsync();
    }

    /// <summary>
    /// Cleans up all data from the Users collection.
    /// Should be called at the beginning of each test to ensure isolation.
    /// </summary>
    public async Task CleanupDatabase()
    {
        await Context.Users.DeleteManyAsync(Builders<User>.Filter.Empty);
    }
}
