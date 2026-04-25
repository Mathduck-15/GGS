using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using GoodGovernanceApp.Services;
using System.Windows;

namespace GoodGovernanceApp.Views
{
    /// <summary>
    /// Interaction logic for OtpVerificationWindow.xaml
    /// </summary>

        public partial class OtpVerificationWindow : Window
        {
            private readonly OtpService _otpService;
            private readonly EmailService _emailService;
            private readonly string _userEmail;
            private readonly string _actionContext;

            public bool IsVerified { get; private set; } = false;

            public OtpVerificationWindow(string userEmail, string actionContext = "Backup")
            {
                InitializeComponent();
                _otpService = new OtpService();
                _emailService = new EmailService();
                _userEmail = userEmail;
                _actionContext = actionContext;

                TitleText.Text = actionContext == "Login" ? "Login Verification" : "Backup Verification";
                SubtitleText.Text = $"A verification code will be sent to:\n{userEmail}";
                SendOtp();
            }

            private async void SendOtp()
            {
            StatusText.Foreground = System.Windows.Media.Brushes.Gray;
            StatusText.Text = "⏳ Sending OTP to your email...";

            string otp = _otpService.GenerateOtp();

            try
            {
                await _emailService.SendOtpAsync(_userEmail, otp, _actionContext);
                StatusText.Text = "✅ OTP sent! Check your email.";
            }
            catch (Exception ex)
            {
                StatusText.Foreground = System.Windows.Media.Brushes.Red;
                // Show full error details
                StatusText.Text = $"❌ {ex.GetType().Name}: {ex.Message}";

                // Also show inner exception if there is one
                if (ex.InnerException != null)
                    StatusText.Text += $"\n↳ {ex.InnerException.Message}";
            }
        }

            private void VerifyButton_Click(object sender, RoutedEventArgs e)
            {
                string entered = OtpInput.Text.Trim();

                if (string.IsNullOrWhiteSpace(entered))
                {
                    StatusText.Foreground = System.Windows.Media.Brushes.Orange;
                    StatusText.Text = "⚠ Please enter the OTP code.";
                    return;
                }

                if (_otpService.VerifyOtp(entered))
                {
                    IsVerified = true;
                    this.Close();
                }
                else
                {
                    StatusText.Foreground = System.Windows.Media.Brushes.Red;
                    StatusText.Text = "❌ Invalid or expired OTP. Try again or resend.";
                    OtpInput.Clear();
                }
            }

            private void ResendButton_Click(object sender, RoutedEventArgs e)
            {
                OtpInput.Clear();
                SendOtp();
            }
        }
    }

