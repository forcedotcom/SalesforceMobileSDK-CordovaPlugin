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
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Windows.Security.Cryptography.Core;
using Windows.Storage;
using Salesforce.SDK.Auth;
using Salesforce.SDK.Security;
using Salesforce.SDK.Settings;

namespace Salesforce.SDK.Upgrade
{
    public class SDKUpgradeManager
    {
        private static SDKUpgradeManager _instance = null;

        public static SDKUpgradeManager GetInstance()
        {
            if (_instance == null)
            {
                _instance = new SDKUpgradeManager();
            }
            return _instance;
        }

        public async Task UpgradeAsync()
        {
            if (ApplicationData.Current.Version.Equals(0))
            {
                await UpgradeFromEarlierThan4Dot2();
            }
            await ApplicationData.Current.SetVersionAsync(VersionConvertion(SDKManager.SDK_VERSION), VersionRequestHandler);
        }

        private async Task UpgradeFromEarlierThan4Dot2()
        {
            Encryptor.init(new EncryptionSettings(new HmacSHA256KeyGenerator(HashAlgorithmNames.Md5)));
            var authHelper = new AuthHelper();
            var account = authHelper.RetrieveCurrentAccount();
            if (account != null)
            {
                Encryptor.ChangeSettings(new EncryptionSettings(new HmacSHA256KeyGenerator(HashAlgorithmNames.Sha256)));
                await authHelper.PersistCurrentAccountAsync(account);
                await authHelper.PersistCurrentPincodeAsync(account);
            }
        }

        internal uint VersionConvertion(string version)
        {
            return UInt32.Parse(new string(version.Where(Char.IsDigit).ToArray()));
        }

        internal void VersionRequestHandler(SetVersionRequest request)
        {
            var defer = request.GetDeferral();
            foreach (var item in ApplicationData.Current.LocalSettings.Values)
            {
                switch (item.Key)
                {
                    case "Name":
                        ApplicationData.Current.LocalSettings.Values["Name"] = string.Format("V{0}_{1}", request.DesiredVersion, item.Value);
                        break;

                    default:
                        ApplicationData.Current.LocalSettings.Values[item.Key] = string.Format("{0}", item.Value);
                        break;
                }
            }
            ApplicationData.Current.SignalDataChanged();
            defer.Complete();
        }
    }
}
