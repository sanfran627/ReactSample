using System;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Linq;

namespace SampleReact
{
	public interface ISlackProvider
	{
		Task<ResponseModel> Send( string message );
	}

	public class SlackProvider : ISlackProvider
	{
		IOptions<AppSettings> OptionSettings { get; set; }
		IDataManager DataManager = null;

		public SlackProvider( IOptions<AppSettings> settings, IDataManager dataManager )
		{
			this.OptionSettings = settings;
			this.DataManager = dataManager;
		}

		public async Task<ResponseModel> Send( string msg )
		{
			if( this.DataManager.Metadata.Settings.Slack == null 
				|| !this.DataManager.Metadata.Settings.Slack.Enabled 
				|| string.IsNullOrWhiteSpace( this.DataManager.Metadata.Settings.Slack.Channel ) )
				return ResponseModel.Success();

			try
			{
				var request = new HttpRequestMessage { Method = HttpMethod.Post, RequestUri = new Uri(this.DataManager.Metadata.Settings.Slack.Channel) };

				string message = $"Environment: {this.OptionSettings.Value.Environment.ToString()} => {msg}";

				request.Content = new StringContent( new JObject( new JProperty( "text", message ) ).ToString() );

				// Setup client
				using( var client = new System.Net.Http.HttpClient() )
				{
					client.Timeout = TimeSpan.FromSeconds( 30 );

					var response = await client.SendAsync( request );

					string err = null;
					if( response.StatusCode != System.Net.HttpStatusCode.OK )
					{
						if( response.Content != null )
						{
							var str = await response.Content.ReadAsStringAsync();
							err = $"StatusCode: {response.StatusCode.ToString()}, Content: {str}";
						}
						else
						{
							err = $"StatusCode: {response.StatusCode.ToString()}";
						}
						return ResponseModel.Error( ResponseCode.Provider_Error, err );
					}

					return ResponseModel.Success();
				}
			}
			catch( Exception ex )
			{
				this.DataManager.LogExceptionAsync( "SlackProvider.Send", ex );
				return ResponseModel.Error( ex );
			}
		}
	}
}