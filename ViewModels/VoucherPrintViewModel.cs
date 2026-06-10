using GoodGovernanceApp.Models;
using GoodGovernanceApp.Views;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using PdfSharp.Pdf;
using PdfSharp.Drawing;

namespace GoodGovernanceApp.ViewModels
{
    internal class VoucherPrintViewModel : ViewModelBase
    {
        public TransactionRow Row { get; }

        public ICommand PrintCommand { get; }
        public ICommand DownloadPdfCommand { get; }  // ? New command

        public VoucherPrintViewModel(TransactionRow row)
        {
            Row = row;
            PrintCommand = new RelayCommand(_ => Print());
            DownloadPdfCommand = new RelayCommand(_ => DownloadPdf());  // ? New
        }

        private void Print()
        {
            PrintDialog printDialog = new PrintDialog();
            if (printDialog.ShowDialog() == true)
            {
                var window = System.Windows.Application.Current.Windows
                                 .OfType<VoucherPrintWindow>()
                                 .FirstOrDefault();

                if (window != null)
                    printDialog.PrintVisual(window, "Voucher");
            }
        }

        // ? New PDF download method
        private void DownloadPdf()
        {
            try
            {
                // 1. Ask user where to save
                var saveDialog = new SaveFileDialog
                {
                    Title = "Save Voucher as PDF",
                    Filter = "PDF Files (*.pdf)|*.pdf",
                    FileName = $"Voucher_{Row.VoucherCode}_{DateTime.Now:yyyyMMdd}.pdf"
                };

                if (saveDialog.ShowDialog() != true) return;

                // 2. Find the voucher window
                var window = System.Windows.Application.Current.Windows
                                 .OfType<VoucherPrintWindow>()
                                 .FirstOrDefault();

                if (window == null)
                {
                    MessageBox.Show("Voucher window not found.", "Error",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // 3. Render the window to a bitmap image
                window.Measure(new Size(window.ActualWidth, window.ActualHeight));
                window.Arrange(new Rect(new Size(window.ActualWidth, window.ActualHeight)));

                var renderBitmap = new RenderTargetBitmap(
                    (int)window.ActualWidth,
                    (int)window.ActualHeight,
                    96d, 96d,
                    PixelFormats.Pbgra32);

                renderBitmap.Render(window);

                // 4. Save bitmap to a temp PNG file
                string tempImagePath = Path.Combine(Path.GetTempPath(), $"voucher_temp_{Guid.NewGuid()}.png");
                using (var fileStream = new FileStream(tempImagePath, FileMode.Create))
                {
                    var encoder = new PngBitmapEncoder();
                    encoder.Frames.Add(BitmapFrame.Create(renderBitmap));
                    encoder.Save(fileStream);
                }

                // 5. Create PDF and add the image
                var pdf = new PdfDocument();
                pdf.Info.Title = $"Voucher {Row.VoucherCode}";

                var page = pdf.AddPage();
                page.Width = XUnit.FromPoint(window.ActualWidth);
                page.Height = XUnit.FromPoint(window.ActualHeight);

                using (var gfx = XGraphics.FromPdfPage(page))
                {
                    var image = XImage.FromFile(tempImagePath);
                    gfx.DrawImage(image, 0, 0, page.Width, page.Height);
                }

                // 6. Save the PDF
                pdf.Save(saveDialog.FileName);

                // 7. Clean up temp file
                File.Delete(tempImagePath);

                MessageBox.Show($"Voucher saved successfully!\n\n{saveDialog.FileName}",
                    "PDF Saved", MessageBoxButton.OK, MessageBoxImage.Information);

                // 8. Optional — open the PDF automatically
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = saveDialog.FileName,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to save PDF:\n{ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
