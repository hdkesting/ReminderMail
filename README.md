# ReminderMail
Console app to send a reminder mail every so often through office365 mail.

This is meant to be scheduled once a day. Then use the frequency to skip sending on some days.

# Configuration

On the first run a config file is created. This needs to be filled in.
Once the frequency is set to something greater than 0, a password (for the mail account) is asked and stored.

# About 2FA
If two-factor authentication is set up on the mail account, you will need an "app password" instead of your regular password.