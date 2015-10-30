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
using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Salesforce.SDK.Rest;
using Salesforce.SDK.SmartSync.Manager;
using Salesforce.SDK.SmartSync.Util;

namespace Salesforce.SDK.SmartSync.Model
{
    /// <summary>
    ///     MruSyncTarget, sync target for handling MRU syncs.
    /// </summary>
    public class MruSyncDownTarget : SyncDownTarget
    {
        private List<string> FieldList { set; get; }
        private string ObjectType { set; get; }

        private MruSyncDownTarget(List<string> fieldList, string objectType)
        {
            QueryType = QueryTypes.Mru;
            FieldList = fieldList;
            ObjectType = objectType;
        }

        public MruSyncDownTarget(JObject target) : base(target)
        {
            FieldList = target.ExtractValue<JArray>(Constants.FieldList).ToObject<List<string>>();
            ObjectType = target.ExtractValue<string>(Constants.SObjectType);
        }

        /// <summary>
        /// </summary>
        /// <returns>json representation of target</returns>
        public override JObject AsJson()
        {
            var target = base.AsJson();
            if (FieldList != null) target[Constants.FieldList] = new JArray(FieldList);
            if (!String.IsNullOrWhiteSpace(ObjectType)) target[Constants.SObjectType] = ObjectType;
            return target;
        }

        /// <summary>
        ///     Build SyncTarget for mru target
        /// </summary>
        /// <param name="objectType"></param>
        /// <param name="fieldList"></param>
        /// <returns></returns>
        public static SyncDownTarget TargetForMruSyncDown(string objectType, List<string> fieldList)
        {
            return new MruSyncDownTarget(fieldList, objectType);
        }

        public async override Task<JArray> StartFetch(SyncManager syncManager, long maxTimeStamp)
        {
            var request = RestRequest.GetRequestForMetadata(syncManager.ApiVersion, ObjectType);
            var response = await syncManager.SendRestRequest(request);
            var recentItems = SyncManager.Pluck<string>(response.AsJObject.ExtractValue<JArray>(Constants.RecentItems),
                Constants.Id);
            // Building SOQL query to get requested at
            String soql = SOQLBuilder.GetInstanceWithFields(FieldList.ToArray()).From(ObjectType).Where("Id IN ('" + String.Join("', '", recentItems) + "')").Build();

            // Get recent items attributes from server
            request = RestRequest.GetRequestForQuery(syncManager.ApiVersion, soql);
            response = await syncManager.SendRestRequest(request);
            var responseJson = response.AsJObject;
            var records = responseJson.ExtractValue<JArray>(Constants.Records);

            // Recording total size
            TotalSize = records.Count;

            return records;
        }

        /// <summary>
        /// ContinueFetch is not implemented for MruSyncTarget.
        /// </summary>
        /// <param name="syncManager"></param>
        /// <returns></returns>
        public override Task<JArray> ContinueFetch(SyncManager syncManager)
        {
            return null;
        }
    }
}