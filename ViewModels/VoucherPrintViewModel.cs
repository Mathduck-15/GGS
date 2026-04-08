using GoodGovernanceApp.Models;
using GoodGovernanceApp.Views;
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Controls;
using System.Windows.Input;

namespace GoodGovernanceApp.ViewModels
{
    internal class VoucherPrintViewModel : ViewModelBase
    {


        public TransactionRow Row { get; }

        public ICommand PrintCommand { get; }

        public VoucherPrintViewModel(TransactionRow row)
        {
            Row = row;
            PrintCommand = new RelayCommand(_ => Print());
        }

        private void Print()
        {
            PrintDialog printDialog = new PrintDialog();
            if (printDialog.ShowDialog() == true)
            {
                // Get the window and print it
                var window = System.Windows.Application.Current.Windows
                                 .OfType<VoucherPrintWindow>()
                                 .FirstOrDefault();

                if (window != null)
                    printDialog.PrintVisual(window, "Voucher");
            }
        }
    }
}
