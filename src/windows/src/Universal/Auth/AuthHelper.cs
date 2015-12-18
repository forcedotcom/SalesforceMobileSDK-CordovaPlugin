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
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Salesforce.SDK.Pages;
using Salesforce.SDK.Logging;
using Salesforce.SDK.Core;
using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.Web.Http.Filters;
using Windows.Web.Http;

namespace Salesforce.SDK.Auth
{
    /// <summary>
    ///     Store specific implementation if IAuthHelper
    /// </summary>
    public sealed class AuthHelper : IAuthHelper
    {
        private static ILoggingService LoggingService => SDKServiceLocator.Get<ILoggingService>();

        public Type AccountPage
        {
            get
            {
                if (SDKManager.RootAccountPage != null)
                {
                    return SDKManager.RootAccountPage;
                }
                return typeof(AccountPage);
            }
        }

        /// <summary>
        ///     Bring up the WebAuthenticationBroker
        /// </summary>
        public async Task StartLoginFlowAsync()
        {
            var frame = Window.Current.Content as Frame;
            if (frame != null)
            {
                await
                    frame.Dispatcher.RunAsync(CoreDispatcherPriority.Normal,
                        () => { frame.Navigate(typeof (AccountPage)); });
            }
        }

        /// <summary>
        /// This should be called when login is complete. A new account is created
        /// in the AccountManager and the pincode screen is shown if needeed
        /// </summary>
        /// <param name="loginOptions"></param>
        /// <param name="authResponse"></param>
        public async Task OnLoginCompleteAsync(LoginOptions loginOptions, AuthResponse authResponse)
        {
            var frame = Window.Current.Content as Frame;
            var account = await AccountManager.CreateNewAccount(loginOptions, authResponse);
            if (account.Policy != null && (!PincodeManager.IsPincodeSet() || AuthStorageHelper.IsPincodeRequired()))
            {
                PincodeManager.LaunchPincodeScreen();
            }
            else
            {
                SDKServiceLocator.Get<ILoggingService>().Log($"Navigating to {SDKManager.RootApplicationPage}", LoggingLevel.Information);
                frame?.Navigate(SDKManager.RootApplicationPage);
            }
        }

        public Task PersistCurrentAccountAsync(Account account)
        {
            AuthStorageHelper.GetAuthStorageHelper().PersistCurrentCredentials(account);
            return Task.FromResult(0);
        }

        public void RefreshCookies()
        {
            LoggingService.Log("Attempting at refreshing cookies", LoggingLevel.Verbose);
            if (Window.Current == null)
                return;
            Account account = AccountManager.GetAccount();
            if (account != null)
            {
                var loginUri = new Uri(account.LoginUrl);
                var instanceUri = new Uri(account.InstanceUrl);
                var filter = new HttpBaseProtocolFilter();
                var cookie = new HttpCookie("salesforce", loginUri.Host, "/");
                var instance = new HttpCookie("salesforceInstance", instanceUri.Host, "/");
                cookie.Value = account.AccessToken;
                instance.Value = account.AccessToken;
                filter.CookieManager.SetCookie(cookie, false);
                filter.CookieManager.SetCookie(instance, false);
                var httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, instanceUri);
                var web = new WebView();
                web.NavigateWithHttpRequestMessage(httpRequestMessage);
            }
            LoggingService.Log("finished refreshing cookies", LoggingLevel.Verbose);
        }

        public async Task ClearCookiesAsync(LoginOptions options)
        {
            if (Window.Current == null)
                return;
            var frame = Window.Current.Content as Frame;
            if (frame != null)
            {
                await frame.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    var loginUri = new Uri(OAuth2.ComputeAuthorizationUrl(options));
                    var myFilter = new HttpBaseProtocolFilter();
                    HttpCookieManager cookieManager = myFilter.CookieManager;
                    try
                    {
                        LoggingService.Log("attempting to clear cookies", LoggingLevel.Verbose);
                        HttpCookieCollection cookies = cookieManager.GetCookies(loginUri);
                        foreach (HttpCookie cookie in cookies)
                        {
                            cookieManager.DeleteCookie(cookie);
                        }
                        LoggingService.Log("clear cookies done", LoggingLevel.Verbose);
                    }
                    catch (ArgumentException ex)
                    {
                        LoggingService.Log("Exception occurred when clearing cookies", LoggingLevel.Critical);
                        LoggingService.Log(ex, LoggingLevel.Critical);
                    }

                });
            }
        }

        public void DeletePersistedAccount(string userName, string userId)
        {
            AuthStorageHelper.GetAuthStorageHelper().DeletePersistedCredentials(userName, userId);
        }

        public Dictionary<string, Account> RetrieveAllPersistedAccounts()
        {
            return AuthStorageHelper.GetAuthStorageHelper().RetrievePersistedCredentials();
        }

        public Account RetrieveCurrentAccount()
        {
            return AuthStorageHelper.GetAuthStorageHelper().RetrieveCurrentAccount();
        }

        public void SavePinTimer()
        {
            AuthStorageHelper.SavePinTimer();
        }

        public void DeleteAllPersistedAccounts()
        {
            AuthStorageHelper.GetAuthStorageHelper().DeletePersistedCredentials();
        }

        public void WipePincode()
        {
            AuthStorageHelper.WipePincode();
        }
    }
}