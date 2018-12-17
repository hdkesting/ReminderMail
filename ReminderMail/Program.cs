using System;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Threading;

namespace ReminderMail
{
    internal class Program
    {
        public static int Main(string[] args)
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

            ConsoleCountdown(5);

            if (SendMail(config))
            {
                ConsoleDelay(5);
            }
            else
            {
                Console.Write("Press enter > ");
                Console.ReadLine();
            }

            return 0;
        }

        private static string ReadPassword()
        {
            var pwd = new StringBuilder();

            Console.Error.Write("Enter password of mail account (if 2FA enabled, use app-password)> ");

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
            MailMessage message = new MailMessage();
            MailAddress fromAddress = new MailAddress(config.Account);

            message.From = fromAddress;
            message.Bcc.Add(fromAddress);
            message.Subject = config.Subject;
            message.IsBodyHtml = config.MessageIsHtml;
            message.Body = config.Message;
            if (!string.IsNullOrWhiteSpace(config.ReplyTo))
            {
                message.ReplyToList.Add(config.ReplyTo);
            }

            foreach (var addr in config.PrimaryReceivers)
            {
                message.To.Add(new MailAddress(addr));
            }

            foreach (var addr in config.SecondaryReceivers)
            {
                message.CC.Add(new MailAddress(addr));
            }

            foreach (var addr in config.BlindReceivers)
            {
                message.Bcc.Add(new MailAddress(addr));
            }

            if (config.AddAccountToBcc)
            {
                message.Bcc.Add(new MailAddress(config.Account));
            }

            SmtpClient smtpClient = new SmtpClient();
            NetworkCredential basicCredential = new NetworkCredential(config.Account, config.Password);
            // setup up the host, increase the timeout to 5 minutes
            smtpClient.Host = config.SmtpHost;
            smtpClient.Port = config.SmtpPort;
            smtpClient.UseDefaultCredentials = false;
            smtpClient.Credentials = basicCredential;
            smtpClient.Timeout = (int)TimeSpan.FromMinutes(5).TotalMilliseconds;
            smtpClient.EnableSsl = true;

            try
            {
                Console.WriteLine("Sending ...");
                smtpClient.Send(message);
                Console.WriteLine("Done!");
                return true;
            }
            catch (Exception ex)
            {
                config.ClearPassword();
                Console.Error.WriteLine("Error sending mail. Stored password is cleared. Will be asked on next run.");
                while (ex != null)
                {
                    Console.Error.WriteLine(ex.Message);
                    ex = ex.InnerException;
                }

                return false;
            }
        }

        private static void ConsoleCountdown(int seconds)
        {
            while (seconds > 0)
            {
                Console.Write($"Delaying: {seconds}   ");
                Console.Write("\r");
                Thread.Sleep(1000);
                seconds--;
            }

            Console.WriteLine("Working ...     ");
        }

        private static void ConsoleDelay(int seconds)
        {
            while (seconds > 0)
            {
                Console.Write(new string('.', seconds) + "  ");
                Console.Write("\r");
                Thread.Sleep(1000);
                seconds--;
            }
        }
    }
}
