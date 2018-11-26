using System;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Threading;
using MailKit.Net.Imap;
using MimeKit;
using MimeKit.Text;

namespace ReminderMail
{
    internal class Program
    {
        private static int Main(string[] args)
        {
            Configuration config;
            try
            {
                config = Configuration.Read();
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("Error loading configuration:");
                Console.Error.WriteLine(ex.Message);
                Console.Error.Write("Press <enter> to exit > ");
                Console.ReadLine();
                return 1;
            }

            if (config.Frequency <= 0 || DateTime.Now.Day % config.Frequency != 0)
            {
                Console.WriteLine("Sending is skipped.");
                Thread.Sleep(5000);
                return 0;
            }

            if (string.IsNullOrWhiteSpace(config.Password))
            {
                config.SetPassword(ReadPassword());
            }

            if (SendMail(config))
            {
                Thread.Sleep(2000);
            }
            else
            {
                Console.Write("Press enter > ");
                Console.ReadLine();
                return 1;
            }

            return 0;
        }

        private static string ReadPassword()
        {
            var pwd = new StringBuilder();

            Console.Error.Write("Enter password of mail account > ");

            ConsoleKeyInfo key;
            do
            {
                key = Console.ReadKey(true);

                // Ignore any key out of range.
                if (key.KeyChar >= ' ')
                {
                    // Append the character to the password.
                    pwd.Append(key.KeyChar);
                    Console.Write("*");
                }
                // Exit if Enter key is pressed.
            } while (key.Key != ConsoleKey.Enter);

            Console.WriteLine();

            return pwd.ToString();
        }

        private static bool SendMail(Configuration config)
        {
            var message = new MimeMessage();
            var fromAddress = new MailboxAddress(config.Account);


            message.From.Add(fromAddress);
            message.Subject = config.Subject;
            if (config.MessageIsHtml)
            {
                message.Body = new TextPart(TextFormat.Html) { Text = config.Message };
            }
            else
            {
                message.Body = new TextPart(TextFormat.Plain) { Text = config.Message };
            }

            foreach (var addr in config.PrimaryReceivers)
            {
                message.To.Add(new MailboxAddress(addr));
            }

            foreach (var addr in config.SecondaryReceivers)
            {
                message.Cc.Add(new MailboxAddress(addr));
            }

            foreach (var addr in config.BlindReceivers)
            {
                message.Bcc.Add(new MailboxAddress(addr));
            }

            if (config.AddAccountToBcc)
            {
                message.Bcc.Add(new MailboxAddress(config.Account));
            }

            try
            {
                Console.WriteLine("Sending ...");
                // setup up the smtp connection, increase the timeout to 5 minutes
                var smtpClient = new MailKit.Net.Smtp.SmtpClient();
                smtpClient.Connect("smtp.office365.com", 587, MailKit.Security.SecureSocketOptions.Auto);
                smtpClient.Authenticate(config.Account, config.Password);
                smtpClient.Timeout = (int)TimeSpan.FromMinutes(5).TotalMilliseconds;

                smtpClient.Send(message);
                Console.WriteLine("Done!");
                smtpClient.Disconnect(true);
                return true;
            }
            catch (Exception ex)
            {
                config.ClearPassword();
                Console.Error.WriteLine("Error sending mail. Stored password is cleared - will be asked on next run.");
                while (ex != null)
                {
                    Console.Error.WriteLine(ex.Message);
                    ex = ex.InnerException;
                }

                return false;
            }
        }
    }
}
