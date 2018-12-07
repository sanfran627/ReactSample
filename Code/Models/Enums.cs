using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SampleReact
{
	public enum EnvironmentId : Byte
	{
		Development = 0,
		Test = 5,
		Production = 10
	}
	
	public enum IncludeOptions
	{
		None = 0,
		Entities = 1, // keep the entitites that are returned as well
	}

	public enum AuthenticationType
	{
		None,
		Basic
	}

	public enum ServiceType
	{
		Local = 0,
		Remote = 1
	}

	public enum QueueName
	{
		Outbound_Email,
		Outbound_SMS,
		Outbound_Mailing,
		Event,
	}

	public enum CRUD
	{
		Create,
		Read,
		Update,
		Delete
	}

	public enum ItemTypeSource
	{
		None = 0,
		//Sql = 1,
		Blob = 2
	}

	public enum ItemTypeDirectory
	{
		System = 0,
		Core = 1,
		User = 2
	}

	public enum ItemType : Byte
	{
		None = 0,
		Metadata = 1,
		Question = 3,
		RiskFactor = 9,
		Recommendation = 10,
		APIAccount = 11,
		EmailTemplate = 13,
		SMSTemplate = 14,
		ActionItem = 15,
		RecommendationRiskFactorLink = 16,
		Promotion = 23,
		Product = 25,
	}

	public enum OutboundItemType : Byte
	{
		None = 0,
		Email = 1,
		SMS = 2,
		MailChimp
	}

	public enum PeriodId
	{
		None = 0,
		OneTime = 1,
		Daily = 2,
		Weekly = 3,
		BiWeekly = 4,
		SemiWeekly = 5,
		Monthly = 6,
		Quarterly = 7,
		SemiAnnually = 8,
		Annually = 9
	}

	public enum MetadataId
	{
		None = 0,
		Category = 1,
		Comparer = 2,
		Config = 3,
		DataType = 4,
		Environment = 5,
		Event = 6,
		Language = 7,
		Period = 8,
		Phase = 9,
		Promotion = 10,
		QuestionMode = 11,
		QuestionType = 12,
		Relationship = 13,
		ResultCode = 14,
		Subscription = 15,
		UserStatus = 16,
		Plan = 17
	}

	public enum StringType
	{
		None = 0,
		API = 1,
		UI = 2,
		API_UI = 3
	}

	/// <summary>
	/// Each enum must have an identical string representation in the SystemSettings table
	/// </summary>
	public enum ConfigId
	{
		None = 0,

		Availability_Users = 100,
		Availability_Users_Message = 101,
		Availability_Staff = 102,
		Availability_Staff_Message = 103,

		/**** ANYTHING OVER 1000 means NOT user-facing. Can be accessed via Staff Side only ****/

		MobileCodeSize = 1001,
		MobileCodeAttempts = 1002,
	}

	public enum LanguageId
	{
		None = 0,
		en = 1,
		es = 2,
	}

	public enum PractitionerType
	{
		None = 0,
		MedicalDoctor,
		NaturopathicDoctor,
		Acupuncturist,
		Nutritionist,
		Midwife,
		WellnessCoach,
		Other
	}

	public enum CategoryId
	{
		None = 0,
		Nutrition = 1,
		Lifestyle = 2,
		Exposure = 3,
		Medical = 4
	}

	public enum SubcategoryId
	{
		None = 0,
		Diet,
		Supplements,
		Sleep,
		Fitness,
		StressManagement,
		Food,
		Water,
		Household,
		HomeImprovements,
		SelfCare,
		AirQuality,
		Work,
		Electronics,
		History,
		Labs,
		Data,
		Oxidative,
		Beverage
	}

	public enum PlanNoteType
	{
		Public,
		Private
	}

	public enum PlanNoteScope
	{
		None,
		Category,
		ActionItemType,
		ActionItem,
		RiskFactor
	}

	/// <summary>
	/// Note: Any modification to this list must also be made to the BackgroundWorker
	/// </summary>
	public enum ActionType
	{
		Status = 0,
		/* begin user section */
		UserSignup,
		UserUpdateDisplayName,
		UserUpdatePassword,
		UserVerifyEmail,
		UserResetPassword,
		UserSiteInfo,
		UserCancel,
		/* end of user section */

		/* begin (internal) user section */
		InternalMarkContactMethodInvalid,
		/// <summary>Sends a verification code to the user's new email address entered</summary>
		InternalSendEmailVerification,
		InternalSendEmailChangeToOldEmail,
		/* end (internal) user section */


		/* begin admin section */
		AdminDeleteUser,
		AdminReload,
		AdminMarkUserVerified
		/* end admin section */
	}

	public enum ResponseType
	{
		Raw,
		Error,
		Metadata,
		SiteInfo,
		Strings,
		User
	}

	public enum QueryId
	{
		Adhoc,
		Metadata,
		User,
		UserFull,
	}

	public enum ActionOutcome
	{
		None = 0,
		Success = 1,
		Failure = 2
	}
	
	public enum ContactMethodType
	{
		None = 0,
		Email = 1,
		Mobile = 2
	}
	
	public enum ActionStatus
	{
		Ready = 0,
		Processing = 1,
		Error = 2
	}

	public enum OutboundStatus
	{
		Ready = 0,
		Processing = 1,
		Error = 2
	}
	
	public enum UpdateCountEnum
	{
		Decrement = 0,
		Increment = 1
	}

	public enum UserActionItem
	{
		None = 0,
		VerifyEmail = 1,
		VerifyMobile = 2,
		/// <summary>
		/// The User is on a plan (likely via a Promotion) but they haven't set their Relationshp to the plan yet
		/// </summary>
		UpdatePlanRelationshp = 3,
	}
	
	public enum UserType
	{
		Standard,
		Admin
	}

	public enum MessageDirection
	{
		From = 0,
		To = 1
	}


	public enum MailSubscriptionStatus
	{
		subscribed,
		unsubscribed,
		cleaned,
		pending,
		transactional
	}

	public enum ActionItemStatus
	{
		None,
		NotApplicable,
		Resolved,
		RecommendedAgainstByDoctor
	}

	public enum UserStatusId
	{
		None = 0,
		Registered = 1,
		Verified = 2,
		Active = 3,
		Suspended = 4,
		Closed = 5
		//PastDue = 3,
		//Current = 4,
		//Flagged = 5,
	}

	public enum DataTypeId
	{
		None = 0,
		String = 1,
		Integer = 2,
		Double = 3,
		Boolean = 4,
		Date = 5,
		JSON = 10
	}
}