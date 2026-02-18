using FBMMultiMessenger.Buisness.Helpers;
using FBMMultiMessenger.Buisness.Models;
using FBMMultiMessenger.Buisness.Models.SignalR.App;
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

        public async Task<bool> SendWelcomeEmailAsync(string toEmail, string userName, bool hasAvailedTrial, int trialAccounts, int trialDurationDays)
        {
            var subject = "Welcome to FBM Multi Messenger! 🎉";
            var htmlBody = GetWelcomeEmailTemplate(userName, toEmail, hasAvailedTrial, trialAccounts, trialDurationDays);
            return await SendEmailAsync(toEmail, subject, htmlBody);
        }

        public async Task<bool> SendAccountLogoutEmailAsync(string toEmail, string userName, List<AccountStatusSignalRModel> accounts)
        {
            var subject = "⚠️ Account Logout Notification - FBM Multi Messenger";
            var htmlBody = GetAccountLogoutEmailTemplate(userName, accounts);
            return await SendEmailAsync(toEmail, subject, htmlBody);
        }

        private string GetAccountLogoutEmailTemplate(string userName, List<AccountStatusSignalRModel> accounts)
        {
            var accountRows = string.Join("", accounts.Select(a => $"""
        <tr>
            <td style="padding: 12px 16px; border-bottom: 1px solid #f0f0f0; color: #333; font-size: 14px;">{a.AccountId}</td>
            <td style="padding: 12px 16px; border-bottom: 1px solid #f0f0f0; color: #333; font-size: 14px;">{a.AccountName}</td>
            <td style="padding: 12px 16px; border-bottom: 1px solid #f0f0f0; font-size: 14px;">
                <span style="background-color: #fff3cd; color: #856404; padding: 3px 10px; border-radius: 12px; font-size: 12px; font-weight: 600;">
                    {a.AuthStatus}
                </span>
            </td>
            <td style="padding: 12px 16px; border-bottom: 1px solid #f0f0f0; color: #dc3545; font-size: 14px;">{a.Reason}</td>
        </tr>
    """));

            return $"""
        <!DOCTYPE html>
        <html lang="en">
        <head>
            <meta charset="UTF-8" />
            <meta name="viewport" content="width=device-width, initial-scale=1.0"/>
            <title>Account Logout Notification</title>
        </head>
        <body style="margin: 0; padding: 0; background-color: #f4f6f9; font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif;">
            <table width="100%" cellpadding="0" cellspacing="0" style="background-color: #f4f6f9; padding: 40px 0;">
                <tr>
                    <td align="center">
                        <table width="620" cellpadding="0" cellspacing="0" style="background-color: #ffffff; border-radius: 12px; overflow: hidden; box-shadow: 0 4px 20px rgba(0,0,0,0.08);">

                            <!-- Header -->
                            <tr>
                                <td style="background: linear-gradient(135deg, #e74c3c 0%, #c0392b 100%); padding: 36px 40px; text-align: center;">
                                    <div style="font-size: 40px; margin-bottom: 10px;">⚠️</div>
                                    <h1 style="margin: 0; color: #ffffff; font-size: 24px; font-weight: 700; letter-spacing: 0.5px;">
                                        Account Logout Alert
                                    </h1>
                                    <p style="margin: 8px 0 0; color: rgba(255,255,255,0.85); font-size: 14px;">
                                        FBM Multi Messenger
                                    </p>
                                </td>
                            </tr>

                            <!-- Body -->
                            <tr>
                                <td style="padding: 36px 40px;">
                                    <p style="margin: 0 0 8px; color: #555; font-size: 15px;">Hello <strong style="color: #222;">{userName}</strong>,</p>
                                    <p style="margin: 0 0 24px; color: #555; font-size: 15px; line-height: 1.6;">
                                        We detected that <strong>{accounts.Count} account(s)</strong> associated with your profile have been logged out or require re-authentication. Please review the details below and take action as needed.
                                    </p>

                                    <!-- Accounts Table -->
                                    <table width="100%" cellpadding="0" cellspacing="0" style="border-radius: 8px; overflow: hidden; border: 1px solid #e8e8e8;">
                                        <thead>
                                            <tr style="background-color: #f8f9fa;">
                                                <th style="padding: 12px 16px; text-align: left; font-size: 12px; font-weight: 700; color: #888; text-transform: uppercase; letter-spacing: 0.8px; border-bottom: 2px solid #e8e8e8;">ID</th>
                                                <th style="padding: 12px 16px; text-align: left; font-size: 12px; font-weight: 700; color: #888; text-transform: uppercase; letter-spacing: 0.8px; border-bottom: 2px solid #e8e8e8;">Account Name</th>
                                                <th style="padding: 12px 16px; text-align: left; font-size: 12px; font-weight: 700; color: #888; text-transform: uppercase; letter-spacing: 0.8px; border-bottom: 2px solid #e8e8e8;">Status</th>
                                                <th style="padding: 12px 16px; text-align: left; font-size: 12px; font-weight: 700; color: #888; text-transform: uppercase; letter-spacing: 0.8px; border-bottom: 2px solid #e8e8e8;">Reason</th>
                                            </tr>
                                        </thead>
                                        <tbody>
                                            {accountRows}
                                        </tbody>
                                    </table>

                                    <!-- Action Steps -->
                                    <div style="margin: 32px 0 0; background-color: #f8f9fa; border-radius: 10px; padding: 28px 32px; border-left: 4px solid #e74c3c;">
                                        <h2 style="margin: 0 0 20px; color: #222; font-size: 16px; font-weight: 700;">
                                            🛠️ Steps to Restore Your Account
                                        </h2>

                                        <!-- Step 1 -->
                                        <div style="display: flex; margin-bottom: 18px;">
                                            <div style="min-width: 28px; height: 28px; background-color: #e74c3c; border-radius: 50%; color: #fff; font-size: 13px; font-weight: 700; text-align: center; line-height: 28px; margin-right: 14px;">1</div>
                                            <div>
                                                <p style="margin: 0; color: #333; font-size: 14px; line-height: 1.6;">
                                                    <strong>Open Facebook in your browser.</strong> You will notice that your account has been logged out.
                                                </p>
                                            </div>
                                        </div>

                                        <!-- Step 2 -->
                                        <div style="display: flex; margin-bottom: 18px;">
                                            <div style="min-width: 28px; height: 28px; background-color: #e74c3c; border-radius: 50%; color: #fff; font-size: 13px; font-weight: 700; text-align: center; line-height: 28px; margin-right: 14px;">2</div>
                                            <div>
                                                <p style="margin: 0; color: #333; font-size: 14px; line-height: 1.6;">
                                                    <strong>Enter your credentials</strong> and log back into your Facebook account.
                                                </p>
                                            </div>
                                        </div>

                                        <!-- Step 3 -->
                                        <div style="display: flex; margin-bottom: 18px;">
                                            <div style="min-width: 28px; height: 28px; background-color: #e74c3c; border-radius: 50%; color: #fff; font-size: 13px; font-weight: 700; text-align: center; line-height: 28px; margin-right: 14px;">3</div>
                                            <div>
                                                <p style="margin: 0; color: #333; font-size: 14px; line-height: 1.6;">
                                                    <strong>Open your Cookie Extractor extension</strong> in the browser, find your Facebook session cookie, and click <em>"Copy as Header Cookie"</em> to copy it to your clipboard.
                                                </p>
                                            </div>
                                        </div>

                                        <!-- Step 4 -->
                                        <div style="display: flex; margin-bottom: 18px;">
                                            <div style="min-width: 28px; height: 28px; background-color: #e74c3c; border-radius: 50%; color: #fff; font-size: 13px; font-weight: 700; text-align: center; line-height: 28px; margin-right: 14px;">4</div>
                                            <div>
                                                <p style="margin: 0; color: #333; font-size: 14px; line-height: 1.6;">
                                                    <strong>Open the FBM Multi Messenger app,</strong> search for this account, paste the copied cookie, and save — your account will be back in sync!
                                                </p>
                                            </div>
                                        </div>

                                        <!-- Note -->
                                        <div style="background-color: #fff8e1; border: 1px solid #ffe082; border-radius: 8px; padding: 14px 16px; margin-top: 8px;">
                                            <p style="margin: 0; color: #795548; font-size: 13px; line-height: 1.6;">
                                                💡 <strong>Don't have the Cookie Extractor?</strong> Please contact our support team and we'll guide you through the setup.
                                            </p>
                                        </div>
                                    </div>

                                    <!-- Apology Note -->
                                    <p style="margin: 24px 0 0; color: #888; font-size: 13px; line-height: 1.6; text-align: center; font-style: italic;">
                                        We sincerely apologize for the inconvenience. Facebook enforces strict security policies that may occasionally require re-authentication.
                                    </p>
                                </td>
                            </tr>

                            <!-- Footer -->
                            <tr>
                                <td style="background-color: #f8f9fa; padding: 24px 40px; text-align: center; border-top: 1px solid #e8e8e8;">
                                    <p style="margin: 0; color: #aaa; font-size: 12px;">
                                        © {DateTime.UtcNow.Year} FBM Multi Messenger. All rights reserved.
                                    </p>
                                    <p style="margin: 6px 0 0; color: #aaa; font-size: 12px;">
                                        This is an automated notification — please do not reply to this email.
                                    </p>
                                </td>
                            </tr>

                        </table>
                    </td>
                </tr>
            </table>
        </body>
        </html>
    """;
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



        private string GetWelcomeEmailTemplate(string userName, string userEmail, bool hasAvailedTrial, int trialAccounts, int trialDurationDays)
        {
            var trialSection = hasAvailedTrial ? $"""
        <!-- Free Trial Gift Banner -->
        <div style="background: linear-gradient(135deg, #28a745 0%, #1e7e34 100%); border-radius: 10px; padding: 28px 30px; margin: 30px 0; text-align: center; box-shadow: 0 4px 12px rgba(40,167,69,0.25);">
            <div style="font-size: 38px; margin-bottom: 10px;">🎁</div>
            <h2 style="margin: 0 0 10px; color: #ffffff; font-size: 20px; font-weight: 700; letter-spacing: 0.4px;">
                You've Received a Free Trial — Our Gift to You!
            </h2>
            <p style="margin: 0 0 18px; color: rgba(255,255,255,0.9); font-size: 14px; line-height: 1.7;">
                As a warm welcome, <strong>FBM Multi Messenger</strong> is gifting you a completely free trial so you can explore everything we have to offer — no payment required.
            </p>
            <div style="display: inline-block; background: rgba(255,255,255,0.15); border: 2px solid rgba(255,255,255,0.5); border-radius: 10px; padding: 16px 32px; margin-bottom: 6px;">
                <p style="margin: 0 0 6px; color: #fff; font-size: 13px; text-transform: uppercase; letter-spacing: 1px; font-weight: 600;">Your Trial Includes</p>
                <p style="margin: 0; color: #ffffff; font-size: 26px; font-weight: 800;">
                    {trialAccounts} Accounts &nbsp;•&nbsp; {trialDurationDays} Days Free
                </p>
            </div>
        </div>
    """ : string.Empty;

            return $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ 
            font-family: 'Segoe UI', Arial, sans-serif; 
            line-height: 1.6; 
            color: #333; 
            margin: 0; 
            padding: 20px; 
            background: #f5f5f5; 
        }}
        .container {{ 
            max-width: 600px; 
            margin: 0 auto; 
            background: white; 
            border-radius: 8px; 
            overflow: hidden; 
            box-shadow: 0 2px 8px rgba(0,0,0,0.1); 
        }}
        .header {{ 
            background: linear-gradient(135deg, #0866FF 0%, #042B5C 100%); 
            color: white; 
            padding: 40px 30px; 
            text-align: center; 
        }}
        .header h1 {{ 
            margin: 0 0 10px 0; 
            font-size: 28px; 
        }}
        .header p {{ 
            margin: 0; 
            font-size: 16px; 
            opacity: 0.9; 
        }}
        .content {{ 
            padding: 40px 30px; 
        }}
        .welcome-text {{ 
            text-align: center; 
            margin-bottom: 30px; 
        }}
        .welcome-text h2 {{ 
            color: #0866FF; 
            font-size: 24px; 
            margin: 0 0 15px 0; 
        }}
        .welcome-text p {{ 
            color: #555; 
            font-size: 16px; 
            line-height: 1.8; 
        }}
        .section {{ 
            margin: 30px 0; 
            padding: 25px; 
            background: #f8f9fa; 
            border-radius: 8px; 
            border-left: 4px solid #0866FF; 
        }}
        .section h3 {{ 
            color: #2d3748; 
            font-size: 18px; 
            margin: 0 0 15px 0; 
        }}
        .section p {{ 
            color: #555; 
            margin: 0 0 10px 0; 
            font-size: 15px; 
        }}
        .pricing-table {{ 
            margin: 20px 0; 
            border-collapse: collapse; 
            width: 100%; 
        }}
        .pricing-table th, 
        .pricing-table td {{ 
            padding: 12px; 
            text-align: left; 
            border-bottom: 1px solid #e2e8f0; 
        }}
        .pricing-table th {{ 
            background: #0866FF; 
            color: white; 
            font-weight: 600; 
        }}
        .pricing-table tr:last-child td {{ 
            border-bottom: none; 
        }}
        .highlight-box {{ 
            background: #fff3cd; 
            border: 2px solid #ffc107; 
            border-radius: 8px; 
            padding: 20px; 
            margin: 25px 0; 
            text-align: center; 
        }}
        .highlight-box strong {{ 
            color: #856404; 
            font-size: 16px; 
        }}
        .steps {{ 
            counter-reset: step-counter; 
            list-style: none; 
            padding: 0; 
        }}
        .steps li {{ 
            counter-increment: step-counter; 
            margin: 20px 0; 
            padding-left: 50px; 
            position: relative; 
            font-size: 15px; 
            color: #555; 
        }}
        .steps li:before {{ 
            content: counter(step-counter); 
            position: absolute; 
            left: 0; 
            top: -5px; 
            background: #0866FF; 
            color: white; 
            width: 35px; 
            height: 35px; 
            border-radius: 50%; 
            display: flex; 
            align-items: center; 
            justify-content: center; 
            font-weight: bold; 
            font-size: 18px; 
        }}
        .steps li strong {{ 
            color: #2d3748; 
        }}
        .support-box {{ 
            background: #e6f7ff; 
            border-radius: 8px; 
            padding: 20px; 
            margin: 30px 0; 
            text-align: center; 
        }}
        .support-box p {{ 
            margin: 0 0 10px 0; 
            color: #0066cc; 
        }}
        .footer {{ 
            background: #f8f9fa; 
            text-align: center; 
            padding: 25px; 
            color: #6c757d; 
            font-size: 13px; 
        }}
        .footer p {{ 
            margin: 5px 0; 
        }}
        @media only screen and (max-width: 600px) {{
            .content {{ 
                padding: 25px 20px; 
            }}
            .section {{ 
                padding: 20px 15px; 
            }}
            .steps li {{ 
                padding-left: 45px; 
            }}
        }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>🎉 Welcome to FBM Multi Messenger!</h1>
            <p>Your trusted partner for multi-account messaging</p>
        </div>
        
        <div class='content'>
            <div class='welcome-text'>
                <h2>Hello, {userName}! 👋</h2>
                <p>Congratulations on taking the first step towards effortless multi-account management! Your account has been successfully created, and you're now in the right hands.</p>
                <p style='margin-top: 15px;'><strong>We're here to guide you every step of the way.</strong></p>
            </div>

            {trialSection}
            
            <div class='section'>
                <h3>🚀 Why Choose FBM Multi Messenger?</h3>
                <p>✓ <strong>Trusted by thousands</strong> of businesses and professionals</p>
                <p>✓ <strong>Secure & reliable</strong> platform with 24/7 support</p>
                <p>✓ <strong>Easy setup</strong> - get started in minutes</p>
                <p>✓ <strong>Flexible pricing</strong> with incredible discounts</p>
            </div>

            <div class='highlight-box'>
                <strong>🎁 Special Launch Offer - Save Up To 70%!</strong>
            </div>

            <div class='section'>
                <h3>💰 Our Pricing & Exclusive Discounts</h3>
                <p>We believe in rewarding our customers. The more accounts you manage, the more you save!</p>
                
                <table class='pricing-table'>
                    <tr>
                        <th>Accounts</th>
                        <th>Discount</th>
                        <th>Your Savings</th>
                    </tr>
                    <tr>
                        <td>1-10 accounts</td>
                        <td>Standard Rate</td>
                        <td>—</td>
                    </tr>
                    <tr>
                        <td>11-20 accounts</td>
                        <td><strong style='color: #0866FF;'>50% OFF</strong></td>
                        <td>Save Big! 💰</td>
                    </tr>
                    <tr>
                        <td>21-100 accounts</td>
                        <td><strong style='color: #0866FF;'>60% OFF</strong></td>
                        <td>Massive Savings! 🎉</td>
                    </tr>
                    <tr>
                        <td>100+ accounts</td>
                        <td><strong style='color: #0866FF;'>70% OFF</strong></td>
                        <td>Maximum Savings! 🚀</td>
                    </tr>
                </table>
            </div>

            <div class='section'>
                <h3>📋 How to Complete Your Setup - Simple 4-Step Process</h3>
                <ul class='steps'>
                    <li>
                        <strong>Use Our Pricing Calculator</strong><br>
                        On your screen, you'll see our interactive pricing calculator. Simply enter the number of accounts you want to manage, and watch as the price updates automatically with your applicable discount!
                    </li>
                    <li>
                        <strong>Review Your Order</strong><br>
                        Click the <strong>""Proceed to Checkout""</strong> button. You'll see a complete breakdown of your subscription details, final amount, and payment instructions.
                    </li>
                    <li>
                        <strong>Make Your Payment</strong><br>
                        Send the displayed amount to the payment details shown on your screen. Make sure to save your transaction receipt or take a screenshot of the payment confirmation.
                    </li>
                    <li>
                        <strong>Submit Payment Verification</strong><br>
                        Upload your payment receipt/screenshot through the verification form. Our team will verify your payment within 24 hours and activate your subscription immediately.
                    </li>
                </ul>
            </div>

            <div class='highlight-box'>
                <p style='margin: 0; color: #856404;'><strong>⚡ Quick Approval:</strong> Most payments are verified within 2-6 hours during business hours!</p>
            </div>

            <div class='section'>
                <h3>🔒 Safe & Secure Process</h3>
                <p>Your payment information is handled securely. Our verification team manually reviews each transaction to ensure your account is activated correctly and safely.</p>
                <p style='margin-top: 10px;'><strong>What happens after verification?</strong></p>
                <p>✓ Instant subscription activation<br>
                   ✓ Full access to all features<br>
                   ✓ Email confirmation with subscription details<br>
                   ✓ Dedicated support for your account setup</p>
            </div>

            <div class='support-box'>
                <p><strong>Need Help? We're Here For You! 💬</strong></p>
                <p>Our support team is available 24/7 to assist you with any questions.</p>
                <p style='margin-top: 15px;'>
                    📧 Email: support@fbmmultimessenger.com<br>
                </p>
            </div>

            <div style='text-align: center; margin: 30px 0;'>
                <p style='font-size: 15px; color: #555; line-height: 1.8;'>
                    Thank you for choosing FBM Multi Messenger. We're excited to have you on board and committed to making your experience exceptional.
                </p>
                <p style='margin-top: 25px; font-size: 16px;'>
                    <strong style='color: #2d3748;'>Welcome to the family! 🎊</strong><br>
                    <span style='color: #0866FF; font-weight: 600;'>The FBM Multi Messenger Team</span>
                </p>
            </div>
        </div>
        
        <div class='footer'>
            <p><strong>© {DateTime.Now.Year} FBM Multi Messenger. All rights reserved.</strong></p>
            <p style='margin-top: 10px;'>You're receiving this email because you created an account with us.</p>
            <p style='margin-top: 15px;'>
                <a href='#' style='color: #0866FF; text-decoration: none;'>Privacy Policy</a> | 
                <a href='#' style='color: #0866FF; text-decoration: none;'>Terms of Service</a>
            </p>
        </div>
    </div>
</body>
</html>";
        }

    }
}
