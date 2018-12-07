using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Microsoft.WindowsAzure.Storage.Table;

namespace SampleReact
{
	public class ActionProcessorLocal : IActionProcessor
	{
		IOptions<AppSettings> OptionSettings { get; set; }
		IHttpContextAccessor HttpContextAccessor = null;
		IDataManager DataManager = null;
		IPasswordService PasswordManager = null;
		IEmailProvider EmailProvider = null;
		ISlackProvider SlackProvider = null;

		public ActionProcessorLocal(
			IOptions<AppSettings> settings
			, IHttpContextAccessor httpContextAccessor
			, IDataManager dataManager
			, IPasswordService passwordManager
			, IEmailProvider emailProvider
			, ISlackProvider slackProvider
			)
		{
			this.OptionSettings = settings;
			this.HttpContextAccessor = httpContextAccessor;
			this.DataManager = dataManager;
			this.PasswordManager = passwordManager;
			this.EmailProvider = emailProvider;
			this.SlackProvider = slackProvider;
		}

		public async Task<APIResponseModel> Process( APIRequestModel request, SampleUser user = null )
		{
			switch( request.Action )
			{
				case ActionType.Status: throw new NotImplementedException();
				//case ActionType.AdminDeleteUser: return await this.Process( user, request.AdminDeleteUser() );
				//case ActionType.AdminReload: return await this.Process( user, request.AdminReload() );

				//case ActionType.InternalSendEmailVerification: return await this.Process( request.InternalSendEmailVerification() );

				case ActionType.UserCancel: return await this.Process( user, request.UserCancel() );
				case ActionType.UserSignup: return await this.Process( request.UserSignup() );
				case ActionType.UserSiteInfo: return await this.Process( user, request.UserSiteInfo() );
				case ActionType.UserUpdateDisplayName: return await this.Process( user, request.UserUpdateDisplayName() );
				case ActionType.UserUpdatePassword: return await this.Process( user, request.UserUpdatePassword() );
				case ActionType.UserVerifyEmail: return await this.Process( request.UserVerifyEmail() );
				default: return APIResponseModel.Error( ResponseCode.Internal_UnhandledError, "ActionType not implemented in Action Processor" );
			}
		}

		#region User
		
		public async Task<APIResponseModel> Process( UserSignupAction action )
		{
			// just be safe
			action.Email = action.Email.ToLower();

			if( string.IsNullOrWhiteSpace( action.Email ) ) return APIResponseModel.Error( ResponseCode.InvalidParameter, "Email" );
			if( string.IsNullOrWhiteSpace( action.Password ) ) return APIResponseModel.Error( ResponseCode.InvalidParameter, "Password" );
			if( action.Language == LanguageId.None ) action.Language = LanguageId.en;

			var passwordStrength = new PasswordStrengthValidator().Test( action.Password );
			if( !passwordStrength.Good ) return APIResponseModel.Error( ResponseCode.InvalidParameter, "Password Strength" );

			// prepare the user 
			UserPasswordModel password = new UserPasswordModel( await this.PasswordManager.CreatePasswordHash( action.Password ) );
			
			UserModel user = UserModel.Create(UserType.Standard, UserStatusId.Registered, action.Language, action.Email, password, action.DisplayName );
			
			var rs = await this.DataManager.CreateUserAsync( user );

			// failed - get out
			if( rs.Code != ResponseCode.Ok )
			{
				//await this.DataManager.LogEventAsync( LogEventModel.Failure( action.Action, user.ToJson(), user.UserId ) );
				return APIResponseModel.Result( rs );
			}

			try
			{
				var j = new Newtonsoft.Json.Linq.JObject(
						new Newtonsoft.Json.Linq.JProperty( "userId", user.UserId.Encode() ),
						new Newtonsoft.Json.Linq.JProperty( "email", user.Email ?? "{null}" ),
						new Newtonsoft.Json.Linq.JProperty( "name", user.Name ?? "{null}" )
				).ToString(Newtonsoft.Json.Formatting.Indented );
				this.DataManager.WriteEvent( "user-created", j ); // don't await - fire & forget
			}
			catch { }


			// move everything from the email address to the user
			try
			{
				rs = await this.DataManager.ConvertEmailToUserId( user );
				if( rs.Code != ResponseCode.Ok )
				{
					await this.DataManager.LogErrorAsync("Process(UserSignupAction)", rs.ToJson() );
				}
			}
			catch( Exception ex )
			{
				this.DataManager.LogExceptionAsync( "Process(UserSignupAction)", ex );
			}

			// pipeline the email verification request
			try
			{
				APIResponseModel rs1 = await this.Process( new InternalSendEmailVerificationAction() { UserId = user.UserId } );
			}
			catch( Exception ex )
			{
				this.DataManager.LogErrorAsync( "InternalSendEmailVerificationAction", ex.Message + Environment.NewLine + ( ex.StackTrace ?? string.Empty ) );
			}

			this.SlackProvider.Send( $"New user! {action.Email}" ); //fire and forget			

			ResponseData response = new ResponseData();
			response.Add( ResponseType.User, new UserViewModel( user ) );
			return APIResponseModel.ResultWithData( response  );
		}
		
		public async Task<APIResponseModel> Process( SampleUser userContext, UserCancelAction action )
		{
			UserModel user = userContext.OBO != null ? userContext.OBO : userContext.SiteUser;

			var rs = await this.DataManager.DeleteUserAsync( user );

			return APIResponseModel.Error( rs.Code, rs.Message );
		}

		public async Task<APIResponseModel> Process( SampleUser userContext, UserUpdatePasswordAction action )
		{
			UserModel user = userContext.OBO != null ? userContext.OBO : userContext.SiteUser;

			if( string.IsNullOrWhiteSpace( action.NewPassword ) ) return APIResponseModel.Error( ResponseCode.InvalidParameter, "Password" );

			var passwordStrength = new PasswordStrengthValidator().Test( action.NewPassword );
			if( !passwordStrength.Good ) return APIResponseModel.Error( ResponseCode.InvalidParameter, "Password Strength" );

			if( !(await this.PasswordManager.Verify( action.OldPassword, user.Password.Hash ) ) )
			{
				return APIResponseModel.Error( ResponseCode.InvalidCredentials );
			}
			else
			{
				var pwd = new UserPasswordModel( await this.PasswordManager.CreatePasswordHash( action.NewPassword ) );
				var rs = await this.DataManager.UpdateEntityProperty( EntityTableType.user, UserEntity.UpdatePasswordBuilder( user.UserId, pwd, PasswordMode.UpdatePassword ) );
				if( rs.Code != ResponseCode.Ok ) return APIResponseModel.Error( rs.Code, rs.Message );
			}

			this.DataManager.WriteEvent( "user-password-updated", action ); // don't await - fire & forget

			return APIResponseModel.Success();
		}

		public async Task<APIResponseModel> Process( SampleUser userContext, UserUpdateDisplayNameAction action )
		{
			UserModel user = userContext.OBO != null ? userContext.OBO : userContext.SiteUser;

			var dte = UserEntity.UpdateDisplayName( user.UserId, action.DisplayName );
			var rs = await this.DataManager.UpdateEntityProperty( EntityTableType.user, dte );
			if( rs.Code != ResponseCode.Ok )
				return APIResponseModel.Error( rs.Code, rs.Message );

			return APIResponseModel.Success();
		}
		
		#endregion

		#region Email

		public async Task<APIResponseModel> Process( InternalSendEmailVerificationAction action )
		{
			var rsU = await this.DataManager.QueryUser( action.UserId );
			if( rsU.Code != ResponseCode.Ok ) return APIResponseModel.Result( rsU.ToResponse() );

			var user = rsU.Data[0].User;

			if( user == null ) return APIResponseModel.Error( ResponseCode.NotFound );
			if( user.EmailVerification == null ) return APIResponseModel.Success();

			string vs = await this.PasswordManager.BuildEmailVerificationString( user.UserId, user.EmailVerification.Code );
			
			var url = this.EmailProvider.BaseUrl() + "verify?code=" + Uri.EscapeDataString( vs );

			var cd = new Dictionary<string,string>{
				{ "EmailVerificationUrl", url },
				{ "DisplayName", user.DisplayName }
			};

			var rs =  await this.EmailProvider.ProcessTemplate( action.UserId, EmailTemplateId.EmailVerification, user.Email, cd, user );
			return APIResponseModel.Result( rs );
		}
				
		#endregion
		
		#region UserVerifyEmailAction

		public async Task<APIResponseModel> Process( UserVerifyEmailAction action )
		{
			var rs = await this.DataManager.VerifyContactAsync( action.UserId, ContactMethodType.Email, action.Code );
			switch( rs.Code )
			{
				case ResponseCode.Ok:

					this.DataManager.WriteEvent( "user-email-verified", action ); // don't await - fire & forget
					break;

				default:

					// don't wait, just queue
					this.DataManager.WriteEvent( action.Action.ToString(), Newtonsoft.Json.JsonConvert.SerializeObject( rs ) );
					break;
			}

			return APIResponseModel.Error( rs.Code, rs.Message );
		}

		#endregion
		
		#region UserSiteInfoAction 

		public async Task<APIResponseModel> Process( SampleUser userContext, UserSiteInfoAction action )
		{
			UserModel user = null;
			if( userContext.SiteUser != null )
			{
				user = userContext.OBO != null ? userContext.OBO : userContext.SiteUser;
			}

			if( user.SiteSettings == null ) user.SiteSettings = new UserSiteInfoModel();
			foreach( var kvp in action.Settings )
			{
				if( user.SiteSettings.Settings.ContainsKey( kvp.Key ) )
					user.SiteSettings.Settings[kvp.Key] = kvp.Value;
				else
					user.SiteSettings.Settings.Add( kvp.Key, kvp.Value );
			}
			var dte = UserEntity.UpdateSiteBuilder( user.UserId, user.SiteSettings );
			var rs = await this.DataManager.UpdateEntityProperty(  EntityTableType.user, dte );
			if( rs.Code != ResponseCode.Ok ) return APIResponseModel.Error( rs.Code, rs.Message );
			return APIResponseModel.ResultWithData( ResponseType.SiteInfo, new UserSiteInfoViewModel( user.SiteSettings ) );
		}

		#endregion

		#region AdminReloadAction 
		
		/*
		 * Note: Code omitted, but this is an example of using either a backdoor api call or an admin user via signalR sending
		 *		 an instruction for the system to self-refresh.
		 */

		//public async Task<APIResponseModel> Process( SampleUser userContext, AdminReloadAction action )
		//{
		//	// Add more support for context once we have more API users (including external users)
		//	if( userContext.APIUser.Type != APIAccountType.Core )
		//		return APIResponseModel.Error( ResponseCode.InvalidCredentials );

		//	if( action == null ) return APIResponseModel.Error( ResponseCode.InvalidParameter, "model empty" );

		//	if( action.Type == DataManagerDataType.System )
		//		return APIResponseModel.Result( await this.DataManager.LoadSystemDataAsync( true ) );

		//	return APIResponseModel.Success();
		//}

		#endregion
			
		#region Admin - Delete User

		//public async Task<APIResponseModel> Process( SampleUser userContext, AdminDeleteUserAction action )
		//{
		//	// Add more support for context once we have more API users (including external users)
		//	if( userContext.APIUser.Type != APIAccountType.Core )
		//		return APIResponseModel.Error( ResponseCode.InvalidCredentials );

		//	if( action == null ) return APIResponseModel.Error( ResponseCode.InvalidParameter, "model empty" );

		//	if( action.Field == AdminDeleteUserFieldOption.UserId && ( action.UserId == null || action.UserId == Guid.Empty ) ) return APIResponseModel.Error( ResponseCode.InvalidParameter, "Missing UserId" );
		//	if( action.Field == AdminDeleteUserFieldOption.Email && string.IsNullOrWhiteSpace( action.Email ) ) return APIResponseModel.Error( ResponseCode.InvalidParameter, "Missing Email" );

		//	var rsU = await this.DataManager.QueryUser( action.UserId );
		//	if( rsU.Code != ResponseCode.Ok ) return APIResponseModel.Error( ResponseCode.NotFound, "User not found" );

		//	var u = rsU.Data[0].User;
		//	var rs = await this.DataManager.DeleteUserAsync( u );

		//	this.DataManager.WriteEvent( "user-deleted", u ); // don't await - fire & forget
		//	return APIResponseModel.Error( rs.Code, rs.Message );
		//}

		#endregion
		
	}
}
