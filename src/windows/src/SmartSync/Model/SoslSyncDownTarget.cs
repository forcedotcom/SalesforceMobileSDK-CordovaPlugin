/*
 * Copyright (c) 2015, salesforce.com, inc.
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
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Salesforce.SDK.Rest;
using Salesforce.SDK.SmartSync.Manager;
using Salesforce.SDK.SmartSync.Util;

namespace Salesforce.SDK.SmartSync.Model
{
    /// <summary>
    ///     Target for sync u i.e. set of objects to download from server
    /// </summary>
    public class SoslSyncDownTarget : SyncDownTarget
    {
        public SoslSyncDownTarget(JObject target) : base(target)
        {
            this.Query = target.ExtractValue<string>(Constants.Query);
        }

        public SoslSyncDownTarget(string query)
        {
            QueryType = QueryTypes.Sosl;
            Query = query;
        }


        private new string Query { set; get; }
        private string NextRecordsUrl { set; get; }

        /// <summary>
        /// </summary>
        /// <returns>json representation of target</returns>
        public override JObject AsJson()
        {
            var target = base.AsJson();
            if (!String.IsNullOrWhiteSpace(Query)) target[Constants.Query] = Query;
            return target;
        }

        public override async Task<JArray> StartFetch(SyncManager syncManager, long maxTimeStamp)
        {
            var request = RestRequest.GetRequestForSearch(syncManager.ApiVersion, Query);
            var response = await syncManager.SendRestRequest(request);
            var records = response.AsJArray;

            // Recording total size
            TotalSize = records.Count;

            return records;
        }

        public override Task<JArray> ContinueFetch(SyncManager syncManager)
        {
            return null;
        }

    }
}