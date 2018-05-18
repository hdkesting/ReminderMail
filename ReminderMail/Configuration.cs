using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Security;
using System.Text;
using System.Xml.Linq;

namespace ReminderMail
{
    /// <summary>
    /// The mail configuration.
    /// </summary>
    internal class Configuration
    {
        /// <summary>
        /// Prevents a default instance of the <see cref="Configuration"/> class from being created.
        /// </summary>
        private Configuration()
        {
        }

        /// <summary>
        /// Gets the frequency to send. 0 = do not send at all; 1 = every day; 2 = every second day; etc
        /// </summary>
        /// <value>
        /// The frequency.
        /// </value>
        public int Frequency { get; private set; }

        /// <summary>
        /// Gets the list of primary receivers ("to").
        /// </summary>
        /// <value>
        /// The primary receivers.
        /// </value>
        public List<string> PrimaryReceivers { get; } = new List<string>();

        /// <summary>
        /// Gets the list of secondary receivers ("cc").
        /// </summary>
        /// <value>
        /// The secondary receivers.
        /// </value>
        public List<string> SecondaryReceivers { get; } = new List<string>();

        /// <summary>
        /// Gets the list of blind receivers ("bcc").
        /// </summary>
        /// <value>
        /// The blind receivers.
        /// </value>
        public List<string> BlindReceivers { get; } = new List<string>();

        /// <summary>
        /// Gets the account, used as "from" and in sending.
        /// </summary>
        /// <value>
        /// The account.
        /// </value>
        public string Account { get; private set; }

        /// <summary>
        /// Gets a value indicating whether to add the <see cref="Account"/> to the <see cref="BlindReceivers"/>.
        /// </summary>
        /// <value>
        ///   <c>true</c> if add account to BCC; otherwise, <c>false</c>.
        /// </value>
        public bool AddAccountToBcc { get; private set; }

        /// <summary>
        /// Gets the subject of the mail message.
        /// </summary>
        /// <value>
        /// The subject.
        /// </value>
        public string Subject { get; private set; }

        /// <summary>
        /// Gets the mail message to send.
        /// </summary>
        /// <value>
        /// The message.
        /// </value>
        public string Message { get; private set; }

        /// <summary>
        /// Gets a value indicating whether the message is specified as HTML.
        /// </summary>
        /// <value>
        ///   <c>true</c> if message is HTML; otherwise, <c>false</c>.
        /// </value>
        public bool MessageIsHtml { get; private set; }

        /// <summary>
        /// Gets the password for the account.
        /// </summary>
        /// <value>
        /// The password.
        /// </value>
        public string Password { get; private set; }

        /// <summary>
        /// Reads the configuration file.
        /// </summary>
        /// <remarks>
        /// Will create it when it doesn't exist yet.
        /// </remarks>
        /// <returns></returns>
        public static Configuration Read()
        {
            var path = GetConfigPath();

            var config = XDocument.Load(path);
            var cfg = new Configuration();
            cfg.Read(config);

            return cfg;
        }

        public void ClearPassword()
        {
            this.Password = null;

            this.UpdateConfig();
        }

        public void SetPassword(string password)
        {
            this.Password = password;

            this.UpdateConfig();
        }

        private static string GetConfigPath()
        {
            string path = Path.Combine(Path.GetDirectoryName(typeof(Configuration).Assembly.Location), "MailConfig.xml");

            if (!File.Exists(path))
            {
                // config file doesn't exist, so create it
                using (var resstream = typeof(Configuration).Assembly.GetManifestResourceStream(typeof(Configuration), "SampleConfig.xml"))
                using (var filestream = File.OpenWrite(path))
                {
                    resstream.CopyTo(filestream);
                }
            }

            return path;
        }

        private void UpdateConfig()
        {
            var path = GetConfigPath();
            var config = XDocument.Load(path);

            var pwdelt = config.Element("mail").Element("password");

            if (pwdelt == null)
            {
                pwdelt = new XElement("password");
                config.Element("mail").Add(pwdelt);
            }

            pwdelt.Value = this.Encrypt(this.Password);

            config.Save(path, SaveOptions.None);
        }

        private string Encrypt(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return string.Empty;
            }

            var bytes = Encoding.UTF8.GetBytes(text);

            return Convert.ToBase64String(bytes);
        }

        private string Decrypt(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return null;
            }

            try
            {
                var bytes = Convert.FromBase64String(text);

                return Encoding.UTF8.GetString(bytes);
            }
            catch
            {
                this.Password = null;
                this.UpdateConfig();
                return null;
            }
        }

        /// <summary>
        /// Reads the specified configuration.
        /// </summary>
        /// <param name="config">The configuration.</param>
        private void Read(XDocument config)
        {
            var root = config.Element("mail");

            this.Frequency = int.TryParse(root.Element("frequency").Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int freq) ? freq : 0;
            var rec = root.Element("receivers");

            var elts = rec.Elements("to");
            AddReceivers(elts, this.PrimaryReceivers);

            elts = rec.Elements("cc");
            AddReceivers(elts, this.SecondaryReceivers);

            elts = rec.Elements("bcc");
            AddReceivers(elts, this.BlindReceivers);

            this.Account = root.Element("from")?.Value;

            this.AddAccountToBcc = string.Equals("true", root.Element("from")?.Attribute("bcc")?.Value, StringComparison.OrdinalIgnoreCase);

            this.Subject = root.Element("subject")?.Value;

            var msg = root.Element("message");
            this.Message = msg?.Value;

            this.MessageIsHtml = string.Equals("html", msg?.Attribute("type")?.Value, StringComparison.OrdinalIgnoreCase);

            // TODO decode password (if any)
            this.Password = this.Decrypt(root.Element("password")?.Value);

            void AddReceivers(IEnumerable<XElement> elements, List<string> target)
            {
                foreach (var elt in elements)
                {
                    if (!string.IsNullOrWhiteSpace(elt.Value))
                    {
                        target.Add(elt.Value);
                    }
                }
            }
        }
    }
}
