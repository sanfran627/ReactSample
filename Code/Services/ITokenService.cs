using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using Microsoft.Extensions.Options;

namespace SampleReact
{
	public interface ITokenService
	{
		string BuildToken( Guid userId, UserModel oboUser = null );
	}
}
