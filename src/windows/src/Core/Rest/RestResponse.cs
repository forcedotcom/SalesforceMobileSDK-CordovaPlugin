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
using System.Net;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Salesforce.SDK.Net;

namespace Salesforce.SDK.Rest
{
    public class RestResponse : IRestResponse
    {
        private readonly HttpCall _call;
        private string _prettyBody;
        private JArray _responseArray;
        private JObject _responseObject;

        public RestResponse(HttpCall call)
        {
            _call = call;
        }

        public bool Success
        {
            get { return _call.Success; }
        }

        public Exception Error
        {
            get { return _call.Error; }
        }

        public string AsString
        {
            get { return _call.ResponseBody; }
        }

        public JArray AsJArray
        {
            get
            {
                if (_responseArray == null)
                {
                    _responseArray = JArray.Parse(AsString);
                }
                return _responseArray;
            }
        }

        public JObject AsJObject
        {
            get
            {
                if (_responseObject == null)
                {
                    _responseObject = JObject.Parse(AsString);
                }
                return _responseObject;
            }
        }

        public HttpStatusCode StatusCode
        {
            get { return _call.StatusCode; }
        }

        public string PrettyBody
        {
            get
            {
                if (_prettyBody == null)
                {
                    try
                    {
                        _prettyBody = JsonConvert.SerializeObject(AsJObject, Formatting.Indented);
                    }
                    catch
                    {
                        try
                        {
                            _prettyBody = JsonConvert.SerializeObject(AsJArray, Formatting.Indented);
                        }
                        catch
                        {
                            _prettyBody = AsString;
                        }
                    }
                }
                return _prettyBody;
            }
        }
    }
}