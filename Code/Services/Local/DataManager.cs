using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage.Queue;
using Microsoft.WindowsAzure.Storage.Table;
using Microsoft.WindowsAzure.Storage.RetryPolicies;

namespace SampleReact
{
	public enum DataManagerDataType
	{
		System
	}

	public interface IDataManager
	{
		bool Loaded( DataManagerDataType type );

		Task<Tuple<bool, string>> TryConnectCloudStorageAsync( string key );

		MetadataModel Metadata { get; }
		SettingsModel Settings { get; }

		EmailTemplateModel GetEmailTemplate( EmailTemplateId id );
		SMSTemplateModel GetSMSTemplate( SMSTemplateId id );

		Task<ResponseModel> LoadSystemDataAsync( bool force = false );
		List<string> GetSystemStartupLogs();
		
		Task LogErrorAsync( string method, string message );
		Task LogErrorAsync( string method, string message, Guid userId );
		Task LogExceptionAsync( string method, Exception ex );

		#region Metadata Section

		IEnumerable<EnumModel> ReadLanguages( Func<EnumModel, bool> predicate = null );

		#endregion

		#region Queue Storage

		Task<ResponseModel> SendEmail( SendEmailModel outbound );
		Task<ResponseModel> SendSMS( SendSMSModel outbound );
		Task<ResponseModel> WriteToQueue( QueueName name, string payload );
		Task<ResponseModel> WriteEvent( string eventName, object obj );

		#endregion

		#region Table Storage

		Task<EmailComposite> ReadEmail( string email, IncludeOptions options = IncludeOptions.None );
		Task<UserComposite> ReadUser( string email, IncludeOptions options = IncludeOptions.None );
		Task<ResponseModel<UserModel>> SigninUserAsync( string email, string password, string userAgent );
		Task<ResponseModel<UserModel>> VerifyContactAsync( Guid userId, ContactMethodType method, string userSubmittedVerificationCode );
		//move over invitations, discounts, etc. to the user from the email address
		Task<ResponseModel> ConvertEmailToUserId( UserModel user );
		Task<ResponseModel> CreateUserAsync( UserModel user );
		Task<ResponseModel<UserModel>> ChangeUserEmailAsync( Guid userId, string newEmail );
		//note - deleting the user deletes everything under the user...
		Task<ResponseModel> DeleteUserAsync( UserModel user );
		Task<ResponseModel<T>> QueryEntities<T>( TableQuery<T> query ) where T : EntityBase, ITableEntity, new();
		Task<ResponseModel> InsertEntity<T>( T entity ) where T : EntityBase, ITableEntity, new();
		Task<ResponseModel> UpdateEntity<T>( T entity ) where T : EntityBase, ITableEntity, new();
		Task<ResponseModel> UpdateEntityProperty( EntityTableType table, DynamicTableEntity entity );
		Task<ResponseModel> DeleteEntity<T>( T entity ) where T : EntityBase, ITableEntity, new();
		Task<ResponseModel> DeleteEntity( EntityTableType type, string partitionKey, string rowKey );
		Task<ResponseModel<UserComposite>> QueryUser( string email, IncludeOptions options = IncludeOptions.None );
		Task<ResponseModel<UserComposite>> QueryUser( Guid userId, IncludeOptions options = IncludeOptions.None );
		Task<ResponseModel<EmailComposite>> QueryEmail( string email, IncludeOptions options = IncludeOptions.None );
		Task<ResponseModel> BatchOperations( EntityTableType table, TableBatchOperation batch );
		Task<ResponseModel> BatchOperations( Dictionary<EntityTableType, TableBatchOperation> batches );
		Task<ResponseModel> DeletePartition( EntityTableType table, string partitionKey );

		#endregion
	}

	public class DataManager : IDataManager
	{
		#region variables

		static Dictionary<DataManagerDataType,ManualResetEvent> _dataTypes = new Dictionary<DataManagerDataType, ManualResetEvent>()
		{
			{ DataManagerDataType.System, new ManualResetEvent(false) }
		};

		protected IOptions<AppSettings> _appSettings;
		protected IPasswordService _pwdMgr;

		protected AppSettings AppSettings => _appSettings.Value;
		protected IPasswordService PasswordManager => _pwdMgr;

		// these are found in app settings under CloudConnections
		protected const string KEY = "sample";
		protected const string LOGS_KEY = "sample-logs";

		//used to show results of loading system and/or core data
		private readonly static List<string> _systemLogs = new List<string>();

		private readonly static ConcurrentDictionary<EmailTemplateId,EmailTemplateModel> _emailTemplates = new ConcurrentDictionary<EmailTemplateId,EmailTemplateModel>();
		private readonly static ConcurrentDictionary<SMSTemplateId,SMSTemplateModel> _smsTemplates = new ConcurrentDictionary<SMSTemplateId,SMSTemplateModel>();
		private volatile static MetadataModel _metadata = null;
		private readonly static Dictionary<string,CloudStorageAccount> _cloudStorageAccounts = new Dictionary<string, CloudStorageAccount>();

		#endregion

		public DataManager( IOptions<AppSettings> settings, IPasswordService pwdMgr = null )
		{
			_appSettings = settings;
			_pwdMgr = pwdMgr;
		}

		public bool Loaded( DataManagerDataType type ) => _dataTypes[type].WaitOne( 0 );

		public MetadataModel Metadata => _metadata;
		public SettingsModel Settings => this.Metadata != null ? this.Metadata.Settings : null;
		public EmailTemplateModel GetEmailTemplate( EmailTemplateId id ) => _emailTemplates.TryGetValue( id, out EmailTemplateModel item ) ? item : null;
		public SMSTemplateModel GetSMSTemplate( SMSTemplateId id ) => _smsTemplates.TryGetValue( id, out SMSTemplateModel item ) ? item : null;
		public List<string> GetSystemStartupLogs() => _systemLogs.AsReadOnly().ToList();

		private CloudStorageAccount GetCloudStorageAccount( string key )
		{
			CloudStorageAccount csa = null;
			if( !_cloudStorageAccounts.TryGetValue( key, out csa ) )
			{
				var cs = GetCloudConnectionString( key );
				csa = CloudStorageAccount.Parse( cs );
				_cloudStorageAccounts.Add( key, csa );
			}
			return csa;
		}

		private string GetCloudConnectionString( string key )
		{
			return
				!string.IsNullOrWhiteSpace( key )
				&& this.AppSettings != null
				&& this.AppSettings.CloudConnections.TryGetValue( key, out string connectionstring )
				? connectionstring
				: string.Empty;
		}

		public async Task<Tuple<bool, string>> TryConnectCloudStorageAsync( string key )
		{
			string error = null;
			if( !this.AppSettings.CloudConnections.ContainsKey( key ) )
			{
				error = $"Connection string not found for key: {key}";
			}
			else
			{
				try
				{
					var client = GetCloudStorageAccount(key).CreateCloudBlobClient();
					var path = this.GetCloudContainerAPIPath();
					var container = client.GetContainerReference( path );
					bool exists = await container.ExistsAsync();
					if( !exists ) error = $"Container '{path}' does not exist";
				}
				catch( Exception ex )
				{
					error = ex.Message;
				}
				finally
				{
				}
			}

			return new Tuple<bool, string>( error == null, error );
		}

		public string GetCloudContainerAPIPath()
		{
			return "api";
		}

		public string GetCloudContainerLogPath( string logType )
		{
			return string.Join( "/", new string[] {
				logType,
				DateTime.Now.Year.ToString(),
				DateTime.Now.Month.ToString( "00" ),
				DateTime.Now.Day.ToString( "00" ),
				DateTime.Now.Hour.ToString( "00" )
				} );
		}

		public async Task<CloudTable> GetTable( string key, string tableName, bool deleteIfExists = false )
		{
			// Create the table client.
			CloudTableClient tableClient = GetCloudStorageAccount(key).CreateCloudTableClient();

			tableClient.DefaultRequestOptions = new TableRequestOptions
			{
				RetryPolicy = new ExponentialRetry( TimeSpan.FromSeconds( 30 ), 5 ),
				MaximumExecutionTime = TimeSpan.FromSeconds( 30 ),
				ServerTimeout = TimeSpan.FromSeconds( 30 )
			};

			// Retrieve a reference to the table.
			CloudTable table = tableClient.GetTableReference( tableName );

			if( deleteIfExists ) await table.DeleteIfExistsAsync();

			// Create the table if it doesn't exist.
			await table.CreateIfNotExistsAsync();


			return table;
		}

		#region Logging

		public async Task LogErrorAsync( string method, string message ) => await LogErrorAsync( method, message, Guid.Empty );

		public async Task LogErrorAsync( string method, string message, Guid userId )
		{
			// fire and forget
			WriteLog( new ErrorLog( method, message, new Dictionary<string, string> { { "userId", userId.Encode() } } ) );
		}

		public async Task LogExceptionAsync( string method, Exception ex )
		{
			// fire and forget
			WriteLog( new ErrorLog( method, ex ) );
		}

		#endregion

		#region Loaders

		public async Task<ResponseModel> LoadSystemDataAsync( bool force = false )
		{
			_systemLogs.Clear();
			_systemLogs.Add( $"LoadSystemDataAsync (force = {force})" );

			var status = _dataTypes[DataManagerDataType.System].WaitOne( 0 );
			if( status && !force ) return ResponseModel.Error( ResponseCode.Ok );
			_dataTypes[DataManagerDataType.System].Reset();

			DateTime started = DateTime.UtcNow;

			_systemLogs.Add( $"Fetching System Settings" );

			var data = await GetSystem();

			if( data != null )
			{
				_metadata = data.Metadata;
				data.EmailTemplates.ForEach( c => _emailTemplates.AddOrUpdate( c.TemplateId, c, ( k, v ) => c ) );
				data.SMSTemplates.ForEach( c => _smsTemplates.AddOrUpdate( c.TemplateId, c, ( k, v ) => c ) );
			}

			_dataTypes[DataManagerDataType.System].Set();

			if( _metadata != null )
			{
				double dur = DateTime.UtcNow.Subtract( started ).TotalSeconds;

				#region Printout
				_systemLogs.Add( $"{Metadata.Config.Count()} Config Loaded" );
				_systemLogs.Add( $"{Metadata.Events.Count()} Events Loaded" );
				_systemLogs.Add( $"{Metadata.Languages.Count()} Languages Loaded" );
				_systemLogs.Add( $"{Metadata.ResponseCodes.Count()} ResponseCodes Loaded" );
				_systemLogs.Add( $"{Metadata.UserStatus.Count()} UserStatus Loaded" );

				if( this.AppSettings.Environment != EnvironmentId.Development )
				{
					Microsoft.ApplicationInsights.TelemetryClient client = new Microsoft.ApplicationInsights.TelemetryClient();
					try
					{
						_systemLogs.Add( "DataManager.LoadSystemDataAsync -> Startup Duration (metadata): " + dur.ToString() );
						client.TrackEvent( "DataManager.LoadSystemDataAsync", new Dictionary<string, string> { { "duration-metadata", dur.ToString() } }, new Dictionary<string, double> { { "duration-metadata", dur } } );
					}
					catch { }
				}
				#endregion

				_systemLogs.ForEach( c => Console.WriteLine( c ) );

				return ResponseModel.Success();
			}

			return ResponseModel.Error( ResponseCode.NoData, "Metadata not loaded from cache. World ends now" );
		}
		
		#endregion

		#region Metadata Section

		public IEnumerable<EnumModel> ReadLanguages( Func<EnumModel, bool> predicate = null ) =>
			_metadata != null
					? ( predicate == null
									? _metadata.Languages
									: _metadata.Languages.Where( predicate ) )
					: new List<EnumModel>();

		#endregion

		#region Cloud Queue

		public async Task<ResponseModel> SendEmail( SendEmailModel m ) => 
			await WriteToQueue( QueueName.Outbound_Email, JsonConvert.SerializeObject( m, JSON.SerializationSettings ) );

		public async Task<ResponseModel> SendSMS( SendSMSModel m ) => 
			await WriteToQueue( QueueName.Outbound_Mailing, JsonConvert.SerializeObject( m, JSON.SerializationSettings ) );

		public async Task<ResponseModel> WriteToQueue( QueueName name, string payload )
		{
			try
			{
				CloudStorageAccount storageAccount = this.GetCloudStorageAccount(KEY);
				var client = storageAccount.CreateCloudQueueClient();
				var path = this.GetCloudContainerAPIPath();

				// Retrieve a reference to a container.
				string queueNameRef = $"{name.ToString().ToLower().Replace("_","-")}";
				//lookup actual queue name. If not found, default to the reference name
				string queueName = this.AppSettings.QueueNames.TryGetValue( queueNameRef, out string qn ) ? qn : queueNameRef;
				CloudQueue queue = client.GetQueueReference(queueName);

				// Create the queue if it doesn't already exist
				await queue.CreateIfNotExistsAsync();

				// Create a message and add it to the queue.
				CloudQueueMessage message = new CloudQueueMessage(payload);
				await queue.AddMessageAsync( message );
				return ResponseModel.Success();
			}
			catch( Exception ex )
			{
				await this.LogErrorAsync( "WriteToQueue", ex.Message );
				return ResponseModel.Error( ex );
			}
		}

		public async Task<ResponseModel> WriteEvent( string eventName, object obj )
		{
			if( obj == null || string.IsNullOrWhiteSpace( eventName ) ) return ResponseModel.Success();
			string payload = string.Empty;
			try
			{
				payload = ( obj.GetType() == typeof( string ) ) ? ( string ) obj : JsonConvert.SerializeObject( obj );
			}
			catch( Exception ex )
			{
				this.LogExceptionAsync( "WriteToQueue(name,crud,obj)", ex );
				return ResponseModel.Error( ex );
			}

			try
			{
				CloudStorageAccount storageAccount = this.GetCloudStorageAccount(KEY);
				var client = storageAccount.CreateCloudQueueClient();
				var path = this.GetCloudContainerAPIPath();

				// Retrieve a reference to a container.
				CloudQueue queue = client.GetQueueReference($"{QueueName.Event.ToString().ToLower().Replace("_","-")}");

				// Create the queue if it doesn't already exist
				await queue.CreateIfNotExistsAsync();

				// Create a message and add it to the queue.
				var json = new JObject(
						new JProperty( "event", eventName ),
						new JProperty( "payload", payload )
					).ToString(Formatting.Indented);

				CloudQueueMessage message = new CloudQueueMessage(json);
				await queue.AddMessageAsync( message );
				return ResponseModel.Success();
			}
			catch( Exception ex )
			{
				await this.LogErrorAsync( "WriteToQueue", ex.Message );
				return ResponseModel.Error( ex );
			}

		}


		#endregion

		#region Cloud Storage

		public async Task<CloudBlobContainer> GetCloudBlobContainer( string key )
		{
			CloudBlobContainer container = null;
			try
			{
				CloudStorageAccount storageAccount = this.GetCloudStorageAccount(key);
				var client = storageAccount.CreateCloudBlobClient();
				var path = this.GetCloudContainerAPIPath();
				container = client.GetContainerReference( path );
				if( !( await container.ExistsAsync() ) )
				{
					await container.CreateAsync();
				}
			}
			catch( Exception ex )
			{
				await this.LogErrorAsync( "GetCloudBlobContainer", ex.Message );
				return null;
			}

			return container;
		}

		public async Task<CloudBlobContainer> GetCloudBlobLogsContainer( string key )
		{
			CloudBlobContainer container = null;
			try
			{
				CloudStorageAccount storageAccount = this.GetCloudStorageAccount(key);
				var client = storageAccount.CreateCloudBlobClient();
				container = client.GetContainerReference( "logs" );
				if( !( await container.ExistsAsync() ) )
				{
					await container.CreateAsync();
				}
			}
			catch( Exception ex )
			{
				await this.LogErrorAsync( "GetCloudBlobLogsContainer", ex.Message );
				return null;
			}

			return container;
		}

		async Task<SystemModels> GetSystem()
		{
			SystemModels s = new SystemModels();
			
			var container = await GetCloudBlobContainer( KEY );

			var dir = container.GetDirectoryReference("");
			var list = await dir.ListBlobsSegmentedAsync( null );
			foreach( var file in list.Results.OfType<CloudBlockBlob>().Where( c => c.Name.EndsWith( ".json" ) ) )
			{
				var name = file.Name.Split("/".ToCharArray() ).Last().Replace(".json","");
				var json = await file.DownloadTextAsync();
				switch( name ) {
					case "Metadata":
						s.Metadata = JsonConvert.DeserializeObject<List<MetadataModel>>( json, JSON.SerializationSettings ).FirstOrDefault();
						break;
					case "EmailTemplate":
						s.EmailTemplates = JsonConvert.DeserializeObject<List<EmailTemplateModel>>( json, JSON.SerializationSettings );
						break;
					case "SMSTemplate":
						s.SMSTemplates = JsonConvert.DeserializeObject<List<SMSTemplateModel>>( json, JSON.SerializationSettings );
						break;
				}
			}

			if( s.EmailTemplates == null ) s.EmailTemplates = new List<EmailTemplateModel>();
			if( s.SMSTemplates == null ) s.SMSTemplates = new List<SMSTemplateModel>();
			return s;
		}

		async Task RecurseDirectory( ItemType type, BlobContinuationToken continuationToken, IEnumerable<IListBlobItem> blobList, Dictionary<ItemType, List<CloudBlockBlob>> items, HashSet<ItemType> types )
		{
			foreach( var blobitem in blobList )
			{
				if( blobitem is CloudBlobDirectory )
				{
					var directory = blobitem as CloudBlobDirectory;
					var list = await directory.ListBlobsSegmentedAsync( continuationToken );
					await RecurseDirectory( type, continuationToken, list.Results, items, types );
				}

				if( blobitem is CloudBlockBlob )
				{
					items[type].Add( blobitem as CloudBlockBlob );
				}
			}
		}
		
		public async Task WriteLog( ILog log, CloudBlobContainer container = null )
		{
			try
			{
				if( container == null ) container = await GetCloudBlobLogsContainer( LOGS_KEY );
				var path = this.GetCloudContainerLogPath(log.LogType);
				var dir = container.GetDirectoryReference(path);
				var file = DateTime.Now.Hour.ToString("00") + "00.log";
				string json = JsonConvert.SerializeObject( log, JSON.SerializationSettings );

				// Note: AppendBlockBlob is not available in the Storage Emulator/Explorer
				if( GetCloudConnectionString( LOGS_KEY ) == "UseDevelopmentStorage=true" )
				{
					CloudBlockBlob blob = dir.GetBlockBlobReference( file );
					try
					{
						await blob.UploadTextAsync( json );
					}
					catch( StorageException ex )
					{
					}
					catch( Exception ex )
					{
					}
				}
				else
				{
					CloudAppendBlob blob = dir.GetAppendBlobReference( DateTime.Now.Hour.ToString("00") + "00.log" );
					if( !( await blob.ExistsAsync() ) )
					{
						try
						{
							await blob.CreateOrReplaceAsync( AccessCondition.GenerateIfNotExistsCondition(), null, null );
						}
						catch( StorageException ex )
						{
						}
						catch( Exception ex )
						{
						}
					}

					using( var stream = new System.IO.MemoryStream( System.Text.Encoding.UTF8.GetBytes( json ) ) )
					{
						await blob.AppendBlockAsync( stream );
					}

				}

			}
			catch( StorageException ex )
			{
			}
			catch( Exception ex )
			{
			}
		}

		#endregion

		#region Table Storage


		#region Reads


		public async Task<EmailComposite> ReadEmail( string email, IncludeOptions options = IncludeOptions.None )
		{
			var rs = await this.QueryEmail( email, options );
			if( rs.Code == ResponseCode.Ok ) return rs.Data.First();
			return null;
		}

		public async Task<UserComposite> ReadUser( string email, IncludeOptions options = IncludeOptions.None )
		{
			var rs = await this.QueryUser( email, options );
			if( rs.Code == ResponseCode.Ok ) return rs.Data.First();
			return null;
		}

		#endregion

		#region Writes

		public async Task<ResponseModel> CreateUserAsync( UserModel user )
		{
			var rs = await this.QueryEmail( user.Email, IncludeOptions.Entities );
			if( rs.Code == ResponseCode.NoData )
			{
				EmailEntity e = new EmailEntity(new EmailModel(user.Email, user.UserId ) );
				var rsE = await this.InsertEntity<EmailEntity>( e );
				if( rsE.Code != ResponseCode.Ok ) return rsE;
			}
			else if( rs.Code == ResponseCode.Ok )
			{
				//exists, see if the userid is set.. if not, update it
				if( rs.Data[0].HasUser ) return ResponseModel.Error( ResponseCode.EmailUnavailable );
				rs.Data[0].EmailEntity.Email.UserId = user.UserId;
				var rsE = await this.UpdateEntity<EmailEntity>( rs.Data[0].EmailEntity );
				if( rsE.Code != ResponseCode.Ok ) return rsE;
			}
			else
				return rs.ToResponse();

			//we're here - email entity set - now create the user
			UserEntity ue = new UserEntity( user );
			var rsU = await this.InsertEntity( ue );
			//return an error and we'll also need to revert the userId on the email record
			if( rsU.Code != ResponseCode.Ok )
			{
				var dte = EmailEntity.SetUser( user.Email, Guid.Empty );
				var rsDTE = await this.UpdateEntityProperty( EntityTableType.email, dte ); // fire and forget
				if( rsDTE.Code != ResponseCode.Ok )
				{
					// handle appropriately
				}
				return rsU;
			}
			return ResponseModel.Success();
		}

		public async Task<ResponseModel<UserModel>> ChangeUserEmailAsync( Guid userId, string newEmail )
		{
			var rsUser = await this.QueryUser( userId, IncludeOptions.Entities );
			if( rsUser.Code != ResponseCode.Ok ) return ResponseModel<UserModel>.Error( rsUser.Code, rsUser.Message );

			var user = rsUser.Data[0].User;

			var rsOldEmail = await this.QueryEmail( user.Email );
			if( rsOldEmail.Code != ResponseCode.Ok ) return ResponseModel<UserModel>.Error( rsOldEmail.Code, rsOldEmail.Message );

			var rsNewEmail = await this.QueryEmail( newEmail );
			if( rsNewEmail.Code != ResponseCode.Ok && rsNewEmail.Code != ResponseCode.NoData ) return ResponseModel<UserModel>.Error( rsNewEmail.Code, rsNewEmail.Message );

			if( rsNewEmail.Code == ResponseCode.Ok && rsNewEmail.Data[0].HasUser ) return ResponseModel<UserModel>.Error( ResponseCode.EmailUnavailable );

			//release the userId from the old email -> or maybe wait to do that once the email has been verified?
			var rsOld = await this.UpdateEntityProperty( EntityTableType.email, EmailEntity.SetUser( user.Email, Guid.Empty ) );
			if( rsOld.Code != ResponseCode.Ok ) return ResponseModel<UserModel>.Error( rsOld.Code, rsOld.Message );

			ResponseModel rsNew = null;
			if( rsNewEmail.Code == ResponseCode.NoData )
			{
				rsNew = await this.InsertEntity<EmailEntity>( new EmailEntity( new EmailModel( newEmail, userId ) ) );
			}
			else if( rsNewEmail.Code == ResponseCode.Ok )
			{
				rsNew = await this.UpdateEntityProperty( EntityTableType.email, EmailEntity.SetUser( newEmail, userId ) );
			}

			if( rsNew.Code != ResponseCode.Ok )
			{
				//revert the old email address update
				rsOld = await this.UpdateEntityProperty( EntityTableType.email, EmailEntity.SetUser( user.Email, user.UserId ) );
				if( rsOld.Code != ResponseCode.Ok ) return ResponseModel<UserModel>.Error( rsOld.Code, rsOld.Message );
				return ResponseModel<UserModel>.Error( rsNew.Code, rsNew.Message );
			}



			//we're here - email entity set - now update the user
			user.EmailOld = user.Email;
			user.Email = newEmail;
			user.EmailInvalid = false;
			user.EmailVerification = new UserContactVerificationModel( ContactMethodType.Email );
			var dte = UserEntity.UpdateEmail( userId, user.EmailOld, user.Email, user.EmailVerification );
			var rsU = await this.UpdateEntityProperty( EntityTableType.user, dte );
			//return an error and we'll also need to revert the userId on the email record
			if( rsU.Code != ResponseCode.Ok )
			{
				// handle appropriately
			}

			return ResponseModel<UserModel>.Success( user );
		}

		public async Task<ResponseModel> ConvertEmailToUserId( UserModel user )
		{
			//lookup all email discount records
			var rsEmail = await this.QueryEmail( user.Email, IncludeOptions.Entities );
			if( rsEmail.Code != ResponseCode.Ok ) return rsEmail.ToResponse();

			bool changed = false;

			var e = rsEmail.Data[0].EmailEntity;

			// example: If the there is data associated initially with an email (e.g. you get a discount!), that data would need to move over to the user once associated
			
			if( changed )
			{
				return await this.UpdateEntity<EmailEntity>( e );
			}
			return ResponseModel.Success();
		}

		public async Task<ResponseModel> DeleteUserAsync( UserModel user )
		{
			ResponseModel rs = null;
			
			// delete everything about the user...
			rs = await this.DeletePartition( EntityTableType.user, user.UserId.Encode() );

			if( rs.Code == ResponseCode.Ok )
			{
				//release the email
				var ee = await this.QueryEmail( user.Email, IncludeOptions.Entities );
				if( ee.Code == ResponseCode.Ok )
				{
					EmailEntity e = ee.Data[0].EmailEntity;
					e.Email.UserId = Guid.Empty;
					rs = await this.UpdateEntity<EmailEntity>( e );
				}
			}

			return rs;
		}

		public async Task<ResponseModel<UserModel>> VerifyContactAsync( Guid userId, ContactMethodType method, string userSubmittedVerificationCode )
		{
			var rs = await this.QueryUser( userId, IncludeOptions.Entities );

			if( rs.Code == ResponseCode.NoData ) return ResponseModel<UserModel>.Error( ResponseCode.InvalidCredentials );
			if( rs.Code != ResponseCode.Ok ) return ResponseModel<UserModel>.Error( rs.Code, rs.Message );

			UserComposite composite = rs.Data[0];

			switch( method )
			{
				case ContactMethodType.Email:
					if( composite.User.EmailVerification == null )
					{
						break;
					}
					else if( composite.User.EmailVerification.ExpiresAt < DateTime.UtcNow )
					{
						return ResponseModel<UserModel>.Error( ResponseCode.EmailVerificationCodeExpired );
					}
					else if( composite.User.EmailVerification.Code != userSubmittedVerificationCode )
					{
						return ResponseModel<UserModel>.Error( ResponseCode.InvalidEmailVerificationCode );
					}
					else
					{
						composite.UserEntity.User.Verified( method );
						// update the entity
						// send it back to table storage with the updates
						var rsUpdate = await this.UpdateEntity<UserEntity>( composite.UserEntity );

					}
					break;
					
				default:
					break;
			}

			return ResponseModel<UserModel>.Success( composite.UserEntity.User );
		}

		public async Task<ResponseModel<UserModel>> SigninUserAsync( string email, string password, string userAgent )
		{
			ResponseModel<UserModel> result = null;

			var rs = await this.QueryUser( email.ToLower() );

			if( rs.Code == ResponseCode.NoData )
			{
				var log = new SigninLog( email, userAgent, ResponseCode.NoData, new Dictionary<string, string> { { "notes", "No user found with that email address" } } );
				// fire and forget
				this.WriteLog( log );
				return ResponseModel<UserModel>.Error( ResponseCode.InvalidCredentials );
			}
			if( rs.Code != ResponseCode.Ok )
			{
				var log = new SigninLog( email, userAgent, ResponseCode.NoData, new Dictionary<string, string> { { "message", rs.Message ?? "{null}" } } );
				// fire and forget
				this.WriteLog( log );
				return ResponseModel<UserModel>.Error( rs.Code, rs.Message );
			}

			var user = rs.Data[0].User;

			// see if the user is locked out
			if( user.Password.Locked )
			{
				result = ResponseModel<UserModel>.Error( ResponseCode.InvalidCredentials );
			}
			else
			{
				if( ! (await this.PasswordManager.Verify( password, user.Password.Hash ) ) )
				{
					//we do NOT do updates to data inline... They MUST go through the background thread
					//user.Password.IncrementStrikes( user.Password.Strikes >= 2 ? true : false );
					//var rsIncremenet = await this.SetItemAsync( user );
					//// log something?

					result = ResponseModel<UserModel>.Error( ResponseCode.InvalidCredentials );
					bool locked = user.Password.Strikes >= 5;
					user.Password.IncrementStrikes( locked );
					var dte = UserEntity.UpdatePasswordBuilder(user.UserId, user.Password, PasswordMode.NormalSignin);
					this.UpdateEntityProperty( EntityTableType.user, dte ); //fire and forget
				}
				else
				{
					result = ResponseModel<UserModel>.Success( user );
					if( user.Password.Strikes > 0 || user.Password.Locked )
					{
						user.Password.Reset();
						var dte = UserEntity.UpdatePasswordBuilder(user.UserId, user.Password, PasswordMode.NormalSignin);
						this.UpdateEntityProperty( EntityTableType.user, dte ); //fire and forget
					}
				}
			}

			this.WriteLog( new SigninLog( email, userAgent, result.Code ) );

			return result;
		}

		#endregion


		public async Task<ResponseModel<T>> QueryEntities<T>( TableQuery<T> query ) where T : EntityBase, ITableEntity, new()
		{
			try
			{
				var t = await GetTable(KEY, EntityBase.GetTableFor<T>() );
				var rs = ( await t.ExecuteQuerySegmentedAsync( query, null ) );
				return rs.Count() > 0
					? ResponseModel<T>.Success( rs.ToList() )
					: ResponseModel<T>.Error( ResponseCode.NoData );
			}
			catch( StorageException ex )
			{
				return ResponseModel<T>.Error( ex );
			}
			catch( Exception ex )
			{
				return ResponseModel<T>.Error( ex );
			}
		}

		public async Task<ResponseModel<DynamicTableEntity>> QueryEntities( EntityTableType type, TableQuery query )
		{
			try
			{
				var t = await GetTable(KEY, type.ToString() );
				var rs = ( await t.ExecuteQuerySegmentedAsync( query, null ) );
				return rs.Count() > 0
					? ResponseModel<DynamicTableEntity>.Success( rs.ToList() )
					: ResponseModel<DynamicTableEntity>.Error( ResponseCode.NoData );
			}
			catch( StorageException ex )
			{
				return ResponseModel<DynamicTableEntity>.Error( ex );
			}
			catch( Exception ex )
			{
				return ResponseModel<DynamicTableEntity>.Error( ex );
			}
		}

		public async Task<ResponseModel> InsertEntity<T>( T entity ) where T : EntityBase, ITableEntity, new()
		{
			try
			{
				var t = await GetTable(KEY, EntityBase.GetTableFor<T>() );
				var rs = ( await t.ExecuteAsync( TableOperation.Insert( entity, false ) ) );
				return rs.HttpStatusCode >= 200 && rs.HttpStatusCode < 300
					? ResponseModel.Success()
					: ResponseModel.Error( rs.HttpStatusCode );
			}
			catch( StorageException ex )
			{
				return ResponseModel.Error( ex );
			}
			catch( Exception ex )
			{
				return ResponseModel.Error( ex );
			}
		}

		public async Task<ResponseModel> UpdateEntity<T>( T entity ) where T : EntityBase, ITableEntity, new()
		{
			try
			{
				var t = await GetTable(KEY, EntityBase.GetTableFor<T>() );
				var rs = ( await t.ExecuteAsync( TableOperation.Replace( entity ) ) );
				return rs.HttpStatusCode >= 200 && rs.HttpStatusCode < 300
					? ResponseModel.Success()
					: ResponseModel.Error( rs.HttpStatusCode );
			}
			catch( StorageException ex )
			{
				return ResponseModel.Error( ex );
			}
			catch( Exception ex )
			{
				return ResponseModel.Error( ex );
			}
		}

		public async Task<ResponseModel> UpdateEntityProperty( EntityTableType table, DynamicTableEntity entity )
		{
			try
			{
				var t = await GetTable(KEY, table.ToString() );
				var rs = ( await t.ExecuteAsync( TableOperation.Merge( entity ) ) );
				return rs.HttpStatusCode >= 200 && rs.HttpStatusCode < 300
					? ResponseModel.Success()
					: ResponseModel.Error( rs.HttpStatusCode );
			}
			catch( StorageException ex )
			{
				return ResponseModel.Error( ex );
			}
			catch( Exception ex )
			{
				return ResponseModel.Error( ex );
			}
		}

		public async Task<ResponseModel> DeleteEntity<T>( T entity ) where T : EntityBase, ITableEntity, new()
		{
			try
			{
				var t = await GetTable(KEY, EntityBase.GetTableFor<T>() );
				var te = new TableEntity( entity.PartitionKey, entity.RowKey );
				var rs = ( await t.ExecuteAsync( TableOperation.Delete( te ) ) );
				return rs.HttpStatusCode >= 200 && rs.HttpStatusCode < 300
					? ResponseModel.Success()
					: ResponseModel.Error( rs.HttpStatusCode );
			}
			catch( StorageException ex )
			{
				return ResponseModel.Error( ex );
			}
			catch( Exception ex )
			{
				return ResponseModel.Error( ex );
			}
		}

		public async Task<ResponseModel> DeleteEntity( EntityTableType type, string partitionKey, string rowKey )
		{
			try
			{
				var t = await GetTable(KEY, type.ToString() );
				var te = new TableEntity( partitionKey, rowKey );
				var rs = ( await t.ExecuteAsync( TableOperation.Delete( te ) ) );
				return rs.HttpStatusCode >= 200 && rs.HttpStatusCode < 300
					? ResponseModel.Success()
					: ResponseModel.Error( rs.HttpStatusCode );
			}
			catch( StorageException ex )
			{
				return ResponseModel.Error( ex );
			}
			catch( Exception ex )
			{
				return ResponseModel.Error( ex );
			}
		}

		public async Task<ResponseModel> DeletePartition( EntityTableType table, string partitionKey )
		{
			try
			{
				var t = await GetTable(KEY, table.ToString() );
				TableQuery query = new TableQuery().Where( TableQuery.GenerateFilterCondition( EntityBase.PARTITION_KEY, QueryComparisons.Equal, partitionKey ) );
				query.SelectColumns = new List<string> { "Timestamp" };
				var rs = ( await t.ExecuteQuerySegmentedAsync( query, null ) );
				if( rs.Count() > 0 )
				{
					TableBatchOperation batch = new TableBatchOperation();
					foreach( var item in rs )
						batch.Add( TableOperation.Delete( item ) );
					await t.ExecuteBatchAsync( batch );
					return ResponseModel.Success();
				}
				return ResponseModel.Success();
			}
			catch( StorageException ex )
			{
				return ResponseModel.Error( ex );
			}
			catch( Exception ex )
			{
				return ResponseModel.Error( ex );
			}
		}

		public async Task<ResponseModel> BatchOperations( EntityTableType table, TableBatchOperation batch )
		{
			try
			{
				var t = await GetTable(KEY, table.ToString() );
				var rs = await t.ExecuteBatchAsync( batch );
				var errs = rs.Where( c => c.HttpStatusCode < 200 || c.HttpStatusCode > 299 );
				if( errs.Count() > 0 )
				{
					System.Text.StringBuilder sb = new System.Text.StringBuilder();
					sb.AppendLine( "For Table: " + table.ToString() );
					foreach( var err in errs ) sb.AppendLine( JsonConvert.SerializeObject( err.Result ) );
					return ResponseModel.Error( ResponseCode.Internal_DatabaseError, sb.ToString() );
				}
			}
			catch( StorageException ex )
			{
				if( ex.RequestInformation.HttpStatusCode == 412 )
					return ResponseModel.Error( ResponseCode.StoragePreconditionFailed, ex.Message );

				return ResponseModel.Error( ex );
			}
			catch( Exception ex )
			{
				return ResponseModel.Error( ex );
			}

			return ResponseModel.Success();
		}

		public async Task<ResponseModel> BatchOperations( Dictionary<EntityTableType, TableBatchOperation> batches )
		{
			System.Text.StringBuilder sb = new System.Text.StringBuilder();
			Dictionary<EntityTableType, ResponseModel> results = new Dictionary<EntityTableType, ResponseModel>();
			try
			{
				foreach( var kvp in batches )
				{
					var rs = await BatchOperations(kvp.Key, kvp.Value );
					results.Add( kvp.Key, rs );
				}

				if( results.Values.Any( c => c.Code != ResponseCode.Ok ) )
				{
					foreach( var err in results.Values.Where( c => c.Code != ResponseCode.Ok ) ) sb.AppendLine( err.Message );
					return ResponseModel.Error( ResponseCode.Internal_DatabaseError, sb.ToString() );
				}
				return ResponseModel.Success();
			}
			catch( StorageException ex )
			{
				return ResponseModel.Error( ex );
			}
			catch( Exception ex )
			{
				return ResponseModel.Error( ex );
			}
		}

		public async Task<ResponseModel<EmailComposite>> QueryEmail( string email, IncludeOptions options = IncludeOptions.None )
		{
			EmailComposite c = new EmailComposite();
			TableQuery<EmailEntity> query = new TableQuery<EmailEntity>().Where( EmailEntity.QueryKey( email ) );
			var rs = await QueryEntities<EmailEntity>( query );
			if( rs.Code != ResponseCode.Ok ) return ResponseModel<EmailComposite>.Error( rs.Code, rs.Message );
			c.Email = rs.Data[0].Email;
			if( ( options & IncludeOptions.Entities ) == IncludeOptions.Entities ) c.EmailEntity = rs.Data[0];
			return ResponseModel<EmailComposite>.Success( c );
		}

		public async Task<ResponseModel<UserComposite>> QueryUser( string email, IncludeOptions options = IncludeOptions.None )
		{
			TableQuery<EmailEntity> query = new TableQuery<EmailEntity>().Where( EmailEntity.QueryKey( email ) );
			var rs = await QueryEntities<EmailEntity>( query );
			if( rs.Code != ResponseCode.Ok ) return ResponseModel<UserComposite>.Error( rs.Code, rs.Message );
			if( rs.Data[0].Email.UserId == null || rs.Data[0].Email.UserId == Guid.Empty ) return ResponseModel<UserComposite>.Error( ResponseCode.NotFound );
			return await QueryUser( rs.Data[0].Email.UserId, options );
		}

		public async Task<ResponseModel<UserComposite>> QueryUser( Guid userId, IncludeOptions options = IncludeOptions.None )
		{
			TableQuery<UserEntity> query = new TableQuery<UserEntity>().Where( UserEntity.QueryKey( userId ) );
			var rs = await QueryEntities<UserEntity>( query );
			if( rs.Code != ResponseCode.Ok ) return ResponseModel<UserComposite>.Error( rs.Code, rs.Message );
			UserComposite c = new UserComposite(userId);
			c.User = rs.Data[0].User;
			if( ( options & IncludeOptions.Entities ) == IncludeOptions.Entities ) c.UserEntity = rs.Data[0];
			return ResponseModel<UserComposite>.Success( c );
		}

		#endregion
	}
	public class SystemModels
	{
		public MetadataModel Metadata { get; set; }
		public List<EmailTemplateModel> EmailTemplates { get; set; }
		public List<SMSTemplateModel> SMSTemplates { get; set; }
	}
}