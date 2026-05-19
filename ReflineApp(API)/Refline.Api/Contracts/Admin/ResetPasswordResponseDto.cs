namespace Refline.Api.Contracts.Admin;

public sealed record ResetPasswordResponseDto(
    long UserId,
    string Login,
    string TemporaryPassword,
    bool MustChangePassword);
