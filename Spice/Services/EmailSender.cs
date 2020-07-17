using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Options;
using MimeKit;
using MimeKit.Text;
using Spice.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Spice.Services
{
    public class EmailSender : IEmailSender
    {
        private readonly EmailOptions _options;
        public EmailSender(IOptions<EmailOptions> options)
        {
            _options = options.Value;
        }
        public async Task SendEmailAsync(string email, string subject, string message)
        {
            // create message
            var emailDetails = new MimeMessage();
            emailDetails.Sender = MailboxAddress.Parse(_options.UserName);
            emailDetails.To.Add(MailboxAddress.Parse(email));
            emailDetails.Subject = subject;
            emailDetails.Body = new TextPart(TextFormat.Html) { Text = message };

            // send email
            using var smtp = new SmtpClient();
            smtp.Connect(_options.SmtpHost, Int32.Parse(_options.SmtpPort), SecureSocketOptions.StartTls);
            await smtp.AuthenticateAsync(_options.UserName, _options.Password);
            await smtp.SendAsync(emailDetails);
            await smtp.DisconnectAsync(true);
        }


    }
}
