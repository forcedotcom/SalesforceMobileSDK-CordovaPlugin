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
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Storage;
using Windows.Storage.Streams;
using Microsoft.CSharp.RuntimeBinder;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Salesforce.SDK.Hybrid.Auth;
using Salesforce.SDK.SmartStore.Store;
using Account = Salesforce.SDK.Hybrid.Auth.Account;
using ISmartStore = Salesforce.SDK.Hybrid.SmartStore.Store.ISmartStore;

namespace Salesforce.SDK.Hybrid.SmartStore
{
    public sealed class SmartStore : ISmartStore
    {
        private static Dictionary<int, StoreCursor> _cursors;

        private SDK.Auth.Account _account;

        private SDK.SmartStore.Store.SmartStore NativeSmartStore
        {
            get { return SDK.SmartStore.Store.SmartStore.GetSmartStore(_account); }
        }

        public SmartStore()
        {
            _account = SDK.Auth.AccountManager.GetAccount();
            _cursors = new Dictionary<int, StoreCursor>();
        }

        public SmartStore(Account account)
        {
            _account = JsonConvert.DeserializeObject<SDK.Auth.Account>(Account.ToJson(account));
            _cursors = new Dictionary<int, StoreCursor>();
        }

        public static SmartStore GetSmartStore()
        {
            return new SmartStore();
        }

        public static SmartStore GetSmartStore(Account account)
        {
            return new SmartStore(account);
        }

        public static SmartStore GetGlobalSmartStore()
        {
            // generate a "global" smartstore
            var store = SDK.SmartStore.Store.SmartStore.GetGlobalSmartStore();
            return new SmartStore(null);
        }

        public static string GenerateDatabasePath(Account account)
        {
            DBOpenHelper open = DBOpenHelper.GetOpenHelper(HybridAccountManager.GetAccount());
            return Path.Combine(ApplicationData.Current.LocalFolder.Path, open.DatabaseFile);
        }

        public static IAsyncOperation<bool> HasGlobalSmartStore()
        {
            return Task.Run(async () => await HasSmartStore(null)).AsAsyncOperation<bool>();
        }

        public static IAsyncOperation<bool> HasSmartStore(Account account)
        {
            var accountJson = JsonConvert.SerializeObject(account);
            var sdkAccount = JsonConvert.DeserializeObject<SDK.Auth.Account>(accountJson);
            return SDK.SmartStore.Store.SmartStore.HasSmartStore(sdkAccount).AsAsyncOperation();
        }

        public void ResetDatabase()
        {
            NativeSmartStore.ResetDatabase();
        }

        public void RegisterSoup(String soupName, [ReadOnlyArray()]IndexSpec[] indexSpecs)
        {
            NativeSmartStore.RegisterSoup(soupName, IndexSpec.ConvertToSdkIndexSpecs(indexSpecs));
        }

        public static void DeleteAllDatabases(bool includeGlobal)
        {
            SDK.SmartStore.Store.SmartStore.DeleteAllDatabases(true);
        }

        public void CreateMetaTables()
        {
            NativeSmartStore.CreateMetaTables();
        }


        public void ReIndexSoup(string soupName, [ReadOnlyArray()]string[] indexPaths, bool handleTx)
        {
            NativeSmartStore.ReIndexSoup(soupName, indexPaths, handleTx);
        }

        public IndexSpec[] GetSoupIndexSpecs(string soupName)
        {
            return IndexSpec.ConvertToHybridIndexSpecs(NativeSmartStore.GetSoupIndexSpecs(soupName));
        }

        public void ClearSoup(string soupName)
        {
            NativeSmartStore.ClearSoup(soupName);
        }

        public bool HasSoup(string soupName)
        {
            return NativeSmartStore.HasSoup(soupName);
        }

        public void DropSoup(string soupName)
        {
            NativeSmartStore.DropSoup(soupName);
        }

        public void DropAllSoups()
        {
            NativeSmartStore.DropAllSoups();
        }

        public void DropAllSoups(string databasePath)
        {
            SDK.SmartStore.Store.SmartStore.DropAllSoups(databasePath);
        }

        public IList<string> GetAllSoupNames()
        {
            return NativeSmartStore.GetAllSoupNames();
        }

        public string Query(QuerySpec querySpec, int pageIndex)
        {
            return JsonConvert.SerializeObject(NativeSmartStore.Query(querySpec.SdkQuerySpec, pageIndex));
        }

        public long CountQuery(QuerySpec querySpec)
        {
            return NativeSmartStore.CountQuery(querySpec.SdkQuerySpec);
        }

        public bool Delete(string soupName, [ReadOnlyArray()]long[] soupEntryIds, Boolean handleTx)
        {
            return NativeSmartStore.Delete(soupName, soupEntryIds, handleTx);
        }

        public string Create(string soupName, string soupElt)
        {
            return JsonConvert.SerializeObject(NativeSmartStore.Create(soupName, JObject.Parse(soupElt)));
        }

        public string Create(string soupName, string soupElt, bool handleTx)
        {
            return JsonConvert.SerializeObject(NativeSmartStore.Create(soupName, JObject.Parse(soupElt), handleTx));
        }

        public string Upsert(string soupName, string soupElt, string externalIdPath)
        {
            return JsonConvert.SerializeObject(NativeSmartStore.Upsert(soupName, JObject.Parse(soupElt), externalIdPath));
        }

        public string Upsert(string soupName, string soupElt)
        {
            return JsonConvert.SerializeObject(NativeSmartStore.Upsert(soupName, JObject.Parse(soupElt)));
        }

        public string Upsert(string soupName, string soupElt, string externalIdPath, bool handleTx)
        {
            return JsonConvert.SerializeObject(NativeSmartStore.Upsert(soupName, JObject.Parse(soupElt), externalIdPath, handleTx));
        }

        public bool BeginDatabaseTransaction()
        {
            return NativeSmartStore.Database.BeginTransaction();
        }

        public bool CommitDatabaseTransaction()
        {
            return NativeSmartStore.Database.CommitTransaction();
        }

        public long LookupSoupEntryId(string soupName, string fieldPath, string fieldValue)
        {
            return NativeSmartStore.LookupSoupEntryId(soupName, fieldPath, fieldValue);
        }

        public string Update(String soupName, string soupElt, long soupEntryId, bool handleTx)
        {
            return JsonConvert.SerializeObject(NativeSmartStore.Update(soupName, JObject.Parse(soupElt), soupEntryId, handleTx));
        }

        public string Retrieve(string soupName, params long[] soupEntryIds)
        {
            return JsonConvert.SerializeObject(NativeSmartStore.Retrieve(soupName, soupEntryIds));
        }

        public static object Project(object soup, string path)
        {
            return SDK.SmartStore.Store.SmartStore.Project((JObject)soup, path);
        }

        public string ConvertSmartSql(string smartSql)
        {
            return NativeSmartStore.ConvertSmartSql(smartSql);
        }

        public static string GetSoupTableName(long soupId)
        {
            return SDK.SmartStore.Store.SmartStore.GetSoupTableName(soupId);
        }

        /// <summary>
        /// Native implementation of pgCloseCursor
        /// </summary>
        /// <param name="id"></param>
        public void CloseCursor(int id)
        {
            if (!_cursors.ContainsKey(id))
            {
                throw new SmartStoreException("cursor id " + id + " does not exist");
            }

            _cursors.Remove(id);

        }

        /// <summary>
        /// Native implementation of pgMoveCursorToPageIndex
        /// </summary>
        public string MoveCursorToPageIndex(int id, int index)
        {
            if (!_cursors.ContainsKey(id))
            {
                throw new SmartStoreException("cursor id " + id + " does not exist");
            }

            var cursor = _cursors[id];
            cursor.MoveToPageIndex(index);

            return cursor.GetCursorData(this);
        }

        /// <summary>
        /// Native implementation of pgQuerySoup
        /// </summary>
        /// <returns></returns>
        public string QuerySoup(QuerySpec querySpec)
        {
            if (querySpec.QueryType == SmartQueryType.Smart)
            {
                throw new RuntimeBinderException("Smart queries can only be run through runSmartQuery");
            }

            return RunQuery(querySpec);
        }

        /// <summary>
        /// Native implementation of pgRunSmartQuery
        /// </summary>
        /// <returns></returns>
        public string RunSmartQuery(QuerySpec querySpec)
        {
            if (querySpec.QueryType != SmartQueryType.Smart)
            {
                throw new RuntimeBinderException("RunSmartQuery can only run smart queries");
            }

            return RunQuery(querySpec);
        }

        /// <summary>
        /// Helper function for query functions
        /// </summary>
        /// <param name="querySpec"></param>
        /// <returns></returns>
        private string RunQuery(QuerySpec querySpec)
        {
            var cursor = new StoreCursor(this, querySpec);

            _cursors.Add(cursor.CursorId, cursor);

            var data = cursor.GetCursorData(this);
            return data;
        }
    }
}
