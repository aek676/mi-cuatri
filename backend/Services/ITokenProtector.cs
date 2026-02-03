namespace backend.Services
{
    public interface ITokenProtector
    {
        string Protect(string? plaintext);
        string? Unprotect(string? protectedText);
    }
}