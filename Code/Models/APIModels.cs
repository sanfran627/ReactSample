using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace SampleReact
{
	#region Request/Response models

	public class APIRequestModel : IActionConverter
	{
		public APIRequestModel() { }
		
		[JsonIgnore]
		public Guid UserId { get; set; }

		/// <summary>
		/// This property is only used via the Site Controller with UserType = Admin
		/// </summary>
		[JsonProperty("obo")]
		[JsonConverter(typeof(JsonIdConverter))]
		public Guid OBO { get; set; }

		[JsonProperty]
		[Required]
		[EnumValidation]
		public ActionType Action { get; set; }

		[JsonProperty]
		public JObject Request { get; set; }

		[JsonIgnore]
		public AuthenticationMode Mode { get; internal set; }

		public T ConvertTo<T>() where T : new()
		{
			if( this.Request != null )
			{
				//enforce UserId in the target object before serialization
				switch( this.Mode )
				{
					case AuthenticationMode.UserCookie:
						if( !this.Request.ContainsKey( "UserId" ) )
							this.Request.Add( "UserId", IDConverter.Encode( this.UserId ) );
						else
							this.Request["UserId"] = IDConverter.Encode( this.UserId );
						break;

					case AuthenticationMode.API:
						if( this.Request.ContainsKey( "UserId" ) )
							this.UserId = IDConverter.Decode( this.Request["UserId"].ToString() );
						break;
				}

				return this.Request.ToObject<T>();
			}

			return new T();
		}

		public void InjectUserId( Guid userId )
		{
			this.UserId = userId;
		}
		
		public void InjectUserId( SampleUser u ) => this.UserId = u.OBO != null ? u.OBO.UserId : u.SiteUser.UserId;
		
		/// <summary>
		/// Side note: This actually works in the model validation automatically. AWESOME.
		/// </summary>
		public UserCancelAction UserCancel() => this.Action == ActionType.UserCancel ? ConvertTo<UserCancelAction>() : null;
		public UserSignupAction UserSignup() => this.Action == ActionType.UserSignup ? ConvertTo<UserSignupAction>() : null;
		public UserUpdateDisplayNameAction UserUpdateDisplayName() => this.Action == ActionType.UserUpdateDisplayName ? ConvertTo<UserUpdateDisplayNameAction>() : null;
		public UserUpdatePasswordAction UserUpdatePassword() => this.Action == ActionType.UserUpdatePassword ? ConvertTo<UserUpdatePasswordAction>() : null;
		public UserSiteInfoAction UserSiteInfo() => this.Action == ActionType.UserSiteInfo ? ConvertTo<UserSiteInfoAction>() : null;
		public UserVerifyEmailAction UserVerifyEmail() => this.Action == ActionType.UserVerifyEmail ? ConvertTo<UserVerifyEmailAction>() : null;
	}

	public class ResponseData : Dictionary<ResponseType, object> { }

	public class APIResponseModel
	{
		[JsonProperty( "codeText" )]
		public ResponseCode Code { get; set; }
		[JsonProperty( "code" )]
		public int CodeNum => ( int ) this.Code;
		public string Message { get; set; }
		public ResponseData Data { get; set; }

		public APIResponseModel()
		{
			this.Code = ResponseCode.Ok;
			this.Message = string.Empty;
		}

		public APIResponseModel( ResponseModel response ) : this()
		{
			this.Code = response.Code;
			this.Message = response.Message;
		}

		public APIResponseModel( ResponseCode code, string message ) : this()
		{
			this.Code = code;
			this.Message = message;
		}

		public APIResponseModel( Exception ex ) : this()
		{
			this.Code = ResponseCode.Internal_UnhandledError;
			this.Data = new ResponseData { { ResponseType.Error, ex } };
		}

		public void SetMessage( string message )
		{
			if( this.Code != ResponseCode.Ok )
				this.Message = message;
		}

		public static APIResponseModel Success()
		{
			return new APIResponseModel();
		}

		public static APIResponseModel Error( ResponseCode code, string message = null )
		{
			return new APIResponseModel( code, message ?? string.Empty );
		}

		public static APIResponseModel Error( ResponseModel response )
		{
			return new APIResponseModel( response.Code, response.Message ?? string.Empty );
		}

		public static APIResponseModel Exception( Exception ex )
		{
			return new APIResponseModel( ex );
		}

		public static APIResponseModel Result( ResponseModel response )
		{
			var model = new APIResponseModel();
			model.Code = response.Code;
			model.Message = response.Message;
			return model;
		}

		public static APIResponseModel ResultWithData( ResponseData data )
		{
			var model = new APIResponseModel();
			model.Code = data == null ? ResponseCode.NoData : ResponseCode.Ok;
			model.Message = string.Empty;
			model.Data = data;
			return model;
		}

		public static APIResponseModel ResultWithData( ResponseType type, object data )
		{
			var model = new APIResponseModel();
			model.Code = data == null ? ResponseCode.NoData : ResponseCode.Ok;
			model.Message = string.Empty;
			model.Data = new ResponseData { { type, data } };
			return model;
		}

		public static APIResponseModel ResultNoData()
		{
			var model = new APIResponseModel();
			model.Code = ResponseCode.NoData;
			return model;
		}

		public string ToJson() => JsonConvert.SerializeObject( this, JSON.CamelSerializationSettings );
	}

	#endregion

}
