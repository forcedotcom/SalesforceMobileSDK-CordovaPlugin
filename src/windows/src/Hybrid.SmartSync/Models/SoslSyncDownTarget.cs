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

namespace Salesforce.SDK.Hybrid.SmartSync.Models
{
    public sealed class SoslSyncDownTarget
    {
        private SDK.SmartSync.Model.SoslSyncDownTarget _soslSyncDownTarget;

        public SoslSyncDownTarget(string query)
        {
            _soslSyncDownTarget = new SDK.SmartSync.Model.SoslSyncDownTarget(query);
        }

        public string AsJson()
        {
            return JsonConvert.SerializeObject(_soslSyncDownTarget.AsJson());
        }

        public string StartFetch(SyncManager syncManager, long maxTimeStamp)
        {
            var manager = JsonConvert.SerializeObject(syncManager);
            var target = Task.Run(async () => await _soslSyncDownTarget.StartFetch(JsonConvert.DeserializeObject<SDK.SmartSync.Manager.SyncManager>(manager),
                maxTimeStamp)).AsAsyncOperation();
            var array = target.GetResults();
            return array.ToString();
        }

        public string ContinueFetch(SyncManager syncManager)
        {
            var manager = JsonConvert.SerializeObject(syncManager);
            var result = _soslSyncDownTarget.ContinueFetch(JsonConvert.DeserializeObject<SDK.SmartSync.Manager.SyncManager>(manager));
            return result.Result.ToString();
        }

    }
}
