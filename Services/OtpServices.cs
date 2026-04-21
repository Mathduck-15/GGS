using System;
using System.Collections.Generic;
using System.Text;

namespace GoodGovernanceApp.Services
{
    public class OtpService
    {
        private string? _generatedOtp;
        private DateTime _otpExpiry;

        public string GenerateOtp()
        {
            var random = new Random();
            _generatedOtp = random.Next(100000, 999999).ToString();
            _otpExpiry = DateTime.UtcNow.AddMinutes(5);
            return _generatedOtp;
        }

        public bool VerifyOtp(string enteredOtp)
        {
            if (string.IsNullOrEmpty(_generatedOtp)) return false;
            if (DateTime.UtcNow > _otpExpiry) return false;

            bool isValid = _generatedOtp == enteredOtp.Trim();
            if (isValid) _generatedOtp = null; // one-time use
            return isValid;
        }
    }
}
