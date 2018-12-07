using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SampleReact
{
	public static class CodeGenerator
	{
		public static String GeneratePassword()
		{

			//get 3 of these
			var upperCase = new char[]
						{
								'A', 'B', 'C', 'D', 'E', 'F', 'G', 'H', 'I', 'J', 'K', 'L', 'M', 'N', 'O', 'P', 'Q', 'R', 'S', 'T', 'U',
								'V', 'W', 'X', 'Y', 'Z'
						};

			//get 3 of these
			var lowerCase = new char[]
						{
								'a', 'b', 'c', 'd', 'e', 'f', 'g', 'h', 'i', 'j', 'k', 'l', 'm', 'n', 'o', 'p', 'q', 'r', 's', 't', 'u',
								'v', 'w', 'x', 'y', 'z'
						};

			//get 1 of these
			var numerals = new char[] { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9' };

			//get 1 of these
			var symbols = new char[] { '~', '!', '@', '#', '$', '%', '^', '&', '*', '+', ':', '?' };

			char[] chars = new char[8];

			Random ran = new Random();

			chars[0] = upperCase[ran.Next( 0, upperCase.Length - 1 )];
			chars[1] = lowerCase[ran.Next( 0, lowerCase.Length - 1 )];

			chars[2] = numerals[ran.Next( 0, numerals.Length - 1 )];
			chars[3] = lowerCase[ran.Next( 0, lowerCase.Length - 1 )];

			chars[4] = upperCase[ran.Next( 0, numerals.Length - 1 )];
			chars[5] = upperCase[ran.Next( 0, numerals.Length - 1 )];

			chars[6] = lowerCase[ran.Next( 0, lowerCase.Length - 1 )];
			chars[7] = symbols[ran.Next( 0, symbols.Length - 1 )];

			return new string( chars );
		}

		public static String Generate( ContactMethodType method )
		{
			switch ( method )
			{
				case ContactMethodType.Email: return Guid.NewGuid().ToString().Replace( "-", "" );
				case ContactMethodType.Mobile: return GenerateMobileCode();
				default: return null;
			}
		}

		public static String GenerateMobileCode()
		{

			//get 1 of these
			var numerals = new char[] { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9' };

			char[] chars = new char[6];

			Random ran = new Random();
			for ( int i = 0; i < chars.Length; i++ )
				chars[i] = numerals[ran.Next( 0, numerals.Length - 1 )];

			return new string( chars );
		}
	}
}
