using Serede.Identidade.Models;
using System.Net.Mail;
using System.Text;

namespace Serede.Identidade.Services;

public class EmailService
{
    private readonly IConfiguration _configuration;

    public EmailService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public async Task<Result> EnviarEmail(List<string> emails, string titulo, string corpo) => await EnviarEmail(emails, titulo, corpo, null, null);

    public async Task<Result> EnviarEmail(List<string> emails, string titulo, string corpo, byte[] atachment, string atachmentName)
    {
        var result = new Result();
        MailMessage mailMessage = new MailMessage
        {
            From = new MailAddress(_configuration.GetSection("Email:Remetente").Value,
                _configuration.GetSection("Email:Nome").Value),
            BodyEncoding = UTF8Encoding.UTF8,
            IsBodyHtml = true
        };

        mailMessage.Headers.Add("X-Sender", _configuration.GetSection("Email:HeaderSender").Value);
        mailMessage.Subject = titulo;
        mailMessage.Body = corpo;

        SmtpClient client = new SmtpClient(_configuration.GetSection("Email:Smtp").Value, int.Parse(_configuration.GetSection("Email:SmtpPorta").Value));

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
                    catch (Exception ex)
                    {
                        result.AddNotification(i, "Email não pode ser enviado");
                    }

                }
            }
        }
        return result;
    }
}
