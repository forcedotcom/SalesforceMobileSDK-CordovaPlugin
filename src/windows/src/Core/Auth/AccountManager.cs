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
using System.Threading.Tasks;
using Salesforce.SDK.Logging;
using Newtonsoft.Json;
using Salesforce.SDK.Core;
using Salesforce.SDK.Rest;

namespace Salesforce.SDK.Auth
{
    /// <summary>
    ///     Class providing (static) methods for creating/deleting or retrieving an Account
    /// </summary>
    public class AccountManager
    {
        private static IAuthHelper AuthStorageHelper => SDKServiceLocator.Get<IAuthHelper>();
        private static ILoggingService LoggingService => SDKServiceLocator.Get<ILoggingService>();

        /// <summary>
        /// This event notifies consumers that the authenticated account has changed.
        /// </summary>
        public static event AuthenticatedAccountChangedHandler AuthenticatedAccountChanged;
        public delegate void AuthenticatedAccountChangedHandler(AuthenticatedAccountChangedEventArgs e);

        /// <summary>
        /// Raises the account changed event. This method exists because AuthStorageHelper truly knows when
        /// an account changes but the event belongs in the AccountManager class, so this method is necessary
        /// to enable AuthStorageHelper to raise that event.
        /// </summary>
        public static void RaiseAuthenticatedAccountChangedEvent(Account oldAccount, Account newAccount)
        {
            AuthenticatedAccountChanged?.Invoke(new AuthenticatedAccountChangedEventArgs(oldAccount, newAccount));
        }

        /// <summary>
        ///     Delete Account for currently authenticated user
        /// </summary>
        public static void DeleteAccount()
        {
            Account account = GetAccount();
            AuthStorageHelper.DeletePersistedCredentials(account.UserName, account.UserId);
        }

        public static Dictionary<string, Account> GetAccounts()
        {
            return AuthStorageHelper.RetrievePersistedCredentials();
        }

        /// <summary>
        ///     Return Account for currently authenticated user
        /// </summary>
        /// <returns></returns>
        public static Account GetAccount()
        {
            return AuthStorageHelper.RetrieveCurrentAccount();
        }

        public static async Task<bool> SwitchToAccount(Account account)
        {
            if (account != null && account.UserId != null)
            {
                AuthStorageHelper.SavePinTimer();
                await AuthStorageHelper.PersistCredentialsAsync(account);
                var client = SDKManager.GlobalClientManager.PeekRestClient();
                if (client != null)
                {
                    AuthStorageHelper.ClearCookies(account.GetLoginOptions());
                    IdentityResponse identity = await OAuth2.CallIdentityService(account.IdentityUrl, client);
                    if (identity != null)
                    {
                        account.UserId = identity.UserId;
                        account.UserName = identity.UserName;
                        account.Policy = identity.MobilePolicy;
                        await AuthStorageHelper.PersistCredentialsAsync(account);
                    }
                    AuthStorageHelper.RefreshCookies();
                    LoggingService.Log("switched accounts, result = true", LoggingLevel.Verbose);
                    return true;
                }
            }
            LoggingService.Log("switched accounts, result = false", LoggingLevel.Verbose);
            return false;
        }

        public static void WipeAccounts()
        {
            AuthStorageHelper.DeletePersistedCredentials();
            AuthStorageHelper.WipePincode();
            SwitchAccount();
        }

        public static void SwitchAccount()
        {
            AuthStorageHelper.StartLoginFlow();
        }

        /// <summary>
        ///     Create and persist Account for newly authenticated user
        /// </summary>
        /// <param name="loginOptions"></param>
        /// <param name="authResponse"></param>
        public static async Task<Account> CreateNewAccount(LoginOptions loginOptions, AuthResponse authResponse)
        {
            LoggingService.Log("Create account object", LoggingLevel.Verbose);
            var account = new Account(loginOptions.LoginUrl, loginOptions.ClientId, loginOptions.CallbackUrl,
                loginOptions.Scopes,
                authResponse.InstanceUrl, authResponse.IdentityUrl, authResponse.AccessToken, authResponse.RefreshToken);
            account.CommunityId = authResponse.CommunityId;
            account.CommunityUrl = authResponse.CommunityUrl;
            var cm = new ClientManager();
            cm.PeekRestClient();
            IdentityResponse identity = null;
            try
            {
                identity = await OAuth2.CallIdentityService(authResponse.IdentityUrl, authResponse.AccessToken);
            }
            catch (JsonException ex)
            {
                LoggingService.Log("Exception occurred when retrieving account identity:",
                    LoggingLevel.Critical);
                LoggingService.Log(ex, LoggingLevel.Critical);
                Debug.WriteLine("Error retrieving account identity");
            }
            if (identity != null)
            {
                account.UserId = identity.UserId;
                account.UserName = identity.UserName;
                account.Policy = identity.MobilePolicy;
                await AuthStorageHelper.PersistCredentialsAsync(account);
            }
            LoggingService.Log("Finished creating account", LoggingLevel.Verbose);
            return account;
        }
    }
}