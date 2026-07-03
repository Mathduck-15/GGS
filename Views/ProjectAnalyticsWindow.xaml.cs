using System.Windows;
using GoodGovernanceApp.ViewModels;

namespace GoodGovernanceApp.Views
{
    public partial class ProjectAnalyticsWindow : Window
    {
        public ProjectAnalyticsWindow(ProjectAnalyticsViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
        }
    }
}
