using Identidade.Server.Models;
using Identidade.Server.Settings;
using System.Net.Mail;
using System.Text;

namespace Identidade.Server.Services;

public class EmailService
{
    private readonly EmailSettings _configuration;

    public EmailService(EmailSettings configuration)
    {
        _configuration = configuration;
    }

    public async Task<ResultViewModel> EnviarEmail(List<string> emails, string titulo, string corpo) => await EnviarEmail(emails, titulo, corpo, null, null);

    public async Task<ResultViewModel> EnviarEmail(List<string> emails, string titulo, string corpo, byte[] atachment, string atachmentName)
    {
        var result = new ResultViewModel();
        MailMessage mailMessage = new MailMessage
        {
            From = new MailAddress(_configuration.Remetente, _configuration.Nome),
            BodyEncoding = Encoding.UTF8,
            IsBodyHtml = true
        };

        mailMessage.Headers.Add("X-Sender", _configuration.CabecarioEnvio);
        mailMessage.Subject = titulo;
        mailMessage.Body = corpo;

        SmtpClient client = new SmtpClient(_configuration.Smtp, _configuration.Porta);

        if (atachment != null)
        {
            var stream = new MemoryStream(atachment);
            mailMessage.Attachments.Add(new Attachment(stream, atachmentName));
        }

        foreach (var i in emails)
        {
            if (!string.IsNullOrEmpty(i))
            {
                var emailSplit = i.Split(";");
                if (emailSplit.Length > 1)
                {
                    foreach (var ii in emailSplit)
                    {
                        if (!string.IsNullOrEmpty(ii))
                        {
                            try
                            {
                                mailMessage.To.Clear();
                                mailMessage.To.Add(ii);
                                await client.SendMailAsync(mailMessage);
                            }
                            catch
                            {
                                result.AddNotification(ii, "Email não pode ser enviado");
                            }
                        }
                    }
                }
                else
                {
                    try
                    {
                        mailMessage.To.Clear();
                        mailMessage.To.Add(i);
                        await client.SendMailAsync(mailMessage);
                    }
                    catch
                    {
                        result.AddNotification(i, "Email não pode ser enviado");
                    }
                }
            }
        }
        return result;
    }
}
