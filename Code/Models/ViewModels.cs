using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace SampleReact
{
	public class UserViewModel
	{
		public UserViewModel() { }

		public UserViewModel( UserModel model )
		{
			this.UserId = model.UserId;
			this.DisplayName = model.Name;
			this.language = model.Language.ToString();
			this.Email = model.Email;
			this.EmailVerified = model.EmailVerification == null ? true : false;
			this.Status = model.StatusId;
			if( model.Type == UserType.Admin ) this.IsAdmin = true;
		}

		[JsonConverter( typeof( JsonIdConverter ) )]
		public Guid UserId { get; set; }
		public string DisplayName { get; private set; }
		public string language { get; private set; }
		public string Email { get; private set; }
		public bool EmailVerified { get; private set; }
		public UserStatusId Status { get; private set; }
		[JsonProperty( DefaultValueHandling = DefaultValueHandling.Ignore, NullValueHandling = NullValueHandling.Ignore )]
		public bool? IsAdmin { get; private set; }
	}
	
	public class UserSiteInfoViewModel
	{
		public UserSiteInfoViewModel() => this.Settings = new Dictionary<string, string>();
		public UserSiteInfoViewModel(UserSiteInfoModel model) => this.Settings = model.Settings;
		
		public Dictionary<string,string> Settings { get; private set; }
	}

}