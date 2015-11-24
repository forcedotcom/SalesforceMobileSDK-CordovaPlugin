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
using Salesforce.SDK.Rest;
using Salesforce.SDK.Settings;
using System.Threading.Tasks;

namespace Salesforce.SDK.Auth
{
    public class SDKManager
    {
        /// <summary>
        /// The root application page your app should move to after login/pincode. For UI apps only.
        /// </summary>
        public static Type RootApplicationPage { get; set; }

        /// <summary>
        /// Set RootAccountPage if you wish to provide your own account settings page.
        /// 
        /// If you do wish to do this, be warned that you should still call,
        /// 1. PlatformAdapter.Resolve<IAuthHelper>().StartLoginFlowAsync();
        /// 2. PlatformAdapter.Resolve<IAuthHelper>().OnLoginCompleteAsync(loginOptions, authResponse);
        /// Both methods will assist in displaying and finishing the login steps, including creation of the account and pincode settings.
        /// 
        /// You can also provide this own functionality yourself. Please look at AuthHelper.cs in the SDK.Phone and SDK.Store application for example code and work from there
        /// if you wish to completely customize your login flow.
        /// </summary>
        public static Type RootAccountPage { get; set; }


        /// <summary>
        ///     The global client manager is provided for ease of accessing clients such as the RestClient.
        /// </summary>
        public static ClientManager GlobalClientManager { get; private set; }

        /// <summary>
        ///     The current configuration for the application.
        /// </summary>
        public static SalesforceConfig ServerConfiguration { get; private set; }

        public static void ResetClientManager()
        {
            GlobalClientManager = new ClientManager();
        }

        public static void CreateClientManager(bool reset)
        {
            if (GlobalClientManager != null && !reset) return;
            GlobalClientManager = new ClientManager();
        }

        public static async Task<T> InitializeConfigAsync<T>() where T : SalesforceConfig
        {
            T config = await SalesforceConfig.RetrieveConfig<T>();
            if (config == null)
            {
                config = Activator.CreateInstance<T>();
                await config.InitializeAsync();
            }

            if (config.ServerList == null)
            {
                config.ServerList = new System.Collections.ObjectModel.ObservableCollection<ServerSetting>();
            }

            await config.SaveConfigAsync();
            ServerConfiguration = config;
            return config;
        }
    }
}
