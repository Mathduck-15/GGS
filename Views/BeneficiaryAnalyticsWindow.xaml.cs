using GoodGovernanceApp.ViewModels;
using System.Windows;

namespace GoodGovernanceApp.Views
{
    public partial class BeneficiaryAnalyticsWindow : Window
    {
        public BeneficiaryAnalyticsWindow(BeneficiaryAnalyticsViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
        }
    }
}
