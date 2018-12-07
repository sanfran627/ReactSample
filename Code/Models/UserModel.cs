using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace SampleReact
{
	public class UserComposite
	{
		public UserComposite( Guid userId ) => this.UserId = userId;

		public Guid UserId { get; set; }
		public UserModel User { get; set; }
		public UserEntity UserEntity { get; set; }
	}

	public class UserModel
	{
		#region Constructors

		public UserModel()
		{
			this.UserId = Guid.NewGuid();
			this.Password = new UserPasswordModel();
			this.StatusId = UserStatusId.None;
			this.Language = LanguageId.en;
			this.Type = UserType.Standard;
			this.Email = string.Empty;
			this.EmailOld = string.Empty;
			this.Name = string.Empty;
		}

		public UserModel( Guid userId )
		{
			this.UserId = userId;
		}

		public static UserModel Create( 
			UserType type
			, UserStatusId status
			, LanguageId language
			, string email
			, UserPasswordModel password
			, string displayName )
		{
			return new UserModel
			{
				StatusId = status,
				Type = type,
				Language = language,
				Email = email,
				EmailVerification = status == UserStatusId.Registered ? new UserContactVerificationModel( ContactMethodType.Email ) : null,
				Password = password,
				Name = displayName
			};
		}
		
		#endregion

		[JsonIgnore] public Guid UserId { get; set; }
		[JsonIgnore] public DateTime Updated { get; set; }
		public DateTime Created { get; set; }
		public UserPasswordModel Password { get; set; }
		public UserStatusId StatusId { get; set; }
		public LanguageId Language { get; set; }
		public UserType Type { get; set; }
		public string Email { get; set; }
		public bool EmailInvalid { get; set; }
		public UserContactVerificationModel EmailVerification { get; set; }
		public string EmailOld { get; set; }
		public string Name { get; set; }
		public UserSiteInfoModel SiteSettings { get; set; }
		public string DisplayName => !string.IsNullOrWhiteSpace( this.Name ) ? this.Name : this.Email;

		public void Verified( ContactMethodType type )
		{
			switch( type )
			{
				case ContactMethodType.Email:
					this.EmailVerification = null;
					if( this.StatusId == UserStatusId.Registered )
						this.StatusId = UserStatusId.Verified;
					break;
			}
		}

		public void MarkContactMethodInvalid( ContactMethodType method )
		{
			switch( method )
			{
				case ContactMethodType.Email: this.EmailInvalid = true; return;
			}
		}
	}

	public class UserSiteInfoModel
	{
		public UserSiteInfoModel() { }
		public UserSiteInfoModel( UserSiteInfoAction action ) : this() => this.Settings = action.Settings;
		public readonly Dictionary<string, string> Settings = new Dictionary<string, string>();
	}
	
	public class UserContactVerificationModel
	{
		public UserContactVerificationModel() { }

		public UserContactVerificationModel( ContactMethodType method, bool verified = false )
		{
			switch ( method )
			{
				case ContactMethodType.Email:
					this.Code = IDConverter.Generate();
					break;

				case ContactMethodType.Mobile:
					this.Code = CodeGenerator.GenerateMobileCode();
					break;
			}

			this.ExpiresAt = DateTime.UtcNow.AddHours( 1 );
		}

		public string Code { get; set; }
		public DateTime? ExpiresAt { get; set; }
	}
	
	public class UserPasswordModel
	{
		public UserPasswordModel() { }

		public UserPasswordModel( string hashedPassword ) => this.Hash = hashedPassword;

		public string Hash { get; set; }
		public int Strikes { get; set; }
		public bool Locked { get; set; }

		public void IncrementStrikes( bool locked = false )
		{
			this.Strikes++;
			this.Locked = locked;
		}

		public void Reset()
		{
			this.Strikes = 0;
			this.Locked = false;
		}
	}
}
