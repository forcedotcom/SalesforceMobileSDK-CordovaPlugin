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

using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestPlatform.UnitTestFramework;
using Newtonsoft.Json;
using Salesforce.SDK.Net;
using System.Net;
using Salesforce.SDK.App;
using Salesforce.SDK.Auth;
using Salesforce.SDK.Core;
using Salesforce.SDK.Logging;
using Salesforce.SDK.Rest;
using Salesforce.SDK.Security;

namespace Salesforce.SDK.Auth
{
    [TestClass]
    public class OAuth2Test
    {

        [ClassInitialize]
        public static void SetupClass(TestContext context)
        {
            SFApplicationHelper.RegisterServices();
            SDKServiceLocator.RegisterService<ILoggingService, Hybrid.Logging.Logger>();
        }

        [TestInitialize]
        public void Setup()
        {
            var settings = new EncryptionSettings(new HmacSHA256KeyGenerator());
            Encryptor.init(settings);
        }

        [TestMethod]
        public void TestComputeAuthorizationUrl()
        {
            string loginUrl = "https://login.salesforce.com";
            string clientId = "TEST_CLIENT_ID";
            string callbackUrl = "test://sfdc";
            string[] scopes = {"web", "api"};
            var loginOptions = new LoginOptions(loginUrl, clientId, callbackUrl, scopes);

            string expectedUrl =
                "https://login.salesforce.com/services/oauth2/authorize?display=touch&response_type=token&client_id=TEST_CLIENT_ID&redirect_uri=test%3A%2F%2Fsfdc&scope=web+api+refresh_token";
            string actualUrl = OAuth2.ComputeAuthorizationUrl(loginOptions);
            Assert.AreEqual(expectedUrl, actualUrl, "Wrong authorization url");
        }

        [TestMethod]
        public void TestComputeFrontDoorUrl()
        {
            string instanceUrl = "https://fake.instance";
            string accessToken = "FAKE_ACCESS_TOKEN";
            string url = "https://target.url";

            string expectedUrl =
                "https://fake.instance/secur/frontdoor.jsp?display=touch&sid=FAKE_ACCESS_TOKEN&retURL=https%3A%2F%2Ftarget.url";
            string actualUrl = OAuth2.ComputeFrontDoorUrl(instanceUrl, accessToken, url);
            Assert.AreEqual(expectedUrl, actualUrl, "Wrong front door url");
        }

        [TestMethod]
        public async Task TestRefreshAuthToken()
        {
            // Try describe without being authenticated, expect 401
            Assert.AreEqual(HttpStatusCode.Unauthorized, await DoDescribe(null));

            var account = await OAuth2.RefreshAuthTokenAsync(TestCredentials.TestAccount);

            // Try describe again, expect 200
            Assert.AreEqual(HttpStatusCode.OK, await DoDescribe(account.AccessToken));
        }

        [TestMethod]
        public async Task TestCallIdentityService()
        {
            // Get auth token and identity url (through refresh)
            var account = await OAuth2.RefreshAuthTokenAsync(TestCredentials.TestAccount);

            // Call the identity service
            IdentityResponse identityResponse = await OAuth2.CallIdentityServiceAsync(account.IdentityUrl, account.AccessToken);

            // Check username
            Assert.AreEqual("sdktest@cs1.com", identityResponse.UserName);
        }

        [TestMethod]
        public void testDeserializeIdentityResponseWithoutMobilePolicy()
        {
            string idUrl = "https://login.salesforce.com/id/00D50000000IZ3ZEAW/00550000001fg5OAAQ";
            string userId = "00550000001fg5OAAQ";
            string organizationId = "00D50000000IZ3ZEAW";
            string userName = "user@example.com";
            string partialResponseWithoutMobilePolicy = "{\"id\":\"" + idUrl + "\",\"user_id\":\"" + userId +
                                                        "\",\"organization_id\":\"" + organizationId +
                                                        "\",\"username\":\"" + userName + "\"}";
            var idResponse = JsonConvert.DeserializeObject<IdentityResponse>(partialResponseWithoutMobilePolicy);

            Assert.AreEqual(idUrl, idResponse.IdentityUrl);
            Assert.AreEqual(userId, idResponse.UserId);
            Assert.AreEqual(organizationId, idResponse.OrganizationId);
            Assert.AreEqual(userName, idResponse.UserName);
            Assert.IsNull(idResponse.MobilePolicy);
        }

        [TestMethod]
        public void testDeserializeIdentityResponseWithMobilePolicy()
        {
            string idUrl = "https://login.salesforce.com/id/00D50000000IZ3ZEAW/00550000001fg5OAAQ";
            string userId = "00550000001fg5OAAQ";
            string organizationId = "00D50000000IZ3ZEAW";
            string userName = "user@example.com";
            string partialResponseWithMobilePolicy = "{\"id\":\"" + idUrl + "\",\"user_id\":\"" + userId +
                                                     "\",\"organization_id\":\"" + organizationId + "\",\"username\":\"" +
                                                     userName +
                                                     "\",\"mobile_policy\":{\"pin_length\":6,\"screen_lock\":1}}";
            var idResponse = JsonConvert.DeserializeObject<IdentityResponse>(partialResponseWithMobilePolicy);

            Assert.AreEqual(idUrl, idResponse.IdentityUrl);
            Assert.AreEqual(userId, idResponse.UserId);
            Assert.AreEqual(organizationId, idResponse.OrganizationId);
            Assert.AreEqual(userName, idResponse.UserName);
            Assert.IsNotNull(idResponse.MobilePolicy);
            Assert.AreEqual(6, idResponse.MobilePolicy.PinLength);
            Assert.AreEqual(1, idResponse.MobilePolicy.ScreenLockTimeout);
        }

        private async Task<HttpStatusCode> DoDescribe(string authToken)
        {
            string describeAccountPath = "/services/data/" + ApiVersionStrings.VersionNumber + "/sobjects/Account/describe";
            var headers = new HttpCallHeaders(authToken, new Dictionary<string, string>());
            HttpCall result =
                await HttpCall.CreateGet(headers, TestCredentials.InstanceServer + describeAccountPath).ExecuteAsync();
            return result.StatusCode;
        }
    }
}