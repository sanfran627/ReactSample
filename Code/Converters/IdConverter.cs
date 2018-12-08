using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace SampleReact
{
	class JsonIdConverter : JsonConverter
	{
		public override bool CanConvert(Type objectType) => (objectType == typeof(Guid));

		public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer) => writer.WriteValue(IDConverter.Encode((Guid)value));

		public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer) =>
			reader.TokenType == JsonToken.Null
				? null
				: reader.TokenType != JsonToken.String
					? null
					: (object)IDConverter.Decode(reader.Value.ToString());
	}
}