using Microsoft.Extensions.Configuration;
using MongoDB.Driver;
using MongoDB.Driver.GridFS;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;
using YourNamespace.DTO;
using YourNamespace.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;

namespace YourNamespace.Services
{
    public class EmailService
    {
        private readonly IConfiguration _configuration;
        private readonly IMongoCollection<EmailRecord> _emailCollection;
        private readonly GridFSBucket _gridFs;
        private readonly IMongoDatabase _database;

        public EmailService(IConfiguration configuration, IMongoDatabase database)
        {
            _configuration = configuration;
            _database = database;
            _emailCollection = database.GetCollection<EmailRecord>("EmailRecords");
            _gridFs = new GridFSBucket(database, new GridFSBucketOptions
            {
                BucketName = "documents",
                ChunkSizeBytes = 255 * 1024
            });
        }

        public async Task SendEmailAsync(EmailSendDto dto)
        {
            if (dto.To == null || !dto.To.Any() || dto.To.Any(string.IsNullOrWhiteSpace))
            {
                throw new ArgumentException("At least one valid recipient email address is required.");
            }

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

            // Add To recipients (already validated above)
            dto.To.Where(email => !string.IsNullOrWhiteSpace(email))
               .ToList()
               .ForEach(email => mailMessage.To.Add(email));

            // Add CC recipients if any
            if (dto.Cc != null)
            {
                dto.Cc.Where(email => !string.IsNullOrWhiteSpace(email))
                    .ToList()
                    .ForEach(email => mailMessage.CC.Add(email));
            }

            // Add BCC recipients if any
            if (dto.Bcc != null)
            {
                dto.Bcc.Where(email => !string.IsNullOrWhiteSpace(email))
                    .ToList()
                    .ForEach(email => mailMessage.Bcc.Add(email));
            }

            string? attachmentFileName = null;
            Stream? attachmentStream = null;
            string? attachmentContentType = null;
            try
            {
                // Priority: Use client-uploaded attachment if present, else fetch from GridFS using documentId
                if (dto.Attachment != null && dto.Attachment.Length > 0)
                {
                    attachmentStream = dto.Attachment.OpenReadStream();
                    attachmentFileName = dto.Attachment.FileName;
                    attachmentContentType = dto.Attachment.ContentType;
                    var attachment = new Attachment(attachmentStream, attachmentFileName, attachmentContentType);
                    mailMessage.Attachments.Add(attachment);
                }
                else if (!string.IsNullOrEmpty(dto.DocumentId))
                {
                    // Try to fetch from GridFS
                    var objectId = MongoDB.Bson.ObjectId.Parse(dto.DocumentId);
                    var fileInfo = await _gridFs.Find(Builders<GridFSFileInfo>.Filter.Eq("_id", objectId)).FirstOrDefaultAsync();
                    if (fileInfo != null)
                    {
                        attachmentStream = await _gridFs.OpenDownloadStreamAsync(objectId);
                        attachmentFileName = fileInfo.Filename;
                        attachmentContentType = fileInfo.Metadata != null && fileInfo.Metadata.Contains("contentType")
                            ? fileInfo.Metadata["contentType"].AsString
                            : "application/octet-stream";
                        var attachment = new Attachment(attachmentStream, attachmentFileName, attachmentContentType);
                        mailMessage.Attachments.Add(attachment);
                    }
                }

                await smtpClient.SendMailAsync(mailMessage);
            }
            finally
            {
                // Dispose the stream if we opened one
                attachmentStream?.Dispose();
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