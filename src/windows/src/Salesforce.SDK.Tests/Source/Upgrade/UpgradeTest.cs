/*
 * Copyright (c) 2016-present, salesforce.com, inc.
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
using Microsoft.VisualStudio.TestPlatform.UnitTestFramework;
using Salesforce.SDK.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Windows.Security.Cryptography.Core;
using Windows.Storage;
using Salesforce.SDK.Core;
using Salesforce.SDK.App;
using Salesforce.SDK.Auth;
using Salesforce.SDK.Logging;
using Salesforce.SDK.Security;

namespace Salesforce.SDK.Upgrade.Test
{
    [TestClass]
    public class UpgradeTest
    {
        [TestInitialize]
        public void SetupTest()
        {
            SFApplicationHelper.RegisterServices();
            SDKServiceLocator.RegisterService<ILoggingService, Hybrid.Logging.Logger>();
        }

        [TestMethod]
        public async Task TestUpgradeFromEearlierThan4Dot2()
        {
            //set up the previous version and a test account
            var authHelper = new AuthHelper();
            Account account = await SetupTestAccountAsync(HashAlgorithmNames.Md5);
            await authHelper.PersistCurrentAccountAsync(account);

            var sdkUpgradeManager = Upgrade.SDKUpgradeManager.GetInstance();
            //before version 4.2.0, there is no version for SDK
            await ApplicationData.Current.SetVersionAsync(0, sdkUpgradeManager.VersionRequestHandler);

            //do upgrade
            await sdkUpgradeManager.UpgradeAsync();

            //verify
            Assert.AreEqual(ApplicationData.Current.Version, sdkUpgradeManager.VersionConvertion(SDKManager.SDK_VERSION), "Incorrect app version after upgrading");
            Assert.AreEqual(account.ToString(), authHelper.RetrieveCurrentAccount().ToString(), "the account does not match after upgrading");
        }

        [TestMethod]
        public async Task TestUpgradeFrom4Dot2()
        {
            //set up the previous version and a test account
            var authHelper = new AuthHelper();
            Account account = await SetupTestAccountAsync(HashAlgorithmNames.Sha256);
            await authHelper.PersistCurrentAccountAsync(account);
            var sdkUpgradeManager = Upgrade.SDKUpgradeManager.GetInstance();
            var newVersion = sdkUpgradeManager.VersionConvertion(SDKManager.SDK_VERSION);
            await ApplicationData.Current.SetVersionAsync(newVersion, sdkUpgradeManager.VersionRequestHandler);

            //do upgrade
            await sdkUpgradeManager.UpgradeAsync();

            //verify
            Assert.AreEqual(ApplicationData.Current.Version, newVersion, "Incorrect app version after upgrading");
            Assert.AreEqual(account.ToString(), authHelper.RetrieveCurrentAccount().ToString(), "the account does not match after upgrading");

        }

        [TestMethod]
        public async Task TestUpgradeFromLaterThan4Dot2()
        {
            //set up the previous version and a test account
            var authHelper = new AuthHelper();
            Account account = await SetupTestAccountAsync(HashAlgorithmNames.Sha256);
            await authHelper.PersistCurrentAccountAsync(account);

            SDKManager.SDK_VERSION = "5.0.0";
            var sdkUpgradeManager = Upgrade.SDKUpgradeManager.GetInstance();
            var newVersion = sdkUpgradeManager.VersionConvertion("5.0.0");
            await ApplicationData.Current.SetVersionAsync(newVersion, sdkUpgradeManager.VersionRequestHandler);

            //do upgrade
            await sdkUpgradeManager.UpgradeAsync();

            //verify
            Assert.AreEqual(ApplicationData.Current.Version, sdkUpgradeManager.VersionConvertion("5.0.0"), "Incorrect app version after upgrading");
            Assert.AreEqual(account.ToString(), authHelper.RetrieveCurrentAccount().ToString(), "the account does not match after upgrading");
        }

        private async Task<Account> SetupTestAccountAsync(string hashAlgorithmName)
        {
            var testScope = new[] { "scopeA", "scopeB" };
            SFApplicationHelper.RegisterServices();
            var settings = new EncryptionSettings(new HmacSHA256KeyGenerator(hashAlgorithmName));
            Encryptor.init(settings);
            var options = new LoginOptions(SmartStore.Store.TestCredentials.LoginUrl, SmartStore.Store.TestCredentials.ClientId,
                TestCredentials.CallbackUrl, "mobile", testScope);
            var response = new AuthResponse
            {
                RefreshToken = SmartStore.Store.TestCredentials.RefreshToken,
                AccessToken = TestCredentials.AccessToken,
                InstanceUrl = SmartStore.Store.TestCredentials.InstanceUrl,
                IdentityUrl = SmartStore.Store.TestCredentials.IdentityUrl,
                Scopes = testScope,
            };
            var account = await AccountManager.CreateNewAccount(options, response);
            account.UserId = SmartStore.Store.TestCredentials.UserId;
            account.UserName = SmartStore.Store.TestCredentials.Username;
            return await Task.FromResult(account);
        }
    }
}