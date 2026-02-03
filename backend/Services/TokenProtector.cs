using Microsoft.AspNetCore.DataProtection;
using System.Security.Cryptography;

namespace backend.Services
{
    public class TokenProtector : ITokenProtector
    {
        private readonly IDataProtector _protector;

        public TokenProtector(IDataProtectionProvider provider)
        {
            // Purpose string isolates this protector to token usage
            _protector = provider.CreateProtector("mi-cuatri.GoogleAccountTokens.v1");
        }

        public string Protect(string? plaintext)
        {
            if (string.IsNullOrEmpty(plaintext)) return string.Empty;
            return _protector.Protect(plaintext);
        }

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