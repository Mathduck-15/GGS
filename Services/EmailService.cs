using System;
using System.Collections.Generic;
using System.Text;
using MailKitSimplified.Sender.Services;
using System.Threading.Tasks;

namespace GoodGovernanceApp.Services
{
    public class EmailService
    {
        private const string GmailAddress = "bryanluy822@gmail.com";  // 🔁 Replace
        private const string GmailAppPassword = "nxwtwytteuffhhjp";     // 🔁 Replace 16-char

        public async Task SendOtpAsync(string toEmail, string otp)
        {
            using var sender = SmtpSender.Create("smtp.gmail.com:587")
                .SetCredential(GmailAddress, GmailAppPassword);

            await sender.WriteEmail
                .From(GmailAddress)
                .To(toEmail)
                .Subject("GGMS — Backup Verification OTP")
                .BodyHtml($@"
                    <div style='font-family:sans-serif;max-width:480px;margin:auto'>
                        <h2 style='color:#1976D2'>GGMS Backup Verification</h2>
                        <p>A backup action was requested. Use the code below to confirm:</p>
                        <h1 style='letter-spacing:10px;color:#1976D2'>{otp}</h1>
                        <p>This code expires in <strong>5 minutes</strong>.</p>
                        <p style='color:gray;font-size:12px'>
                            If you did not request this, please contact your administrator.
                        </p>
                    </div>")
                .SendAsync();
        }
    }
}
