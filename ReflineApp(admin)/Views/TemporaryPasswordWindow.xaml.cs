using System.Windows;

namespace Refline.Admin.Views;

public partial class TemporaryPasswordWindow : Window
{
    private readonly string _temporaryPassword;

    public TemporaryPasswordWindow(string title, string subtitle, string login, string temporaryPassword)
    {
        InitializeComponent();
        _temporaryPassword = temporaryPassword;
        DialogTitleText.Text = title;
        DialogSubtitleText.Text = subtitle;
        LoginValueText.Text = login;
        TemporaryPasswordTextBox.Text = temporaryPassword;
    }

    private void CopyButton_Click(object sender, RoutedEventArgs e)
    {
        Clipboard.SetText(_temporaryPassword);
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
        Close();
    }
}
