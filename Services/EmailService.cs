using System;
using System.Collections.Generic;
using System.Text;
using MailKitSimplified.Sender.Services;
using System.Threading.Tasks;

namespace GoodGovernanceApp.Services
{
    public class EmailService
    {
        public async Task SendOtpAsync(string toEmail, string otp, string context = "Backup")
        {
            var config = GoodGovernanceApp.Utilities.ConfigHelper.ReadConfig("SmtpConfig.txt");
            string gmailAddress = config.GetValueOrDefault("EmailAddress", "bryanluy822@gmail.com");
            string gmailAppPassword = config.GetValueOrDefault("AppPassword", "nxwtwytteuffhhjp");

            string subject = context == "Login" ? "GGMS — Login Verification Code" : "GGMS — Backup Verification OTP";
            string title = context == "Login" ? "GGMS Login Verification" : "GGMS Backup Verification";
            string desc = context == "Login" ? "A login attempt requires verification. Use the code below to proceed:" 
                                             : "A backup action was requested. Use the code below to confirm:";

            using var sender = SmtpSender.Create("smtp.gmail.com:587")
                .SetCredential(gmailAddress, gmailAppPassword);

            await sender.WriteEmail
                .From(gmailAddress)
                .To(toEmail)
                .Subject(subject)
                .BodyHtml($@"
                    <div style='font-family:sans-serif;max-width:480px;margin:auto'>
                        <h2 style='color:#1976D2'>{title}</h2>
                        <p>{desc}</p>
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
