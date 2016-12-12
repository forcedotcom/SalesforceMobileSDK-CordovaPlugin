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
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Salesforce.SDK.Analytics.Model
{
    /// <summary>
    /// Represents common attributes specific to this app and device. These
    /// attributes will be appended to the events stored using this library.
    /// </summary>
    public class DeviceAppAttributes
    {
        #region Statics

        private const string AppVersionKey = "appVersion";
        private const string AppnameKey = "appName";
        private const string OsVersionKey = "osVersion";
        private const string OsNameKey = "osName";
        private const string NativeAppTypeKey = "nativeAppType";
        private const string MobileSdkVersionKey = "mobileSdkVersion";
        private const string DeviceModelKey = "deviceModel";
        private const string DeviceIdKey = "deviceId";
        private const string ClientIdKey = "clientId";

        #endregion

        private readonly string _appVersion;
        private readonly string _appName;
        private readonly string _osVersion;
        private readonly string _osName;
        private readonly string _nativeAppType;
        private readonly string _mobileSdkVersion;
        private readonly string _deviceModel;
        private readonly string _deviceId;
        private readonly string _clientId;

        [JsonProperty]
        public string AppVersion => _appVersion;
        [JsonProperty]
        public string AppName => _appName;
        [JsonProperty]
        public string OsVersion => _osVersion;
        [JsonProperty]
        public string OsName => _osName;
        [JsonProperty]
        public string NativeAppType => _nativeAppType;
        [JsonProperty]
        public string MobileSdkVersion => _mobileSdkVersion;
        [JsonProperty]
        public string DeviceModel => _deviceModel;
        [JsonProperty]
        public string DeviceId => _deviceId;
        [JsonProperty]
        public string Clientid => _clientId;

        [JsonConstructor]
        public DeviceAppAttributes(string appVersion, string appName, string osVersion, string osName,
            string nativeAppType, string mobileSdkVersion, string deviceModel,
            string deviceId, string clientId)
        {
            _appVersion = appVersion;
            _appName = appName;
            _osVersion = osVersion;
            _osName = osName;
            _nativeAppType = nativeAppType;
            _mobileSdkVersion = mobileSdkVersion;
            _deviceModel = deviceModel;
            _deviceId = deviceId;
            _clientId = clientId;
        }

        public DeviceAppAttributes(JObject json)
        {
            if (json != null)
            {
                _appVersion = json.GetValue(AppVersionKey).ToString();
                _appName = json.GetValue(AppnameKey).ToString();
                _osVersion = json.GetValue(OsVersionKey).ToString();
                _osName = json.GetValue(OsNameKey).ToString();
                _nativeAppType = json.GetValue(NativeAppTypeKey).ToString();
                _mobileSdkVersion = json.GetValue(MobileSdkVersionKey).ToString();
                _deviceModel = json.GetValue(DeviceModelKey).ToString();
                _deviceId = json.GetValue(DeviceIdKey).ToString();
                _clientId = json.GetValue(ClientIdKey).ToString();
            }
        }

        public JObject ToJson()
        {
            var json = new JObject
            {
                { AppVersionKey, _appVersion },
                { AppnameKey, _appName },
                { OsVersionKey, _osVersion},
                { OsNameKey, _osName},
                { NativeAppTypeKey, _nativeAppType},
                { MobileSdkVersionKey, _mobileSdkVersion},
                { DeviceModelKey, _deviceModel},
                { DeviceIdKey, _deviceId},
                { ClientIdKey, _clientId}
            };

            return json;
        }

    }
}
