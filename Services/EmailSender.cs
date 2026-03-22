using System;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

// 🔌 MẪU THIẾT KẾ ADAPTER - Interface định nghĩa hợp đồng email
// Chuẩn hóa interface cho việc gửi email
// 🔗 INTERFACE: IEmailSender là interface được implement bởi EmailSender
public interface IEmailSender
{
    Task SendEmailAsync(string toEmail, string subject, string message);
}

// 🔌 MẪU THIẾT KẾ ADAPTER - Adapts SmtpClient to IEmailSender interface
// Wraps .NET's SmtpClient trong một interface email chuẩn hóa
// Cho phép dễ dàng thay thế hoặc mock trong tests
// 🔗 IMPLEMENT: EmailSender implement interface IEmailSender
public class EmailSender : IEmailSender
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<EmailSender> _logger;

    public EmailSender(IConfiguration configuration, ILogger<EmailSender> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task SendEmailAsync(string toEmail, string subject, string message)
    {
        try
        {
            var emailSettings = _configuration.GetSection("EmailSettings");
            var smtpClient = new SmtpClient(emailSettings["SmtpServer"])
            {
                Port = int.Parse(emailSettings["SmtpPort"]),
                Credentials = new NetworkCredential(emailSettings["SenderEmail"], emailSettings["Password"]),
                EnableSsl = true
            };

            var mailMessage = new MailMessage
            {
                From = new MailAddress(emailSettings["SenderEmail"], emailSettings["SenderName"]),
                Subject = subject,
                Body = message,
                IsBodyHtml = true
            };
            mailMessage.To.Add(toEmail);

            _logger.LogInformation("Đang gửi email tới {ToEmail}", toEmail);
            await smtpClient.SendMailAsync(mailMessage);
            _logger.LogInformation("Gửi email thành công tới {ToEmail}", toEmail);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi gửi email tới {ToEmail}", toEmail);
            throw; // hoặc return Task.CompletedTask nếu không muốn app crash
        }
    }
}
