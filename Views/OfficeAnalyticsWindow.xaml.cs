using System.Windows;
using GoodGovernanceApp.ViewModels;

namespace GoodGovernanceApp.Views
{
    public partial class OfficeAnalyticsWindow : Window
    {
        public OfficeAnalyticsWindow(OfficeAnalyticsViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
        }
    }
}
