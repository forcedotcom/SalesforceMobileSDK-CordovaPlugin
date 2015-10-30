using Salesforce.SDK.Net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Web.Http;


namespace Salesforce.SDK.Hybrid.Rest
{
    public delegate IAsyncOperation<string> AccessTokenProvider();

    public sealed class RestClient
    {
        private SDK.Rest.IRestClient _restClient;

        internal RestClient(SDK.Rest.IRestClient restClient)
        {
            _restClient = restClient;
        }

        public string InstanceUrl
        {
            get { return _restClient.InstanceUrl; }
        }

        public string AccessToken
        {
            get { return _restClient.AccessToken; }
        }

        public IAsyncOperation<RestResponse> SendAsync(HttpMethod method, string url)
        {
            Task<RestResponse> response = InternalSendAsync(new System.Net.Http.HttpMethod(method.Method), url);
            return response.AsAsyncOperation<RestResponse>();
        }

        private async Task<RestResponse> InternalSendAsync(System.Net.Http.HttpMethod method, string url)
        {
            var response = await _restClient.SendAsync(method, url);
            return new RestResponse(response);
        }

        public IAsyncOperation<RestResponse> SendAsync(RestRequest request)
        {
            Task<RestResponse> response = InternalSendAsync(request);
            return response.AsAsyncOperation<RestResponse>();
        }

        private async Task<RestResponse> InternalSendAsync(RestRequest request)
        {
            var result = await _restClient.SendAsync(request.Request);
            return new RestResponse(result);
        }
    }
}
