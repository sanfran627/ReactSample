using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Cors;

namespace SampleReact
{
	public class Credentials
	{
		public string Username { get; set; }
		public string Password { get; set; }
		public LanguageId Language { get; set; }
	}

	//[AllowAnonymous]
	[EnableCors("CorsPolicy")]
	public class SiteHub : Hub
	{
		ICredentialsService CredentialsManager = null;
		AppSettings Settings = null;
		IDataManager DataManager = null;
		IActionProcessor ActionProcessor = null;
		ITokenService TokenManager = null;

		public SiteHub(
			IOptions<AppSettings> settings = null
			, IDataManager d = null
			, ICredentialsService c = null
			, IActionProcessor a = null
			, ITokenService t = null
			)
		{
			this.Settings = settings.Value;
			this.DataManager = d;
			this.CredentialsManager = c;
			this.ActionProcessor = a;
			this.TokenManager = t;
		}

		public async Task Config()
		{
			//do something
			var response = !this.DataManager.Loaded(DataManagerDataType.System)
				? APIResponseModel.Error(ResponseCode.Loading)
				: APIResponseModel.ResultWithData(ResponseType.Metadata, new MetadataViewModel(this.DataManager.Metadata));

			await Clients.Caller.SendAsync("config", response.ToJson());
		}

		#region Messages

		public async Task Signup(string messageId, UserSignupAction action)
		{
			APIResponseModel response = null;
			string token = null;

			try
			{
				var msg = this.DataManager.Metadata.GetTextForResponseCode(ResponseCode.Loading, action.Language);

				if (!this.DataManager.Loaded(DataManagerDataType.System))
				{
					response = APIResponseModel.Error(ResponseCode.Loading, msg);
				}
				else
				{
					response = await this.ActionProcessor.Process(action);
					if (response.Code == ResponseCode.Ok)
						token = this.TokenManager.BuildToken((response.Data[ResponseType.User] as UserViewModel).UserId);
				}

				msg = this.DataManager.Metadata.GetTextForResponseCode(response.Code, action.Language);
				response.SetMessage(msg);
			}
			catch (Exception ex)
			{
				response = APIResponseModel.Exception(ex);
			}

			await Clients.Caller.SendAsync("signup", messageId, response.ToJson(), token);
		}

		public async Task Signin(string messageId, Credentials credentials)
		{
			//do something
			var response = await this.SigninAsync(credentials);
			string token = string.Empty;

			if (response.Code == ResponseCode.Ok)
				token = this.TokenManager.BuildToken((response.Data[ResponseType.User] as UserViewModel).UserId);

			await Clients.Caller.SendAsync("signin", messageId, response.ToJson(), token);
		}

		[Authorize]
		[HubMethodName("obo")]
		public async Task OBO(string messageId, string oboEmail)
		{
			APIResponseModel response = null;
			string token = string.Empty;

			UserModel oboUser = null;
			var rsO = await this.DataManager.QueryUser(oboEmail);
			if (rsO.Code == ResponseCode.Ok)
			{
				oboUser = rsO.Data[0].User;
				if (!this.DataManager.Loaded(DataManagerDataType.System))
					response = APIResponseModel.Error(ResponseCode.Loading);
				else
					response = await Check(oboUser);

				token = this.TokenManager.BuildToken((response.Data[ResponseType.User] as UserViewModel).UserId, oboUser);
			}
			else
				response = APIResponseModel.Error(ResponseCode.NotFound, "User not found");

			await Clients.Caller.SendAsync("obo", messageId, response.ToJson(), token);
		}

		[Authorize]
		public async Task RevertOBO(string messageId)
		{
			UserModel user = this.SampleUser != null ? this.SampleUser.SiteUser : null;
			var token = this.TokenManager.BuildToken(user.UserId);

			APIResponseModel response = null;

			if (!this.DataManager.Loaded(DataManagerDataType.System))
				response = APIResponseModel.Error(ResponseCode.Loading);
			else
				response = await Check(user);

			await Clients.Caller.SendAsync("robo", messageId, response.ToJson(), token);
		}

		[HubMethodName("verify-email")]
		public async Task VerifyEmail(string messageId, string code)
		{
			APIResponseModel response = null;
			if (!Crypto.TryParseEmailVerificationString(code, out Guid userId, out string verificationCode))
				response = APIResponseModel.Error(ResponseCode.EmailVerificationCodeExpired, "The verification email has expired. Please request another one before continuing");
			else
			{
				var action = new UserVerifyEmailAction(userId, verificationCode);
				response = await this.ActionProcessor.Process(action);
			}

			if (string.IsNullOrWhiteSpace(response.Message))
				response.SetMessage(this.DataManager.Metadata.GetTextForResponseCode(response.Code, LanguageId.en));

			await Clients.Caller.SendAsync("verify-email", messageId, response.ToJson());
		}

		[Authorize]
		public async Task Check()
		{
			#region check

			APIResponseModel response = null;

			//called because the user is cached by the hub, and we need to force reload it if there are data changes
			await this.ReloadUser();

			if (!this.DataManager.Loaded(DataManagerDataType.System))
				response = APIResponseModel.Error(ResponseCode.Loading);
			else
				response = await Check(this.User);

			if (string.IsNullOrWhiteSpace(response.Message))
				response.SetMessage(this.DataManager.Metadata.GetTextForResponseCode(response.Code, LanguageId.en));

			await Clients.Caller.SendAsync("check", response.ToJson());
			#endregion
		}
		
		[Authorize]
		public async Task Request(string messageId, string payload)
		{
			APIResponseModel response = null;
			if (!string.IsNullOrWhiteSpace(payload))
			{
				//do something
				APIRequestModel model = Newtonsoft.Json.JsonConvert.DeserializeObject<APIRequestModel>(payload);
				model.Mode = AuthenticationMode.UserCookie;
				model.InjectUserId(this.SampleUser);
				response = await this.ProcessAsync(model, this.SampleUser);
			}
			else
			{
				await Clients.Caller.SendAsync("signoff");
				return;
			}

			if (response != null)
			{
				if (string.IsNullOrWhiteSpace(response.Message))
					response.SetMessage(this.DataManager.Metadata.GetTextForResponseCode(response.Code, LanguageId.en));

				await Clients.Caller.SendAsync("response", messageId, response.ToJson());
			}
		}

		[Authorize] public async Task SendResponse(string connectionId, string payload) => await Clients.Client(connectionId).SendAsync("response", payload);
		[Authorize] public async Task SendMessageToGroup(string group, string message) => await Clients.Groups(group).SendAsync("message", message);

		#endregion

		#region Connection/Disconnect

		public override async Task OnConnectedAsync()
		{
			Console.WriteLine("Hub connection established!");
			//await Groups.AddToGroupAsync( Context.ConnectionId, "SignalR Users" );
			await Clients.Caller.SendAsync("connected");
			await base.OnConnectedAsync();
		}

		public override async Task OnDisconnectedAsync(Exception exception)
		{
			//await Groups.RemoveFromGroupAsync( Context.ConnectionId, "SignalR Users" );
			await base.OnDisconnectedAsync(exception);
		}

		#endregion

		#region Internal Helpers

		SampleUser _sampleUser = null;

		SampleUser SampleUser
		{
			get
			{
				if (_sampleUser == null)
				{
					if (this.Context.User == null) return null;
					if (this.Context.User is SampleUser) return this.Context.User as SampleUser;
					_sampleUser = this.CredentialsManager.TryPrepareUserFrom(this.Context, out SampleUser u) ? u : null;
				}
				return _sampleUser;
			}
		}

		protected UserModel User
		{
			get => this.SampleUser != null ? this.SampleUser.OBO != null ? this.SampleUser.OBO : this.SampleUser.SiteUser : null;
			set
			{
				if (this.SampleUser == null) return;
				if (this.SampleUser.OBO != null)
					this.SampleUser.OBO = value;
				else
					this.SampleUser.SiteUser = value;
			}
		}

		private async Task ReloadUser()
		{
			var rsUser = await this.DataManager.QueryUser(this.User.UserId);
			if (rsUser.Code != ResponseCode.Ok) this.User = rsUser.Data[0].User;
		}

		private async Task<APIResponseModel> ProcessAsync(APIRequestModel model, SampleUser user)
		{
			var msg = this.DataManager.Metadata.GetTextForResponseCode(ResponseCode.Loading, user.SiteUser.Language);

			if (!this.DataManager.Loaded(DataManagerDataType.System)) return APIResponseModel.Error(ResponseCode.Loading, msg);

			model.Mode = AuthenticationMode.UserCookie;

			if (user.OBO != null)
			{
				//use the OBO user
				model.InjectUserId(user.OBO.UserId);
			}
			else
			{
				//business as usual
				model.InjectUserId(user.SiteUser.UserId);
			}

			if (user.SiteUser.Type == UserType.Admin)
			{
			}

			APIResponseModel result = null;

			// nothing non-user get through here...
			if (model.Action > ActionType.UserCancel) return APIResponseModel.Error(ResponseCode.NotFound);

			// ok carry on
			result = await this.ActionProcessor.Process(model, user);

			if (string.IsNullOrWhiteSpace(result.Message))
			{
				msg = this.DataManager.Metadata.GetTextForResponseCode(result.Code, user.SiteUser.Language);
				result.SetMessage(msg);
			}

			return result;
		}

		private async Task<APIResponseModel> SigninAsync(Credentials credentials)
		{
			var msg = this.DataManager.Metadata.GetTextForResponseCode(ResponseCode.Loading, credentials.Language);

			if (!this.DataManager.Loaded(DataManagerDataType.System)) return APIResponseModel.Error(ResponseCode.Loading, msg);
			var rs = await this.DataManager.SigninUserAsync(credentials.Username, credentials.Password, null);
			if (rs.Code != ResponseCode.Ok) return APIResponseModel.Error(rs.Code, this.DataManager.Metadata.GetTextForResponseCode(rs.Code, credentials.Language));

			var user = rs.Data[0];

			ResponseData data = new ResponseData();
			data.Add(ResponseType.User, new UserViewModel(user));
			if (user.SiteSettings != null) data.Add(ResponseType.SiteInfo, new UserSiteInfoViewModel(user.SiteSettings));
			return APIResponseModel.ResultWithData(data);
		}

		private async Task<APIResponseModel> Check(UserModel user)
		{
			if (!this.DataManager.Loaded(DataManagerDataType.System)) return APIResponseModel.Error(ResponseCode.Loading);

			ResponseData data = new ResponseData();
			data.Add(ResponseType.User, new UserViewModel(user));
			if (user.SiteSettings != null) data.Add(ResponseType.SiteInfo, new UserSiteInfoViewModel(user.SiteSettings));
			return APIResponseModel.ResultWithData(data);
		}
		
		#endregion
	}
}