using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;
using Refline.ViewModels;

namespace Refline.Views
{
    public partial class ReportsPage : Page
    {
        public ReportsPage()
        {
            InitializeComponent();
        }

        private void ShowReport_Click(object sender, RoutedEventArgs e)
        {
            ShowReportButton.Visibility = Visibility.Collapsed;
            ReportArea.Visibility = Visibility.Visible;

            if (DataContext is MainViewModel mainViewModel)
            {
                mainViewModel.RefreshReportData();
            }

            ReportArea.UpdateLayout();
            CategoryPieChart.UpdateLayout();
            TopApplicationsChart.UpdateLayout();

            var fade = new DoubleAnimation(0, 1, new Duration(TimeSpan.FromMilliseconds(350)));
            ReportArea.BeginAnimation(UIElement.OpacityProperty, fade);
        }
    }
}
