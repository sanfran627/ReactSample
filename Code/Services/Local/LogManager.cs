using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;


namespace SampleReact
{
	public interface ILogManager : Microsoft.Extensions.Logging.ILogger, Microsoft.Extensions.Logging.ILoggerFactory
	{
		Task LogErrorAsync( string method, string message );
		Task LogErrorAsync( string method, string message, Guid userId );
		Task LogErrorAsync( string method, Exception ex );
		Task LogErrorAsync( string method, Exception ex, Guid userId );
	}

	public class Disposable : IDisposable
	{
		public void Dispose() { }
	}

	public class LogManager : ILogManager
	{
		IDataManager DataManager = null;

		public LogManager( IOptions<AppSettings> settings = null, IDataManager dataMgr = null )
		{
			this.OptionSettings = settings;
			this.DataManager = dataMgr;
		}

		public IOptions<AppSettings> OptionSettings { get; set; }
		
		public IDisposable BeginScope<TState>( TState state )
		{
			return new Disposable();
		}

		public void Dispose()
		{
		}

		public void AddProvider( ILoggerProvider provider ) { }

		public ILogger CreateLogger( string categoryName ) { return new LogManager( this.OptionSettings ); }

		public bool IsEnabled( LogLevel logLevel ) => false;

		public void Log<TState>( LogLevel logLevel, Microsoft.Extensions.Logging.EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter )
		{
		}

		public async Task LogErrorAsync( string method, string message )
			=> await LogErrorAsync( method, message );

		public async Task LogErrorAsync( string method, string message, Guid userId )
		{
			await this.DataManager.LogErrorAsync( method, message, userId );
		}


		public async Task LogErrorAsync( string method, Exception ex )
			=> await LogErrorAsync( method, ex, Guid.Empty );

		public async Task LogErrorAsync( string method, Exception ex, Guid userId )
		{
			await this.LogErrorAsync(
				method
				, ex.Message + ex.StackTrace != null ? ( Environment.NewLine + ex.StackTrace ) : string.Empty
				, userId );
		}

	}
}