using System;
using System.IO;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using BussinessLayer.Interfaces;

namespace BussinessLayer.Services
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _configuration;
        private const string LogFilePath = @"e:\PRN222\Ass2\Assignment2\verification_emails.log";

        public EmailService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        private async Task WriteToLogFileAsync(string toName, string toEmail, string subject, string textBody)
        {
            try
            {
                var logContent = $"========================================\n" +
                                 $"TIME: {DateTime.Now:yyyy-MM-dd HH:mm:ss}\n" +
                                 $"TO: {toName} ({toEmail})\n" +
                                 $"SUBJECT: {subject}\n" +
                                 $"BODY:\n{textBody}\n" +
                                 $"========================================\n\n";

                await File.AppendAllTextAsync(LogFilePath, logContent, Encoding.UTF8);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Lỗi ghi file log email: " + ex.Message);
            }
        }

        private async Task SendSmtpEmailAsync(string toEmail, string toName, string subject, string htmlBody)
        {
            var smtpServer = _configuration["EmailSettings:SmtpServer"];
            var portStr = _configuration["EmailSettings:Port"];
            var senderName = _configuration["EmailSettings:SenderName"] ?? "Chatbot";
            var senderEmail = _configuration["EmailSettings:SenderEmail"];
            var password = _configuration["EmailSettings:Password"];

            if (!string.IsNullOrEmpty(smtpServer) && int.TryParse(portStr, out var port) && !string.IsNullOrEmpty(senderEmail) && !string.IsNullOrEmpty(password))
            {
                try
                {
                    using (var mail = new MailMessage())
                    {
                        mail.From = new MailAddress(senderEmail, senderName);
                        mail.To.Add(toEmail);
                        mail.Subject = subject;
                        mail.Body = htmlBody;
                        mail.IsBodyHtml = true;

                        using (var smtp = new SmtpClient(smtpServer, port))
                        {
                            smtp.Credentials = new NetworkCredential(senderEmail, password.Trim());
                            smtp.EnableSsl = true;
                            await smtp.SendMailAsync(mail);
                        }
                    }
                }
                catch (Exception smtpEx)
                {
                    var errorMsg = $"[SMTP ERROR at {DateTime.Now:yyyy-MM-dd HH:mm:ss}]: {smtpEx.Message}\n\n";
                    try
                    {
                        await File.AppendAllTextAsync(LogFilePath, errorMsg, Encoding.UTF8);
                    }
                    catch { }
                    Console.WriteLine("Lỗi gửi email qua SMTP: " + smtpEx.Message);
                }
            }
        }

        public async Task SendActivationLinkAsync(string email, string name, string activationLink)
        {
            var senderName = _configuration["EmailSettings:SenderName"] ?? "Chatbot";

            var textBody = $@"
Xin chào {name},

Một tài khoản giảng viên đã được đăng ký cho thầy/cô trên hệ thống RAG AI Assistant.
Vui lòng nhấn vào liên kết dưới đây để kích hoạt tài khoản và nhận mật khẩu đăng nhập:

{activationLink}

Liên kết này có hiệu lực trong vòng 24 giờ.

Trân trọng,
{senderName}";

            var htmlBody = $@"
<div style='font-family: ""Inter"", Arial, sans-serif; background-color: #f8fafc; padding: 2rem; border-radius: 12px; max-width: 600px; margin: 0 auto; color: #1e293b; border: 1px solid #e2e8f0;'>
    <div style='text-align: center; margin-bottom: 1.5rem;'>
        <h2 style='color: #6c63ff; margin-bottom: 0.5rem;'>{senderName}</h2>
        <p style='color: #64748b; font-size: 0.9rem; margin-top: 0;'>Hệ thống Trợ lý Học tập AI</p>
    </div>
    
    <div style='background-color: #ffffff; padding: 1.75rem; border-radius: 8px; box-shadow: 0 4px 6px -1px rgba(0, 0, 0, 0.05); border: 1px solid #f1f5f9;'>
        <p style='font-size: 1rem; line-height: 1.6; margin-top: 0;'>Xin chào <strong>{name}</strong>,</p>
        
        <p style='font-size: 0.95rem; line-height: 1.6;'>Một tài khoản giảng viên đã được tạo cho thầy/cô trên hệ thống. Vui lòng bấm vào nút bên dưới để kích hoạt tài khoản của thầy/cô:</p>
        
        <div style='text-align: center; margin: 2rem 0;'>
            <a href='{activationLink}' style='background: linear-gradient(135deg, #6c63ff, #8b5cf6); color: #ffffff; text-decoration: none; padding: 0.8rem 2rem; border-radius: 6px; font-weight: 600; font-size: 0.95rem; display: inline-block; box-shadow: 0 4px 12px rgba(108, 99, 255, 0.3);'>Kích hoạt tài khoản</a>
        </div>

        <p style='font-size: 0.85rem; line-height: 1.6; color: #64748b; word-break: break-all;'>Nếu nút trên không hoạt động, thầy/cô có thể sao chép liên kết dưới đây và dán vào thanh địa chỉ trình duyệt:<br>
        <a href='{activationLink}' style='color: #6c63ff;'>{activationLink}</a></p>
        
        <p style='font-size: 0.9rem; line-height: 1.6; color: #64748b; margin-top: 1.5rem;'><em>Lưu ý: Liên kết kích hoạt này có hiệu lực trong vòng 24 giờ.</em></p>
    </div>
    
    <div style='text-align: center; margin-top: 1.5rem; font-size: 0.8rem; color: #94a3b8;'>
        <p>&copy; {DateTime.Now.Year} RAG AI Assistant. All rights reserved.</p>
    </div>
</div>";

            await WriteToLogFileAsync(name, email, "Kích hoạt tài khoản Giảng viên", textBody);
            await SendSmtpEmailAsync(email, name, $"[RAG Assistant] Kích hoạt tài khoản Giảng viên của bạn", htmlBody);
        }

        public async Task SendLecturerCredentialsAsync(string email, string name, string tempPassword, string loginUrl)
        {
            var senderName = _configuration["EmailSettings:SenderName"] ?? "Chatbot";

            var textBody = $@"
Xin chào {name},

Tài khoản giảng viên của thầy/cô đã được kích hoạt thành công trên hệ thống RAG AI Assistant.

Thông tin đăng nhập của thầy/cô như sau:
- Email đăng nhập: {email}
- Mật khẩu: {tempPassword}

Thầy/cô có thể đăng nhập tại đây: {loginUrl}

Vui lòng đăng nhập và đổi mật khẩu sau lần đăng nhập đầu tiên để bảo mật tài khoản.

Trân trọng,
{senderName}";

            var htmlBody = $@"
<div style='font-family: ""Inter"", Arial, sans-serif; background-color: #f8fafc; padding: 2rem; border-radius: 12px; max-width: 600px; margin: 0 auto; color: #1e293b; border: 1px solid #e2e8f0;'>
    <div style='text-align: center; margin-bottom: 1.5rem;'>
        <h2 style='color: #6c63ff; margin-bottom: 0.5rem;'>{senderName}</h2>
        <p style='color: #64748b; font-size: 0.9rem; margin-top: 0;'>Hệ thống Trợ lý Học tập AI</p>
    </div>
    
    <div style='background-color: #ffffff; padding: 1.75rem; border-radius: 8px; box-shadow: 0 4px 6px -1px rgba(0, 0, 0, 0.05); border: 1px solid #f1f5f9;'>
        <p style='font-size: 1rem; line-height: 1.6; margin-top: 0;'>Xin chào <strong>{name}</strong>,</p>
        
        <p style='font-size: 0.95rem; line-height: 1.6;'>Chúc mừng! Tài khoản giảng viên của thầy/cô đã được kích hoạt thành công. Thầy/cô có thể đăng nhập bằng thông tin dưới đây:</p>
        
        <div style='background-color: #f1f5f9; padding: 1rem; border-radius: 6px; margin: 1.5rem 0;'>
            <table style='width: 100%; border-collapse: collapse;'>
                <tr>
                    <td style='padding: 6px 0; color: #64748b; font-size: 0.9rem; font-weight: 500;'>Email đăng nhập:</td>
                    <td style='padding: 6px 0; font-size: 0.95rem; font-weight: 600; color: #1e293b;'>{email}</td>
                </tr>
                <tr>
                    <td style='padding: 6px 0; color: #64748b; font-size: 0.9rem; font-weight: 500;'>Mật khẩu:</td>
                    <td style='padding: 6px 0; font-size: 0.95rem; font-weight: 600; color: #6c63ff; font-family: monospace;'>{tempPassword}</td>
                </tr>
            </table>
        </div>
        
        <div style='text-align: center; margin: 2rem 0;'>
            <a href='{loginUrl}' style='background: linear-gradient(135deg, #00d4aa, #00b090); color: #ffffff; text-decoration: none; padding: 0.8rem 2rem; border-radius: 6px; font-weight: 600; font-size: 0.95rem; display: inline-block; box-shadow: 0 4px 12px rgba(0, 212, 170, 0.3);'>Đăng nhập ngay</a>
        </div>
        
        <p style='font-size: 0.9rem; line-height: 1.6; color: #64748b;'><em>Lưu ý: Vui lòng đổi mật khẩu sau khi đăng nhập để đảm bảo bảo mật.</em></p>
    </div>
    
    <div style='text-align: center; margin-top: 1.5rem; font-size: 0.8rem; color: #94a3b8;'>
        <p>&copy; {DateTime.Now.Year} RAG AI Assistant. All rights reserved.</p>
    </div>
</div>";

            await WriteToLogFileAsync(name, email, "Kích hoạt tài khoản thành công", textBody);
            await SendSmtpEmailAsync(email, name, $"[RAG Assistant] Tài khoản giảng viên đã kích hoạt thành công", htmlBody);
        }
    }
}
