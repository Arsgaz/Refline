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
            Dispatcher.BeginInvoke(DispatcherPriority.Loaded, new Action(() =>
            {
                if (DataContext is MainViewModel mainViewModel)
                {
                    mainViewModel.RefreshReportData();
                }

                UpdateLayout();
                InvalidateVisual();
            }));
        }
    }
}
