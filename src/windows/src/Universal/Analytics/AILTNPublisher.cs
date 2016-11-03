/*
 * Copyright (c) 2016-present, salesforce.com, inc.
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
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Salesforce.SDK.Analytics.Model;
using Salesforce.SDK.Auth;
using Salesforce.SDK.Net;
using Salesforce.SDK.Rest;

namespace Salesforce.SDK.Universal.Analytics
{
    public class AiltnPublisher : IAnalyticsPublisher
    {
        private static readonly string CODE = "code";
        private static readonly string AILTN = "ailtn";
        private static string DATA = "data";
        private static string LOG_LINES = "logLines";
        private static string PAYLOAD = "payload";
        private static string API_PATH = "/services/data/{0}/connect/proxy/app-analytics-logging";
        private static string ACCEPT_ENCODING = "AcceptEncoding";
        private static string GZIP = "gzip";

        public async Task<bool> PublishAsync(JArray events)
        {
            if (events == null || events.Count == 0)
            {
                return true;
            }

            //Build the POST body of the request
            var body = new JObject();
            try
            {
                var loglines = new JArray();
                for (var i = 0; i < events.Count; i++)
                {
                    var eventToPublish = events.ElementAt(i);
                    if (eventToPublish != null)
                    {
                        var trackingInfo = new JObject { { CODE, AILTN } };
                        var data = new JObject();
                        var schemaType = eventToPublish[InstrumentationEvent.SCHEMA_TYPE_KEY];
                        data.Add(InstrumentationEvent.SCHEMA_TYPE_KEY, schemaType);
                        var property =
                            eventToPublish.Children<JProperty>()
                                .FirstOrDefault(p => p.Name == InstrumentationEvent.SCHEMA_TYPE_KEY);
                        property?.Remove();
                        data.Add(PAYLOAD, eventToPublish.ToString(Newtonsoft.Json.Formatting.None));
                        trackingInfo.Add(DATA, data);
                        loglines.Add(trackingInfo);
                    }
                }
                body.Add(LOG_LINES, loglines);
            }
            catch (Exception ex)
            {
                return false;
            }
            var path = string.Format(API_PATH, ApiVersionStrings.VersionNumber);
            var headers = new Dictionary<string, string>();
            var request = new RestRequest(HttpMethod.Post, path, body.ToString(Newtonsoft.Json.Formatting.None), ContentTypeValues.Gzip, headers);
            var restClient = SDKManager.GlobalClientManager.PeekRestClient();
            var response = await restClient.SendAsync(request);
            if (response.Success)
            {
                return true;
            }
            return false;
        }
    }
}
