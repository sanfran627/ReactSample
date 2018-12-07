using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Text.RegularExpressions;

namespace SampleReact
{

    public interface IPasswordStrengthValidator
    {
		PasswordStrengthResult Test( string password );
    }

	public class PasswordStrengthValidator : IPasswordStrengthValidator
	{
		Regex 
			upper = new Regex("[A-Z]"),
			lower = new Regex("[a-z]"),
			number = new Regex("[0-9]"),
			repeat = new Regex(@"(.)\1{2}"),
			special = new Regex("[^A-Za-z0-9]");

		int minLength = 8,
			maxLength = 32;

		public PasswordStrengthResult Test( string password )
		{
			bool hasUpper = upper.IsMatch(password),
				hasLower = lower.IsMatch(password),
				hasNumber = number.IsMatch(password),
				//hasSpecial = special.IsMatch(password),
				//hasRepeat = repeat.IsMatch(password), // neegative
				tooShort = password.Length < minLength,
				tooLong = password.Length > maxLength;

			int score = 0;

			// Increment the score for each of these conditions
			if( hasUpper ) score++;
			  if (hasLower) score++;
			  if (hasNumber) score++;
			  //if (hasSpecial) score++;

			// Penalize if there aren't at least three char types
			if (score > 0 && score < 3) score--;
			if (tooShort || tooLong ) score = 0;

			var result = new PasswordStrengthResult
			{
				Upper = hasUpper,
				Lower = hasLower,
				Number = hasNumber,
				//Special = hasSpecial,
				Length = !tooLong && !tooLong,
				//Repeat = !hasRepeat,
				Rank = score < 3
				  ? PasswordRank.Weak
				  : score < 4
					? PasswordRank.Medium
					: score < 6
					  ? PasswordRank.Strong
					  : PasswordRank.VeryStrong,
				Score = score,
			};

			result.Good = hasUpper && hasLower && ( hasNumber ) && result.Rank >= PasswordRank.Medium;

			return result;
		}
	}

	public enum PasswordRank
	{
		Weak = 0,
		Medium = 1,
		Strong = 2,
		VeryStrong = 3
	}

	public class PasswordStrengthResult
	{
		public bool Upper { get; set; }
		public bool Lower { get; set; }
		public bool Number { get; set; }
		public bool Special { get; set; }
		public bool Repeat { get; set; }
		public bool Length { get; set; }
		public bool Good { get; set; }
		public PasswordRank Rank { get; set; }
		public int Score { get; set; }
	}
}
