/*
 * Copyright (c) 2014, salesforce.com, inc.
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

using Salesforce.SDK.Auth;
using Salesforce.SDK.Core;
using Salesforce.SDK.Logging;
using Salesforce.SDK.Security;
using Salesforce.SDK.Settings;
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Windows.ApplicationModel;
using Windows.Security.ExchangeActiveSyncProvisioning;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.System.Profile;

namespace Salesforce.SDK.App
{
    public class ApplicationService : IApplicationInformationService
    {
        private static IEncryptionService EncryptionService => SDKServiceLocator.Get<IEncryptionService>();
        private static ILoggingService LoggingService => SDKServiceLocator.Get<ILoggingService>();
        private const string UserAgentHeaderFormat = "SalesforceMobileSDK/{0} {1} ({2}) {3}/{4} {5} uid_{6}";
        private const string SdkVersion = "4.0.0";

        /// <summary>
        ///     Settings key for config.
        /// </summary>
        private const string ConfigSettings = "salesforceConfig";

        private const string DefaultServerPath = "Salesforce.SDK.Resources.servers.xml";

        public Task ClearConfigurationSettingsAsync()
        {
            ApplicationDataContainer settings = ApplicationData.Current.LocalSettings;
            return Task.FromResult<bool>(settings.Values.Remove(ConfigSettings));
        }

        public async Task<bool> DoesFileExistAsync(string path)
        {
            var file = await ApplicationData.Current.LocalFolder.TryGetItemAsync(path) as IStorageFile;
            return (file != null);
        }

        public Task<string> GenerateUserAgentHeaderAsync(bool isHybrid, string qualifier)
        {
            var appName = GetApplicationDisplayNameAsync().Result;
            var deviceInfo = AnalyticsInfo.VersionInfo.DeviceFamily + "/" + GetDeviceFamilyVersion(AnalyticsInfo.VersionInfo.DeviceFamilyVersion);
            var deviceModel = new EasClientDeviceInformation().SystemProductName;
            var deviceId = new EasClientDeviceInformation().Id;
            PackageVersion packageVersion = Package.Current.Id.Version;
            string packageVersionString = packageVersion.Major + "." + packageVersion.Minor + "." +
                                          packageVersion.Build;
            var appType = new StringBuilder(isHybrid ? "Hybrid" : "Native");
            if (!String.IsNullOrWhiteSpace(qualifier))
            {
                appType.Append(qualifier);
            }
            var UserAgentHeader = String.Format(UserAgentHeaderFormat, SdkVersion, deviceInfo, deviceModel,
            appName, packageVersionString, appType, deviceId);
            return Task.FromResult(UserAgentHeader);
        }

        private string GetDeviceFamilyVersion(string version)
        {
            //Voodoo magic to parse OS version and build from DeviceFamilyVersion
            ulong v = ulong.Parse(version);
            ulong v1 = (v & 0xFFFF000000000000L) >> 48;
            ulong v2 = (v & 0x0000FFFF00000000L) >> 32;
            ulong v3 = (v & 0x00000000FFFF0000L) >> 16;
            ulong v4 = v & 0x000000000000FFFFL;
            return $"{v1}.{v2}.{v3}.{v4}";
        }


        public Task<string> GetApplicationDisplayNameAsync()
        {
            string displayName = String.Empty;

            try
            {
                var config = SDKManager.ServerConfiguration;
                if (config == null)
                {
                    throw new Exception();
                }
                if (!String.IsNullOrWhiteSpace(config.ApplicationTitle))
                {
                    displayName = config.ApplicationTitle;
                }
                else
                {
                    displayName = Package.Current.DisplayName;
                }
            }
            catch (Exception)
            {
                //If ApplicationTitle and Display Name both fail, fall back to Package Id
                LoggingService.Log("Error retrieving application name; using package id name instead", LoggingLevel.Warning);
                displayName = Package.Current.Id.Name;
            }
            return Task.FromResult<string>(displayName);
        }

        public string GetApplicationLocalFolderPath()
        {
            return ApplicationData.Current.LocalFolder.Path;
        }

        public Task<string> GetConfigurationSettingsAsync()
        {
            ApplicationDataContainer settings = ApplicationData.Current.LocalSettings;
            if (settings.Values.ContainsKey(ConfigSettings))
            {
                return Task.FromResult<string>(EncryptionService.Decrypt(settings.Values[ConfigSettings].ToString()));
            }
            return Task.FromResult<string>(String.Empty);
        }

        public async Task<string> ReadApplicationFileAsync(string path)
        {
            var fileUri = new Uri(@"ms-appx:///" + path);
            StorageFile file = await StorageFile.GetFileFromApplicationUriAsync(fileUri);
            if (file != null)
            {
                Stream stream = await file.OpenStreamForReadAsync();
                using (var reader = new StreamReader(stream))
                {
                    return reader.ReadToEnd();
                }
            }
            throw new FileNotFoundException("Resource file not found", path);
        }

        public Task SaveConfigurationSettingsAsync(string config)
        {
            ApplicationDataContainer settings = ApplicationData.Current.LocalSettings;
            settings.Values[ConfigSettings] = EncryptionService.Encrypt(config);
            return Task.FromResult<bool>(true);
        }
    }
}
