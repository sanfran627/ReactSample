using System;
using System.Threading.Tasks;
using System.Net.Http;
using Microsoft.Extensions.Options;

namespace SampleReact
{
	public class RestServiceLocal : IRestService
	{
		static HttpClient Client = new HttpClient();
		public AppSettings Settings { get; private set; }

		public RestServiceLocal( IOptions<AppSettings> settings )
		{
			this.Settings = settings.Value;
		}

		public void BasicHeader( HttpRequestMessage msg, string username, string password )
		{
			msg.Headers.Add( "Authorization", "Basic " + Convert.ToBase64String( System.Text.ASCIIEncoding.ASCII.GetBytes( string.Format( "{0}:{1}", username, password ) ) ) );
		}

		public async Task<ResponseModel<TResponse>> Get<TResponse>( string requestUri, string username = null, string password = null ) where TResponse : class, new()
		{
			try
			{
				HttpRequestMessage msg = new HttpRequestMessage(HttpMethod.Get, requestUri);
				if( !string.IsNullOrWhiteSpace( username ) && !string.IsNullOrWhiteSpace( password ) ) BasicHeader( msg, username, password );

				HttpResponseMessage rs = await Client.SendAsync(msg);
				if( rs.StatusCode == System.Net.HttpStatusCode.OK )
				{
					string json = await rs.Content.ReadAsStringAsync();
					var response = Newtonsoft.Json.JsonConvert.DeserializeObject<TResponse>( json );
					if( response == null )
						return ResponseModel<TResponse>.SuccessNoData();
					else
						return ResponseModel<TResponse>.Success( response );
				}
				else
				{
					return ResponseModel<TResponse>.Error( ResponseCode.Provider_Down, rs.StatusCode.ToString() );
				}
			}
			catch( Exception ex )
			{
				return ResponseModel<TResponse>.Error( ex );
			}
		}

		public async Task<ResponseModel> Post<T>( string requestUri, T request, string username = null, string password = null )
		{
			try
			{
				HttpRequestMessage msg = new HttpRequestMessage(HttpMethod.Post, requestUri)
				{
					Content = new StringContent( Newtonsoft.Json.JsonConvert.SerializeObject(request), System.Text.Encoding.UTF8, "application/json" )
				};

				if( !string.IsNullOrWhiteSpace( username ) && !string.IsNullOrWhiteSpace( password ) ) BasicHeader( msg, username, password );

				
				HttpResponseMessage rs = await Client.SendAsync( msg );

				if( rs.StatusCode == System.Net.HttpStatusCode.OK )
				{
					return ResponseModel.Success();
				}
				else
				{
					return ResponseModel.Error( ResponseCode.Provider_Down, rs.StatusCode.ToString() );
				}
			}
			catch( Exception ex )
			{
				return ResponseModel.Error( ex );
			}
		}

		public async Task<ResponseModel<TResponse>> Post<TRequest, TResponse>( string requestUri, TRequest request, string username = null, string password = null )
			where TResponse : class, new()
		{
			try
			{
				HttpRequestMessage msg = new HttpRequestMessage(HttpMethod.Post, requestUri)
				{
					Content = new StringContent( Newtonsoft.Json.JsonConvert.SerializeObject(request), System.Text.Encoding.UTF8, "application/json" )
				};

				if( !string.IsNullOrWhiteSpace( username ) && !string.IsNullOrWhiteSpace( password ) ) BasicHeader( msg, username, password );

				HttpResponseMessage rs = await Client.SendAsync( msg );

				if( rs.StatusCode == System.Net.HttpStatusCode.OK )
				{
					string json = await rs.Content.ReadAsStringAsync();
					var response = Newtonsoft.Json.JsonConvert.DeserializeObject<TResponse>( json );
					if( response == null )
						return ResponseModel<TResponse>.SuccessNoData();
					else
						return ResponseModel<TResponse>.Success( response );
				}
				else
				{
					return ResponseModel<TResponse>.Error( ResponseCode.Provider_Down, rs.StatusCode.ToString() );
				}
			}
			catch( Exception ex )
			{
				return ResponseModel<TResponse>.Error( ex );
			}
		}
	}
}
