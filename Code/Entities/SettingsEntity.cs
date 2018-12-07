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
	public class SettingsEntity : EntityBase
	{
		const string PARTITIONKEY = "system";
		const string ROWKEY = "settings";

		public SettingsEntity() : base( PARTITIONKEY, ROWKEY ) { }

		public override EntityTableType Table => EntityTableType.system;

		public int Version { get; set; }

		public static string QueryKey() => 
			TableQuery.CombineFilters(
				TableQuery.GenerateFilterCondition( EntityBase.PARTITION_KEY, QueryComparisons.Equal, PARTITIONKEY ),
				TableOperators.And,
				TableQuery.GenerateFilterCondition( EntityBase.ROW_KEY, QueryComparisons.Equal, ROWKEY ) );

	}
}
