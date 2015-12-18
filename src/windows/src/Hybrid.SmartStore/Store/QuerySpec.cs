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
using Newtonsoft.Json;
using Salesforce.SDK.SmartStore.Store;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Salesforce.SDK.Hybrid.SmartStore
{
    public enum SmartQueryType
    {
        Smart,
        Exact,
        Range,
        Like
    };

    public enum SqlOrder
    {
        ASC,
        DESC
    };
    public sealed class QuerySpec
    {
        [JsonProperty]
        internal SDK.SmartStore.Store.QuerySpec SdkQuerySpec;

        public string BeginKey
        {
            get { return SdkQuerySpec.BeginKey; }
        }

        public string CountSmartSql
        {
            get { return SdkQuerySpec.CountSmartSql; }
        }

        public string EndKey
        {
            get { return SdkQuerySpec.EndKey; }
        }

        public string LikeKey
        {
            get { return SdkQuerySpec.LikeKey; }
        }

        public string MatchKey
        {
            get { return SdkQuerySpec.MatchKey; }
        }

        public SqlOrder Order
        {
            get
            {
                var order = JsonConvert.SerializeObject(SdkQuerySpec.Order);
                return JsonConvert.DeserializeObject<SqlOrder>(order);
            }
        }

        public int PageSize
        {
            get { return SdkQuerySpec.PageSize; }
        }

        public string Path
        {
            get { return SdkQuerySpec.Path; }
        }

        public SmartQueryType QueryType
        {
            get
            {
                var queryType = JsonConvert.SerializeObject(SdkQuerySpec.QueryType);
                return JsonConvert.DeserializeObject<SmartQueryType>(queryType);
            }
        }

        public string SmartSql
        {
            get { return SdkQuerySpec.SmartSql; }
        }

        public string SoupName
        {
            get { return SdkQuerySpec.SoupName; }
        }

        public static QuerySpec BuildAllQuerySpec(string soupName, string path, SqlOrder order, int pageSize)
        {
            var sqlOrder = JsonConvert.SerializeObject(order);
            var nativeQuerySpec = SDK.SmartStore.Store.QuerySpec.BuildAllQuerySpec(soupName, path, JsonConvert.DeserializeObject<SDK.SmartStore.Store.QuerySpec.SqlOrder>(sqlOrder), pageSize);
            var querySpec = new QuerySpec {SdkQuerySpec = nativeQuerySpec};
            return querySpec;
        }

        public static QuerySpec BuildExactQuerySpec(string soupName, string path, string exactMatchKey, int pageSize)
        {
            var nativeQuerySpec = SDK.SmartStore.Store.QuerySpec.BuildExactQuerySpec(soupName, path, exactMatchKey,
                pageSize);
            var querySpec = new QuerySpec {SdkQuerySpec = nativeQuerySpec};
            return querySpec;
        }

        public static QuerySpec BuildRangeQuerySpec(string soupName, string path, string beginKey, string endKey,
            SqlOrder order, int pageSize)
        {
            var sqlOrder = JsonConvert.SerializeObject(order);
            var nativeQuerySpec = SDK.SmartStore.Store.QuerySpec.BuildRangeQuerySpec(soupName, path, beginKey, endKey, JsonConvert.DeserializeObject<SDK.SmartStore.Store.QuerySpec.SqlOrder>(sqlOrder), pageSize);
            var querySpec = new QuerySpec {SdkQuerySpec = nativeQuerySpec};
            return querySpec;
        }

        public static QuerySpec BuildLikeQuerySpec(string soupName, string path, string likeKey, SqlOrder order,
            int pageSize)
        {
            var sqlOrder = JsonConvert.SerializeObject(order);
            var nativeQuerySpec = SDK.SmartStore.Store.QuerySpec.BuildLikeQuerySpec(soupName, path, likeKey, JsonConvert.DeserializeObject<SDK.SmartStore.Store.QuerySpec.SqlOrder>(sqlOrder), pageSize);
            var querySpec = new QuerySpec {SdkQuerySpec = nativeQuerySpec};
            return querySpec;
        }

        public static QuerySpec BuildSmartQuerySpec(string smartSql, int pageSize)
        {
            var nativeQuerySpec = SDK.SmartStore.Store.QuerySpec.BuildSmartQuerySpec(smartSql, pageSize);
            var querySpec = new QuerySpec { SdkQuerySpec = nativeQuerySpec };
            return querySpec;
        }
    }
}
