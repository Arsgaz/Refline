using System.Windows;
using Refline.Admin.ViewModels;

namespace Refline.Admin.Views;

public partial class ChangePasswordWindow : Window
{
    private readonly ChangePasswordViewModel _viewModel;

    public ChangePasswordWindow(ChangePasswordViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        _viewModel.PasswordChangedSuccessfully += OnPasswordChangedSuccessfully;
        _viewModel.CancelRequested += OnCancelRequested;
        DataContext = _viewModel;
    }

    private void CurrentPasswordBox_OnPasswordChanged(object sender, RoutedEventArgs e)
    {
        _viewModel.CurrentPassword = CurrentPasswordBox.Password;
    }

    private void NewPasswordBox_OnPasswordChanged(object sender, RoutedEventArgs e)
    {
        _viewModel.NewPassword = NewPasswordBox.Password;
    }

    private void ConfirmPasswordBox_OnPasswordChanged(object sender, RoutedEventArgs e)
    {
        _viewModel.ConfirmPassword = ConfirmPasswordBox.Password;
    }

    private void OnPasswordChangedSuccessfully()
    {
        DialogResult = true;
        Close();
    }

    private void OnCancelRequested()
    {
        DialogResult = false;
        Close();
    }

    protected override void OnClosed(EventArgs e)
    {
        _viewModel.PasswordChangedSuccessfully -= OnPasswordChangedSuccessfully;
        _viewModel.CancelRequested -= OnCancelRequested;
        base.OnClosed(e);
    }
}
