/*
 * Copyright (c) 2014-present, salesforce.com, inc.
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
using System.Runtime.InteropServices.ComTypes;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Salesforce.SDK.Core;
using Salesforce.SDK.Hybrid.Auth;
using Salesforce.SDK.Rest;
using Salesforce.SDK.Settings;
using Salesforce.SDK.SmartSync.Model;
using Salesforce.SDK.SmartSync.Util;
using Newtonsoft.Json.Serialization;
using Salesforce.SDK.Hybrid.SmartSync.Models;
using SoqlSyncDownTarget = Salesforce.SDK.SmartSync.Model.SoqlSyncDownTarget;
using SyncOptions = Salesforce.SDK.SmartSync.Model.SyncOptions;
using SyncState = Salesforce.SDK.SmartSync.Model.SyncState;
using SyncUpTarget = Salesforce.SDK.SmartSync.Model.SyncUpTarget;

namespace Salesforce.SDK.Hybrid.SmartSync
{
    public sealed class SyncManager
    {
        private SDK.SmartSync.Manager.SyncManager _syncManager = SDK.SmartSync.Manager.SyncManager.GetInstance();

        private static IApplicationInformationService ApplicationInformationService
            => SDKServiceLocator.Get<IApplicationInformationService>();

        private const string _smartSync = "SmartSync";
        public static SyncManager GetInstance()
        {
            return GetInstance(null, null);
        }

        public static SyncManager GetInstance(Account account, string communityId)
        {
            return GetInstance(account, communityId, null);
        }

        public static SyncManager GetInstance(Account account, string communityId, SmartStore.SmartStore smartStore)
        {
            var nativeAccount = JsonConvert.SerializeObject(account);
            var nativeSmartStore = JsonConvert.SerializeObject(smartStore);
            var nativeSyncManager = SDK.SmartSync.Manager.SyncManager.GetInstance(
                JsonConvert.DeserializeObject<SDK.Auth.Account>(nativeAccount), communityId, JsonConvert.DeserializeObject<SDK.SmartStore.Store.SmartStore>(nativeSmartStore));

            var nativeJson = JsonConvert.SerializeObject(nativeSyncManager);
            return JsonConvert.DeserializeObject<SyncManager>(nativeJson);
        }

        public static void Reset()
        {
            SDK.SmartSync.Manager.SyncManager.Reset();
        }

        public Models.SyncState GetSyncStatus(long syncId)
        {
            var state = _syncManager.GetSyncStatus(syncId);
            return Models.SyncState.FromJson(state.AsJson().ToString());
        }

        public Models.SyncState SyncDown(SyncEvent syncEvent, string target, string soupName, Models.SyncOptions options)
        {
            var soqlsyncDownTarget = JObject.Parse(target);
            var soqlsyncDown = new SoqlSyncDownTarget(soqlsyncDownTarget);
            SyncDownTarget syncDown = soqlsyncDown;
            var syncOptions = options != null ? SyncOptions.FromJson(JObject.Parse(JsonConvert.SerializeObject(options))) : null;
            var state = _syncManager.SyncDown(syncDown, soupName, syncEvent.OnSyncEventRaised, syncOptions);
            return Models.SyncState.FromJson(state.AsJson().ToString());
        }

        public Models.SyncState SyncUp(SyncEvent syncEvent, Models.SyncUpTarget target, Models.SyncOptions options, string soupName)
        {

            var syncUp = JsonConvert.SerializeObject(target);
            var syncOptions = JsonConvert.SerializeObject(options, new JsonSerializerSettings
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver()
            });
            var state = _syncManager.SyncUp(JsonConvert.DeserializeObject<SyncUpTarget>(syncUp), SyncOptions.FromJson(JObject.Parse(syncOptions)), soupName, syncEvent.OnSyncEventRaised);
            return Models.SyncState.FromJson(state.AsJson().ToString());
        }

        public Models.SyncState ReSync(SyncEvent syncEvent, long syncId)
        {
            var state = _syncManager.ReSync(syncId, syncEvent.OnSyncEventRaised);
            return Models.SyncState.FromJson(state.AsJson().ToString());
        }

        public void RunSync(Models.SyncState sync, string callback)
        {
            var action = JsonConvert.DeserializeObject<Action<SyncState>>(callback);
            _syncManager.RunSync(JsonConvert.DeserializeObject<SyncState>(sync.AsJson()), action);
        }

        public static IList<string> Pluck(string jArray, string key)
        {
            var array = JsonConvert.DeserializeObject<JToken>(jArray);
            var list = SDK.SmartSync.Manager.SyncManager.Pluck<string>(array, key);
            return list;
        }

        public IAsyncOperation<Rest.RestResponse> SendRestRequest(Rest.RestRequest request)
        {
            return Task.Run(async () =>
            {
                var restRequest = JsonConvert.SerializeObject(request);
                var req = JsonConvert.DeserializeObject<RestRequest>(restRequest);
                var userAgent = await ApplicationInformationService.GenerateUserAgentHeaderAsync(true, _smartSync);
                req.AdditionalHeaders?.Add("User-Agent", userAgent);
                var response = await _syncManager.SendRestRequest(req);
                var restResponse = JsonConvert.SerializeObject(response);
                return JsonConvert.DeserializeObject<Rest.RestResponse>(restResponse);
            }).AsAsyncOperation();
        }
    }

    public sealed class SyncEvent
    {
        public event EventHandler<SyncEventArgs> SyncUpdateEvent;

        public void OnSyncEventRaised(object sync)
        {
            SyncUpdateEvent?.Invoke(this, new SyncEventArgs
            {
                Sync = Models.SyncState.FromJson(((SyncState)sync).AsJson().ToString())
            });
        }
    }

    public sealed class SyncEventArgs
    {
        public Models.SyncState Sync { get; set; }
    }
}
