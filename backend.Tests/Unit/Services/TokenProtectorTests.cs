using backend.Services;
using Microsoft.AspNetCore.DataProtection;

namespace backend.Tests.Unit.Services;

/// <summary>
/// Unit tests for TokenProtector service.
/// Tests protection/unprotection of tokens using a real DataProtectionProvider.
/// </summary>
public class TokenProtectorTests
{
    private readonly TokenProtector _sut; // System Under Test

    public TokenProtectorTests()
    {
        // Use real IDataProtectionProvider for testing
        // EphemeralDataProtectionProvider creates a temporary, in-memory protection provider
        var provider = new EphemeralDataProtectionProvider();
        _sut = new TokenProtector(provider);
    }

    #region Protect Method Tests

    [Fact]
    public void Protect_WithNullInput_ReturnsEmptyString()
    {
        // Act
        var result = _sut.Protect(null);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void Protect_WithEmptyString_ReturnsEmptyString()
    {
        // Act
        var result = _sut.Protect(string.Empty);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void Protect_WithValidToken_ReturnsNonEmptyString()
    {
        // Arrange
        var plaintext = "test-token-12345";

        // Act
        var result = _sut.Protect(plaintext);

        // Assert
        result.Should().NotBeEmpty();
        result.Should().NotBe(plaintext); // Should be encrypted, not plaintext
    }

    [Theory]
    [InlineData("token1")]
    [InlineData("token2")]
    [InlineData("token-with-special-chars-!@#$%^&*()")]
    public void Protect_WithVariousValidInputs_ReturnsEncryptedValue(string token)
    {
        // Act
        var result = _sut.Protect(token);

        // Assert
        result.Should().NotBeEmpty();
        result.Should().NotBe(token); // Should be encrypted
    }

    #endregion

    #region Unprotect Method Tests

    [Fact]
    public void Unprotect_WithNullInput_ReturnsNull()
    {
        // Act
        var result = _sut.Unprotect(null);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void Unprotect_WithEmptyString_ReturnsEmptyString()
    {
        // Act
        var result = _sut.Unprotect(string.Empty);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void Unprotect_WithInvalidEncryptedData_ReturnOriginalValue()
    {
        // Arrange
        var invalidProtectedText = "definitely-not-a-valid-encrypted-value";

        // Act
        var result = _sut.Unprotect(invalidProtectedText);

        // Assert
        // TokenProtector catches CryptographicException and returns original input (backward compatibility)
        result.Should().Be(invalidProtectedText);
    }

    #endregion

    #region Integration Tests (Protect/Unprotect Cycle)

    [Fact]
    public void ProtectThenUnprotect_WithValidToken_ReturnsOriginalValue()
    {
        // Arrange
        var originalToken = "original-token-value-to-encrypt";

        // Act
        var encrypted = _sut.Protect(originalToken);
        var decrypted = _sut.Unprotect(encrypted);

        // Assert
        decrypted.Should().Be(originalToken);
        encrypted.Should().NotBe(originalToken); // Verify encryption happened
    }

    [Theory]
    [InlineData("token1")]
    [InlineData("token2")]
    [InlineData("complex-token-!@#$%^")]
    public void ProtectUnprotectCycle_WithVariousTokens_Succeeds(string token)
    {
        // Act
        var encrypted = _sut.Protect(token);
        var decrypted = _sut.Unprotect(encrypted);

        // Assert
        decrypted.Should().Be(token);
    }

    [Fact]
    public void ProtectNull_ThenUnprotectEmpty_ReturnsBothEmpty()
    {
        // Act
        var protectedNull = _sut.Protect(null);
        var unprotectedEmpty = _sut.Unprotect(protectedNull);

        // Assert
        protectedNull.Should().BeEmpty();
        unprotectedEmpty.Should().BeEmpty();
    }

    [Fact]
    public void MultipleDifferentTokens_AreEncryptedDifferently()
    {
        // Arrange
        var token1 = "token-one";
        var token2 = "token-two";

        // Act
        var encrypted1 = _sut.Protect(token1);
        var encrypted2 = _sut.Protect(token2);

        // Assert
        encrypted1.Should().NotBe(encrypted2);
    }

    #endregion
}
