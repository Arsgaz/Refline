using System;
using System.Windows.Controls;
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

        private void ReportsPage_Loaded(object sender, System.Windows.RoutedEventArgs e)
        {
            // Первый тик: дать WPF завершить Layout pass страницы
            Dispatcher.BeginInvoke(DispatcherPriority.Loaded, new Action(() =>
            {
                if (DataContext is MainViewModel mainViewModel)
                {
                    mainViewModel.RefreshReportData();
                }

                // Второй тик: принудительно заставить LiveCharts пересчитать размеры
                // после того как данные уже установлены
                Dispatcher.BeginInvoke(DispatcherPriority.Render, new Action(() =>
                {
                    DailyTrendChart.InvalidateMeasure();
                    DailyTrendChart.InvalidateVisual();
                    DailyTrendChart.UpdateLayout();
                }));
            }));
        }
    }
}
