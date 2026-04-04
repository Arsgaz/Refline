using System.Security.Cryptography;
using System.Text;

namespace Refline.Api.Security;

public static class PasswordHashHelper
{
    public static string ComputeHash(string password)
    {
        var normalizedPassword = password ?? string.Empty;
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(normalizedPassword));

        return Convert.ToHexString(bytes);
    }

    public static bool Verify(string password, string passwordHash)
    {
        return string.Equals(
            ComputeHash(password),
            passwordHash ?? string.Empty,
            StringComparison.OrdinalIgnoreCase);
    }
}
