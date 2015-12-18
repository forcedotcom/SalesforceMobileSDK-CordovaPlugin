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
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestPlatform.UnitTestFramework;
using Newtonsoft.Json.Linq;
using Salesforce.SDK.Auth;
using Salesforce.SDK.Rest;
using Salesforce.SDK.SmartSync.Manager;
using Salesforce.SDK.SmartSync.Model;
using Salesforce.SDK.SmartSync.Util;
using Salesforce.SDK.Core;
using Salesforce.SDK.Logging;
using Salesforce.SDK.Security;
using Salesforce.SDK.App;

namespace Salesforce.SDK.SmartStore.Store
{
    [TestClass]
    public class SmartSyncTests
    {
        private const string TestAuthToken = "test_auth_token";
        private const string TestCallbackUrl = "test://callback";
        private const string TypeStr = "type";
        private const string RecordsStr = "records";
        private const string LidStr = "id";
        private const string LocalIdPrefixStr = "local_";
        private const string AccountsSoup = "accounts";
        private const int CountTestAccounts = 10;
        private static SyncManager _syncManager;
        private static IRestClient _restClient;
        private static SmartStore _smartStore;
        private static readonly string[] TestScopes = { "web" };
        private static Dictionary<string, string> _idToNames;
        private static readonly Random Rand = new Random();
        private static int _syncCheck;
        private static int _numberOfChanges;

        [ClassInitialize]
        public static async Task TestSetup(TestContext context)
        {
            SFApplicationHelper.RegisterServices();
            SDKServiceLocator.RegisterService<ILoggingService, Hybrid.Logging.Logger>();
            var settings = new EncryptionSettings(new HmacSHA256KeyGenerator());
            Encryptor.init(settings);
            var options = new LoginOptions(TestCredentials.LoginUrl, TestCredentials.ClientId, TestCallbackUrl, "mobile",
                TestScopes);
            var response = new AuthResponse
            {
                RefreshToken = TestCredentials.RefreshToken,
                AccessToken = TestAuthToken,
                InstanceUrl = TestCredentials.InstanceUrl,
                IdentityUrl = TestCredentials.IdentityUrl,
                Scopes = TestScopes,
            };
            Account account = await AccountManager.CreateNewAccount(options, response);
            account.UserId = TestCredentials.UserId;
            account.UserName = TestCredentials.Username;
            await OAuth2.RefreshAuthTokenAsync(account);
            _smartStore = SmartStore.GetSmartStore();
            _smartStore.ResetDatabase();
            _syncManager = SyncManager.GetInstance();
            _restClient = new RestClient(account.InstanceUrl, account.AccessToken,
                async () =>
                {
                    account = AccountManager.GetAccount();
                    account = await OAuth2.RefreshAuthTokenAsync(account);
                    return account.AccessToken;
                }
                );
            CreateAccountsSoup();
            _idToNames = await CreateTestAccountsOnServer(CountTestAccounts);
        }

        [ClassCleanup]
        public static async Task Cleanup()
        {
            await DeleteTestAccountsOnServer(_idToNames);
            DropAccountsSoup();
        }

        [TestMethod]
        public void TestGetSyncStatusForInvalidSyncId()
        {
            SyncState sync = _syncManager.GetSyncStatus(-1);
            Assert.IsNull(sync, "Sync status should be null");
        }

        [TestMethod]
        public void TestSyncDown()
        {
            TrySyncDown(SyncState.MergeModeOptions.Overwrite);
            CheckDb(_idToNames);
        }

        [TestMethod]
        public void TestSyncDownWithoutOverwrite()
        {
            // first sync down
            TrySyncDown(SyncState.MergeModeOptions.Overwrite);

            // make local changes
            Dictionary<string, string> idToNamesLocallyUpdated = MakeSomeLocalChanges();

            // sync down again with MergeModeOptions.LeaveIfChanged
            TrySyncDown(SyncState.MergeModeOptions.LeaveIfChanged);

            // check db, verify overwrite did not occur
            var idToNamesExpected = new Dictionary<string, string>(_idToNames);
            foreach (string next in idToNamesLocallyUpdated.Keys)
            {
                idToNamesExpected[next] = idToNamesLocallyUpdated[next];
            }
            CheckDb(idToNamesExpected);

            // sync down again with MergeModeOptions.Overwrite
            TrySyncDown(SyncState.MergeModeOptions.Overwrite);

            // check db
            CheckDb(_idToNames);
        }

        [TestMethod]
        public async Task TestReSync()
        {
            long syncId = TrySyncDown(SyncState.MergeModeOptions.Overwrite);

            // Check sync time stamp
            SyncState sync = _syncManager.GetSyncStatus(syncId);
            long maxTimeStamp = sync.MaxTimeStamp;
            Assert.IsTrue(maxTimeStamp > 0, "Wrong time stamp");

            string[] allIds = _idToNames.Keys.ToArray();
            var ids = new[] { allIds[0], allIds[2] };
            Dictionary<string, string> idToNamesLocallyUpdated = ids.ToDictionary(id => id,
                id => _idToNames[id] + "_updated");
            await UpdateAccountsOnServer(idToNamesLocallyUpdated);

            // resync
            _syncCheck = 0;
            DateTime end = DateTime.Now.AddSeconds(2000);
            _syncManager.ReSync(syncId, HandleResyncCheck);
            while (_syncCheck < 3)
            {
                if (DateTime.Now > end)
                    Assert.Fail("Sync timed out");
            }

            // check db
            Dictionary<string, string> updatedAllIds = _idToNames;
            foreach (string next in idToNamesLocallyUpdated.Keys)
            {
                updatedAllIds[next] = idToNamesLocallyUpdated[next];
            }
            CheckDb(updatedAllIds);

            Assert.IsTrue(_syncManager.GetSyncStatus(syncId).MaxTimeStamp > maxTimeStamp, "Wrong time stamp");
        }

        [TestMethod]
        public async Task TestSyncUpWithLocallyUpdatedRecords()
        {
            // first sync down
            TrySyncDown(SyncState.MergeModeOptions.Overwrite);

            // make local changes
            Dictionary<string, string> idToNamesLocallyUpdated = MakeSomeLocalChanges();

            // sync up
            TrySyncUp(3);

            // check that the db doesn't show entries as locally modified anymore
            Dictionary<string, string>.KeyCollection ids = idToNamesLocallyUpdated.Keys;
            string idsClause = "('" + String.Join("', '", ids) + "')";
            QuerySpec smartStoreQuery =
                QuerySpec.BuildSmartQuerySpec(
                    "SELECT {accounts:_soup} FROM {accounts} WHERE {accounts:Id} IN " + idsClause, ids.Count);
            JArray accountsFromDb = _smartStore.Query(smartStoreQuery, 0);
            foreach (JArray row in accountsFromDb.Select(row => row.ToObject<JArray>()))
            {
                var soupElt = row[0].ToObject<JObject>();
                Assert.AreEqual(false, soupElt.ExtractValue<bool>(SyncManager.Local), "Wrong local flag");
                Assert.AreEqual(false, soupElt.ExtractValue<bool>(SyncManager.LocallyUpdated), "Wrong local flag");
            }
            string soql = "SELECT Id, Name FROM Account WHERE Id IN " + idsClause;
            var request = RestRequest.GetRequestForQuery(ApiVersionStrings.VersionNumber, soql);
            var response = await _restClient.SendAsync(request);
            var records = response.AsJObject.ExtractValue<JArray>(RecordsStr);
            var idToNamesFromServer = new JObject();
            foreach (JToken rowToken in records)
            {
                var row = rowToken.ToObject<JObject>();
                idToNamesFromServer[row.ExtractValue<string>(Constants.Id)] = row.ExtractValue<string>(Constants.Name);
            }
            Assert.IsTrue(JToken.DeepEquals(JObject.FromObject(idToNamesLocallyUpdated), idToNamesFromServer));
        }

        [TestMethod]
        public async Task TestSyncUpWithLocallyUpdatedRecordsWithoutOverwrite()
        {
            TrySyncDown(SyncState.MergeModeOptions.LeaveIfChanged);

            Dictionary<string, string> idToNamesLocallyUpdated = MakeSomeLocalChanges();

            var idToNamesRemotelyUpdated = new Dictionary<string, string>();
            foreach (string id in idToNamesLocallyUpdated.Keys)
            {
                idToNamesRemotelyUpdated[id] = idToNamesLocallyUpdated[id] + "_updated_again";
            }
            await UpdateAccountsOnServer(idToNamesRemotelyUpdated);

            // sync up
            TrySyncUp(3, SyncState.MergeModeOptions.LeaveIfChanged);

            String idsClause = "('" + String.Join("', '", idToNamesRemotelyUpdated.Keys) + "')";
            QuerySpec smartStoreQuery =
                QuerySpec.BuildSmartQuerySpec(
                    "SELECT {accounts:_soup} FROM {accounts} WHERE {accounts:Id} IN " + idsClause,
                    idToNamesRemotelyUpdated.Keys.Count);
            JArray accountsFromDb = _smartStore.Query(smartStoreQuery, 0);
            foreach (JToken token in accountsFromDb)
            {
                var row = token.ToObject<JArray>();
                var soupElt = row[0].ToObject<JObject>();
                Assert.IsTrue(soupElt.ExtractValue<bool>(SyncManager.Local), "Wrong local flag");
                Assert.IsTrue(soupElt.ExtractValue<bool>(SyncManager.LocallyUpdated), "Wrong local flag");
            }

            // check server
            String soql = "SELECT Id, Name FROM Account WHERE Id IN " + idsClause;
            RestRequest request = RestRequest.GetRequestForQuery(ApiVersionStrings.VersionNumber, soql);
            var idToNamesFromServer = new JObject();
            var response = await _restClient.SendAsync(request);
            var records = response.AsJObject.ExtractValue<JArray>(RecordsStr);
            foreach (JToken token in records)
            {
                var row = token.ToObject<JObject>();
                idToNamesFromServer[row.ExtractValue<string>(Constants.Id)] = row.ExtractValue<string>(Constants.Name);
            }
            Assert.IsTrue(JToken.DeepEquals(JObject.FromObject(idToNamesRemotelyUpdated), idToNamesFromServer));
        }

        [TestMethod]
        public async Task TestSyncUpWithLocallyDeletedRecordsWithoutOverwrite()
        {
            TrySyncDown(SyncState.MergeModeOptions.LeaveIfChanged);
            string[] allIds = _idToNames.Keys.ToArray();
            string[] idsLocallyDeleted = { allIds[0], allIds[1], allIds[2] };
            DeleteAccountsLocally(idsLocallyDeleted);

            var idToNamesRemotelyDeleted = new Dictionary<string, string>();
            foreach (string id in idsLocallyDeleted)
            {
                idToNamesRemotelyDeleted[id] = _idToNames[id] + "_updated";
            }
            await UpdateAccountsOnServer(idToNamesRemotelyDeleted);

            TrySyncUp(3, SyncState.MergeModeOptions.LeaveIfChanged);
            String idsClause = "('" + String.Join("', '", idsLocallyDeleted) + "')";
            QuerySpec smartStoreQuery =
                QuerySpec.BuildSmartQuerySpec(
                    "SELECT {accounts:_soup}, {accounts:Name} FROM {accounts} WHERE {accounts:Id} IN " + idsClause,
                    idsLocallyDeleted.Count());
            JArray accountsFromDb = _smartStore.Query(smartStoreQuery, 0);
            Assert.IsTrue(accountsFromDb.Count == 3, "3 accounts should have been returned by smartstore");

            // check server
            String soql = "SELECT Id, Name FROM Account WHERE Id IN " + idsClause;
            RestRequest request = RestRequest.GetRequestForQuery(ApiVersionStrings.VersionNumber, soql);
            var response = await _restClient.SendAsync(request);
            var records = response.AsJObject.ExtractValue<JArray>(RecordsStr);
            Assert.AreEqual(records.Count, 3, "3 accounts should have been returned from server");
        }

        private long TrySyncUp(int numberOfChanges,
            SyncState.MergeModeOptions mergeMode = SyncState.MergeModeOptions.Overwrite)
        {
            // Create sync
            SyncOptions options = SyncOptions.OptionsForSyncUp(new List<string>(new[] { Constants.Name }), mergeMode);
            var target = new SyncUpTarget();
            SyncState sync = SyncState.CreateSyncUp(_smartStore, target, options, AccountsSoup);
            long syncId = sync.Id;
            CheckStatus(sync, SyncState.SyncTypes.SyncUp, syncId, SyncState.SyncStatusTypes.New, 0, -1);

            // run sync

            _syncCheck = 0;
            _numberOfChanges = numberOfChanges;
            DateTime end = DateTime.Now.AddSeconds(60);
            _syncManager.RunSync(sync, HandleSyncUpCheck);
            while (_syncCheck < 1)
            {
                if (DateTime.Now > end)
                    Assert.Fail("Sync timed out");
            }
            return syncId;
        }

        private long TrySyncDown(SyncState.MergeModeOptions mergeMode)
        {
            // Ids clause
            String idsClause = "('" + String.Join("', '", _idToNames.Keys) + "')";

            // Create sync
            SyncDownTarget target =
               new SoqlSyncDownTarget("SELECT Id, Name, " + Constants.LastModifiedDate + " FROM Account WHERE Id IN " +
                                                 idsClause);
            SyncOptions options = SyncOptions.OptionsForSyncDown(mergeMode);
            SyncState sync = SyncState.CreateSyncDown(_smartStore, target, AccountsSoup, options);
            long syncId = sync.Id;
            CheckStatus(sync, SyncState.SyncTypes.SyncDown, syncId, SyncState.SyncStatusTypes.New, 0, -1);
            // Run sync
            _syncCheck = 0;
            _syncManager.RunSync(sync, HandleSyncDownCheck);
            DateTime end = DateTime.Now.AddSeconds(60);
            while (_syncCheck < 3)
            {
                if (DateTime.Now > end)
                    Assert.Fail("Sync timed out");
            }
            return syncId;
        }

        private void HandleSyncUpCheck(SyncState sync)
        {
            if (sync.Progress == 100 || sync.Status == SyncState.SyncStatusTypes.Failed)
                _syncCheck = 1;
            switch (_syncCheck)
            {
                case 1:
                    CheckStatus(sync, SyncState.SyncTypes.SyncUp, sync.Id, SyncState.SyncStatusTypes.Done, 100,
                        _numberOfChanges);
                    break;
            }
        }

        private void HandleResyncCheck(SyncState sync)
        {
            if (sync.Status == SyncState.SyncStatusTypes.Done)
                _syncCheck = 3;
            switch (_syncCheck)
            {
                default:
                    CheckStatus(sync, SyncState.SyncTypes.SyncDown, sync.Id, SyncState.SyncStatusTypes.Running, 0,
                        _idToNames.Count);
                    break;
                case 3:
                    CheckStatus(sync, SyncState.SyncTypes.SyncDown, sync.Id, SyncState.SyncStatusTypes.Done, 100,
                        _idToNames.Count);
                    break;
            }
        }

        private void HandleSyncDownCheck(SyncState sync)
        {
            _syncCheck++;
            switch (_syncCheck)
            {
                case 1:
                    CheckStatus(sync, SyncState.SyncTypes.SyncDown, sync.Id, SyncState.SyncStatusTypes.Running, 0, -1);
                    // we get an update right away before getting records to sync
                    break;
                case 2:
                    CheckStatus(sync, SyncState.SyncTypes.SyncDown, sync.Id, SyncState.SyncStatusTypes.Running, 0,
                        _idToNames.Count);
                    break;
                default:
                    CheckStatus(sync, SyncState.SyncTypes.SyncDown, sync.Id, SyncState.SyncStatusTypes.Done, 100,
                        _idToNames.Count);
                    break;
            }
        }

        private static void CheckStatus(SyncState sync, SyncState.SyncTypes expectedType, long expectedId,
            SyncState.SyncStatusTypes expectedStatus,
            int expectedProgress, int expectedSize)
        {
            Assert.AreEqual(expectedType, sync.SyncType, "Wrong type");
            Assert.AreEqual(expectedId, sync.Id, "Wrong id");
            Assert.AreEqual(expectedStatus, sync.Status, "Wrong status");
            Assert.AreEqual(expectedProgress, sync.Progress, "Wrong progress");
            Assert.AreEqual(expectedSize, sync.TotalSize, "Wrong total size");
        }

        private static void CreateAccountsSoup()
        {
            IndexSpec[] indexSpecs =
            {
                new IndexSpec(Constants.Id, SmartStoreType.SmartString),
                new IndexSpec(Constants.Name, SmartStoreType.SmartString),
                new IndexSpec(SyncManager.Local, SmartStoreType.SmartString)
            };
            _smartStore.RegisterSoup(AccountsSoup, indexSpecs);
        }

        private static void DropAccountsSoup()
        {
            _smartStore.DropSoup(AccountsSoup);
        }

        private static void DeleteSyncs()
        {
            _smartStore.ClearSoup(Constants.SyncsSoup);
        }

        private static string CreateLocalId()
        {
            string id = LocalIdPrefixStr + (Rand.Next() * 10000000).ToString("D8");
            return id;
        }

        private static string CreateAccountName()
        {
            string name = "SyncManagerTest" + (Rand.Next() * 10000000).ToString("D8");
            return name;
        }

        private void CreateAccountsLocally(string[] names)
        {
            var attributes = new JObject { { TypeStr, Constants.Account } };
            foreach (string name in names)
            {
                var account = new JObject
                {
                    {Constants.Id, CreateLocalId()},
                    {Constants.Name, name},
                    {Constants.Attributes, attributes},
                    {SyncManager.Local, true},
                    {SyncManager.LocallyCreated, true},
                    {SyncManager.LocallyDeleted, false},
                    {SyncManager.LocallyUpdated, false}
                };
                _smartStore.Create(AccountsSoup, account);
            }
        }

        private void UpdateAccountsLocally(Dictionary<String, String> idToNamesLocallyUpdated)
        {
            foreach (string id in idToNamesLocallyUpdated.Keys)
            {
                string updatedName = idToNamesLocallyUpdated[id];
                var account =
                    _smartStore.Retrieve(AccountsSoup, _smartStore.LookupSoupEntryId(AccountsSoup, Constants.Id, id))[0]
                        .ToObject<JObject>();
                account[Constants.Name] = updatedName;
                account[SyncManager.Local] = true;
                account[SyncManager.LocallyCreated] = false;
                account[SyncManager.LocallyDeleted] = false;
                account[SyncManager.LocallyUpdated] = true;
                _smartStore.Upsert(AccountsSoup, account);
            }
        }

        private void DeleteAccountsLocally(IEnumerable<string> idsLocallyDeleted)
        {
            foreach (string id in idsLocallyDeleted)
            {
                var account =
                    _smartStore.Retrieve(AccountsSoup, _smartStore.LookupSoupEntryId(AccountsSoup, Constants.Id, id))[0]
                        .ToObject<JObject>();
                account[SyncManager.Local] = true;
                account[SyncManager.LocallyCreated] = false;
                account[SyncManager.LocallyDeleted] = true;
                account[SyncManager.LocallyUpdated] = false;
                _smartStore.Upsert(AccountsSoup, account);
            }
        }

        private void CheckDb(Dictionary<string, string> expectedIdToNames)
        {
            string idsClause = "('" + String.Join("', '", expectedIdToNames.Keys) + "')";
            QuerySpec smartStoreQuery =
                QuerySpec.BuildSmartQuerySpec(
                    "SELECT {accounts:Id}, {accounts:Name} FROM {accounts} WHERE {accounts:Id} IN " + idsClause,
                    CountTestAccounts);
            JArray accountsFromDb = _smartStore.Query(smartStoreQuery, 0);
            var idToNamesFromDb = new JObject();
            foreach (JToken t in accountsFromDb)
            {
                var row = t.ToObject<JArray>();
                idToNamesFromDb[row[0].ToObject<string>()] = row[1].ToObject<string>();
            }
            Assert.IsTrue(JToken.DeepEquals(JObject.FromObject(expectedIdToNames), idToNamesFromDb));
        }

        private static async Task<Dictionary<string, string>> CreateTestAccountsOnServer(int count)
        {
            var toCreate = new Dictionary<string, string>();
            for (int i = 0; i < count; i++)
            {
                string name = CreateAccountName();
                var fields = new Dictionary<string, object>();
                fields.Add(Constants.Name, name);
                RestRequest request = RestRequest.GetRequestForCreate(ApiVersionStrings.VersionNumber, Constants.Account,
                    fields);
                var response = await _restClient.SendAsync(request);
                Assert.IsTrue(response.Success, "Create failed");
                var id = response.AsJObject.ExtractValue<string>(LidStr);
                toCreate[id] = name;
            }
            return toCreate;
        }

        private static async Task<bool> DeleteTestAccountsOnServer(Dictionary<string, string> idToNames)
        {
            foreach (string id in idToNames.Keys)
            {
                RestRequest request = RestRequest.GetRequestForDelete(ApiVersionStrings.VersionNumber, Constants.Account,
                    id);
                await _restClient.SendAsync(request);
            }
            return true;
        }

        private Dictionary<string, string> MakeSomeLocalChanges()
        {
            string[] allIds = _idToNames.Keys.ToArray();
            var ids = new[] { allIds[0], allIds[1], allIds[2] };
            Dictionary<string, string> idToNamesLocallyUpdated = ids.ToDictionary(id => id,
                id => _idToNames[id] + "_updated");
            UpdateAccountsLocally(idToNamesLocallyUpdated);
            return idToNamesLocallyUpdated;
        }

        private async Task<bool> UpdateAccountsOnServer(Dictionary<string, string> idToNamesUpdated)
        {
            foreach (string id in idToNamesUpdated.Keys)
            {
                string updatedName = idToNamesUpdated[id];
                var fields = new Dictionary<string, object>();
                fields.Add(Constants.Name, updatedName);
                RestRequest request = RestRequest.GetRequestForUpdate(ApiVersionStrings.VersionNumber, Constants.Account,
                    id, fields);
                var response = await _restClient.SendAsync(request);
                Assert.IsTrue(response.Success, "Update failed");
            }
            return true;
        }
    }
}