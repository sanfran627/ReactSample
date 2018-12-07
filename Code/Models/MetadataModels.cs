using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace SampleReact
{
    public interface IMetadata
    {
        MetadataId MetadataId { get; }
    }
    
    public class MetadataModel
    {
		public SettingsModel Settings { get; set; }
        public IEnumerable<ConfigModel> Config { get; set; }
        public IEnumerable<EventModel> Events { get; set; }
        public IEnumerable<EnumModel> Languages { get; set; }
        public IEnumerable<ResponseCodeModel> ResponseCodes { get; set; }
        public IEnumerable<UserStatusModel> UserStatus { get; set; }

		public string GetTextForResponseCode( ResponseCode code, LanguageId language )
		{
			const string DEFAULT = "{no code defined for this error}";
			if( this.ResponseCodes == null ) return DEFAULT;
			var rc = this.ResponseCodes.FirstOrDefault( c=> c.ResponseCode == code );
			if( rc == null ) return DEFAULT;
			return rc.Text.GetFor( language );
		}
	}
	
    public class ResponseCodeModel
    {
        [JsonProperty]
		public ResponseCode ResponseCode { get; private set; }

        [JsonProperty]
		public LanguagesModel Text { get; private set; }
    }

    public class ConfigModel
    {
        [JsonProperty]
		public ConfigId ConfigId { get; private set; }
        [JsonProperty]
		public DataTypeId Type { get; private set; }
        [JsonProperty]
		public string Name { get; private set; }
        [JsonProperty]
		public string Value { get; private set; }
    }
	
    public class EventModel
    {
        [JsonProperty]
		public ActionType Action { get; private set; }
        [JsonProperty]
		public ActionOutcome Outcome { get; private set; }
        [JsonProperty]
		public string Name { get; private set; }
        [JsonProperty]
		public LanguagesModel Text { get; private set; }
    }
	
	public class EnumModel
	{
		[JsonProperty]
		public string Enum { get; private set; }
		[JsonProperty]
		public int Sequence { get; private set; }
		[JsonProperty]
		public LanguagesModel Text { get; private set; }
		[JsonProperty]
		public LanguagesModel Note { get; private set; }
	}
	
    public class UserStatusModel
    {
        [JsonProperty]
		public UserStatusId UserStatusId { get; private set; }
        [JsonProperty]
		public int Sequence { get; private set; }
        [JsonProperty]
		public LanguagesModel Text { get; private set; }
        [JsonProperty]
		public LanguagesModel Caption { get; private set; }
    }
    
    public class LanguagesModel
    {
		public LanguagesModel () { }
		public LanguagesModel( string en ) => this.en = en;

        [JsonProperty(NullValueHandling = NullValueHandling.Include)]
        public string en { get; private set; }
        [JsonProperty]
		public string es { get; private set; }
        //[JsonProperty]
		//public string ja { get; private set; }

		public string GetFor( LanguageId language )
		{
			switch( language )
			{
				case LanguageId.es: return this.es;
				default: return this.en;
			}
		}
    }
	
}
