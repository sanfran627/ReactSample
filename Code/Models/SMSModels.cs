using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace SampleReact
{
	public class SendSMSModel
	{
		public SendSMSModel() { }

		public SendSMSModel( string mobile, string message )
		{
			this.UserId = Guid.Empty;
			this.Created = DateTime.UtcNow;
			this.Mobile = mobile;
			this.Message = message;
		}

		public SendSMSModel( Guid userId, string mobile, string message )
		{
			this.UserId = userId;
			this.Created = DateTime.UtcNow;
			this.Mobile = mobile;
			this.Message = message;
		}
		
        [JsonProperty]
		public Guid UserId { get; private set; }
		[JsonProperty]
		public DateTime Created { get; private set; }
		[JsonProperty]
		public string Mobile { get; private set; }
		[JsonProperty]
		public string Message { get; private set; }
		[JsonIgnore]
		public int Attempts { get; set; }
		[JsonIgnore]
		public SMSStatus Status { get; set; }
		[JsonIgnore]
		public DateTime Date { get; set; }
		[JsonIgnore]
		public string Error { get; set; }
		[JsonIgnore]
		public string SmsStatus { get; set; }
		[JsonIgnore]
		public SMSErrorCode ErrorCode { get; set; }
		[JsonIgnore]
		public string ErrorMessage { get; set; }

		public void SetStatus( SMSStatus status, int? errorCode = null, string errorMessage = null )
		{
			this.Attempts++;
			this.Status = status;
			this.Error = $"{errorCode}: {errorMessage}";
			this.ErrorCode = errorCode != null ? (SMSErrorCode)errorCode : SMSErrorCode.None;
			this.ErrorMessage = errorMessage;
		}

		public string ToJson( bool format = false ) => JsonConvert.SerializeObject( this, format ? Formatting.Indented : Formatting.None );
	}

	public enum SMSPrepStatus
	{
		Ready = 0,
		Processing = 1,
		ErrorGeneratingSMS = 2,
		Processed = 255
	}
	
	public enum SMSStatus
	{
		accepted,
		queued,
		sending,
		sent,
		receiving,
		received,
		delivered,
		undelivered,
		failed
	}

	public enum SMSErrorCode
	{
		/*
			30001	Queue overflow	You tried to send too many messages too quickly and your message queue overflowed. Try sending your message again after waiting some time.
			30002	Account suspended	Your account was suspended between the time of message send and delivery. Please contact Twilio.
			30003	Unreachable destination handset	The destination handset you are trying to reach is switched off or otherwise unavailable.
			30004	Message blocked	The destination number you are trying to reach is blocked from receiving this message (e.g. due to blacklisting).
			30005	Unknown destination handset	The destination number you are trying to reach is unknown and may no longer exist.
			30006	Landline or unreachable carrier	The destination number is unable to receive this message. Potential reasons could include trying to reach a landline or, in the case of short codes, an unreachable carrier.
			30007	Carrier violation	Your message was flagged as objectionable by the carrier. In order to protect their subscribers, many carriers have implemented content or spam filtering. Learn more about carrier filtering
			30008	Unknown error	The error does not fit into any of the above categories.
			30009	Missing segment	One or more segments associated with your multi-part inbound message was not received.
			30010	Message price exceeds max price.	The price of your message exceeds the max price parameter.

			63001	Channel could not authenticate the request	Channel authentication credentials are incorrect. Please check the credentials in Channel page in Console or re-authenticate with the Channel.
			63002	Channel could not find From address	The From address does not map to any configured Channels. Check that you are using the correct Channel endpoint address from the Channel page.
			63003	Channel could not find To address	The To address is incorrect.
			63005	Channel did not accept given content	
			63006	Could not format given content for the channel	
			63007	Twilio could not find a Channel with the specified From address	The From address does not map to any configured Channels. Check that you are using the correct Channel endpoint address from the Channel page.
			63008	Could not execute the request because the channel module has been misconfigured	Please check the Channel configuration in Twilio.
			63009	Channel returned an error when executing the request	Please see Channel specific error message for more information.
			63010	Channels - Twilio Internal error	
			63011	Invalid Channel request	
			63012	Channel returned an internal error that prevented it from completing the request	
			63013	This message send failed because it violates Channel provider's policy.	Please see Channel specific error message for more information.
			63014	This message failed to be delivered to the user because it was blocked by a user action.	Please see Channel specific error message for more information.
		*/
		None = 0,
		QueueOverflow = 30001,
		AccountSuspended = 30002,
		UnreachableDestination = 30003,
		MessageBlocked = 30004,
		UnknownDestinationHandset = 30005,
		LandlineOrUnreachableCarrier = 30006,
		CarrierViolation = 30007,
		UnknownError = 30008,
		MissingSegment = 30009,
		MessagePriceExceedsMaxPrice = 30010,
		ChannelCouldNotAuthenticate = 63001,
		ChannelCouldNotFindFromAddress = 63002,
		ChannelCouldNotFindToAddress = 63003,
		ChannelDidNotAcceptContent = 63005,
		ChannelCouldNotFormatContent = 63006,
		ChannelCouldNotFindChannel = 63007,
		ChannelModuleMisconfigured = 63008,
		ChannelRandomError = 63009,
		ChannelTwilioInternalError = 63010,
		ChannelInvalidRequest = 63011,
		ChannelProviderInternalError = 63012,
		ChannelViolatesPolicy = 63013,
		ChannelBlockedByUserAction = 63014
	}

	public enum SMSTemplateId
	{
		None = 0,
		Test,
		MobileVerification
	}

	public class SMSTemplateModel : ItemBaseWithRef
	{
		public override string RefId => this.TemplateId.ToString();

		public SMSTemplateModel() { }
		public SMSTemplateModel( SMSTemplateId templateId, string message )
		{
			this.TemplateId = templateId;
			this.Message = message;
		}

		[JsonProperty]
		public Guid SMSTemplateId
		{
			get { return this.ItemId; }
			private set { this.ItemId = value; }
		}

		[JsonProperty]
		public SMSTemplateId TemplateId { get; private set; }
		[JsonProperty]
		public string Message { get; private set; }

		public void UpdateMessage( string message ) => this.Message = message;
	}

	public class SMSTemplateProcessor
	{
		public SMSTemplateProcessor( SMSTemplateModel cachedTemplate, Dictionary<string, string> customParameters, params IItem[] items )
		{
			//make a copy as we do NOT want to modify the cached object
			this.Template = new SMSTemplateModel( cachedTemplate.TemplateId, cachedTemplate.Message );
			this.CustomParameters = customParameters ?? new Dictionary<string, string>();
			this.Items = items;
		}

		public SMSTemplateProcessor( string error )
		{
			this.Errors.Add( error );
		}

		public void AddError( string error )
		{
			this.Errors.Add( error );
		}

		public SMSTemplateModel Template { get; private set; }
		/// <summary>
		/// Set of injectable key-value-pair items to process in the template (independ of IItem[])
		/// </summary>
		public Dictionary<string, string> CustomParameters { get; private set; }
		public IItem[] Items { get; private set; }

		public readonly List<string> Errors = new List<string>();
		public readonly HashSet<string> MessageParameters = new HashSet<string>();

		public bool Successful => this.Errors.Count == 0;
	}
}
