using Newtonsoft.Json;
using System;
using System.Collections.Generic;


namespace SampleReact
{
	public interface IAction
	{
		ActionType Action { get; }
		Guid ActionId { get; set; }
		string ToJson();
	}

	public abstract class ActionBase : IAction
	{
		public ActionBase()
		{
			this.ActionId = Guid.NewGuid();
		}

		public Guid ActionId { get; set; }
		public abstract ActionType Action { get; }
		public string ToJson() => JsonConvert.SerializeObject( this );
	}


	public abstract class UserAction : ActionBase
	{
		[JsonProperty]
		[JsonConverter(typeof(JsonIdConverter))]
		public Guid UserId { get; set; }
	}
	
	public class UserSignupAction : ActionBase
	{
		string _email = null;

		public override ActionType Action => ActionType.UserSignup;

		public string DisplayName { get; set; }
		public LanguageId Language { get; set; }
		public string Email { get => this._email; set => _email = value != null ? value.ToLower() : null; }

		[JsonProperty]
		public string Password { get; set; }

		[JsonProperty]
		public string Mobile { get; set; }

		[JsonProperty]
		public bool Agree { get; set; }

		[JsonProperty]
		public bool Subscribe { get; set; }
	}
	
	public class UserSiteInfoAction : UserAction
	{
		public override ActionType Action => ActionType.UserSiteInfo;

		[JsonProperty]
		public Dictionary<string,string> Settings { get; private set; }
	}

	public class UserCancelAction : UserAction
	{
		public override ActionType Action => ActionType.UserCancel;

		[JsonProperty]
		public string Reason { get; set; }
	}

	public class UserVerifyEmailAction : UserAction
	{
		public UserVerifyEmailAction() { }
		public UserVerifyEmailAction( Guid userId, string code )
		{
			this.UserId = userId;
			this.Code = code;
		}

		public override ActionType Action => ActionType.UserVerifyEmail;
		public string Code { get; set; }
	}
	
	public class UserUpdatePasswordAction : UserAction
	{
		public override ActionType Action => ActionType.UserUpdatePassword;

		[JsonProperty]
		public string OldPassword { get; set; }

		[JsonProperty]
		public string NewPassword { get; set; }
	}

	public class UserUpdateDisplayNameAction : UserAction
	{
		public override ActionType Action => ActionType.UserUpdateDisplayName;

		[JsonProperty]
		public string DisplayName { get; set; }
	}

	public class InternalSendEmailVerificationAction : UserAction
	{
		public override ActionType Action => ActionType.InternalSendEmailVerification;
	}
}
