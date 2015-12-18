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

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Salesforce.SDK.Logging;
using Newtonsoft.Json;
using Salesforce.SDK.Core;
using Salesforce.SDK.Exceptions;
using Salesforce.SDK.Utilities;
using Salesforce.SDK.Settings;

namespace Salesforce.SDK.Net
{
    /// <summary>
    ///     Enumeration used to represent the content type of a HTTP request
    /// </summary>
    public enum ContentTypeValues
    {
        FormUrlEncoded,
        Json,
        Xml,
        None
    }

    public class HttpCallHeaders
    {
        public HttpCallHeaders(string authorization, Dictionary<string, string> headers)
        {
            if (!String.IsNullOrWhiteSpace(authorization))
            {
                Authorization = new AuthenticationHeaderValue("Bearer", authorization);
            }
            Headers = headers;
        }

        public AuthenticationHeaderValue Authorization { get; private set; }
        public Dictionary<string, string> Headers { get; private set; }
    }

    /// <summary>
    ///     Extension for ContentType enum (to get the mime type of a given content type)
    /// </summary>
    public static class Extensions
    {
        public static string MimeType(this ContentTypeValues contentType)
        {
            switch (contentType)
            {
                case ContentTypeValues.Json:
                    return "application/json";
                case ContentTypeValues.FormUrlEncoded:
                    return "application/x-www-form-urlencoded";
                case ContentTypeValues.Xml:
                    return "text/xml";
                default:
                    return null;
            }
        }
    }

    /// <summary>
    ///     A portable class to send HTTP requests
    ///     HttpCall objects can only be used once
    /// </summary>
    public sealed class HttpCall : IDisposable
    {
        private readonly ContentTypeValues _contentType;
        private readonly HttpCallHeaders _headers;
        private readonly HttpMethod _method;
        private readonly string _requestBody;
        private readonly string _url;
        private readonly HttpClient _httpClient;
        private Exception _httpCallErrorException;
        private string _responseBodyText;
        private Dictionary<string, IEnumerable<string>> _responseHeaders; 
        private HttpStatusCode _statusCodeValue;
        private string _errorReasonPhrase;

        /// <summary>
        ///     Constructor for HttpCall
        /// </summary>
        /// <param name="method"></param>
        /// <param name="headers"></param>
        /// <param name="url"></param>
        /// <param name="requestBody"></param>
        /// <param name="contentType"></param>
        public HttpCall(HttpMethod method, HttpCallHeaders headers, string url, string requestBody,
            ContentTypeValues contentType)
        {
            var handler = new HttpClientHandler()
            {
                AllowAutoRedirect = false,
                AutomaticDecompression = DecompressionMethods.None,
            };

            _httpClient = new HttpClient(handler);

            _method = method;
            _headers = headers;
            _url = url;
            _requestBody = requestBody;
            _contentType = contentType;
        }

        /// <summary>
        ///     Use this property to retrieve the user agent.
        /// </summary>
        public static string UserAgentHeader { private set; get; }

        /// <summary>
        ///     True if HTTP request has been executed
        /// </summary>
        public bool Executed => (_responseBodyText != null || _httpCallErrorException != null);

        /// <summary>
        ///     True if HTTP request was successfully executed
        /// </summary>
        public bool Success
        {
            get
            {
                CheckExecuted();
                return _httpCallErrorException == null;
            }
        }

        /// <summary>
        ///     Error that was raised if HTTP request did not execute successfully
        /// </summary>
        public Exception Error
        {
            get
            {
                CheckExecuted();
                return _httpCallErrorException;
            }
        }

        /// <summary>
        ///     True if the HTTP response returned by the server had a body
        /// </summary>
        public bool HasResponse => _responseBodyText != null;

        /// <summary>
        ///     Body of the HTTP response returned by the server
        /// </summary>
        public string ResponseBody
        {
            get
            {
                CheckExecuted();
                return _responseBodyText;
            }
        }

        /// <summary>
        ///     HTTP status code fo the response returned by the server
        /// </summary>
        public HttpStatusCode StatusCode
        {
            get
            {
                CheckExecuted();
                return _statusCodeValue;
            }
        }

        public string ErrorReasonPhrase
        {
            get
            {
                CheckExecuted();
                return _errorReasonPhrase;
            }
        }

        public Dictionary<string, IEnumerable<string>> ResponseHeaders
        {
            get
            {
                CheckExecuted();
                return _responseHeaders;
            }
        }

        private void CheckExecuted()
        {
            if (!Executed)
            {
                throw new InvalidOperationException("HttpCall must be executed first");
            }
        }

        /// <summary>
        ///     Factory method to build a HttpCall objet for a GET request with additional HTTP request headers
        /// </summary>
        /// <param name="headers"></param>
        /// <param name="url"></param>
        /// <returns></returns>
        public static HttpCall CreateGet(HttpCallHeaders headers, string url)
        {
            return new HttpCall(HttpMethod.Get, headers, url, null, ContentTypeValues.None);
        }

        /// <summary>
        ///     Factory method to build a HttpCall object for a GET request
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        public static HttpCall CreateGet(string url)
        {
            return CreateGet(null, url);
        }

        /// <summary>
        ///     Factory method to build a HttpCall object for a POST request with a specific content type
        /// </summary>
        /// <param name="headers"></param>
        /// <param name="url"></param>
        /// <param name="requestBody"></param>
        /// <param name="contentType"></param>
        /// <returns></returns>
        public static HttpCall CreatePost(HttpCallHeaders headers, string url, string requestBody,
            ContentTypeValues contentType)
        {
            return new HttpCall(HttpMethod.Post, headers, url, requestBody, contentType);
        }

        /// <summary>
        ///     Factory method to build a HttpCall object for a POST request with form url encoded arguments
        /// </summary>
        /// <param name="url"></param>
        /// <param name="requestBody"></param>
        /// <returns></returns>
        public static HttpCall CreatePost(string url, string requestBody)
        {
            return CreatePost(null, url, requestBody, ContentTypeValues.FormUrlEncoded);
        }

        /// <summary>
        ///     Async method to execute the HTTP request which expects the HTTP response body to be a Json object that can be
        ///     deserizalized as an instance of type T
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public async Task<T> ExecuteAndDeserializeAsync<T>()
        {
            var call = await ExecuteAsync().ConfigureAwait(false);
            if (call.Success)
            {
                return JsonConvert.DeserializeObject<T>(call.ResponseBody);
            }

            if (!HasResponse)
            {
                // treat non-success calls with empty bodies as offline exceptions since the
                // call was not able to go out
                throw new DeviceOfflineException("Response does not have a body", call.Error);
            }

            throw call.Error;
        }

        /// <summary>
        ///     Executes the HttpCall. This will generate the headers, create the request and populate the HttpCall properties with
        ///     relevant data.
        ///     The HttpCall may only be called once; further attempts to execute the same call will throw an
        ///     InvalidOperationException.
        /// </summary>
        /// <returns>HttpCall with populated data</returns>
        public async Task<HttpCall> ExecuteAsync()
        {
            if (Executed)
            {
                throw new InvalidOperationException("A HttpCall can only be executed once");
            }
            var req = new HttpRequestMessage(_method, new Uri(_url));
            // Setting header
            if (_headers != null)
            {
                if (_headers.Authorization != null)
                {
                    req.Headers.Authorization = _headers.Authorization;
                }
                foreach (var item in _headers.Headers)
                {
                    req.Headers.Add(item.Key, item.Value);
                }
            }
            // if the user agent has not yet been set, set it; we want to make sure this only really happens once since it requires an action that goes to the core thread.
            if (String.IsNullOrWhiteSpace(UserAgentHeader))
            {
                UserAgentHeader = await SDKServiceLocator.Get<IApplicationInformationService>().GenerateUserAgentHeaderAsync(false, String.Empty);
            }
            req.Headers.UserAgent.TryParseAdd(UserAgentHeader);
            if (!String.IsNullOrWhiteSpace(_requestBody))
            {
                switch (_contentType)
                {
                    case ContentTypeValues.FormUrlEncoded:
                        req.Content = new FormUrlEncodedContent(_requestBody.ParseQueryString());
                        break;
                    default:
                        req.Content = new StringContent(_requestBody);
                        req.Content.Headers.ContentType = new MediaTypeHeaderValue(_contentType.MimeType());
                        break;
                }
            }
            HttpResponseMessage message;

            try
            {
                message = await _httpClient.SendAsync(req);
            }
            catch (HttpRequestException ex)
            {
                _httpCallErrorException =
                    new DeviceOfflineException("Request failed to send, most likely because we were offline", ex);
                return this;
            }
            catch (WebException ex)
            {
                _httpCallErrorException =
                    new DeviceOfflineException("Request failed to send, most likely because we were offline", ex);
                return this;
            }

            await HandleMessageResponseAsync(message);

            return this;
        }

        private async Task HandleMessageResponseAsync(HttpResponseMessage response)
        {
            if (response == null)
            {
                throw new ArgumentException("Response message cannot be null", nameof(response));
            }

            _responseHeaders = new Dictionary<string, IEnumerable<string>>();
            foreach (var header in response.Headers)
            {
                _responseHeaders.Add(header.Key, header.Value);
            }

            try
            {
                _responseBodyText = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    _errorReasonPhrase = response.ReasonPhrase;
                }

                _statusCodeValue = response.StatusCode;
            }
            catch (Exception e)
            {
                Debugger.Break();
            }

            // Get the response exception
            try
            {
                response.EnsureSuccessStatusCode();
            }
            catch (Exception ex)
            {
                // if we are offline and fiddler is running, we will get a BadGateway so wrap the exception in
                // a DeviceOfflineException

                _httpCallErrorException = response.StatusCode == HttpStatusCode.BadGateway 
                    ? new DeviceOfflineException("Could not connect to server because of a bad connection", ex) 
                    : ex;
            }
            response.Dispose();
        }

        public void Dispose()
        {
            if (_httpClient == null)
            {
                return;
            }

            try
            {
                _httpClient.Dispose();
            }
            catch (Exception)
            {
                SDKServiceLocator.Get<ILoggingService>().Log("Error occurred while disposing", LoggingLevel.Warning);
            }
        }
    }
}