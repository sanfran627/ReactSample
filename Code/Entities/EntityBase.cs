using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage.Table;

namespace SampleReact
{
	public class GuidConverter : JsonConverter
	{
		public override bool CanRead
		{
			get
			{
				// We only need the converter for writing Guids without dashes, for reading the default mechanism is fine
				return false;
			}
		}

    public override bool CanWrite
		{
			get
			{
				return true;
			}
		}


		public override void WriteJson( JsonWriter writer, object value, JsonSerializer serializer )
		{
			if( value is Guid )
			{
				writer.WriteValue( ( ( Guid ) value ).Encode() );
			}
		}

		public override object ReadJson( JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer )
		{
			throw new NotImplementedException();
		}

		public override bool CanConvert( Type objectType )
		{
			return objectType == typeof( Guid ) || objectType == typeof( Guid? );
		}
	}

	public enum EntityTableType
	{
		email,
		plan,
		user,
		practitioner,
		system
	}

	public abstract class EntityBase : TableEntity
	{
		public const string PARTITION_KEY = "PartitionKey";
		public const string ROW_KEY = "RowKey";

		private static Dictionary<Type,string> _tables = new Dictionary<Type,string>();

		static EntityBase()
		{
			foreach( var t in System.Reflection.Assembly.GetExecutingAssembly().GetTypes().Where( c => c.IsSubclassOf( typeof( EntityBase ) ) && !c.IsAbstract ) )
			{
				string table = ((EntityBase)Activator.CreateInstance( t )).Table.ToString();
				_tables.Add( t, table );
			}
		}

		public static string GetTableFor<T>() where T : EntityBase => _tables.TryGetValue( typeof( T ), out string s ) ? s : null;

		public EntityBase() { }

		public EntityBase( string partitionKey, string rowKey ) : base( partitionKey, rowKey ) { }

		public abstract EntityTableType Table { get; }
	}
}
