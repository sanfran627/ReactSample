using System.Threading.Tasks;

namespace SampleReact
{
	public interface IRestService
	{
		Task<ResponseModel<TResponse>> Get<TResponse>( string requestUri, string username = null, string password = null ) where TResponse : class, new();
		Task<ResponseModel> Post<T>( string requestUri, T request, string username = null, string password = null );
		Task<ResponseModel<TResponse>> Post<TRequest, TResponse>( string requestUri, TRequest request, string username = null, string password = null ) where TResponse : class, new();
	}
}
