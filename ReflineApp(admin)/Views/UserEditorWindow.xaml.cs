using System.Windows;
using Refline.Admin.Models;

namespace Refline.Admin.Views;

public partial class UserEditorWindow : Window
{
    private readonly bool _isCreateMode;
    private bool _isSubmitting;

    public UserEditorWindow(IReadOnlyList<ManagerOption> managerOptions, CompanyUserListItem? user)
    {
        InitializeComponent();

        _isCreateMode = user is null;
        Result = null;

        RoleComboBox.ItemsSource = new[]
        {
            new RoleOption(UserRole.Admin, "Администратор"),
            new RoleOption(UserRole.Manager, "Менеджер"),
            new RoleOption(UserRole.Employee, "Сотрудник")
        };

        ManagerComboBox.ItemsSource = managerOptions;

        if (_isCreateMode)
        {
            Title = "Добавить сотрудника";
            DialogTitleText.Text = "Добавление сотрудника";
            DialogSubtitleText.Text = "Создайте пользователя в компании. Пароль задается только на этом шаге.";
            RoleComboBox.SelectedValue = UserRole.Employee;
            ManagerComboBox.SelectedIndex = 0;
            return;
        }

        Title = "Редактировать сотрудника";
        DialogTitleText.Text = "Редактирование сотрудника";
        DialogSubtitleText.Text = "Изменяются только базовые учетные данные. Смена пароля вынесена из этого MVP.";

        PasswordPanel.Visibility = Visibility.Collapsed;
        FullNameTextBox.Text = user!.FullName;
        LoginTextBox.Text = user.Login;
        RoleComboBox.SelectedValue = user.Role;
        ManagerComboBox.SelectedValue = user.ManagerId;
    }

    public AdminUserEditorResult? Result { get; private set; }

    private void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        if (_isSubmitting)
        {
            return;
        }

        ValidationMessageText.Text = string.Empty;

        var fullName = FullNameTextBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(fullName))
        {
            ValidationMessageText.Text = "Укажите ФИО.";
            return;
        }

        var login = LoginTextBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(login))
        {
            ValidationMessageText.Text = "Укажите логин.";
            return;
        }

        if (RoleComboBox.SelectedValue is not UserRole role)
        {
            ValidationMessageText.Text = "Выберите роль.";
            return;
        }

        var password = PasswordTextBox.Password;
        if (_isCreateMode && string.IsNullOrWhiteSpace(password))
        {
            ValidationMessageText.Text = "Укажите пароль для нового пользователя.";
            return;
        }

        Result = new AdminUserEditorResult
        {
            FullName = fullName,
            Login = login,
            Password = password,
            Role = role,
            ManagerId = ManagerComboBox.SelectedValue is long selectedManagerId ? selectedManagerId : null
        };

        _isSubmitting = true;
        SaveButton.IsEnabled = false;
        CancelButton.IsEnabled = false;
        DialogResult = true;
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }

    private sealed record RoleOption(UserRole Role, string DisplayName);
}
