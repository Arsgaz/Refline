using System.Security.Cryptography;

namespace Refline.Api.Services.Auth;

public static class PasswordPolicy
{
    private const string AllowedChars = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz23456789!@$?";

    public static bool IsValid(string? password, out string? validationError)
    {
        if (string.IsNullOrWhiteSpace(password))
        {
            validationError = "NewPassword is required.";
            return false;
        }

        if (password.Length < 8)
        {
            validationError = "New password must be at least 8 characters long.";
            return false;
        }

        validationError = null;
        return true;
    }

    public static string GenerateTemporaryPassword(int length = 12)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(length, 8);

        Span<char> buffer = stackalloc char[length];
        for (var index = 0; index < buffer.Length; index++)
        {
            buffer[index] = AllowedChars[RandomNumberGenerator.GetInt32(AllowedChars.Length)];
        }

        return new string(buffer);
    }
}
