using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SampleReact
{
	public interface IActionProcessor
	{
		Task<APIResponseModel> Process( APIRequestModel request, SampleUser user = null );

		Task<APIResponseModel> Process( UserSignupAction action );
		Task<APIResponseModel> Process( UserVerifyEmailAction action );

		Task<APIResponseModel> Process( SampleUser userContext, UserCancelAction action );
		Task<APIResponseModel> Process( SampleUser userContext, UserUpdatePasswordAction action );
		Task<APIResponseModel> Process( SampleUser userContext, UserUpdateDisplayNameAction action );
		Task<APIResponseModel> Process( SampleUser userContext, UserSiteInfoAction action );
		Task<APIResponseModel> Process( InternalSendEmailVerificationAction action );

		//Task<APIResponseModel> Process( SampleUser userContext, AdminReloadAction action );
		//Task<APIResponseModel> Process( SampleUser userContext, AdminDeleteUserAction action );
	}

}
