using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SampleReact
{
	public interface IActionConverter
	{
		AuthenticationMode Mode { get; }
		ActionType Action { get; }
		UserCancelAction UserCancel();
		UserSignupAction UserSignup();
		UserUpdateDisplayNameAction UserUpdateDisplayName();
		UserUpdatePasswordAction UserUpdatePassword();
		UserVerifyEmailAction UserVerifyEmail();
		UserSiteInfoAction UserSiteInfo();
	}
}
