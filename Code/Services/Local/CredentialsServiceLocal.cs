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
    public class CredentialsServiceLocal : ICredentialsService
    {
		private IHttpContextAccessor HttpContextAccessor = null;
		private IDataManager DataManager = null;

		public CredentialsServiceLocal( IHttpContextAccessor httpContextAccessor, IDataManager dataManager )
		{
			this.HttpContextAccessor = httpContextAccessor;
			this.DataManager = dataManager;
		}

		public SampleUser GetUser()
		{
			if( this.HttpContextAccessor != null && this.HttpContextAccessor.HttpContext != null )
			{
				return this.HttpContextAccessor.HttpContext.User as SampleUser;
			}

			return null;
		}
		
		public bool TryPrepareUserFrom( CookieValidatePrincipalContext context )
		{
			if( context.Request.Path == "/sitehub/negotiate" ) return true;

			Console.WriteLine( $"TryPrepareUserFromCookie => {context.Request.Path}" );
			var claim = context.Principal.Claims.FirstOrDefault( x => x.Type == ClaimTypes.NameIdentifier );
			if( claim == null ) return false;

			try
			{
				Guid userId = Guid.Parse( claim.Value );
				var obo = context.Principal.Claims.FirstOrDefault( x => x.Type == ClaimTypes.UserData );

				UserModel user = null, oboUser = null;

				var rsC = this.DataManager.QueryUser( userId, IncludeOptions.None ).GetAwaiter().GetResult();
				if( rsC.Code != ResponseCode.Ok ) return false;
				user = rsC.Data[0].User;

				if( obo != null )
				{
					rsC = this.DataManager.QueryUser( obo.Value.Decode(), IncludeOptions.None ).GetAwaiter().GetResult();
					if( rsC.Code != ResponseCode.Ok ) return false;
					oboUser = rsC.Data[0].User;
				}

				context.Principal = new SampleUser( user, context.Principal.Identity, oboUser );
				return true;
			}
			catch( Exception ex )
			{
				return false;
			}
		}

		public bool TryPrepareUserFrom( Microsoft.AspNetCore.SignalR.HubCallerContext context, out SampleUser tfUser )
		{
			tfUser = null;

			var claim = context.User.Claims.FirstOrDefault( c => c.Type == ClaimTypes.NameIdentifier );
			if( claim == null ) return false;

			try
			{
				Guid userId = Guid.Parse( claim.Value );
				var obo = context.User.Claims.FirstOrDefault( x => x.Type == ClaimTypes.UserData );

				UserModel user = null, oboUser = null;

				var rsC = this.DataManager.QueryUser( userId, IncludeOptions.None ).GetAwaiter().GetResult();
				if( rsC.Code != ResponseCode.Ok ) return false;
				user = rsC.Data[0].User;

				if( obo != null )
				{
					rsC = this.DataManager.QueryUser( obo.Value.Decode(), IncludeOptions.None ).GetAwaiter().GetResult();
					if( rsC.Code != ResponseCode.Ok ) return false;
					oboUser = rsC.Data[0].User;
				}

				tfUser = new SampleUser( user, context.User.Identity, oboUser );
				return true;
			}
			catch( Exception ex )
			{
				return false;
			}
		}


		public bool TryPrepareUserFrom( HttpContext httpContext, out SampleUser tfUser )
		{
			tfUser = null;

			var claim = httpContext.User.Claims.FirstOrDefault( c => c.Type == ClaimTypes.NameIdentifier );
			if( claim == null ) return false;

			try
			{
				Guid userId = Guid.Parse( claim.Value );
				var obo = httpContext.User.Claims.FirstOrDefault( x => x.Type == ClaimTypes.UserData );

				UserModel user = null, oboUser = null;

				var rsC = this.DataManager.QueryUser( userId, IncludeOptions.None ).GetAwaiter().GetResult();
				if( rsC.Code != ResponseCode.Ok ) return false;
				user = rsC.Data[0].User;

				if( obo != null )
				{
					rsC = this.DataManager.QueryUser( obo.Value.Decode(), IncludeOptions.None ).GetAwaiter().GetResult();
					if( rsC.Code != ResponseCode.Ok ) return false;
					oboUser = rsC.Data[0].User;
				}

				tfUser = new SampleUser( user, httpContext.User.Identity, oboUser );
				httpContext.User = tfUser;
				return true;
			}
			catch( Exception ex )
			{
				return false;
			}
		}
		
		public async Task Signin( UserModel user, HttpContext context )
		{
			var claims = new List<Claim>();
			claims.Add( new Claim( ClaimTypes.NameIdentifier, user.UserId.Encode() ) );
			var claimsIdentity = new ClaimsIdentity( claims, CookieAuthenticationDefaults.AuthenticationScheme);

			var authProperties = new AuthenticationProperties
			{
				IsPersistent = true,
				IssuedUtc = DateTimeOffset.UtcNow,
				AllowRefresh = true
			};

			SampleUser p = new SampleUser( user, claimsIdentity );

			await context.SignInAsync( CookieAuthenticationDefaults.AuthenticationScheme, p, authProperties );
		}
		
		public async Task OBO( UserModel user, UserModel oboUser )
		{
			var claims = new List<Claim>();
			claims.Add( new Claim( ClaimTypes.NameIdentifier, user.UserId.Encode() ) );
			claims.Add( new Claim( ClaimTypes.UserData, oboUser.UserId.Encode() ) );
			var claimsIdentity = new ClaimsIdentity( claims, CookieAuthenticationDefaults.AuthenticationScheme);

			var authProperties = new AuthenticationProperties
			{
				IsPersistent = true,
				IssuedUtc = DateTimeOffset.UtcNow,
				AllowRefresh = true
			};

			SampleUser p = new SampleUser( user, claimsIdentity, oboUser );

			await this.HttpContextAccessor.HttpContext.SignInAsync( CookieAuthenticationDefaults.AuthenticationScheme, p, authProperties );
		}
    }
}