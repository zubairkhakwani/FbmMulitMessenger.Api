using FBMMultiMessenger.Buisness.Models;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;

namespace FBMMultiMessenger.Buisness.Service
{
    internal class EmailService : IEmailService
    {
        private readonly EmailSettings _emailSettings;

        public EmailService(IOptions<EmailSettings> emailSettings)
        {
            _emailSettings = emailSettings.Value;
        }

        public async Task<bool> SendPasswordResetEmailAsync(string toEmail, string otpCode, string userName = "")
        {
            var subject = "Reset Your Password - FBM Multi Messenger";
            var htmlBody = GetPasswordResetEmailTemplate(otpCode, userName);

            return await SendEmailAsync(toEmail, subject, htmlBody);
        }

        public async Task<bool> SendEmailAsync(string toEmail, string subject, string htmlBody)
        {
            try
            {
                var message = new MimeMessage();
                message.From.Add(new MailboxAddress(_emailSettings.SenderName, _emailSettings.SenderEmail));
                message.To.Add(new MailboxAddress("", toEmail));
                message.Subject = subject;

                var builder = new BodyBuilder
                {
                    HtmlBody = htmlBody
                };
                message.Body = builder.ToMessageBody();

                using var client = new SmtpClient();

                // Connect to SMTP server
                await client.ConnectAsync(_emailSettings.SmtpServer, _emailSettings.SmtpPort, SecureSocketOptions.StartTls);

                // Authenticate
                await client.AuthenticateAsync(_emailSettings.Username, _emailSettings.Password);

                // Send email
                await client.SendAsync(message);

                // Disconnect
                await client.DisconnectAsync(true);

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Email sending failed: {ex.Message}");
                return false;
            }
        }

        private string GetPasswordResetEmailTemplate(string otpCode, string userName)
        {
            var digits = otpCode.ToCharArray();
            return $@"
    <!DOCTYPE html>
    <html>
    <head>
        <style>
            body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; margin: 0; padding: 0; background: #f4f4f4; }}
            .container {{ max-width: 800px; margin: 40px auto; background: white; border-radius: 12px; overflow: hidden; box-shadow: 0 4px 15px rgba(0, 0, 0, 0.1); }}
            .header {{ background: linear-gradient(135deg, #0866FF 0%, #042B5C 100%); color: white; padding: 40px 30px; text-align: center; }}
            .icon {{ font-size: 50px; margin-bottom: 15px; }}
            .content {{ padding: 40px 30px; }}
            .otp-container {{ background: linear-gradient(135deg, #f7fafc 0%, #edf2f7 100%); border: 2px dashed #cbd5e0; border-radius: 12px; padding: 30px; margin: 30px 0; text-align: center; }}
            .otp-label {{ font-size: 14px; color: #718096; margin-bottom: 15px; text-transform: uppercase; letter-spacing: 1px; font-weight: 600; }}
            .otp-digits {{ display: flex; justify-content: center; gap: 10px; margin: 20px 0; }}
            .otp-digit {{ width: 60px; height: 70px; background: white; border: 2px solid #e2e8f0; border-radius: 10px; display: flex; align-items: center; justify-content: center; font-size: 32px; font-weight: 700; color: #0866FF; font-family: 'Courier New', monospace; box-shadow: 0 2px 6px rgba(0, 0, 0, 0.08); }}
            .expiry-notice {{ background: #fff5f5; border-left: 4px solid #fc8181; padding: 15px 20px; margin: 25px 0; border-radius: 6px; }}
            .info-box {{ background: #ebf8ff; border-left: 4px solid #4299e1; padding: 15px 20px; margin: 25px 0; border-radius: 6px; }}
            .security-tips {{ background: #f7fafc; padding: 20px; border-radius: 8px; margin: 25px 0; }}
            .security-tips h3 {{ margin: 0 0 15px 0; color: #2d3748; font-size: 16px; }}
            .security-tips ul {{ margin: 0; padding-left: 20px; color: #4a5568; font-size: 14px; }}
            .security-tips li {{ margin-bottom: 8px; }}
            .footer {{ background: #f7fafc; text-align: center; padding: 30px 20px; color: #718096; font-size: 13px; }}
            @media only screen and (max-width: 600px) {{
                .container {{ margin: 20px; }}
                .otp-digit {{ width: 45px; height: 55px; font-size: 24px; }}
                .otp-digits {{ gap: 6px; }}
            }}
        </style>
    </head>
    <body>
        <div class='container'>
            <div class='header'>
                <div class='icon'>🔐</div>
                <h1>Password Reset Code</h1>
                <p>Your verification code is ready</p>
            </div>
            <div class='content'>
                <div style='font-size: 16px; margin-bottom: 20px; color: #2d3748;'>
                    Hello, <strong>{userName}</strong>
                </div>
                <div style='font-size: 15px; color: #4a5568; margin-bottom: 30px; line-height: 1.8;'>
                    <p>We received a request to reset your password for your <strong>FBM Multi Messenger</strong> account.</p>
                    <p>Use the verification code below to complete your password reset:</p>
                </div>
                
                <div class='otp-container'>
                    <div class='otp-label'>Your Verification Code</div>
                   <div class=""otp-digits"" style=""text-align:center; margin: 20px 0;"">
  <table role=""presentation"" cellspacing=""10"" cellpadding=""0"" border=""0"" align=""center"">
    <tr>
      <td style=""background:#fff; border:2px solid #e2e8f0; border-radius:10px; width:60px; height:70px; text-align:center; font-size:32px; font-weight:700; color:#0866ff; font-family:'Courier New', monospace;"">{digits[0]}</td>
      <td style=""background:#fff; border:2px solid #e2e8f0; border-radius:10px; width:60px; height:70px; text-align:center; font-size:32px; font-weight:700; color:#0866ff; font-family:'Courier New', monospace;"">{digits[1]}</td>
      <td style=""background:#fff; border:2px solid #e2e8f0; border-radius:10px; width:60px; height:70px; text-align:center; font-size:32px; font-weight:700; color:#0866ff; font-family:'Courier New', monospace;"">{digits[2]}</td>
      <td style=""background:#fff; border:2px solid #e2e8f0; border-radius:10px; width:60px; height:70px; text-align:center; font-size:32px; font-weight:700; color:#0866ff; font-family:'Courier New', monospace;"">{digits[3]}</td>
      <td style=""background:#fff; border:2px solid #e2e8f0; border-radius:10px; width:60px; height:70px; text-align:center; font-size:32px; font-weight:700; color:#0866ff; font-family:'Courier New', monospace;"">{digits[4]}</td>
      <td style=""background:#fff; border:2px solid #e2e8f0; border-radius:10px; width:60px; height:70px; text-align:center; font-size:32px; font-weight:700; color:#0866ff; font-family:'Courier New', monospace;"">{digits[5]}</td>
    </tr>
  </table>
</div>
                </div>
                
                <div class='expiry-notice'>
                    <strong style='color: #c53030;'>⏰ Important:</strong>
                    <p style='margin: 5px 0 0 0; color: #742a2a; font-size: 14px;'>This code will expire in <strong>30 minutes</strong>. Please use it immediately.</p>
                </div>
                
                <div class='info-box'>
                    <p style='margin: 0; color: #2c5282; font-size: 14px;'>💡 <strong>Tip:</strong> Enter this code on the password reset page to set your new password.</p>
                </div>
                
                <div class='security-tips'>
                    <h3>🛡️ Security Tips:</h3>
                    <ul>
                        <li>Never share this code with anyone</li>
                        <li>Our team will never ask you for this code</li>
                        <li>If you didn't request this, please ignore this email</li>
                    </ul>
                </div>
                
                <div style='font-size: 15px; color: #4a5568; margin-bottom: 30px; line-height: 1.8;'>
                    <p>If you didn't request a password reset, please ignore this email or contact our support team if you have concerns about your account security.</p>
                    <p style='margin-top: 20px;'>Best regards,<br><strong>FBM Multi Messenger Team</strong></p>
                </div>
                
                <div style='text-align: center; margin-top: 20px;'>
                    <a href='#' style='color: #0866FF; text-decoration: none; font-weight: 500;'>Need help? Contact Support</a>
                </div>
            </div>
            
            <div class='footer'>
                <p><strong>© {DateTime.Now.Year} FBM Multi Messenger. All rights reserved.</strong></p>
                <p>This is an automated email. Please do not reply to this message.</p>
                <p><a href='#' style='color: #0866FF; text-decoration: none;'>Privacy Policy</a> | <a href='#' style='color: #0866FF; text-decoration: none;'>Terms of Service</a> | <a href='#' style='color: #0866FF; text-decoration: none;'>Contact Us</a></p>
            </div>
        </div>
    </body>
    </html>";

        }
    }
}
