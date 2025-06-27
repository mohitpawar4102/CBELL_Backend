using Microsoft.Extensions.Configuration;
using MongoDB.Driver;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;
using YourNamespace.DTO;
using YourNamespace.Models;
using System;
using System.Collections.Generic;

namespace YourNamespace.Services
{
    public class EmailService
    {
        private readonly IConfiguration _configuration;
        private readonly IMongoCollection<EmailRecord> _emailCollection;

        public EmailService(IConfiguration configuration, IMongoDatabase database)
        {
            _configuration = configuration;
            _emailCollection = database.GetCollection<EmailRecord>("EmailRecords");
        }

        public async Task SendEmailAsync(EmailSendDto dto)
        {
            var smtpClient = new SmtpClient("smtp.gmail.com")
            {
                Port = 587,
                Credentials = new NetworkCredential(
                    _configuration["Smtp:Email"],
                    _configuration["Smtp:Password"]),
                EnableSsl = true
            };

            var mailMessage = new MailMessage
            {
                From = new MailAddress(_configuration["Smtp:Email"]),
                Subject = dto.Subject,
                Body = dto.Message,
                IsBodyHtml = true,
            };
            dto.To.ForEach(email => mailMessage.To.Add(email));
            dto.Cc?.ForEach(email => mailMessage.CC.Add(email));
            dto.Bcc?.ForEach(email => mailMessage.Bcc.Add(email));

            string? attachmentFileName = null;
            if (dto.Attachment != null && dto.Attachment.Length > 0)
            {
                using (var stream = dto.Attachment.OpenReadStream())
                {
                    var attachment = new Attachment(stream, dto.Attachment.FileName);
                    mailMessage.Attachments.Add(attachment);
                    attachmentFileName = dto.Attachment.FileName;
                    await smtpClient.SendMailAsync(mailMessage);
                }
            }
            else
            {
                await smtpClient.SendMailAsync(mailMessage);
            }

            var record = new EmailRecord
            {
                To = dto.To,
                Cc = dto.Cc,
                Bcc = dto.Bcc,
                Subject = dto.Subject,
                Message = dto.Message,
                DocumentId = dto.DocumentId,
                AttachmentFileName = attachmentFileName,
                SentAt = DateTime.UtcNow,
                Status = "Sent"
            };
            await _emailCollection.InsertOneAsync(record);
        }
    }
} 