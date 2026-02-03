using Microsoft.AspNetCore.DataProtection;
using System.Security.Cryptography;

namespace backend.Services
{
    /// <summary>
    /// Protects and unprotects tokens using ASP.NET Core data protection.
    /// </summary>
    public class TokenProtector : ITokenProtector
    {
        private readonly IDataProtector _protector;

        /// <summary>
        /// Initializes a new instance of the <see cref="TokenProtector"/> class.
        /// </summary>
        /// <param name="provider">Data protection provider used to create a scoped protector.</param>
        public TokenProtector(IDataProtectionProvider provider)
        {
            // Purpose string isolates this protector to token usage
            _protector = provider.CreateProtector("mi-cuatri.GoogleAccountTokens.v1");
        }

        /// <summary>Protects (encrypts) the provided plaintext token. Returns empty string for null or empty input.</summary>
        public string Protect(string? plaintext)
        {
            if (string.IsNullOrEmpty(plaintext)) return string.Empty;
            return _protector.Protect(plaintext);
        }

        /// <summary>Attempts to unprotect (decrypt) the token; returns original input on failure.</summary>
        public string? Unprotect(string? protectedText)
        {
            if (string.IsNullOrEmpty(protectedText)) return protectedText;

            try
            {
                return _protector.Unprotect(protectedText);
            }
            catch (CryptographicException)
            {
                // If unprotect fails, return the original string so we remain backward compatible
                return protectedText;
            }
        }
    }
}