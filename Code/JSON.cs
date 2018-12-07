using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;

namespace SampleReact
{
	public class JSON
	{
		public static JsonSerializerSettings SerializationSettings = new JsonSerializerSettings
		{
			Converters = new System.Collections.Generic.List<JsonConverter>(){
						new IsoDateTimeConverter(),
						new StringEnumConverter(),
						new GuidConverter()
			},
			ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
			DateFormatHandling = DateFormatHandling.IsoDateFormat,
			DateTimeZoneHandling = DateTimeZoneHandling.Utc,
			DateFormatString = "o",
			DefaultValueHandling = DefaultValueHandling.Include,
			Formatting = Formatting.None,
			NullValueHandling = NullValueHandling.Ignore
		};

		public static JsonSerializerSettings CamelSerializationSettings = new JsonSerializerSettings
		{
			ContractResolver =  new CamelCasePropertyNamesContractResolver(),
			Converters = new System.Collections.Generic.List<JsonConverter>(){
						new IsoDateTimeConverter(),
						new StringEnumConverter(),
						new GuidConverter()
			},
			ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
			DateFormatHandling = DateFormatHandling.IsoDateFormat,
			DateTimeZoneHandling = DateTimeZoneHandling.Utc,
			DateFormatString = "o",
			DefaultValueHandling = DefaultValueHandling.Include,
			Formatting = Formatting.None,
			NullValueHandling = NullValueHandling.Ignore
		};
	}
}
