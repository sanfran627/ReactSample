using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace SampleReact
{
	public enum AuthenticationMode
	{
		None,
		Background,
		UserCookie,
		StaffCookie,
		API
	}

	public interface ICredentialsService
	{
		SampleUser GetUser();
		bool TryPrepareUserFrom( CookieValidatePrincipalContext context );
		bool TryPrepareUserFrom( HttpContext httpContext, out SampleUser user );
		bool TryPrepareUserFrom( Microsoft.AspNetCore.SignalR.HubCallerContext context, out SampleUser user );
		Task Signin( UserModel user, HttpContext context );
		Task OBO( UserModel user, UserModel oboUser );
	}
}