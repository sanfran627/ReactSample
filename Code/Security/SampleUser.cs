using System;
using System.Security.Claims;


namespace SampleReact
{
	public class SampleUser: ClaimsPrincipal
	{
		public AuthenticationMode Mode { get; private set; }
		public UserModel SiteUser { get; set; }
		public UserModel OBO { get; set; }

		public SampleUser( System.Security.Principal.IIdentity identity ) : base( identity ) { }

		public SampleUser( System.Security.Principal.IIdentity identity, UserModel oboUser = null ) : base( identity )
		{
			this.Mode = AuthenticationMode.API;
			this.OBO = oboUser;
		}

		public SampleUser( UserModel user, System.Security.Principal.IIdentity identity, UserModel oboUser = null ) : base( identity )
		{
			this.Mode = AuthenticationMode.UserCookie;
			this.SiteUser = user;
			this.OBO = oboUser;
		}

		public void SetOBO( UserModel obo ) => this.OBO = obo;
	}

}
