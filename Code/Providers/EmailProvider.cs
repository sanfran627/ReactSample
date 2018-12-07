using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Net;
using System.Net.Mail;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Microsoft.AspNetCore.Http;

namespace SampleReact
{
	public interface IEmailProvider
	{
		string BaseUrl();
		bool SendEmergencyEmail( string subject, string body, bool queue = false );
		bool SendEmail( Guid userId, string to, string subject, string body, bool isHtml = false );
		bool SendEmail( SendEmailModel model );
		Task<ResponseModel> SendEmailAsync( string to, string subject, string body, bool isHtml = false );
		Task<ResponseModel> ProcessTemplate( Guid userId, EmailTemplateId templateId, string to, params object[] items );
		Task<ResponseModel> ProcessTemplate( Guid userId, EmailTemplateId templateId, string to, Dictionary<string, string> parameters, params object[] items );
	}

	public class EmailProvider : IEmailProvider
	{
		IDataManager DataManager = null;
		IOptions<AppSettings> OptionSettings = null;

		public EmailProvider( IOptions<AppSettings> settings , IDataManager dataManager )
		{
			this.OptionSettings = settings;
			this.DataManager = dataManager;
		}

		public string BaseUrl() => this.OptionSettings.Value.Url;


		#region Template Methods

		private void TemplateParameterDiscoverer( System.Text.RegularExpressions.Match m, HashSet<string> parmeters )
		{
			if( m != null && m.Captures.Count > 0 )
			{
				var g = m.Groups.Last();
				if( !string.IsNullOrWhiteSpace( g.Value ) )
					if( !parmeters.Contains( g.Value ) )
						parmeters.Add( g.Value );

				do
				{
					m = m.NextMatch();
					if( m != null && m.Captures.Count > 0 )
					{
						g = m.Groups.Last();
						if( !string.IsNullOrWhiteSpace( g.Value ) )
							if( !parmeters.Contains( g.Value ) )
								parmeters.Add( g.Value );
					}
					else
						break;

				}
				while( m != null );
			}
		}

		private void TemplateParameterPopulator( EmailTemplateProcessor processor )
		{
			var jTemplate = new JObject();


			foreach( var item in processor.Items )
			{
				string n = item.GetType().Name;
				if( n.EndsWith( "Model" ) )
					n = n.Substring( 0, n.Length - "Model".Length );
				else if( n.EndsWith( "Object" ) )
					n = n.Substring( 0, n.Length - "Object".Length );

				jTemplate.Add( n, Newtonsoft.Json.Linq.JObject.Parse( JsonConvert.SerializeObject( item ) ) );
			}

			// walk through the injected custom parameters, if any 
			if( processor.CustomParameters != null )
			{
				//remove any subject or body parameters that are also in the custom parameters
				processor.SubjParameters.RemoveWhere( c=> processor.CustomParameters.Keys.Contains( c ) );
				processor.BodyParameters.RemoveWhere( c=> processor.CustomParameters.Keys.Contains( c ) );

				foreach( var item in processor.CustomParameters.Where( c => !string.IsNullOrWhiteSpace( c.Value ) ) )
				{
					processor.Template.UpdateSubject( processor.Template.Subject.Replace( "{" + item.Key + "}", item.Value ) );
					processor.Template.UpdateText( processor.Template.Text.Replace( "{" + item.Key + "}", item.Value ) );
				}
			}

			// walk through the injected Items for the subject
			foreach( var key in processor.SubjParameters )
			{
				var token = jTemplate.SelectToken( key );
				if( token != null )
					processor.Template.UpdateSubject( processor.Template.Subject.Replace( "{" + key + "}", token.ToString() ) );
				else
					processor.AddError( $"Value for subject parameter '{key}' is null" );
			}

			// walk through the injected Items for the body
			foreach( var key in processor.BodyParameters )
			{
				var token = jTemplate.SelectToken( key );
				if( token != null )
					processor.Template.UpdateText( processor.Template.Text.Replace( "{" + key + "}", token.ToString() ) );
				else
					processor.AddError( $"Value for html/body parameter '{key}' is null" );
			}
		}

		public bool TryProcessTemplate( out EmailTemplateProcessor processor, EmailTemplateId templateId, Dictionary<string,string> parameters, params object[] items )
		{
			var t = this.DataManager.GetEmailTemplate( templateId );

			processor  = null;

			if( t == null )
			{
				processor = new EmailTemplateProcessor( "No template found for: " + templateId.ToString() );
			}
			else
			{
				var url = this.BaseUrl();

				if( parameters == null ) parameters = new Dictionary<string, string>();

				UserModel user = null;
				if( items != null && items.Length > 0 )
					user = items.FirstOrDefault( c=> c is UserModel ) as UserModel;

				//DisplayName is used in emails as a safety valve to identify who the user is...
				if( user != null )
					parameters.TryAdd( "DisplayName", user.Name ?? string.Empty );
				else
					parameters.TryAdd( "DisplayName", "" );

				parameters.TryAdd( "HomeUrl", url );
				parameters.TryAdd( "LoginUrl", url + "login" );
				parameters.TryAdd( "SignupUrl", url + "signup" );
				parameters.TryAdd( "LogoUrl", url + "statics/logo.png" );
				parameters.TryAdd( "PhishingUrl", url + "phishing" );

				//get the html containing the master layout for all emails
				//string layout = string.Copy( this.DataManager.EmailLayout.Text );
				//processor = new EmailTemplateProcessor( t, layout, parameters, items );

				processor = new EmailTemplateProcessor( t, parameters, items );

				//var matches = Regex.Matches("Test {Token1} {Token 2}", @"{([^{}]*)");
				var regex = new System.Text.RegularExpressions.Regex(@"{([^{}]*)", System.Text.RegularExpressions.RegexOptions.Multiline);

				try
				{
					TemplateParameterDiscoverer( regex.Match( processor.Template.Subject ), processor.SubjParameters );
				}
				catch( Exception ex )
				{
					processor.AddError( "Error parsing subject parameters: " + ex.ToString() );
				}
				try
				{
					TemplateParameterDiscoverer( regex.Match( processor.Template.Text ), processor.BodyParameters );
				}
				catch( Exception ex )
				{
					processor.AddError( "Error parsing html parameters: " + ex.ToString() );
				}

				try
				{
					TemplateParameterPopulator( processor );
				}
				catch( Exception ex )
				{
					processor.AddError( "Error populating parameters: " + ex.ToString() );
				}
			}

			return processor.Successful;
		}
		
		public async Task<ResponseModel> ProcessTemplate( Guid userId, EmailTemplateId templateId, string to, params object[] items )
			=> await ProcessTemplate( userId, templateId, to, null, items );

		public async Task<ResponseModel> ProcessTemplate( Guid userId, EmailTemplateId templateId, string to, Dictionary<string,string> parameters, params object[] items )
		{
			if( TryProcessTemplate( out EmailTemplateProcessor p, templateId, parameters, items ) )
			{
				SendEmailModel sem = new SendEmailModel ( userId, to, p.Template.Subject, p.Template.Text );

				return await this.DataManager.SendEmail( sem );
			}
			else
			{
				return ResponseModel.Error( ResponseCode.Internal_UnhandledError, string.Join( Environment.NewLine, p.Errors ) );
			}
		}

		#endregion

		#region SendMail

		public bool SendEmergencyEmail( string subject, string body, bool queue = false )
		{
			var info = this.DataManager.Settings.SMTP;

			SendEmailModel sem = new SendEmailModel (
				Guid.Empty,
				info.Alerts,
				subject,
				body,
				false
			);

			if( !queue ) return SendEmail( sem );

			//fire and forget
			this.DataManager.SendEmail( sem );
			return true;
		}

		public bool SendEmail( Guid userId, string to, string subject, string body, bool isHtml = false )
		{
			SendEmailModel sem = new SendEmailModel (
				userId,
				to,
				subject,
				body,
				isHtml
			);

			return SendEmail( sem );
		}

		public async Task<ResponseModel> SendEmailAsync( string to, string subject, string body, bool isHtml = false )
		{
			SendEmailModel sem = new SendEmailModel (
				Guid.Empty,
				to,
				subject,
				body,
				isHtml
			);

			return await this.DataManager.SendEmail( sem );
		}

		public bool SendEmail( SendEmailModel model )
		{
			var info = this.DataManager.Settings.SMTP;

			try
			{
				model.Attempts++;
				using( System.Net.Mail.SmtpClient sc = new System.Net.Mail.SmtpClient( info.Host, info.Port ) )
				{
					sc.Timeout = info.TimeoutInSeconds;
					sc.EnableSsl = true;
					sc.Credentials = new System.Net.NetworkCredential( info.Username, info.Password );
					sc.EnableSsl = info.UseSSL;
					sc.Send( new MailMessage( info.From, model.To, model.Subj, model.Body ) { IsBodyHtml = model.Html } );
					//sc.Timeout = info.Timeout;
					model.Status = EmailStatus.Sent;
				}
				return true;
			}
			catch( SmtpFailedRecipientException ex )
			{
				model.Status = EmailStatus.BadRecipient;
				model.SmtpStatus = ex.StatusCode.ToString();
				model.Error = ex.Message;
				return false;
			}
			catch( SmtpException ex )
			{
				switch( ex.StatusCode )
				{
					case SmtpStatusCode.Ok:
						return true;

					case SmtpStatusCode.GeneralFailure:
						model.Status = EmailStatus.HostNotFound;
						model.Error = ex.Message;
						return false;

					case SmtpStatusCode.MailboxNameNotAllowed:
					case SmtpStatusCode.MailboxUnavailable:
						model.Status = EmailStatus.BadRecipient;
						model.SmtpStatus = ex.StatusCode.ToString();
						model.Error = ex.Message;
						return false;

					case SmtpStatusCode.ServiceClosingTransmissionChannel:
					case SmtpStatusCode.ServiceNotAvailable:
						model.Status = EmailStatus.ServerDown;
						model.SmtpStatus = ex.StatusCode.ToString();
						model.Error = ex.Message;
						return false;

					case SmtpStatusCode.MailboxBusy:
					case SmtpStatusCode.InsufficientStorage:
					case SmtpStatusCode.ExceededStorageAllocation:
						model.Status = EmailStatus.BadRecipient;
						model.SmtpStatus = ex.StatusCode.ToString();
						model.Error = ex.Message;
						return false;

					case SmtpStatusCode.BadCommandSequence:
					case SmtpStatusCode.ClientNotPermitted:
					case SmtpStatusCode.CommandNotImplemented:
					case SmtpStatusCode.CommandParameterNotImplemented:
					case SmtpStatusCode.CommandUnrecognized:
					case SmtpStatusCode.HelpMessage:
					case SmtpStatusCode.LocalErrorInProcessing:
					case SmtpStatusCode.MustIssueStartTlsFirst:
					case SmtpStatusCode.ServiceReady:
					case SmtpStatusCode.StartMailInput:
					case SmtpStatusCode.SyntaxError:
					case SmtpStatusCode.SystemStatus:
					case SmtpStatusCode.TransactionFailed:
					case SmtpStatusCode.UserNotLocalTryAlternatePath:
					case SmtpStatusCode.UserNotLocalWillForward:
					default:
						model.SmtpStatus = ex.StatusCode.ToString();
						model.Error = ex.Message;
						model.Status = EmailStatus.SmtpException;
						return false;
				}
			}
			catch( Exception ex )
			{
				model.Status = EmailStatus.SystemError;
				model.Error = ex.Message;
				return false;
			}
		}

		#endregion
	}
}