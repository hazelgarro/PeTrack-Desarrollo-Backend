using MailKit.Security;
using MimeKit;
using System.Security.Cryptography;
using System.Text;
using MailKit.Net.Smtp;

namespace APIPetrack.Services
{
    public class EmailService
    {
        private readonly string _smtpServer;
        private readonly int _port;
        private readonly string _senderEmail;
        private readonly string _password;
        private const string Key = "1234567890123456";

        public EmailService(IConfiguration configuration)
        {
            _smtpServer = configuration["EmailSettings:SmtpServer"];
            _port = int.Parse(configuration["EmailSettings:Port"]);
            _senderEmail = configuration["EmailSettings:SenderEmail"];
            _password = configuration["EmailSettings:Password"];
        }

        public async Task SendEmailAsync(string recipientEmail, string subject, string messageBody)
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress("Petrack", _senderEmail));
            message.To.Add(new MailboxAddress("", recipientEmail));
            message.Subject = subject;

            var bodyBuilder = new BodyBuilder { HtmlBody = messageBody };
            message.Body = bodyBuilder.ToMessageBody();

            using (var client = new SmtpClient())
            {
                await client.ConnectAsync(_smtpServer, _port, SecureSocketOptions.StartTls);
                await client.AuthenticateAsync(_senderEmail, _password);
                await client.SendAsync(message);
                await client.DisconnectAsync(true);
            }
        }

        public static string Encrypt(string plainText)
        {
            using var aes = Aes.Create();
            aes.Key = Encoding.UTF8.GetBytes(Key);
            aes.GenerateIV();
            var iv = aes.IV; 

            using var encryptor = aes.CreateEncryptor(aes.Key, iv);
            using var ms = new MemoryStream();

            ms.Write(iv, 0, iv.Length);

            using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
            using (var sw = new StreamWriter(cs))
            {
                sw.Write(plainText);
            }

            return Convert.ToBase64String(ms.ToArray());
        }

        public static string Decrypt(string encryptedText)
        {
            if (string.IsNullOrEmpty(encryptedText))
                return null;

            try
            {
                var fullCipher = Convert.FromBase64String(encryptedText);

                using var aes = Aes.Create();
                aes.Key = Encoding.UTF8.GetBytes(Key);

                var iv = new byte[16];
                Array.Copy(fullCipher, iv, iv.Length);

                using var decryptor = aes.CreateDecryptor(aes.Key, iv);
                using var ms = new MemoryStream(fullCipher, iv.Length, fullCipher.Length - iv.Length);
                using var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read);
                using var sr = new StreamReader(cs);

                return sr.ReadToEnd();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Decryption failed: {ex.Message}");
                return null;
            }
        }
    }
}
