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
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Salesforce.SDK.Auth;

namespace Salesforce.SDK.Hybrid.SmartSync.Models
{
    public sealed class SyncUpTarget
    {
         private SDK.SmartSync.Model.SyncUpTarget _syncUpTarget;

        public SyncUpTarget()
        {
            _syncUpTarget = new SDK.SmartSync.Model.SyncUpTarget();
        }

        public SyncUpTarget(string target)
        {
            _syncUpTarget = new SDK.SmartSync.Model.SyncUpTarget(JsonConvert.DeserializeObject<JObject>(target));
        }

        public static SyncUpTarget FromJSON(string target)
        {
            var nativeSyncUpTarget = SDK.SmartSync.Model.SyncUpTarget.FromJSON(JsonConvert.DeserializeObject<JObject>(target));
            var syncUpTarget = JsonConvert.SerializeObject(nativeSyncUpTarget);
            return JsonConvert.DeserializeObject<SyncUpTarget>(syncUpTarget);
        }

        public IAsyncOperation<String> CreateOnServerAsync(SyncManager syncManager, String objectType,
            IDictionary<String, Object> fields)
        {
            var manager = JsonConvert.SerializeObject(syncManager);
            var fieldList = new Dictionary<String, Object>(fields);
            return Task.Run(async() => await _syncUpTarget.CreateOnServerAsync(JsonConvert.DeserializeObject<SDK.SmartSync.Manager.SyncManager>(manager), objectType, fieldList)).AsAsyncOperation();
        }

        public IAsyncOperation<bool> DeleteOnServer(SyncManager syncManager, String objectType, String objectId)
        {
            var manager = JsonConvert.SerializeObject(syncManager);
            return
                Task.Run(
                    async () =>
                        await
                            _syncUpTarget.DeleteOnServer(
                                JsonConvert.DeserializeObject<SDK.SmartSync.Manager.SyncManager>(manager), objectType,
                                objectId)).AsAsyncOperation();
        }

        public IAsyncOperation<bool> UpdateOnServer(SyncManager syncManager, String objectType, String objectId,
            IDictionary<String, Object> fields)
        {
            var manager = JsonConvert.SerializeObject(syncManager);
            var fieldList = new Dictionary<String, Object>(fields);
            return
                Task.Run(
                    async () =>
                        await
                            _syncUpTarget.UpdateOnServer(
                                JsonConvert.DeserializeObject<SDK.SmartSync.Manager.SyncManager>(manager), objectType,
                                objectId, fieldList)).AsAsyncOperation();
        }

        public IAsyncOperation<String> FetchLastModifiedDate(SyncManager syncManager, String objectType, String objectId)
        {
            var manager = JsonConvert.SerializeObject(syncManager);
            return
                Task.Run(
                    async () =>
                        await
                            _syncUpTarget.FetchLastModifiedDate(
                                JsonConvert.DeserializeObject<SDK.SmartSync.Manager.SyncManager>(manager), objectType,
                                objectId)).AsAsyncOperation();
        }

        public object GetIdsOfRecordsToSyncUp(SyncManager syncManager, String soupName)
        {
            var manager = JsonConvert.SerializeObject(syncManager);
            return _syncUpTarget.GetIdsOfRecordsToSyncUp(
                JsonConvert.DeserializeObject<SDK.SmartSync.Manager.SyncManager>(manager), soupName);
        }
    }
}
