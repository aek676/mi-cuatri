namespace backend.Services
{
    /// <summary>
    /// Abstraction for protecting (encrypting) and unprotecting short-lived tokens.
    /// </summary>
    public interface ITokenProtector
    {
        /// <summary>Protects (encrypts) the given plaintext token. Returns empty string for null/empty input.</summary>
        string Protect(string? plaintext);

        /// <summary>Attempts to unprotect (decrypt) the token. Returns original input in case of failure.</summary>
        string? Unprotect(string? protectedText);
    }
} 