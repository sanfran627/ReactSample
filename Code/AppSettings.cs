using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SampleReact
{
	public class JWTSecretKey
	{
		public string Key { get; set; }
		public int DurationInMinutes { get; set; }
	}

	public class ConnectionStrings : Dictionary<string, string> { }
	public class JWTSecretKeys : Dictionary<string, JWTSecretKey> { }
	public class QueueNames : Dictionary<string, string> { }
	public class ServiceLocation
	{
		public ServiceType IPasswordService { get; set; }
		public ServiceType ICalculatorService { get; set; }
		public ServiceType IActionProcessor { get; set; }
	}

	public interface IAppSettings
	{
		JWTSecretKeys JWTSecretKeys { get; set; }
		QueueNames QueueNames { get; set; }
		ConnectionStrings CloudConnections { get; set; }
		EnvironmentId Environment { get; set; }
		ServiceLocation ServiceLocation { get; set; }
		string SignalRConnectionString { get; set; }
		bool UseAzureSignalR { get; set; }
		string Url { get; set; }
	}

	public class AppSettings : IAppSettings
	{
		public JWTSecretKeys JWTSecretKeys { get; set; }
		public QueueNames QueueNames { get; set; }
		public ConnectionStrings CloudConnections { get; set; }
		public EnvironmentId Environment { get; set; }
		public ServiceLocation ServiceLocation { get; set; }
		public string SignalRConnectionString { get; set; }
		public bool UseAzureSignalR { get; set; }
		public string Url { get; set; }
	}
}
