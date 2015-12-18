/*
 * Copyright (c) 2014, salesforce.com, inc.
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
using Newtonsoft.Json.Linq;
using Salesforce.SDK.SmartSync.Util;

namespace Salesforce.SDK.SmartSync.Model
{
    public class SyncOptions
    {
        private SyncOptions(List<string> fieldList)
        {
            FieldList = fieldList;
        }

        private SyncOptions(List<string> fieldList, SyncState.MergeModeOptions mergeMode) : this(fieldList)
        {
            MergeMode = mergeMode;
        }

        public SyncState.MergeModeOptions MergeMode { private set; get; }

        public List<string> FieldList { private set; get; }

        public static SyncOptions FromJson(JObject options)
        {
            if (options == null)
                return null;
            var mergeModeStr = options.ExtractValue<string>(Constants.MergeMode);
            var mergeMode = String.IsNullOrWhiteSpace(mergeModeStr)
                ? SyncState.MergeModeOptions.None
                : (SyncState.MergeModeOptions) Enum.Parse(typeof (SyncState.MergeModeOptions), mergeModeStr);
            var array = options.ExtractValue<JArray>(Constants.FieldList);
            return array == null ? new SyncOptions(null, mergeMode) : new SyncOptions(array.ToObject<List<string>>(), mergeMode);
        }

        public static SyncOptions OptionsForSyncUp(List<string> fieldList, SyncState.MergeModeOptions mergeMode = SyncState.MergeModeOptions.Overwrite)
        {
            return new SyncOptions(fieldList, mergeMode);
        }

        public static SyncOptions OptionsForSyncDown(SyncState.MergeModeOptions mergeMode)
        {
            return new SyncOptions(null, mergeMode);
        }

        public JObject AsJson()
        {
            var options = new JObject {{Constants.FieldList, new JArray(FieldList)}};
            if (MergeMode != SyncState.MergeModeOptions.None) options.Add(Constants.MergeMode, MergeMode.ToString());
            return options;
        }
    }
}