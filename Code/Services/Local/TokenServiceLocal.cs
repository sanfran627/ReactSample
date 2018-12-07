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
	public class TokenServiceLocal : ITokenService
	{
		AppSettings Settings = null;

		public TokenServiceLocal( IOptions<AppSettings> settings = null ) => this.Settings = settings.Value;

		public string BuildToken( Guid userId, UserModel oboUser = null )
		{
			var jwtKey = this.Settings.JWTSecretKeys["Site"];
			var key = System.Text.Encoding.ASCII.GetBytes(jwtKey.Key);

			var tokenHandler = new JwtSecurityTokenHandler();
			var tokenDescriptor = new SecurityTokenDescriptor
			{
				Subject = new ClaimsIdentity(new Claim[] { new Claim(ClaimTypes.NameIdentifier, userId.Encode()) }),
				Expires = DateTime.UtcNow.AddMinutes(jwtKey.DurationInMinutes),
				SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
			};

			if( oboUser != null ) tokenDescriptor.Subject.AddClaim( new Claim( ClaimTypes.UserData, oboUser.UserId.Encode() ) );

			return tokenHandler.WriteToken( tokenHandler.CreateToken( tokenDescriptor ) );
		}
	}
}
