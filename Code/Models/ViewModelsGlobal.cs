using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace SampleReact
{
	public class MetadataViewModel
	{
		public MetadataViewModel() { }

		public MetadataViewModel( MetadataModel model )
		{
			this.Languages = model.Languages;
			this.ResponseCodes = model.ResponseCodes;
			this.UserStatus = model.UserStatus.OrderBy( c => c.Sequence );
		}

		public IEnumerable<EventModel> Events { get; private set; }
		public IEnumerable<EnumModel> Languages { get; private set; }
		public IEnumerable<ResponseCodeModel> ResponseCodes { get; private set; }
		public IEnumerable<UserStatusModel> UserStatus { get; set; }
	}
}