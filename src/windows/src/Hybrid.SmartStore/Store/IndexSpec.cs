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
using System.Collections.ObjectModel;
using System.Runtime.InteropServices.WindowsRuntime;
using Newtonsoft.Json;
using Salesforce.SDK.Auth;
using Salesforce.SDK.SmartStore.Store;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Salesforce.SDK.SmartStore.Util;

namespace Salesforce.SDK.Hybrid.SmartStore
{
    public sealed class IndexSpec
    {
        private SDK.SmartStore.Store.IndexSpec _indexSpec;

        public IndexSpec()
        {
            throw new InvalidOperationException("IndexSpec without parameters is not supported");
        }

        public IndexSpec(String path, SmartStoreType type)
        {
            _indexSpec = new SDK.SmartStore.Store.IndexSpec(path,
                new SDK.SmartStore.Store.SmartStoreType(type.ColumnType));
        }

        public IndexSpec(String path, SmartStoreType type, String columnName)
        {
            _indexSpec = new SDK.SmartStore.Store.IndexSpec(path,
                new SDK.SmartStore.Store.SmartStoreType(type.ColumnType), columnName);
        }

        public static IDictionary<string, IndexSpec> MapForIndexSpecs([ReadOnlyArray()]IndexSpec[] indexSpecs)
        {
            return indexSpecs.ToDictionary(indexspecs => indexspecs._indexSpec.Path);
        }

        internal static SDK.SmartStore.Store.IndexSpec[] ConvertToSdkIndexSpecs(IndexSpec[] indexSpecs)
        {
            var specs = from n in indexSpecs select n._indexSpec;
            return specs.ToArray();
        }

        internal static IndexSpec[] ConvertToHybridIndexSpecs(SDK.SmartStore.Store.IndexSpec[] indexSpecs)
        {
            var specs = JsonConvert.SerializeObject(indexSpecs);
            return JsonConvert.DeserializeObject<IndexSpec[]>(specs);
        }

        public static IndexSpec[] JsonToIndexSpecCollection(string json)
        {
            var jarray = JArray.Parse(json);
            List<IndexSpec> specs = new List<IndexSpec>();
            foreach (JObject next in jarray)
            {
                var indexSpec = new IndexSpec(next.ExtractValue<string>("path"), new SmartStoreType(next.ExtractValue<string>("type")));
                specs.Add(indexSpec);
            }
            return specs.ToArray();
        }
    }
}
