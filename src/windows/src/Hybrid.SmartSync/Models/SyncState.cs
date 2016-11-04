/*
 * Copyright (c) 2015-present, salesforce.com, inc.
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
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Salesforce.SDK.Auth;
using Salesforce.SDK.SmartSync.Model;
using Salesforce.SDK.SmartSync.Util;

namespace Salesforce.SDK.Hybrid.SmartSync.Models
{
    public sealed class SyncState
    {
        private static SDK.SmartSync.Model.SyncState _syncState;

        public SyncState()
        {
            _syncState = new SDK.SmartSync.Model.SyncState();
        }

        public SyncState(string syncState)
        {
            _syncState = SDK.SmartSync.Model.SyncState.FromJson(JObject.Parse(syncState));
        }

        [JsonProperty]
        public long Id => _syncState.Id;

        [JsonProperty]
        public SyncTypes SyncType
        {
            get
            {
                var nativeSyncType = _syncState.Type;
                var hybridSyncType = JsonConvert.SerializeObject(nativeSyncType);
                return JsonConvert.DeserializeObject<SyncTypes>(hybridSyncType);
            }
        }
        [JsonProperty]
        public SyncUpTarget Target { private set; get; }

        [JsonProperty]
        public SyncOptions Options {
            get
            {
                var nativeSyncOptions = _syncState.Options;
                var hybridSyncOptions = JsonConvert.SerializeObject(nativeSyncOptions);
                return JsonConvert.DeserializeObject<SyncOptions>(hybridSyncOptions);
            }
        }
        [JsonProperty]
        public String SoupName => _syncState.SoupName;

        [JsonProperty]
        public SyncStatusTypes Status {
            get
            {
                var nativeStatus = _syncState.Status;
                var hybridStatus = JsonConvert.SerializeObject(nativeStatus);
                return JsonConvert.DeserializeObject<SyncStatusTypes>(hybridStatus);

            }
        }
        [JsonProperty]
        public int Progress => _syncState.Progress;

        [JsonProperty]
        public int TotalSize => _syncState.TotalSize;

        [JsonProperty]
        public long MaxTimeStamp => _syncState.MaxTimeStamp;

        [JsonProperty]
        public MergeModeOptions MergeMode
        {
            get
            {
                var mode = _syncState.MergeMode;
                MergeModeOptions mergeMode;
                Enum.TryParse(mode.ToString(), out mergeMode);
                return mergeMode;
            }
        }

        public static void SetupSyncsSoupIfNeeded(SmartStore.SmartStore store)
        {
            var nativeStore = JsonConvert.SerializeObject(store);
            SDK.SmartSync.Model.SyncState.SetupSyncsSoupIfNeeded(JsonConvert.DeserializeObject<SDK.SmartStore.Store.SmartStore>(nativeStore));
        }

        public static SyncState CreateSyncDown(SmartStore.SmartStore store, string target,
            string soupName, SyncOptions options)
        {
            var nativeStore = JsonConvert.SerializeObject(store);
            var jObject = JsonConvert.DeserializeObject<SDK.SmartSync.Model.SyncDownTarget>(target);
            var nativeOptions = JsonConvert.SerializeObject(options);
            var state = SDK.SmartSync.Model.SyncState.CreateSyncDown(
                JsonConvert.DeserializeObject<SDK.SmartStore.Store.SmartStore>(nativeStore), jObject, soupName, JsonConvert.DeserializeObject<SDK.SmartSync.Model.SyncOptions>(nativeOptions));
            var syncState = JsonConvert.SerializeObject(state);
            return JsonConvert.DeserializeObject<SyncState>(syncState);
        }

        public static SyncState CreateSyncUp(SmartStore.SmartStore store, SyncUpTarget target, SyncOptions options,
            string soupName)
        {
            var nativeStore = JsonConvert.SerializeObject(store);
            var syncUpTarget = JsonConvert.SerializeObject(target);
            var nativeOptions = JsonConvert.SerializeObject(options);
            var state =
                SDK.SmartSync.Model.SyncState.CreateSyncUp(
                    JsonConvert.DeserializeObject<SDK.SmartStore.Store.SmartStore>(nativeStore),
                    JsonConvert.DeserializeObject<SDK.SmartSync.Model.SyncUpTarget>(syncUpTarget),
                    JsonConvert.DeserializeObject<SDK.SmartSync.Model.SyncOptions>(nativeOptions), soupName);
            var syncState = JsonConvert.SerializeObject(state);
            return JsonConvert.DeserializeObject<SyncState>(syncState);
        }

        public static SyncState FromJson(string sync)
        {
            var state = SDK.SmartSync.Model.SyncState.FromJson(JObject.Parse(sync));
            var syncState = JsonConvert.SerializeObject(state.AsJson());
            return new SyncState(syncState);
        }

        public static SyncState ById(SmartStore.SmartStore store, long id)
        {
            var nativeStore = JsonConvert.SerializeObject(store);
            var state = SDK.SmartSync.Model.SyncState.ById(
                JsonConvert.DeserializeObject<SDK.SmartStore.Store.SmartStore>(nativeStore), id);
            var nativeState = JsonConvert.SerializeObject(state);
            return JsonConvert.DeserializeObject<SyncState>(nativeState);
        }

        public string AsJson()
        {
            return JsonConvert.SerializeObject(_syncState.AsJson());
        }

        public void Save(SmartStore.SmartStore store)
        {
            var nativeStore = JsonConvert.SerializeObject(store);
            _syncState.Save(JsonConvert.DeserializeObject<SDK.SmartStore.Store.SmartStore>(nativeStore));
        }
    }

    public enum MergeModeOptions
    {
        None,
        Overwrite,
        LeaveIfChanged
    }

    public enum SyncStatusTypes
    {
        New,
        Running,
        Done,
        Failed
    }

    public enum SyncTypes
    {
        SyncDown,
        SyncUp
    }
}
