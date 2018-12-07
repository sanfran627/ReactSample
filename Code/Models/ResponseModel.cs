using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SampleReact
{
	public enum ResponseCode
	{
		Ok = 0,
		SiteDown = 1,
		NoData = 2,
		NotFound = 3,
		InvalidParameter = 4,
		Loading = 5,
		NotLoaded = 6,
		InvalidVersion = 7,
		Maintenance = 8,
		Duplicate = 9,
        UsernameUnavailable = 100,
        NameUnavailable = 101,
        EmailUnavailable = 102,
        MobileUnavailable = 103,
        DuplicateEntryConditionalQuestion = 104,
        DuplicateEntryCalculatedQuestion = 105,
        QuestionCannotBeEdited = 106,
		InvalidCredentials = 107,
		InvalidEmailAddress = 108,
		InvalidEmailVerificationCode = 109,
		EmailVerificationCodeExpired = 110,
		InvalidMobileNumber = 111,
		InvalidMobileVerificationCode = 112,
		MobileVerificationCodeExpired = 113,
		RefIdAlreadyInUse = 114,
		ParticipantInviteeAlreadyOnPlan = 115,
		PlanAlreadyHasAnOwner = 116,
		PlanAlreadyHasRelationship = 117,
		PlanInvitationAlreadyProcessed = 118,
		PlanDiscountCodeInvalid = 119,
		PlanRelationshipRequired = 120,
		PlanAlreadyPurchased = 121,
		ParticipantAlreadyOnPlan = 122,

		StoragePreconditionFailed = 412,

		InsufficientPermission_Member = 10000,
		InsufficientPermission_Staff = 10001,
		Internal_DatabaseDown = 100000,
		Internal_DatabaseError = 100001,
		Internal_UnhandledError = 100002,
		Provider_Down = 100003,
		Provider_Error = 100004,
		Provider_ErrorWithDetails = 100005,
		NotImplemented = int.MaxValue
	}
	
	public class ResponseModel
	{
		public ResponseCode Code { get; set; }
		public string Friendly => this.Code.ToString();
		[Newtonsoft.Json.JsonIgnore]
		public Exception Exception { get; set; }
		public string Message { get; set; }

		//public double Duration =>
		//	this.CompletedAt != null && this.ReceivedAt != null
		//	? this.CompletedAt.Subtract( this.ReceivedAt ).TotalSeconds
		//	: 0;

		public ResponseModel()
		{
			this.Code = ResponseCode.Ok;
			this.Message = string.Empty;
		}
		
		private ResponseModel( ResponseCode code, string message ) : this()
		{
			this.Code = code;
			this.Message = message;
		}

		public string ToJson()
		{
			try
			{
				return Newtonsoft.Json.JsonConvert.SerializeObject( this );
			}
			catch( Exception ex )
			{
				return string.Empty;
			}
		}

		public static ResponseModel Success()
		{
			return new ResponseModel();
		}

		public static ResponseModel Error( ResponseCode code, string message = null )
		{
			return new ResponseModel( code, message );
		}
		
		public static ResponseModel Error( Exception ex )
		{
			if ( ex is System.Data.SqlClient.SqlException se )
			{
				return new ResponseModel( ResponseCode.Internal_DatabaseError, se.Message );
			}
			else if ( ex is System.Net.Http.HttpRequestException hre )
			{
				return new ResponseModel( ResponseCode.Provider_Error, hre.Message );
			}
			else
			{
				string msg = $"Msg: {ex.Message}\r\nStack: {ex.StackTrace}";
				return new ResponseModel( ResponseCode.Internal_UnhandledError, msg );
			}
		}

		public static ResponseModel Error( int httpStatusCode, string message = null )
		{
			System.Net.HttpStatusCode s = (System.Net.HttpStatusCode)httpStatusCode;
			switch( s )
			{
				case System.Net.HttpStatusCode.OK:
				case System.Net.HttpStatusCode.Created:
				case System.Net.HttpStatusCode.NoContent: return new ResponseModel( ResponseCode.Ok, message );
				case System.Net.HttpStatusCode.Conflict: return new ResponseModel( ResponseCode.Duplicate, message );
				default: return new ResponseModel( ResponseCode.Provider_Error, $"status: {s.ToString()} -> {message}" );
			}
		}
	}

	public class ResponseModel<T>
	{
		public ResponseCode Code { get; set; }
		public string Friendly => this.Code.ToString();
		[Newtonsoft.Json.JsonIgnore]
		public Exception Exception { get; set; }
		public string Message { get; set; }
		public List<T> Data { get; set; }
		
		//public double Duration =>
		//	this.CompletedAt != null && this.ReceivedAt != null
		//	? this.CompletedAt.Subtract( this.ReceivedAt ).TotalSeconds
		//	: 0;

		private ResponseModel()
		{
			this.Code = ResponseCode.NoData;
			this.Message = string.Empty;
		}

		private ResponseModel( T item ) : this()
		{
			if ( item != null )
			{
				this.Data = new List<T> { item };
				this.Code = ResponseCode.Ok;
			}
		}

		private ResponseModel( List<T> items ) : this()
		{
			if ( items != null )
			{
				this.Data = items;
				this.Code = items.Count == 0 ? ResponseCode.NoData : ResponseCode.Ok;
			}
		}


		private ResponseModel( T item, ResponseCode code, string message ) : this()
		{
			if ( item != null )
			{
				this.Data = new List<T> { item };
			this.Code = code;
			this.Message = message;
			}
		}
		
		private ResponseModel( List<T> items, ResponseCode code, string message ) : this()
		{
			this.Data = items;
			this.Code = code;
			this.Message = message;
		}

		private ResponseModel( ResponseCode code, string message ) : this()
		{
			this.Code = code;
			this.Message = message;
		}

		private ResponseModel( ResponseCode code, string message, Exception ex = null ) : this()
		{
			this.Code = code;
			this.Message = message;
			this.Exception = ex;
		}

		public ResponseModel ToResponse()
		{
			if ( this.Exception != null )
				return ResponseModel.Error( this.Exception );
			else
				return ResponseModel.Error( this.Code, this.Message );
		}

		public static ResponseModel<T> Success( T item )
		{
			return new ResponseModel<T>( item );
		}

		public static ResponseModel<T> Success( List<T> items )
		{
			return new ResponseModel<T>( items );
		}

		public static ResponseModel<T> SuccessNoData()
		{
			var r = new ResponseModel<T>();
			r.Code = ResponseCode.NoData;
			return r;
		}

		public static ResponseModel<T> ErrorWithData( T item, ResponseCode code, string message = null )
		{
			return new ResponseModel<T>( item, code, message );
		}

		public static ResponseModel<T> ErrorWithData( List<T> items, ResponseCode code, string message = null )
		{
			return new ResponseModel<T>( items, code, message );
		}


		public static ResponseModel<T> Error( ResponseCode code, string message = null )
		{
			return new ResponseModel<T>( code, message );
		}
		
		public static ResponseModel<T> Error( int httpStatusCode, string message = null )
		{
			System.Net.HttpStatusCode s = (System.Net.HttpStatusCode)httpStatusCode;
			switch( s )
			{
				case System.Net.HttpStatusCode.OK:
				case System.Net.HttpStatusCode.Created:
				case System.Net.HttpStatusCode.NoContent: return new ResponseModel<T>( ResponseCode.Ok, message );
				case System.Net.HttpStatusCode.Conflict: return new ResponseModel<T>( ResponseCode.Duplicate, message );
				default: return new ResponseModel<T>( ResponseCode.Provider_Error, $"status: {s.ToString()} -> {message}" );
			}
		}

		public static ResponseModel<T> Error( ResponseCode code, string message, Exception ex )
		{
			return new ResponseModel<T>( code, message, ex );
		}
		
		private ResponseModel( ResponseCode code, string message, T response = default( T ) ) : this()
		{
			this.Code = code;
			this.Message = message;
			if ( response != null )
				this.Data = new List<T> { response };
		}

		public static ResponseModel<T> Error( Exception ex )
		{
			if ( ex is System.Data.SqlClient.SqlException se )
			{
				return new ResponseModel<T>( ResponseCode.Internal_DatabaseError, se.Message );
			}
			else if ( ex is System.Net.Http.HttpRequestException hre )
			{
				return new ResponseModel<T>( ResponseCode.Provider_Error, hre.Message );
			}
			else
			{
				string msg = $"Msg: {ex.Message}\r\nStack: {ex.StackTrace}";
				return new ResponseModel<T>( ResponseCode.Internal_UnhandledError, msg );
			}
		}
	}

}
