using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace SampleReact
{
	public class SendEmailModel
	{
		public SendEmailModel() { }

		public SendEmailModel( Guid userId, string to, string subj, string body, bool isHtml = false )
		{
			this.UserId = userId;
			this.Created = DateTime.UtcNow;
			this.To = to;
			this.Subj = subj;
			this.Body = body;
			this.Html = isHtml;
		}

		public Guid UserId { get; private set; }
		public DateTime Created { get; private set; }
		public string To { get; private set; }
		public string Subj { get; private set; }
		public string Body { get; private set; }
		public bool Html { get; private set; }
		[JsonIgnore] public int Attempts { get; set; }
		[JsonIgnore] public EmailStatus Status { get; set; }
		[JsonIgnore] public DateTime Date { get; set; }
		[JsonIgnore] public string Error { get; set; }
		[JsonIgnore] public string SmtpStatus { get; set; }

		public void SetStatus( EmailStatus status, string error = null, string smptStatus = null )
		{
			if( status != EmailStatus.Sent ) this.Attempts++;
			this.Error = error;
			this.SmtpStatus = smptStatus;
		}

		public string ToJson( bool format = false ) => JsonConvert.SerializeObject( this, format ? Formatting.Indented : Formatting.None );
	}

	public enum EmailPrepStatus
	{
		Ready = 0,
		Processing = 1,
		ErrorGeneratingEmail = 2,
		Processed = 255
	}

	public enum EmailStatus
	{
		None = 0,
		Sending = 1,
		Sent = 2,
		ServerDown = 3, //requeue
		ServerRefused = 4, //requeue
		HostNotFound = 5,
		BadRecipient = 6,
		SmtpException = 7,
		SystemError = 911
	}

	public enum EmailTemplateId
	{
		None = 0,
		Test,
		EmailVerification,
		EmailChanged,
		PlanInvitation,
		PlanInvitationNewUser,
		PlanInvitationAccepted,
		ResetPassword,
		DiscountCode,
		//Other future items:
		//HaventSeenYouInAwhile,
		//PendingQuestionsToAnswer,
		//PendingConditionsToAddress,
		//ReminderToFollowUpOnRecommedation
		//RecommendationsUpdate,
	}
	public class EmailTemplateModel : ItemBaseWithRef
	{
		public override string RefId => this.TemplateId.ToString();

		public EmailTemplateModel() { }
		public EmailTemplateModel( EmailTemplateId templateId, string subj, string html, string text )
		{
			this.TemplateId = templateId;
			this.Subject = subj;
			this.Html = html;
			this.Text = text;
		}

		public Guid EmailTemplateId
		{
			get { return this.ItemId; }
			private set { this.ItemId = value; }
		}

		public EmailTemplateId TemplateId { get; private set; }
		public string Subject { get; private set; }
		public string Html { get; private set; }
		public string Text { get; private set; }

		public void UpdateSubject( string subject ) => this.Subject = subject;
		public void UpdateHtml( string html ) => this.Html = html;
		public void UpdateText( string text ) => this.Text = text;
	}

	public class EmailTemplateProcessor
	{
		public EmailTemplateProcessor( EmailTemplateModel cachedTemplate, Dictionary<string, string> customParameters, params object[] items )
		{
			var subj = string.Copy( cachedTemplate.Subject );

			this.Template = new EmailTemplateModel( cachedTemplate.TemplateId, subj, cachedTemplate.Html, cachedTemplate.Text );
			this.CustomParameters = customParameters ?? new Dictionary<string, string>();
			this.Items = items;
		}

		public EmailTemplateProcessor( string error )
		{
			this.Errors.Add( error );
		}

		public void AddError( string error )
		{
			this.Errors.Add( error );
		}

		public EmailTemplateModel Template { get; private set; }

		/// <summary>Set of injectable key-value-pair items to process in the template (independ of IItem[])</summary>
		public Dictionary<string, string> CustomParameters { get; private set; }

		public object[] Items { get; private set; }

		public readonly List<string> Errors = new List<string>();
		public readonly HashSet<string> SubjParameters = new HashSet<string>();
		public readonly HashSet<string> BodyParameters = new HashSet<string>();

		public bool Successful => this.Errors.Count == 0;
	}
}
