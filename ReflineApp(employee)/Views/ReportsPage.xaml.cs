using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using Refline.ViewModels;

namespace Refline.Views
{
    public partial class ReportsPage : Page
    {
        public ReportsPage()
        {
            InitializeComponent();
            Loaded += ReportsPage_Loaded;
        }

        private void ShowReport_Click(object sender, RoutedEventArgs e)
        {
            ShowReportButton.Visibility = Visibility.Collapsed;
            ReportArea.Visibility = Visibility.Visible;
            RefreshChartsAfterLayout();

            var fade = new DoubleAnimation(0, 1, new Duration(TimeSpan.FromMilliseconds(350)));
            ReportArea.BeginAnimation(UIElement.OpacityProperty, fade);
        }

        private void ReportsPage_Loaded(object sender, RoutedEventArgs e)
        {
            if (ReportArea.Visibility == Visibility.Visible)
            {
                RefreshChartsAfterLayout();
            }
        }

        private void RefreshChartsAfterLayout()
        {
            ReportArea.UpdateLayout();

            if (DataContext is MainViewModel mainViewModel)
            {
                mainViewModel.RefreshReportData();
            }

            Dispatcher.BeginInvoke(
                DispatcherPriority.Render,
                new Action(() =>
                {
                    CategoryPieChart.UpdateLayout();
                    TopApplicationsChart.UpdateLayout();
                    CategoryPieChart.InvalidateVisual();
                    TopApplicationsChart.InvalidateVisual();
                }));
        }
    }
}
