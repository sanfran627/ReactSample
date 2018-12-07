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
		public override bool CanConvert( Type objectType )
		{
			return ( objectType == typeof( Guid ) );
		}

		public override void WriteJson( JsonWriter writer, object value, JsonSerializer serializer )
		{
			Guid g = (Guid)value;
			writer.WriteValue( IDConverter.Encode( g ) );
		}

		public override object ReadJson( JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer )
		{
			if( reader.TokenType == JsonToken.Null ) return null;
			if( reader.TokenType != JsonToken.String ) return null;

			return IDConverter.Decode( reader.Value.ToString() );
		}
	}
}