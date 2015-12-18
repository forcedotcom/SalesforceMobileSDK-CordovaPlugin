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
using System.Linq;
using System.Reflection;
using Microsoft.VisualStudio.TestPlatform.UnitTestFramework;
using Salesforce.SDK.Security;
using Salesforce.SDK.Core;
using Salesforce.SDK.App;
using Salesforce.SDK.Logging;
using Salesforce.SDK.Auth;

namespace Salesforce.SDK.Auth
{
    [TestClass]
    public class AuthStorageHelperTest
    {
        private const string Password = "password";
        private const string Salt = "salt";
        private static IEncryptionService EncryptionService => SDKServiceLocator.Get<IEncryptionService>();

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
        public void TestGetAuthStorageHelper()
        {
            AuthStorageHelper authStorageHelper = AuthStorageHelper.GetAuthStorageHelper();
            Assert.IsNotNull(authStorageHelper);
        }

        [TestMethod]
        public void TestPersistRetrieveDeleteCredentials()
        {
            var account = new Account("loginUrl", "clientId", "callbackUrl", new[] {"scopeA", "scopeB"}, "instanceUrl",
                "identityUrl", "accessToken", "refreshToken")
            {
                UserId = "userId",
                UserName = "userName"
            };
            AuthStorageHelper authStorageHelper = AuthStorageHelper.GetAuthStorageHelper();
            CheckAccount(account, false);
            TypeInfo auth = authStorageHelper.GetType().GetTypeInfo();
            MethodInfo persist = auth.GetDeclaredMethod("PersistCurrentCredentials");
            MethodInfo delete =
                auth.GetDeclaredMethods("DeletePersistedCredentials")
                    .First(method => method.GetParameters().Count() == 2);
            persist.Invoke(authStorageHelper, new object[] {account});
            CheckAccount(account, true);
            delete.Invoke(authStorageHelper, new object[] { account.UserName, account.UserId });
            CheckAccount(account, false);
        }

        [TestMethod]
        public void TestPersistRetrieveDeleteEncryptionSettings()
        {
            AuthStorageHelper authStorageHelper = AuthStorageHelper.GetAuthStorageHelper();
            
            TypeInfo auth = authStorageHelper.GetType().GetTypeInfo();
            MethodInfo persist = auth.GetDeclaredMethod("PersistEncryptionSettings");
            MethodInfo delete = auth.GetDeclaredMethod("DeleteEncryptionSettings");
            
            persist.Invoke(authStorageHelper, new object[] { Password, Salt });
            CheckEncryptionSettings(true);
            delete.Invoke(authStorageHelper, null);
            CheckEncryptionSettings(false);
        }

        private void CheckAccount(Account expectedAccount, bool exists)
        {
            AuthStorageHelper authStorageHelper = AuthStorageHelper.GetAuthStorageHelper();
            TypeInfo auth = authStorageHelper.GetType().GetTypeInfo();
            MethodInfo retrieve = auth.GetDeclaredMethod("RetrievePersistedCredentials");
            var accounts = (Dictionary<string, Account>) retrieve.Invoke(authStorageHelper, null);
            if (!exists)
            {
                Assert.IsFalse(accounts.ContainsKey(expectedAccount.UserName),
                    "Account " + expectedAccount.UserName + " should not have been found");
            }
            else
            {
                Assert.IsTrue(accounts.ContainsKey(expectedAccount.UserName),
                    "Account " + expectedAccount.UserName + " should exist");
                Account account = accounts[expectedAccount.UserName];
                Assert.AreEqual(expectedAccount.LoginUrl, account.LoginUrl);
                Assert.AreEqual(expectedAccount.ClientId, account.ClientId);
                Assert.AreEqual(expectedAccount.CallbackUrl, account.CallbackUrl);
                Assert.AreEqual(expectedAccount.Scopes.Length, account.Scopes.Length);
                Assert.AreEqual(expectedAccount.InstanceUrl, account.InstanceUrl);
                Assert.AreEqual(expectedAccount.AccessToken, expectedAccount.AccessToken);
                Assert.AreEqual(expectedAccount.RefreshToken, expectedAccount.RefreshToken);
            }
        }

        private void CheckEncryptionSettings(bool exists)
        {
            AuthStorageHelper authStorageHelper = AuthStorageHelper.GetAuthStorageHelper();
            TypeInfo auth = authStorageHelper.GetType().GetTypeInfo();
            MethodInfo tryRetrieve = auth.GetDeclaredMethod("TryRetrieveEncryptionSettings");
            var parameters = new object[] {null, null};
            var success = (bool)tryRetrieve.Invoke(authStorageHelper, parameters);
            if (!exists)
            {
                Assert.IsFalse(success, "Encryption settings should not exist");
            }
            else
            {
                Assert.IsTrue(success, "Encryption settings should exist");
                Assert.AreEqual(Password, parameters[0]);
                Assert.AreEqual(Salt, parameters[1]);

            }
        }
    }
}