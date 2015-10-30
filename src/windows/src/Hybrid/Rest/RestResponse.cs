using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Web.Http;

namespace Salesforce.SDK.Hybrid.Rest
{
    public sealed class RestResponse
    {
        private SDK.Rest.IRestResponse _response;
        private string _responseBody;

        public RestResponse()
        {
            
        }

        internal RestResponse(SDK.Rest.IRestResponse response)
        {
            _response = response;
            _responseBody = _response.AsString;
        }

        public bool Success
        {
            get { return _response.Success; }
        }

        public Exception Error
        {
            get { return _response.Error; }
        }

        public string AsString
        {
            get { return _responseBody; }
        }


        public HttpStatusCode StatusCode
        {
            get { return (HttpStatusCode)_response.StatusCode; }
        }
    }
}
