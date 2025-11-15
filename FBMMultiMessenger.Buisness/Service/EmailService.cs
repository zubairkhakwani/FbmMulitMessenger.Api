using FBMMultiMessenger.Buisness.Helpers;
using FBMMultiMessenger.Buisness.Models;
using FBMMultiMessenger.Buisness.Service.IServices;
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

        public async Task<bool> SendEmailVerificationEmailAsync(string toEmail, string otp, string userName = "")
        {
            var subject = "Verify Your Email - FBM Multi Messenger";
            var htmlBody = GetEmailVerificationEmailTemplate(otp, userName);

            return await SendEmailAsync(toEmail, subject, htmlBody);
        }

        public async Task<bool> SendWelcomeEmailAsync(string toEmail, string userName)
        {
            var subject = "Welcome to FBM Multi Messenger! 🎉";
            var htmlBody = GetWelcomeEmailTemplate(userName, toEmail);

            return await SendEmailAsync(toEmail, subject, htmlBody);
        }

        private async Task<bool> SendEmailAsync(string toEmail, string subject, string htmlBody)
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
            .otp-container {{ background: linear-gradient(135deg, #f7fafc 0%, #edf2f7 100%); border: 2px dashed #cbd5e0; border-radius: 12px; padding: 15px; margin: 30px 0; text-align: center; }}
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
                .container {{ width:100%; }}
                .header {{ padding:15px; }}
                .content {{ padding: 20px 15px; }}
                .h1 {{ font-size:18px; }}
                .otp-digit {{ width: 45px; height: 55px; font-size: 18px; }}
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
                    <p style='margin: 5px 0 0 0; color: #742a2a; font-size: 14px;'>This code will expire in <strong>{OtpManager.PasswordExpiryDuration} minutes</strong>. Please use it immediately.</p>
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

        private string GetEmailVerificationEmailTemplate(string otpCode, string userName)
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
        .otp-container {{ background: linear-gradient(135deg, #f7fafc 0%, #edf2f7 100%); border: 2px dashed #cbd5e0; border-radius: 12px; padding: 15px; margin: 30px 0; text-align: center; }}
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
            .container {{ width:100%; }}
            .header {{ padding:15px; }}
            .content {{ padding: 20px 15px; }}
            .h1 {{ font-size:18px; }}
            .otp-digit {{ width: 45px; height: 55px; font-size: 18px; }}
            .otp-digits {{ gap: 6px; }}
        }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <div class='icon'>✉️</div>
            <h1>Verify Your Email</h1>
            <p>One more step to activate your account</p>
        </div>
        <div class='content'>
            <div style='font-size: 16px; margin-bottom: 20px; color: #2d3748;'>
                Hello, <strong>{userName}</strong>
            </div>
            <div style='font-size: 15px; color: #4a5568; margin-bottom: 30px; line-height: 1.8;'>
                <p>Thank you for signing up for <strong>FBM Multi Messenger</strong>!</p>
                <p>To complete your registration and activate your account, please verify your email address using the code below:</p>
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
                <p style='margin: 5px 0 0 0; color: #742a2a; font-size: 14px;'>This code will expire in <strong>{OtpManager.EmailExpiryDuration} minutes</strong>. Please verify your email promptly.</p>
            </div>
            
            <div class='info-box'>
                <p style='margin: 0; color: #2c5282; font-size: 14px;'>💡 <strong>Tip:</strong> Enter this code on the verification page to activate your account and start using FBM Multi Messenger.</p>
            </div>
            
            <div class='security-tips'>
                <h3>🛡️ Security Tips:</h3>
                <ul>
                    <li>Never share this code with anyone</li>
                    <li>Our team will never ask you for this code</li>
                    <li>If you didn't create an account, please ignore this email</li>
                </ul>
            </div>
            
            <div style='font-size: 15px; color: #4a5568; margin-bottom: 30px; line-height: 1.8;'>
                <p>If you didn't sign up for FBM Multi Messenger, please ignore this email. Your email address will not be used without verification.</p>
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

        private string GetWelcomeEmailTemplate(string userName, string userEmail)
        {
            return $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; margin: 0; padding: 0; background: #f4f4f4; }}
        .container {{ max-width: 800px; margin: 40px auto; background: white; border-radius: 12px; overflow: hidden; box-shadow: 0 4px 15px rgba(0, 0, 0, 0.1); }}
        .header {{ background: linear-gradient(135deg, #0866FF 0%, #042B5C 100%); color: white; padding: 50px 30px; text-align: center; position: relative; }}
        .header::before {{ content: '✨'; position: absolute; top: 20px; left: 30px; font-size: 30px; animation: sparkle 2s infinite; }}
        .header::after {{ content: '✨'; position: absolute; top: 20px; right: 30px; font-size: 30px; animation: sparkle 2s infinite 1s; }}
        @keyframes sparkle {{ 0%, 100% {{ opacity: 0.3; }} 50% {{ opacity: 1; }} }}
        .welcome-icon {{ font-size: 80px; margin-bottom: 20px; }}
        .content {{ padding: 40px 30px; }}
        .welcome-message {{ text-align: center; margin: 30px 0; }}
        .welcome-message h2 {{ color: #0866FF; font-size: 28px; margin-bottom: 10px; }}
        .welcome-message p {{ color: #4a5568; font-size: 16px; line-height: 1.8; }}
        .feature-grid {{ display: grid; grid-template-columns: repeat(2, 1fr); gap: 20px; margin: 40px 0; }}
        .feature-card {{ background: linear-gradient(135deg, #f7fafc 0%, #edf2f7 100%); padding: 25px; border-radius: 12px; text-align: center; border: 2px solid #e2e8f0; }}
        .feature-icon {{ font-size: 40px; margin-bottom: 10px; }}
        .feature-card h3 {{ color: #2d3748; font-size: 16px; margin: 10px 0; }}
        .feature-card p {{ color: #718096; font-size: 14px; margin: 0; }}
        .cta-section {{ background: linear-gradient(135deg, #ebf8ff 0%, #e6fffa 100%); padding: 30px; border-radius: 12px; text-align: center; margin: 30px 0; }}
        .cta-button {{ display: inline-block; background: linear-gradient(135deg, #0866FF 0%, #042B5C 100%); color: white; padding: 15px 40px; border-radius: 8px; text-decoration: none; font-weight: 600; font-size: 16px; margin: 15px 0; box-shadow: 0 4px 12px rgba(8, 102, 255, 0.3); }}
        .user-info-box {{ background: #f7fafc; padding: 20px; border-radius: 8px; border-left: 4px solid #0866FF; margin: 25px 0; }}
        .social-links {{ text-align: center; margin: 30px 0; }}
        .social-links a {{ display: inline-block; margin: 0 10px; color: #0866FF; text-decoration: none; font-size: 14px; }}
        .tips-section {{ background: #fffaf0; border: 2px dashed #fbd38d; border-radius: 12px; padding: 25px; margin: 30px 0; }}
        .tips-section h3 {{ color: #744210; margin: 0 0 15px 0; font-size: 18px; }}
        .tips-section ul {{ margin: 0; padding-left: 20px; color: #975a16; }}
        .tips-section li {{ margin-bottom: 10px; font-size: 14px; }}
        .footer {{ background: #f7fafc; text-align: center; padding: 30px 20px; color: #718096; font-size: 13px; }}
        @media only screen and (max-width: 600px) {{
            .container {{ width: 100%; margin: 0; }}
            .header {{ padding: 30px 15px; }}
            .content {{ padding: 20px 15px; }}
            .feature-grid {{ grid-template-columns: 1fr; gap: 15px; }}
            .welcome-icon {{ font-size: 60px; }}
            .welcome-message h2 {{ font-size: 22px; }}
            .cta-button {{ padding: 12px 30px; font-size: 14px; }}
        }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <div class='welcome-icon'>🎉</div>
            <h1 style='margin: 0; font-size: 32px;'>Welcome to FBM Multi Messenger!</h1>
            <p style='margin: 10px 0 0 0; font-size: 16px; opacity: 0.95;'>Your journey to seamless communication starts here</p>
        </div>
        
        <div class='content'>
            <div class='welcome-message'>
                <h2>Hello, {userName}! 👋</h2>
                <p>We're thrilled to have you join our community! Your account has been successfully created, and you're all set to explore the amazing features of FBM Multi Messenger.</p>
            </div>
            
            <div class='user-info-box'>
                <p style='margin: 0 0 10px 0; color: #2d3748; font-weight: 600;'>📧 Account Details:</p>
                <p style='margin: 5px 0; color: #4a5568;'><strong>Name:</strong> {userName}</p>
                <p style='margin: 5px 0; color: #4a5568;'><strong>Email:</strong> {userEmail}</p>
                <p style='margin: 15px 0 0 0; color: #718096; font-size: 13px;'>Keep this information safe and secure.</p>
            </div>
            
            <div style='text-align: center; margin: 30px 0;'>
                <h3 style='color: #2d3748; font-size: 22px; margin-bottom: 20px;'>✨ What You Can Do Now</h3>
            </div>
            
            <div class='feature-grid'>
                <div class='feature-card'>
                    <div class='feature-icon'>💬</div>
                    <h3>Multi-Platform Messaging</h3>
                    <p>Connect all your messaging accounts in one place</p>
                </div>
                <div class='feature-card'>
                    <div class='feature-icon'>🔔</div>
                    <h3>Smart Notifications</h3>
                    <p>Never miss an important message again</p>
                </div>
                <div class='feature-card'>
                    <div class='feature-icon'>🎨</div>
                    <h3>Customizable Interface</h3>
                    <p>Personalize your messaging experience</p>
                </div>
                <div class='feature-card'>
                    <div class='feature-icon'>🔒</div>
                    <h3>Secure & Private</h3>
                    <p>Your conversations are encrypted and safe</p>
                </div>
            </div>
            
            <div class='cta-section'>
                <h3 style='color: #2d3748; margin: 0 0 10px 0; font-size: 20px;'>Ready to Get Started?</h3>
                <p style='color: #4a5568; margin: 0 0 20px 0;'>Launch the app and start connecting with your contacts!</p>
                <a href='#' class='cta-button'>Open FBM Multi Messenger</a>
            </div>
            
            <div class='tips-section'>
                <h3>💡 Quick Tips to Get Started:</h3>
                <ul>
                    <li><strong>Complete your profile:</strong> Add a photo and bio to personalize your account</li>
                    <li><strong>Connect accounts:</strong> Link your Facebook, Instagram, and other messaging platforms</li>
                    <li><strong>Customize settings:</strong> Set up notifications and preferences the way you like them</li>
                    <li><strong>Explore features:</strong> Check out our tutorials section to make the most of FBM</li>
                </ul>
            </div>
            
            <div style='background: #ebf8ff; border-left: 4px solid #4299e1; padding: 20px; border-radius: 6px; margin: 25px 0;'>
                <p style='margin: 0; color: #2c5282; font-size: 14px;'>
                    <strong>🎁 Special Welcome Bonus:</strong> As a new member, you get premium features free for 30 days! Enjoy unlimited messaging and advanced customization options.
                </p>
            </div>
            
            <div style='text-align: center; margin: 30px 0; padding: 20px; background: #f7fafc; border-radius: 8px;'>
                <p style='color: #4a5568; margin: 0 0 15px 0; font-size: 15px;'>Need help getting started?</p>
                <div style='margin: 15px 0;'>
                    <a href='#' style='color: #0866FF; text-decoration: none; font-weight: 600; margin: 0 15px;'>📚 Help Center</a>
                    <a href='#' style='color: #0866FF; text-decoration: none; font-weight: 600; margin: 0 15px;'>💬 Contact Support</a>
                    <a href='#' style='color: #0866FF; text-decoration: none; font-weight: 600; margin: 0 15px;'>🎥 Video Tutorials</a>
                </div>
            </div>
            
            <div style='text-align: center; margin: 30px 0;'>
                <p style='color: #4a5568; font-size: 15px; line-height: 1.8;'>
                    Thank you for choosing FBM Multi Messenger. We're committed to making your messaging experience seamless and enjoyable.
                </p>
                <p style='margin-top: 20px; color: #2d3748;'>
                    <strong>Welcome aboard! 🚀</strong><br>
                    <span style='color: #0866FF; font-weight: 600;'>The FBM Multi Messenger Team</span>
                </p>
            </div>
            
            <div class='social-links'>
                <p style='color: #718096; margin-bottom: 15px;'>Follow us for updates and tips:</p>
                <a href='#'>Facebook</a> • 
                <a href='#'>Twitter</a> • 
                <a href='#'>Instagram</a> • 
                <a href='#'>YouTube</a>
            </div>
        </div>
        
        <div class='footer'>
            <p><strong>© {DateTime.Now.Year} FBM Multi Messenger. All rights reserved.</strong></p>
            <p>You're receiving this email because you created an account with us.</p>
            <p style='margin-top: 15px;'>
                <a href='#' style='color: #0866FF; text-decoration: none;'>Privacy Policy</a> | 
                <a href='#' style='color: #0866FF; text-decoration: none;'>Terms of Service</a> | 
                <a href='#' style='color: #0866FF; text-decoration: none;'>Unsubscribe</a>
            </p>
        </div>
    </div>
</body>
</html>";
        }


    }
}
