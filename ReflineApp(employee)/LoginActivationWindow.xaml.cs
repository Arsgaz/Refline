using System.Windows;
using Refline.ViewModels;

namespace Refline;

public partial class LoginActivationWindow : Window
{
    private readonly LoginActivationViewModel _viewModel;

    public LoginActivationWindow(LoginActivationViewModel viewModel)
    {
        InitializeComponent();

        _viewModel = viewModel;
        DataContext = _viewModel;

        _viewModel.LoginSucceeded += OnLoginSucceeded;
        Closed += OnClosed;
    }

    private void PasswordInput_PasswordChanged(object sender, RoutedEventArgs e)
    {
        _viewModel.Password = PasswordInput.Password;
    }

    private void OnLoginSucceeded()
    {
        DialogResult = true;
        Close();
    }

    private void OnClosed(object? sender, EventArgs e)
    {
        _viewModel.LoginSucceeded -= OnLoginSucceeded;
        Closed -= OnClosed;
    }
}
