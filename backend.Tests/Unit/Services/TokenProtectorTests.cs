using backend.Services;
using Microsoft.AspNetCore.DataProtection;

namespace backend.Tests.Unit.Services;

/// <summary>
/// Unit tests for TokenProtector service.
/// Tests protection/unprotection of tokens using a real DataProtectionProvider.
/// </summary>
public class TokenProtectorTests
{
    private readonly TokenProtector _sut;

    public TokenProtectorTests()
    {
        var provider = new EphemeralDataProtectionProvider();
        _sut = new TokenProtector(provider);
    }

    #region Protect Method Tests

    [Fact]
    public void Protect_WithNullInput_ReturnsEmptyString()
    {
        var result = _sut.Protect(null);

        result.Should().BeEmpty();
    }

    [Fact]
    public void Protect_WithEmptyString_ReturnsEmptyString()
    {
        var result = _sut.Protect(string.Empty);

        result.Should().BeEmpty();
    }

    [Fact]
    public void Protect_WithValidToken_ReturnsNonEmptyString()
    {
        var plaintext = "test-token-12345";

        var result = _sut.Protect(plaintext);

        result.Should().NotBeEmpty();
        result.Should().NotBe(plaintext);
    }

    [Theory]
    [InlineData("token1")]
    [InlineData("token2")]
    [InlineData("token-with-special-chars-!@#$%^&*()")]
    public void Protect_WithVariousValidInputs_ReturnsEncryptedValue(string token)
    {
        var result = _sut.Protect(token);

        result.Should().NotBeEmpty();
        result.Should().NotBe(token);
    }

    #endregion

    #region Unprotect Method Tests

    [Fact]
    public void Unprotect_WithNullInput_ReturnsNull()
    {
        var result = _sut.Unprotect(null);

        result.Should().BeNull();
    }

    [Fact]
    public void Unprotect_WithEmptyString_ReturnsEmptyString()
    {
        var result = _sut.Unprotect(string.Empty);

        result.Should().BeEmpty();
    }

    [Fact]
    public void Unprotect_WithInvalidEncryptedData_ReturnOriginalValue()
    {
        var invalidProtectedText = "definitely-not-a-valid-encrypted-value";

        var result = _sut.Unprotect(invalidProtectedText);

        result.Should().Be(invalidProtectedText);
    }

    #endregion

    #region Integration Tests (Protect/Unprotect Cycle)

    [Fact]
    public void ProtectThenUnprotect_WithValidToken_ReturnsOriginalValue()
    {
        var originalToken = "original-token-value-to-encrypt";

        var encrypted = _sut.Protect(originalToken);
        var decrypted = _sut.Unprotect(encrypted);

        decrypted.Should().Be(originalToken);
        encrypted.Should().NotBe(originalToken);
    }

    [Theory]
    [InlineData("token1")]
    [InlineData("token2")]
    [InlineData("complex-token-!@#$%^")]
    public void ProtectUnprotectCycle_WithVariousTokens_Succeeds(string token)
    {
        var encrypted = _sut.Protect(token);
        var decrypted = _sut.Unprotect(encrypted);

        decrypted.Should().Be(token);
    }

    [Fact]
    public void ProtectNull_ThenUnprotectEmpty_ReturnsBothEmpty()
    {
        var protectedNull = _sut.Protect(null);
        var unprotectedEmpty = _sut.Unprotect(protectedNull);

        protectedNull.Should().BeEmpty();
        unprotectedEmpty.Should().BeEmpty();
    }

    [Fact]
    public void MultipleDifferentTokens_AreEncryptedDifferently()
    {
        var token1 = "token-one";
        var token2 = "token-two";

        var encrypted1 = _sut.Protect(token1);
        var encrypted2 = _sut.Protect(token2);

        encrypted1.Should().NotBe(encrypted2);
    }

    #endregion
}
