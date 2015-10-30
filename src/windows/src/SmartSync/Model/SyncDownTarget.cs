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
using System.Diagnostics;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Salesforce.SDK.SmartStore.Store;
using Salesforce.SDK.SmartSync.Manager;
using Salesforce.SDK.SmartSync.Util;

namespace Salesforce.SDK.SmartSync.Model
{
    public abstract class SyncDownTarget : SyncTarget
    {
        public enum QueryTypes
        {
            Mru,
            Sosl,
            Soql,
            Custom
        }
        public new const string WindowsImpl = "windowsImpl";
        public new const string WindowsImplType = "windowsImplType";
        public const string Type = "type";
        public const string QueryString = "query";
        public new const string ModificationDateFieldName = "modificationDateFieldName";
        public const int Unchanged = -1;

        public QueryTypes QueryType { protected set; get; }

        public string Query { protected set; get; }
        
        public int TotalSize { protected set; get; } // set during fetch

        /// <summary>
        ///     Build SyncTarget from json
        /// </summary>
        /// <param name="target"></param>
        /// <returns></returns>
        public static SyncDownTarget FromJson(JObject target)
        {
            if (target == null) return null;
            JToken impl;
            if (target.TryGetValue(WindowsImpl, out impl))
            {
                JToken implType;
                if (target.TryGetValue(WindowsImplType, out implType))
                {
                    try
                    {
                        Assembly assembly = Assembly.Load(new AssemblyName(impl.ToObject<string>()));
                        Type type = assembly.GetType(implType.ToObject<string>());
                        if (type.GetTypeInfo().IsSubclassOf(typeof(SyncTarget)))
                        {
                            MethodInfo method = type.GetTypeInfo().GetDeclaredMethod("FromJson");
                            return (SyncDownTarget)method.Invoke(type, new object[] { target });
                        }
                    }
                    catch (Exception)
                    {
                        throw new SmartStoreException("Invalid SyncDownTarget");
                    }
                }
            }
            throw new SmartStoreException("Could not generate SyncDownTarget from json target");
        }

        public static string AddFilterForReSync(string query, long maxTimeStamp)
        {
            if (maxTimeStamp != Unchanged)
            {
                string extraPredicate = Constants.LastModifiedDate + " > " +
                                        new DateTime(maxTimeStamp, DateTimeKind.Utc).ToString("o");
                if (query.Contains(" where "))
                {
                    var reg = new Regex("( where )");
                    query = reg.Replace(query, "$1 where " + extraPredicate + " and ", 1);
                }
                else
                {
                    string pred = "$1 where " + extraPredicate;
                    var reg = new Regex("( from[ ]+[^ ]*)");
                    query = reg.Replace(query, pred, 1);
                }
            }
            return query;
        }

        public override JObject AsJson()
        {
            var target = base.AsJson();
            target[Type] = QueryType.ToString();
            target[QueryString] = Query;
            return target;
        }

        protected SyncDownTarget(JObject target)
            : base(target)
        {
            QueryType = (QueryTypes)Enum.Parse(typeof(QueryTypes), target.ExtractValue<string>(Constants.QueryType));
        }

        protected SyncDownTarget()
        {

        }

        protected SyncDownTarget(string query)
        {
            Query = query;
        }

        public abstract Task<JArray> StartFetch(SyncManager syncManager, long maxTimeStamp);

        public abstract Task<JArray> ContinueFetch(SyncManager syncManager);

        /// <summary>
        /// Method to return number of records to be fetched
        /// </summary>
        /// <returns>Number of records expected to be fetched - is set when startFetch() is called</returns>
        public int GetTotalSize()
        {
            return TotalSize;
        }


        public long GetMaxTimeStamp(JArray jArray)
        {
            long maxTimeStamp = Unchanged;
            foreach (JToken t in jArray)
            {
                var jObj = t.ToObject<JObject>();
                if (jObj != null)
                {
                    var date = jObj.ExtractValue<string>(GetModificationDate());
                    if (String.IsNullOrWhiteSpace(date))
                    {
                        maxTimeStamp = Unchanged;
                        break;
                    }
                    try
                    {
                        long timeStamp = Convert.ToDateTime(date).Ticks;
                        maxTimeStamp = Math.Max(timeStamp, maxTimeStamp);
                    }
                    catch (Exception)
                    {
                        Debug.WriteLine("SmartSync.GetMaxTimeStamp could not parse LastModifiedDate");
                        maxTimeStamp = Unchanged;
                        break;
                    }
                }
            }
            return maxTimeStamp;
        }
    }
}
