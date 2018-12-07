using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SampleReact
{
	public class PasswordOptions
	{
		public int RequiredLength { get; set; }
		public int RequiredUniqueChars { get; set; }
		public bool RequireNonAlphanumeric { get; set; }
		public bool RequireLowercase { get; set; }
		public bool RequireUppercase { get; set; }
		public bool RequireDigit { get; set; }
	}

	public interface IPasswordService
	{
		Task<string> CreatePasswordHash( string password );
		Task<string> GenerateRandomPassword( PasswordOptions opts = null );
		Task<bool> Verify( string passwordGuess, string actualSavedHashResults );
		Task<string> BuildEmailVerificationString( Guid userId, string verificationCode );
		Task<Tuple<bool,Guid,string>> TryParseEmailVerificationString( string value );
	}
}
