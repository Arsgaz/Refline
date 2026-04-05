namespace Refline.Admin.Models;

public sealed class CompanyUserListItem
{
    public long Id { get; init; }

    public long CompanyId { get; init; }

    public string FullName { get; init; } = string.Empty;

    public string Login { get; init; } = string.Empty;

    public UserRole Role { get; init; }

    public long? ManagerId { get; init; }

    public bool IsActive { get; init; }

    public DateTimeOffset CreatedAt { get; init; }

    public string RoleDisplay => Role switch
    {
        UserRole.Admin => "Администратор",
        UserRole.Manager => "Менеджер",
        _ => "Сотрудник"
    };

    public string StatusDisplay => IsActive ? "Активен" : "Отключен";

    public string ActivationActionDisplay => IsActive ? "Деактивировать" : "Активировать";

    public string CreatedAtDisplay => CreatedAt.LocalDateTime.ToString("dd.MM.yyyy HH:mm");
}
