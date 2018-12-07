using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SampleReact
{
	public interface ILog
	{
		Guid Id { get; set; }
		DateTime Created { get; set; }
		String LogType { get; }
	}

	public class ErrorLog : ILog
	{
		public Guid Id { get; set; }
		public DateTime Created { get; set; }
		public string LogType => "errors";
		public string Method { get; set; }
		public string Message { get; set; }
		public Exception Exception { get; set; }
		public readonly Dictionary<string, string> Metadata = new Dictionary<string, string>();

		public ErrorLog()
		{
			this.Id = Guid.NewGuid();
			this.Created = DateTime.UtcNow;
		}

		public ErrorLog( string method, string message, Dictionary<string, string> additional = null ) : this()
		{
			this.Method = method;
			this.Message = message;
			if( additional != null && additional.Count > 0 )
				this.Metadata = additional;
		}

		public ErrorLog( string method, Exception ex, Dictionary<string, string> additional = null ) : this()
		{
			this.Method = method;
			this.Message = string.Empty;
			this.Exception = ex;
			if( additional != null && additional.Count > 0 )
				this.Metadata = additional;
		}
	}

	public class SigninLog : ILog
	{
		public Guid Id { get; set; }
		public DateTime Created { get; set; }
		public string LogType => "signins";
		public string Email { get; set; }
		public ResponseCode Code { get; set; }
		public Exception Exception { get; set; }
		public string UserAgent { get; set; }
		public readonly Dictionary<string, string> Metadata = new Dictionary<string, string>();

		public SigninLog()
		{
			this.Id = Guid.NewGuid();
			this.Created = DateTime.UtcNow;
		}

		public SigninLog( string email, string userAgent, ResponseCode code, Dictionary<string, string> additional = null ) : this()
		{
			this.Email = email;
			this.UserAgent = UserAgent;
			this.Code = code;
			if( additional != null && additional.Count > 0 )
				this.Metadata = additional;
		}

		public SigninLog( string email, string userAgent, Exception ex, Dictionary<string, string> additional = null ) : this()
		{
			this.Email = email;
			this.UserAgent = UserAgent;
			this.Code = ResponseCode.Internal_UnhandledError;
			this.Exception = ex;
			if( additional != null && additional.Count > 0 )
				this.Metadata = additional;
		}
	}
}
