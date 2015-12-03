/*
 * Copyright (c) 2013, salesforce.com, inc.
 * All rights reserved.
 * Redistribution and use of this software in source and binary forms, with or
 * without modification, are permitted provided that the following conditions
 * are met:
 * - Redistributions of source code must retain the above copyright notice, this
 * list of conditions and the following disclaimer.
 * - Redistributions in binary form must reproduce the above copyright notice,
 * this list of conditions and the following disclaimer in the documentation
 * and/or other materials provided with the distribution.
 * - Neither the name of salesforce.com, inc. nor the names of its contributors
 * may be used to endorse or promote products derived from this software without
 * specific prior written permission of salesforce.com, inc.
 * THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS"
 * AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE
 * IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE
 * ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR CONTRIBUTORS BE
 * LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR
 * CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF
 * SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS
 * INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN
 * CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE)
 * ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE
 * POSSIBILITY OF SUCH DAMAGE.
 */

using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Salesforce.SDK.Net;

namespace Salesforce.SDK.Rest
{
    public delegate void AsyncRequestCallback(IRestResponse response);

    public delegate Task<string> AccessTokenProvider();

    public class RestClient : IRestClient
    {
        private readonly AccessTokenProvider _accessTokenProvider;

        private readonly string _instanceUrl;


        private string _accessToken;

        public RestClient(string instanceUrl, string accessToken, AccessTokenProvider accessTokenProvider)
        {
            _instanceUrl = instanceUrl;
            _accessToken = accessToken;
            _accessTokenProvider = accessTokenProvider;
        }

        public RestClient(string instanceUrl)
        {
            _instanceUrl = instanceUrl;
        }

        public string InstanceUrl => _instanceUrl;

        public string AccessToken => _accessToken;

        public async Task<IRestResponse> SendAsync(HttpMethod method, string url)
        {
            return await SendAsync(new RestRequest(method, url));
        }

        public async void SendAsync(RestRequest request, AsyncRequestCallback callback)
        {
            var result = await SendAsync(request).ConfigureAwait(false);
            callback?.Invoke(result);
        }

        public async Task<IRestResponse> SendAsync(RestRequest request)
        {
            var result = await SendAsync(request, true);
            return new RestResponse(result);
        }

        private async Task<HttpCall> SendAsync(RestRequest request, bool retryInvalidToken)
        {
            var url = _instanceUrl + request.Path;
            var headers = request.AdditionalHeaders != null
                ? new HttpCallHeaders(_accessToken, request.AdditionalHeaders)
                : new HttpCallHeaders(_accessToken, new Dictionary<string, string>());

            var call =
                await
                    new HttpCall(request.Method, headers, url, request.RequestBody, request.ContentType).ExecuteAsync()
                        .ConfigureAwait(false);

            if (call.StatusCode != HttpStatusCode.Unauthorized && call.StatusCode != HttpStatusCode.Forbidden)
            {
                return call;
            }

            if (!retryInvalidToken || _accessTokenProvider == null) return call;

            var newAccessToken = await _accessTokenProvider();
            if (newAccessToken == null)
            {
                return call;
            }
                
            _accessToken = newAccessToken;
            call = await SendAsync(request, false);

            // Done
            return call;
        }
    }
}