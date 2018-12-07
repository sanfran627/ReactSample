using System.Collections.Generic;

namespace SampleReact
{
	public class SettingsModel
	{
		public SMTPSettings SMTP { get; set; }
		public SMSSettings SMS { get; set; }
		public StripeSettings Stripe { get; set; }
		public SlackSettings Slack { get; set; }
		public Dictionary<string,string> Misc { get; set; }
	}

	public class SMTPSettings
	{
		public string Host { get; set; }
		public int Port { get; set; }
		public int TimeoutInSeconds { get; set; }
		public bool UseSSL { get; set; }
		public string Username { get; set; }
		public string Password { get; set; }
		public string From { get; set; }
		public string Alerts { get; set; }
	}

	public class SMSSettings
	{
		public string AccountSid { get; set; }
		public string AccountToken { get; set; }
		public string SourceNumber { get; set; }
		public int SecondsPerSMS { get; set; }
		public string[] Alerts { get; set; }
	}

	public class MailChimpSettings
	{
		public string ApiKey { get; set; }
		public string ListId { get; set; }
	}

	public class StripeSettings
	{
		public Dictionary<EnvironmentId,string> Keys { get; set; }
	}

	public class SlackSettings
	{
		public string Channel { get; set; }
		public bool Enabled { get; set; }
	}


}
