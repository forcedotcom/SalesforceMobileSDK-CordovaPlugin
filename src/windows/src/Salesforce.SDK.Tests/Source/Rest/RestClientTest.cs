/*
 * Copyright (c) 2013, salesforce.com, inc.
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
using System.Diagnostics;
using System.Net;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestPlatform.UnitTestFramework;
using Newtonsoft.Json.Linq;
using Salesforce.SDK.Auth;
using Salesforce.SDK.App;
using Salesforce.SDK.Core;
using Salesforce.SDK.Logging;
using Salesforce.SDK.Security;

namespace Salesforce.SDK.Rest
{
    internal class IdName
    {
        private readonly string _id;

        private readonly string _name;

        public IdName(string id, string name)
        {
            _id = id;
            _name = name;
        }

        public string Id
        {
            get { return _id; }
        }

        public string Name
        {
            get { return _name; }
        }
    }


    [TestClass]
    public class RestClientTest
    {
        private const string ENTITY_NAME_PREFIX = "RestClientTest";
        private const string BadToken = "bad-token";

        private string _accessToken;
        private IRestClient _restClient;
        private IRestClient _unauthenticatedRestClient;

        [ClassInitialize]
        public static void SetupClass(TestContext context)
        {
            SFApplicationHelper.RegisterServices();
            SDKServiceLocator.RegisterService<ILoggingService, Hybrid.Logging.Logger>();
        }

        [TestInitialize]
        public async Task SetUp()
        {
            var settings = new EncryptionSettings(new HmacSHA256KeyGenerator());
            Encryptor.init(settings);
            var account = TestCredentials.TestAccount;

            account = await OAuth2.RefreshAuthTokenAsync(account);
            _accessToken = account.AccessToken;

            _restClient = new RestClient(TestCredentials.InstanceUrl, _accessToken, null);
            _unauthenticatedRestClient = new RestClient(TestCredentials.InstanceUrl);
        }

        [TestCleanup]
        public void TearDown()
        {
            Cleanup();
        }

        [TestMethod]
        public void TestGetAuthToken()
        {
            Assert.AreEqual(_accessToken, _restClient.AccessToken, "Wrong access token");
        }


        [TestMethod]
        public async Task TestCallWithBadAuthToken()
        {
            var unauthenticatedRestClient = new RestClient(TestCredentials.InstanceServer, BadToken, null);
            var response =
                await
                    unauthenticatedRestClient.SendAsync(RestRequest.GetRequestForResources(new TestCredentials().ApiVersion));
            Assert.IsFalse(response.Success, "Success not expected");
            Assert.IsNotNull(response.Error, "Expected error");
            Assert.AreEqual(HttpStatusCode.Unauthorized.ToString().ToLower(), response.StatusCode.ToString().ToLower(), "Expected 401");
        }

        [TestMethod]
        public async Task TestCallWithBadAuthTokenAndTokenProvider()
        {
            var unauthenticatedRestClient = new RestClient(TestCredentials.InstanceServer, BadToken,
                () => Task.Factory.StartNew(() => _accessToken));
            Assert.AreEqual(BadToken, unauthenticatedRestClient.AccessToken,
                "RestClient should be using the bad token initially");
            var response =
                await
                    unauthenticatedRestClient.SendAsync(RestRequest.GetRequestForResources(new TestCredentials().ApiVersion));
            Assert.IsTrue(response.Success, "Success expected");
            Assert.IsNull(response.Error, "Expected error");
            Assert.AreEqual(HttpStatusCode.OK.ToString().ToLower(), response.StatusCode.ToString().ToLower(), "Expected 200");
            Assert.AreEqual(_accessToken, unauthenticatedRestClient.AccessToken,
                "RestClient should now using the good token");
        }

        [TestMethod]
        public async Task TestGetVersions()
        {
            // We don't need to be authenticated
            var response = await _unauthenticatedRestClient.SendAsync(RestRequest.GetRequestForVersions());
            CheckResponse(response, HttpStatusCode.OK, true);
        }

        [TestMethod]
        public async Task TestGetResources()
        {
            var response =
                await _restClient.SendAsync(RestRequest.GetRequestForResources(new TestCredentials().ApiVersion));
            CheckResponse(response, HttpStatusCode.OK, false);
            CheckKeys(response.AsJObject, "sobjects", "search", "recent");
        }

        [TestMethod]
        public async Task TestGetResourcesWithUnAuthenticatedClient()
        {
            var response =
                await _unauthenticatedRestClient.SendAsync(RestRequest.GetRequestForResources(new TestCredentials().ApiVersion));
            Assert.AreEqual(response.StatusCode.ToString().ToLower(), HttpStatusCode.Unauthorized.ToString().ToLower());
        }

        [TestMethod]
        public async Task TestDescribeGlobal()
        {
            var response =
                await _restClient.SendAsync(RestRequest.GetRequestForDescribeGlobal(new TestCredentials().ApiVersion));
            CheckResponse(response, HttpStatusCode.OK, false);
            JObject jsonResponse = response.AsJObject;
            CheckKeys(jsonResponse, "encoding", "maxBatchSize", "sobjects");
            CheckKeys((JObject) ((JArray) jsonResponse["sobjects"])[0], "name", "label", "custom", "keyPrefix");
        }

        [TestMethod]
        public async Task TestDescribeGlobalWithUnAuthenticatedClient()
        {
            var response =
                await _unauthenticatedRestClient.SendAsync(RestRequest.GetRequestForDescribeGlobal(new TestCredentials().ApiVersion));
            Assert.AreEqual(response.StatusCode.ToString().ToLower(), HttpStatusCode.Unauthorized.ToString().ToLower());
        }

        [TestMethod]
        public async Task TestMetadata()
        {
            var response =
                await _restClient.SendAsync(RestRequest.GetRequestForMetadata(new TestCredentials().ApiVersion, "account"));
            CheckResponse(response, HttpStatusCode.OK, false);
            JObject jsonResponse = response.AsJObject;
            CheckKeys(jsonResponse, "objectDescribe", "recentItems");
            CheckKeys((JObject) jsonResponse["objectDescribe"], "name", "label", "keyPrefix");
            Assert.AreEqual("Account", jsonResponse["objectDescribe"]["name"], "Wrong object name");
        }

        [TestMethod]
        public async Task TestMetadataWithUnAuthenticatedClient()
        {
            var response =
                await _unauthenticatedRestClient.SendAsync(RestRequest.GetRequestForMetadata(new TestCredentials().ApiVersion, "account"));
            Assert.AreEqual(response.StatusCode.ToString().ToLower(), HttpStatusCode.Unauthorized.ToString().ToLower());
        }

        [TestMethod]
        public async Task TestDescribe()
        {
            var response =
                await _restClient.SendAsync(RestRequest.GetRequestForDescribe(new TestCredentials().ApiVersion, "account"));
            CheckResponse(response, HttpStatusCode.OK, false);
            JObject jsonResponse = response.AsJObject;
            CheckKeys(jsonResponse, "name", "fields", "urls", "label");
            Assert.AreEqual("Account", jsonResponse["name"], "Wrong object name");
        }

        [TestMethod]
        public async Task TestDescribeWithUnAuthenticatedClient()
        {
            var response =
                await _unauthenticatedRestClient.SendAsync(RestRequest.GetRequestForDescribe(new TestCredentials().ApiVersion, "account"));
            Assert.AreEqual(response.StatusCode.ToString().ToLower(), HttpStatusCode.Unauthorized.ToString().ToLower());
        }

        [TestMethod]
        public async Task TestCreate()
        {
            var fields = new Dictionary<string, object> {{"name", generateAccountName()}};
            var response =
                await
                    _restClient.SendAsync(RestRequest.GetRequestForCreate(new TestCredentials().ApiVersion, "account", fields));
            JObject jsonResponse = response.AsJObject;
            CheckKeys(jsonResponse, "id", "errors", "success");
            Assert.IsTrue((bool) jsonResponse["success"], "Create failed");
        }

        [TestMethod]
        public async Task TestCreateWithUnAuthenticatedClient()
        {
            var fields = new Dictionary<string, object> { { "name", generateAccountName() } };
            var response =
                await
                    _unauthenticatedRestClient.SendAsync(RestRequest.GetRequestForCreate(new TestCredentials().ApiVersion, "account", fields));
            Assert.AreEqual(response.StatusCode.ToString().ToLower(), HttpStatusCode.Unauthorized.ToString().ToLower());
        }

        [TestMethod]
        public async Task TestRetrieve()
        {
            string[] fields = {"name", "ownerId"};
            IdName newAccountIdName = await CreateAccount();
            var response =
                await
                    _restClient.SendAsync(RestRequest.GetRequestForRetrieve(new TestCredentials().ApiVersion, "account",
                        newAccountIdName.Id, fields));
            CheckResponse(response, HttpStatusCode.OK, false);
            JObject jsonResponse = response.AsJObject;
            CheckKeys(jsonResponse, "attributes", "Name", "OwnerId", "Id");
            Assert.AreEqual(newAccountIdName.Name, jsonResponse["Name"], "Wrong row returned");
        }

        [TestMethod]
        public async Task TestRetrieveWithUnAuthenticatedClient()
        {
            string[] fields = { "name", "ownerId" };
            IdName newAccountIdName = await CreateAccount();
            var response =
                await
                    _unauthenticatedRestClient.SendAsync(RestRequest.GetRequestForRetrieve(new TestCredentials().ApiVersion, "account",
                        newAccountIdName.Id, fields));
            Assert.AreEqual(response.StatusCode.ToString().ToLower(), HttpStatusCode.Unauthorized.ToString().ToLower());
        }

        [TestMethod]
        public async Task TestUpdate()
        {
            // Create
            IdName newAccountIdName = await CreateAccount();

            // Update
            string updatedAccountName = generateAccountName();
            var fields = new Dictionary<string, object> {{"name", updatedAccountName}};

            var updateResponse =
                await
                    _restClient.SendAsync(RestRequest.GetRequestForUpdate(new TestCredentials().ApiVersion, "account",
                        newAccountIdName.Id, fields));
            Assert.IsTrue(updateResponse.Success, "Update failed");

            // Retrieve - expect updated name
            var response =
                await
                    _restClient.SendAsync(RestRequest.GetRequestForRetrieve(new TestCredentials().ApiVersion, "account",
                        newAccountIdName.Id, new[] {"name"}));
            Assert.AreEqual(updatedAccountName, response.AsJObject["Name"], "Wrong row returned");
        }


        [TestMethod]
        public async Task TestDelete()
        {
            // Create
            IdName newAccountIdName = await CreateAccount();

            // Delete
            var deleteResponse =
                await
                    _restClient.SendAsync(RestRequest.GetRequestForDelete(new TestCredentials().ApiVersion, "account",
                        newAccountIdName.Id));
            Assert.IsTrue(deleteResponse.Success, "Delete failed");

            // Retrieve - expect 404
            var response =
                await
                    _restClient.SendAsync(RestRequest.GetRequestForRetrieve(new TestCredentials().ApiVersion, "account",
                        newAccountIdName.Id, new[] {"name"}));
            Assert.AreEqual(HttpStatusCode.NotFound.ToString().ToLower(), response.StatusCode.ToString().ToLower(), "404 was expected");
        }


        [TestMethod]
        public async Task TestQuery()
        {
            IdName newAccountIdName = await CreateAccount();
            var response =
                await
                    _restClient.SendAsync(RestRequest.GetRequestForQuery(new TestCredentials().ApiVersion,
                        "select name from account where id = '" + newAccountIdName.Id + "'"));
            CheckResponse(response, HttpStatusCode.OK, false);
            JObject jsonResponse = response.AsJObject;
            CheckKeys(jsonResponse, "done", "totalSize", "records");
            Assert.AreEqual(1, jsonResponse["totalSize"], "Expected one row");
            Assert.AreEqual(newAccountIdName.Name, jsonResponse["records"][0]["Name"], "Wrong row returned");
        }

        [TestMethod]
        public async Task TestQueryWithUnAuthenticatedClient()
        {
            IdName newAccountIdName = await CreateAccount();
            var response =
                await
                    _unauthenticatedRestClient.SendAsync(RestRequest.GetRequestForQuery(new TestCredentials().ApiVersion,
                        "select name from account where id = '" + newAccountIdName.Id + "'"));
            Assert.AreEqual(response.StatusCode.ToString().ToLower(), HttpStatusCode.Unauthorized.ToString().ToLower());
        }

        [TestMethod]
        public async Task TestSearch()
        {
            IdName newAccountIdName = await CreateAccount();
            var response =
                await
                    _restClient.SendAsync(RestRequest.GetRequestForSearch(new TestCredentials().ApiVersion,
                        "find {" + newAccountIdName.Name + "}"));
            CheckResponse(response, HttpStatusCode.OK, true);
            JArray matchingRows = response.AsJArray;
            Assert.AreEqual(1, matchingRows.Count, "Expected one row");
            var matchingRow = (JObject) matchingRows[0];
            CheckKeys(matchingRow, "attributes", "Id");
            Assert.AreEqual(newAccountIdName.Id, matchingRow["Id"], "Wrong row returned");
        }

        [TestMethod]
        public async Task TestSearchWithUnAuthenticatedClient()
        {
            IdName newAccountIdName = await CreateAccount();
            var response =
                await
                    _unauthenticatedRestClient.SendAsync(RestRequest.GetRequestForSearch(new TestCredentials().ApiVersion,
                        "find {" + newAccountIdName.Name + "}"));
            Assert.AreEqual(response.StatusCode.ToString().ToLower(), HttpStatusCode.Unauthorized.ToString().ToLower());
        }

        private string generateAccountName()
        {
            return ENTITY_NAME_PREFIX + (new Random()).Next(10000, 99999);
        }

        private async Task<IdName> CreateAccount()
        {
            string newAccountName = generateAccountName();
            var fields = new Dictionary<string, object> {{"name", newAccountName}};
            var response =
                await
                    _restClient.SendAsync(RestRequest.GetRequestForCreate(new TestCredentials().ApiVersion, "account", fields));
            var newAccountId = (string) response.AsJObject["id"];
            return new IdName(newAccountId, newAccountName);
        }

        private async void Cleanup()
        {
            try
            {
                var searchResponse =
                    await
                        _restClient.SendAsync(RestRequest.GetRequestForSearch(new TestCredentials().ApiVersion,
                            "find {" + ENTITY_NAME_PREFIX + "}"));
                JArray matchingRows = searchResponse.AsJArray;
                for (int i = 0; i < matchingRows.Count; i++)
                {
                    var matchingRow = (JObject) matchingRows[i];
                    var matchingRowType = (string) matchingRow["attributes"]["type"];
                    var matchingRowId = (string) matchingRow["Id"];
                    Debug.WriteLine("Trying to delete {0}", matchingRowId);
                    await
                        _restClient.SendAsync(RestRequest.GetRequestForDelete(new TestCredentials().ApiVersion,
                            matchingRowType, matchingRowId));
                    Debug.WriteLine("Successfully deleted {0}", matchingRowId);
                }
            }
            catch
            {
                // We tried our best :-(
            }
        }

        private void CheckResponse(IRestResponse response, HttpStatusCode expectedStatusCode, bool isJArray)
        {
            Assert.AreEqual(expectedStatusCode.ToString().ToLower(), response.StatusCode.ToString().ToLower());
            try
            {
                if (isJArray)
                {
                    Assert.IsNotNull(response.AsJArray);
                }
                else
                {
                    Assert.IsNotNull(response.AsJObject);
                }
            }
            catch
            {
                Assert.Fail("Failed to parse response body");
            }
        }

        private void CheckKeys(JObject jObject, params string[] expectedKeys)
        {
            foreach (string expectedKey in expectedKeys)
            {
                Assert.IsNotNull(jObject[expectedKey]);
            }
        }
    }
}