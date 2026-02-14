using Moq;
using backend.Services;
using backend.Repositories;
using backend.Models;
using backend.Dtos;
using Microsoft.AspNetCore.DataProtection;

namespace backend.Tests.Helpers;

/// <summary>
/// Factory class for creating common mocks used across test suite.
/// Centralizes mock creation to reduce duplication and ensure consistency.
/// </summary>
public static class MockFactory
{
    /// <summary>
    /// Create a mock IDataProtectionProvider for TokenProtector tests.
    /// </summary>
    /// <param name="setupProtector">Optional setup action for the data protector mock</param>
    /// <returns>Mock of IDataProtectionProvider</returns>
    public static Mock<IDataProtectionProvider> CreateMockDataProtectionProvider(
        Action<Mock<IDataProtector>>? setupProtector = null)
    {
        var mockDataProtector = new Mock<IDataProtector>();
        
        // Setup default behavior: protect returns a predictable value
        mockDataProtector
            .Setup(p => p.Protect(It.IsAny<string>()))
            .Returns((string input) => $"protected_{input}");
        
        // Setup default behavior: unprotect reverses the protection
        mockDataProtector
            .Setup(p => p.Unprotect(It.IsAny<string>()))
            .Returns((string input) =>
            {
                if (input.StartsWith("protected_"))
                    return input.Substring("protected_".Length);
                throw new System.Security.Cryptography.CryptographicException("Invalid protected data");
            });

        // Apply custom setup if provided
        setupProtector?.Invoke(mockDataProtector);

        var mockProvider = new Mock<IDataProtectionProvider>();
        mockProvider
            .Setup(p => p.CreateProtector(It.IsAny<string>()))
            .Returns(mockDataProtector.Object);

        return mockProvider;
    }

    /// <summary>
    /// Create a mock ITokenProtector for service tests.
    /// </summary>
    /// <param name="setupProtect">Optional setup for Protect method</param>
    /// <returns>Mock of ITokenProtector</returns>
    public static Mock<ITokenProtector> CreateMockTokenProtector(
        Action<Mock<ITokenProtector>>? setupProtect = null)
    {
        var mock = new Mock<ITokenProtector>();
        
        // Setup default behavior
        mock
            .Setup(t => t.Protect(It.IsAny<string>()))
            .Returns((string input) => string.IsNullOrEmpty(input) ? string.Empty : $"protected_{input}");
        
        mock
            .Setup(t => t.Unprotect(It.IsAny<string>()))
            .Returns((string input) => 
            {
                if (string.IsNullOrEmpty(input)) return null;
                if (input.StartsWith("protected_"))
                    return input.Substring("protected_".Length);
                return input;
            });

        setupProtect?.Invoke(mock);
        return mock;
    }

    /// <summary>
    /// Create a mock IUserRepository for controller/service tests.
    /// </summary>
    /// <param name="setupRepository">Optional setup action for the repository mock</param>
    /// <returns>Mock of IUserRepository</returns>
    public static Mock<IUserRepository> CreateMockUserRepository(
        Action<Mock<IUserRepository>>? setupRepository = null)
    {
        var mock = new Mock<IUserRepository>();
        
        // Setup default behavior for common methods
        mock
            .Setup(r => r.GetByUsernameAsync(It.IsAny<string>()))
            .ReturnsAsync((string username) => new User 
            { 
                Id = Guid.NewGuid().ToString(),
                Username = username,
                Email = $"{username}@test.com",
                Events = new List<Event>()
            });

        mock
            .Setup(r => r.GetByEmailAsync(It.IsAny<string>()))
            .ReturnsAsync((string email) => new User 
            { 
                Id = Guid.NewGuid().ToString(),
                Username = email.Split('@')[0],
                Email = email,
                Events = new List<Event>()
            });

        mock
            .Setup(r => r.UpsertByUsernameAsync(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        mock
            .Setup(r => r.AddEventAsync(It.IsAny<string>(), It.IsAny<Event>()))
            .ReturnsAsync((string username, Event evt) => evt);

        mock
            .Setup(r => r.UpdateEventAsync(It.IsAny<string>(), It.IsAny<Event>()))
            .ReturnsAsync(true);

        mock
            .Setup(r => r.DeleteEventAsync(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        setupRepository?.Invoke(mock);
        return mock;
    }

    /// <summary>
    /// Create a mock IBlackboardService for controller/service tests.
    /// </summary>
    /// <param name="setupService">Optional setup action for the service mock</param>
    /// <returns>Mock of IBlackboardService</returns>
    public static Mock<IBlackboardService> CreateMockBlackboardService(
        Action<Mock<IBlackboardService>>? setupService = null)
    {
        var mock = new Mock<IBlackboardService>();
        
        // Setup default behavior
        mock
            .Setup(s => s.AuthenticateAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(new LoginResponseDto 
            { 
                IsSuccess = true,
                Message = "Login successful",
                SessionCookie = "test-session-cookie"
            });

        setupService?.Invoke(mock);
        return mock;
    }

    /// <summary>
    /// Create a mock IGoogleCalendarService for controller/service tests.
    /// </summary>
    /// <param name="setupService">Optional setup action for the service mock</param>
    /// <returns>Mock of IGoogleCalendarService</returns>
    public static Mock<IGoogleCalendarService> CreateMockGoogleCalendarService(
        Action<Mock<IGoogleCalendarService>>? setupService = null)
    {
        var mock = new Mock<IGoogleCalendarService>();
        
        // Setup default behavior
        mock
            .Setup(s => s.ExportEventsAsync(It.IsAny<string>(), It.IsAny<IEnumerable<CalendarItemDto>>()))
            .ReturnsAsync(new ExportSummaryDto 
            { 
                Created = 0,
                Updated = 0,
                Failed = 0,
                Errors = null
            });

        mock
            .Setup(s => s.ValidateTokenAsync(It.IsAny<string>()))
            .ReturnsAsync(true);

        setupService?.Invoke(mock);
        return mock;
    }
}
