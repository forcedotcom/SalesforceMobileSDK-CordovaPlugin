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
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Salesforce.SDK.SmartSync.Util;

namespace Salesforce.SDK.Hybrid.SmartSync.Models
{
    public sealed class SyncOptions
    {
        [JsonProperty]
        internal MergeModeOptions MergeMode;

        [JsonProperty]
        internal List<string> FieldList;
        
        
        public static SyncOptions FromJson(string options)
        {
            var jObject = JsonConvert.DeserializeObject<JObject>(options);
            var nativeSyncOptions = SDK.SmartSync.Model.SyncOptions.FromJson(jObject);
            var syncOptions = JsonConvert.SerializeObject(nativeSyncOptions);
            return JsonConvert.DeserializeObject<SyncOptions>(syncOptions);
        }

        public static SyncOptions OptionsForSyncUp(IList<string> fieldList, MergeModeOptions mergeMode)
        {
            var nativeMergeMode = JsonConvert.SerializeObject(mergeMode);
            var nativeSyncOptions = SDK.SmartSync.Model.SyncOptions.OptionsForSyncUp(fieldList.ToList(), JsonConvert.DeserializeObject<SDK.SmartSync.Model.SyncState.MergeModeOptions>(nativeMergeMode));
            var syncOptions = JsonConvert.SerializeObject(nativeSyncOptions);
            return JsonConvert.DeserializeObject<SyncOptions>(syncOptions);
        }

        public static SyncOptions OptionsForSyncDown(MergeModeOptions mergeMode)
        {
            var nativeMergeMode = JsonConvert.SerializeObject(mergeMode);
            var nativeSyncOptions = SDK.SmartSync.Model.SyncOptions.OptionsForSyncDown(JsonConvert.DeserializeObject<SDK.SmartSync.Model.SyncState.MergeModeOptions>(nativeMergeMode));
            var syncOptions = JsonConvert.SerializeObject(nativeSyncOptions);
            return JsonConvert.DeserializeObject<SyncOptions>(syncOptions);
        }
    }
}
