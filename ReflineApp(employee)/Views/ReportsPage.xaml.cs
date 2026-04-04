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

            var fade = new DoubleAnimation(0, 1, new Duration(TimeSpan.FromMilliseconds(350)));
            ReportArea.BeginAnimation(UIElement.OpacityProperty, fade);

            ScheduleChartsRefresh();
        }

        private void ReportsPage_Loaded(object sender, RoutedEventArgs e)
        {
            if (ReportArea.Visibility == Visibility.Visible)
            {
                ScheduleChartsRefresh();
            }
        }

        private void ScheduleChartsRefresh()
        {
            Dispatcher.BeginInvoke(DispatcherPriority.Loaded, new Action(() =>
            {
                RefreshChartsAfterLayout();
            }));
        }

        private void RefreshChartsAfterLayout()
        {
            if (DataContext is MainViewModel mainViewModel)
            {
                mainViewModel.RefreshReportData();
            }

            ReportArea.UpdateLayout();
            CategoryPieChart.UpdateLayout();
            TopApplicationsChart.UpdateLayout();

            CategoryPieChart.InvalidateMeasure();
            CategoryPieChart.InvalidateArrange();
            CategoryPieChart.InvalidateVisual();

            TopApplicationsChart.InvalidateMeasure();
            TopApplicationsChart.InvalidateArrange();
            TopApplicationsChart.InvalidateVisual();
        }
    }
}