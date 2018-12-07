using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage.Table;

namespace SampleReact
{
	public class EmailEntity : EntityBase
	{
		const string ROWKEY = "email";
		const string FIELD_CREATED = "created";
		const string FIELD_USERID = "userId";
		const string FIELD_DISCOUNTS = "discounts";

		public static string QueryKey( string email ) => TableQuery.CombineFilters(
				TableQuery.GenerateFilterCondition( EntityBase.PARTITION_KEY, QueryComparisons.Equal, email ),
				TableOperators.And,
				TableQuery.GenerateFilterCondition( EntityBase.ROW_KEY, QueryComparisons.Equal, ROWKEY ) );

		public static DynamicTableEntity SetUser( string email, Guid userId )
		{
			var entity = new DynamicTableEntity( email, ROWKEY );
			entity.ETag = "*";
			entity.Properties.Add( "userId", new EntityProperty( userId.Encode() ) );
			return entity;
		}
		
		public EmailEntity() => this.Created = DateTime.UtcNow;

		public EmailEntity( EmailModel e ) : base( e.Email, ROWKEY )
		{
			this.Created = DateTime.UtcNow;
			this.Email = e;
		}

		public override EntityTableType Table => EntityTableType.email;

		public EmailModel Email { get; set; }
		public DateTime Created { get; set; }

		#region Manual reader/writer

		public override void ReadEntity( IDictionary<string, EntityProperty> properties, OperationContext operationContext )
		{
			this.Email = new EmailModel(this.PartitionKey);
			this.Created = DateTime.UtcNow;
			this.Email.UserId = Guid.Empty;

			foreach( var kvp in properties )
			{
				switch( kvp.Key )
				{
					case FIELD_CREATED: this.Created = kvp.Value.DateTimeOffsetValue.Value.UtcDateTime; break;
					case FIELD_USERID: this.Email.UserId = kvp.Value.StringValue.Decode(); break;
					//case FIELD_DISCOUNTS:
					//	{
					//		var temp = JsonConvert.DeserializeObject<List<DiscountCodeObject>>( kvp.Value.StringValue, JSON.SerializationSettings );
					//		this.Email.Discounts.Clear();
					//		this.Email.Discounts.AddRange( temp );
					//	}
					//	break;
				}
			}
			this.Email.UserId = properties.TryGetValue( "userId", out EntityProperty p1 ) ? p1.StringValue.Decode() : Guid.Empty;
		}

		public override IDictionary<string, EntityProperty> WriteEntity( OperationContext operationContext )
		{
			var items = new Dictionary<string,EntityProperty>();
			items.Add(FIELD_CREATED, new EntityProperty( DateTime.UtcNow ) );
			items.Add(FIELD_USERID, new EntityProperty( this.Email.UserId.Encode() ) );
			//items.Add(FIELD_DISCOUNTS, new EntityProperty( JsonConvert.SerializeObject( this.Email.Discounts, JSON.SerializationSettings ) ) );
			return items;
		}

		#endregion

	}
}
