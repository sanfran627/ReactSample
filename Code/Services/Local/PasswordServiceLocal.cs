using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Identity;

namespace SampleReact
{
	public class PasswordServiceLocal : IPasswordService
	{
		private const int SaltByteLength = 24;
		private const int DerivedKeyLength = 24;

		public async Task<string> CreatePasswordHash( string password )
		{
			var salt = GenerateRandomSalt();
			var iterationCount = GetIterationCount();
			var hashValue = GenerateHashValue(password, salt, iterationCount);
			var iterationCountBtyeArr = BitConverter.GetBytes(iterationCount);
			var valueToSave = new byte[SaltByteLength + DerivedKeyLength + iterationCountBtyeArr.Length];
			Buffer.BlockCopy( salt, 0, valueToSave, 0, SaltByteLength );
			Buffer.BlockCopy( hashValue, 0, valueToSave, SaltByteLength, DerivedKeyLength );
			Buffer.BlockCopy( iterationCountBtyeArr, 0, valueToSave, salt.Length + hashValue.Length, iterationCountBtyeArr.Length );
			return Convert.ToBase64String( valueToSave );
		}

		private static int GetIterationCount()
		{
			return 24 * 1000;
		}

		private static byte[] GenerateRandomSalt()
		{
			var csprng = new RNGCryptoServiceProvider();
			var salt = new byte[SaltByteLength];
			csprng.GetBytes( salt );
			return salt;
		}

		private static byte[] GenerateHashValue( string password, byte[] salt, int iterationCount )
		{
			byte[] hashValue;
			var valueToHash = string.IsNullOrEmpty(password) ? string.Empty : password;
			using( var pbkdf2 = new Rfc2898DeriveBytes( valueToHash, salt, iterationCount ) )
			{
				hashValue = pbkdf2.GetBytes( DerivedKeyLength );
			}
			return hashValue;
		}

		private static bool ConstantTimeComparison( byte[] passwordGuess, byte[] actualPassword )
		{
			uint difference = (uint) passwordGuess.Length ^ (uint) actualPassword.Length;
			for( var i = 0; i < passwordGuess.Length && i < actualPassword.Length; i++ )
			{
				difference |= ( uint ) ( passwordGuess[i] ^ actualPassword[i] );
			}

			return difference == 0;
		}

		public async Task<bool> Verify( string passwordGuess, string actualSavedHashResults )
		{
			//ingredient #1: password salt byte array
			var salt = new byte[SaltByteLength];

			//ingredient #2: byte array of password
			var actualPasswordByteArr = new byte[DerivedKeyLength];

			//convert actualSavedHashResults to byte array
			var actualSavedHashResultsBtyeArr = Convert.FromBase64String(actualSavedHashResults);

			//ingredient #3: iteration count
			var iterationCountLength = actualSavedHashResultsBtyeArr.Length - (salt.Length + actualPasswordByteArr.Length);
			var iterationCountByteArr = new byte[iterationCountLength];
			Buffer.BlockCopy( actualSavedHashResultsBtyeArr, 0, salt, 0, SaltByteLength );
			Buffer.BlockCopy( actualSavedHashResultsBtyeArr, SaltByteLength, actualPasswordByteArr, 0, actualPasswordByteArr.Length );
			Buffer.BlockCopy( actualSavedHashResultsBtyeArr, ( salt.Length + actualPasswordByteArr.Length ), iterationCountByteArr, 0, iterationCountLength );
			var passwordGuessByteArr= GenerateHashValue(passwordGuess, salt, BitConverter.ToInt32(iterationCountByteArr, 0));
			return ConstantTimeComparison( passwordGuessByteArr, actualPasswordByteArr );
		}

		public async Task<string> BuildEmailVerificationString( Guid userId, string verificationCode )
			=> Crypto.BuildEmailVerificationString( userId, verificationCode );

		public async Task<Tuple<bool, Guid, string>> TryParseEmailVerificationString( string value )
		{
			Guid u;
			string v;
			Tuple<bool, Guid, string> t = Crypto.TryParseEmailVerificationString( value, out u, out v )
										? new Tuple<bool, Guid, string>( true, u, v )
										: new Tuple<bool, Guid, string>( false, Guid.Empty, string.Empty );
			return t;
		}

		/// <summary>
		/// Generates a Random Password
		/// respecting the given strength requirements.
		/// </summary>
		/// <param name="opts">A valid PasswordOptions object
		/// containing the password strength requirements.</param>
		/// <returns>A random password</returns>
		public async Task<string> GenerateRandomPassword( PasswordOptions opts = null )
		{
			if( opts == null ) opts = new PasswordOptions()
			{
				RequiredLength = 12,
				RequiredUniqueChars = 8,
				RequireDigit = true,
				RequireLowercase = true,
				RequireNonAlphanumeric = false,
				RequireUppercase = true
			};

			string[] randomChars = new [] {
												"ABCDEFGHJKLMNOPQRSTUVWXYZ",    // uppercase 
												"abcdefghijkmnopqrstuvwxyz",    // lowercase
												"0123456789",                   // digits
												"!@$?#*"                        // non-alphanumeric
											};

			Random rand = new Random(Environment.TickCount);
			List<char> chars = new List<char>();

			if( opts.RequireUppercase )
				chars.Insert( rand.Next( 0, chars.Count ),
					randomChars[0][rand.Next( 0, randomChars[0].Length )] );

			if( opts.RequireLowercase )
				chars.Insert( rand.Next( 0, chars.Count ),
					randomChars[1][rand.Next( 0, randomChars[1].Length )] );

			if( opts.RequireDigit )
				chars.Insert( rand.Next( 0, chars.Count ),
					randomChars[2][rand.Next( 0, randomChars[2].Length )] );

			if( opts.RequireNonAlphanumeric )
				chars.Insert( rand.Next( 0, chars.Count ),
					randomChars[3][rand.Next( 0, randomChars[3].Length )] );

			for( int i = chars.Count; i < opts.RequiredLength
				|| chars.Distinct().Count() < opts.RequiredUniqueChars; i++ )
			{
				string rcs = randomChars[rand.Next(0, randomChars.Length)];
				chars.Insert( rand.Next( 0, chars.Count ),
					rcs[rand.Next( 0, rcs.Length )] );
			}

			return new string( chars.ToArray() );
		}

	}

	#region Crypto and Ciphers
	public static class Crypto
	{
		private static byte[] _dbHttpV1 = null;
		private static CipherV1 cipherHTTPV1 = null;

		static Crypto()
		{
			_dbHttpV1 = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24 };
			cipherHTTPV1 = new CipherV1( _dbHttpV1 );
		}

		public static string Encrypt( string data ) => cipherHTTPV1.Encryption( data );
		public static string Decrypt( string data ) => cipherHTTPV1.Decryption( data );

		public static string BuildEmailVerificationString( Guid userId, string verificationCode )
		{
			var s = string.Join( "|",
										DateTime.UtcNow.Ticks,
										IDConverter.Encode( userId ),
										verificationCode,
										DateTime.UtcNow.AddYears( -20 ).Ticks );

			s = Encrypt( s );

			return s.Replace( "/", "-" ).Replace( "+", "_" );

		}

		public static bool TryParseEmailVerificationString( string value, out Guid userId, out string verificationCode )
		{
			userId = Guid.Empty;
			verificationCode = string.Empty;

			if( string.IsNullOrWhiteSpace( value ) ) return false;

			value = value.Replace( "-", "/" ).Replace( "_", "+" );

			string s = Decrypt( value );

			if( string.IsNullOrWhiteSpace( s ) )
				return false;

			string[] sections = s.Split("|".ToCharArray(),  StringSplitOptions.RemoveEmptyEntries );

			if( sections.Length != 4 ) return false;

			userId = IDConverter.Decode( sections[1] );

			verificationCode = sections[2];

			return userId != Guid.Empty && !string.IsNullOrEmpty( verificationCode );
		}
	}

	public abstract class Cipher
	{
		protected byte[] RIJNDEALPRIVATEKEY = null;
		protected byte[] RIJNDEALVECTORKEY  = null;

		protected Cipher() { }

		protected Cipher( byte[] seed ) => this.GenerateKeys( seed );

		protected abstract void GenerateKeys( byte[] seed );

		public string Encryption( string data )
		{
			string result = string.Empty;

			if( !string.IsNullOrEmpty( data ) )
			{
				try
				{
					//Get the data.
					byte[] byEncrypt = RijndealEncryption( System.Text.Encoding.UTF8.GetBytes( data ) );

					result = Convert.ToBase64String( byEncrypt );
				}
				catch
				{
					result = data;
				}
			}

			return result;
		}

		public string Decryption( string data )
		{
			string result = string.Empty;

			if( !string.IsNullOrEmpty( data ) )
			{
				try
				{
					//Decrypt the data.
					byte[] byDecrypt = RijndealDecryption( Convert.FromBase64String( data ) );

					result = System.Text.Encoding.UTF8.GetString( byDecrypt );
				}
				catch
				{
					result = data;
				}
			}

			return result;
		}

		private byte[] RijndealEncryption( byte[] data )
		{
			byte[] result = null;

			if( RIJNDEALPRIVATEKEY == null )
				throw ( new InvalidOperationException( "Encryption key has not been defined" ) );

			if( data != null && data.Length > 0 )
			{
				try
				{
					Rijndael obj = System.Security.Cryptography.Rijndael.Create();

					try
					{
						using( System.Security.Cryptography.ICryptoTransform encrypt = obj.CreateEncryptor( RIJNDEALPRIVATEKEY, RIJNDEALVECTORKEY ) )
						{
							result = encrypt.TransformFinalBlock( data, 0, data.Length );
						}
					}
					catch { }
				}
				catch { }
			}

			return ( result );
		}

		private byte[] RijndealDecryption( byte[] data )
		{
			byte[] result = null;

			if( RIJNDEALPRIVATEKEY == null )
				throw ( new InvalidOperationException( "Encryption key has not been defined" ) );

			if( data != null && data.Length > 0 )
			{
				try
				{
					Rijndael obj = System.Security.Cryptography.Rijndael.Create();

					try
					{
						using( System.Security.Cryptography.ICryptoTransform encrypt = obj.CreateDecryptor( RIJNDEALPRIVATEKEY, RIJNDEALVECTORKEY ) )
						{
							result = encrypt.TransformFinalBlock( data, 0, data.Length );
						}
					}
					catch { }
				}
				catch { }
			}

			return result;
		}
	}

	public class CipherV1 : Cipher
	{
		private CipherV1() { }

		public CipherV1( byte[] seed ) : base( seed ) { }

		protected override void GenerateKeys( byte[] seed )
		{
			if( seed.Length != 24 )
				throw ( new Exception( "Invalid encryption/decryption key specified.  Key must contain 24 entries[" + seed + "]" ) );

			Rijndael rijndeal = Rijndael.Create();
			byte[]   bigkey   = null;

			// Perform a hash operation using the phrase. This will generate a unique 32 byte (256-bit) value to be used as the key.      
			using( SHA256Managed sha256 = new SHA256Managed() )
			{
				sha256.ComputeHash( seed );
				bigkey = sha256.Hash;
			}

			//Build private key
			RIJNDEALPRIVATEKEY = new Byte[rijndeal.KeySize / 8];
			Array.Copy( bigkey, 0, RIJNDEALPRIVATEKEY, 0, RIJNDEALPRIVATEKEY.Length );

			//Build vectory key
			RIJNDEALVECTORKEY = new Byte[rijndeal.IV.Length];
			Array.Copy( bigkey, RIJNDEALPRIVATEKEY.Length / 2, RIJNDEALVECTORKEY, 0, RIJNDEALVECTORKEY.Length );
		}
	}

	#endregion
}
