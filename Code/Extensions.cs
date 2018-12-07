using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Serialization;

namespace SampleReact
{
	public static class EnumExtensions
	{
		public static TAttribute GetAttribute<TAttribute>( this Enum value )
			where TAttribute : Attribute
		{
			var type = value.GetType();
			var name = Enum.GetName(type, value);
			return type.GetField( name )
				.GetCustomAttributes( false )
				.OfType<TAttribute>()
				.SingleOrDefault();
		}
	}

	public static class GuidExtensions
	{
		public static string Encode( this Guid id ) => IDConverter.Encode( id );
	}

	public static class StringExtensions
	{
		public static Guid Decode( this string id ) => IDConverter.Decode( id );
	}

	public static class IDConverter
	{

		public static string Generate() => Encode(Guid.NewGuid());

		public static string Encode(Guid g)
		{
			if (g == null) return string.Empty;
			return g == Guid.Empty
				? string.Empty
				: g.ToString().Replace("-", "");
		}

		public static Guid Decode(string id) => (string.IsNullOrWhiteSpace(id) || id.Length != 32)
			? Guid.Empty
			: Guid.TryParse(id, out Guid result)
				? result
				: Guid.Empty;


	}

	public static class MyAppExtensions
	{
		public static IApplicationBuilder UseMySignalR( this IApplicationBuilder app, IOptions<AppSettings> options )
		{
			if( options.Value.UseAzureSignalR )
			{
				app.UseAzureSignalR( route =>
				{
					route.MapHub<SiteHub>( "/sitehub" );
				} );
			}
			else
			{
				app.UseSignalR( route =>
				 {
					 route.MapHub<SiteHub>( "/sitehub" );
				 } );
			}
			return app;
		}
	}

	public static class MyServiceCollectionExtensions
	{
		public static IServiceCollection AddMySignalR( this IServiceCollection services, AppSettings settings )
		{
			if( settings.UseAzureSignalR )
			{
				services
					.AddSignalR()
					.AddAzureSignalR(settings.SignalRConnectionString)
					.AddJsonProtocol(options => { options.PayloadSerializerSettings.ContractResolver =  new CamelCasePropertyNamesContractResolver(); });
			}
			else
			{
				services
					.AddSignalR()
					.AddJsonProtocol(options => { options.PayloadSerializerSettings.ContractResolver =  new CamelCasePropertyNamesContractResolver(); });
			}

			return services;
		}
	}
}
