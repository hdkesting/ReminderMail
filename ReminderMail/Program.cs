using System;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Threading;

namespace ReminderMail
{
    class Program
    {
        static int Main(string[] args)
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
            SmtpClient smtpClient = new SmtpClient();
            NetworkCredential basicCredential = new NetworkCredential(config.Account, config.Password);
            MailMessage message = new MailMessage();
            MailAddress fromAddress = new MailAddress(config.Account);

            // setup up the host, increase the timeout to 5 minutes
            smtpClient.Host = "smtp.office365.com";
            smtpClient.Port = 587;
            smtpClient.UseDefaultCredentials = false;
            smtpClient.Credentials = basicCredential;
            smtpClient.Timeout = (60 * 5 * 1000);
            smtpClient.EnableSsl = true;

            message.From = fromAddress;
            message.Bcc.Add(fromAddress);
            message.Subject = config.Subject;
            message.IsBodyHtml = config.MessageIsHtml;
            message.Body = config.Message;
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
    }
}
