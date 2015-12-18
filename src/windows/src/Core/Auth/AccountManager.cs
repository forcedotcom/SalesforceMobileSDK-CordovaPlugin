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
    /// Class providing (static) methods for creating/deleting or retrieving an Account
    /// </summary>
    public class AccountManager
    {
        private static IAuthHelper AuthStorageHelper => SDKServiceLocator.Get<IAuthHelper>();
        private static ILoggingService LoggingService => SDKServiceLocator.Get<ILoggingService>();

        private static Account _loggedInAccount;
        private static Account LoggedInAccount
        {
            set
            {
                var oldAccount = _loggedInAccount;
                _loggedInAccount = value;

                // Raise the event in AccountManager if this is a different account.
                // This check is necessary as sometimes CurrentAccount.Set is called
                // even if the same account was already set, so use the unique combo of
                // InstanceUrl and UserId to tell if the account has actually changed (login/logout).
                if (oldAccount?.OrganizationId != value?.OrganizationId && oldAccount?.UserId != value?.UserId)
                {
                    RaiseAuthenticatedAccountChangedEvent(oldAccount, value);
                }
            }
            get { return _loggedInAccount; }
        }

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
        private static void RaiseAuthenticatedAccountChangedEvent(Account oldAccount, Account newAccount)
        {
            AuthenticatedAccountChanged?.Invoke(new AuthenticatedAccountChangedEventArgs(oldAccount, newAccount));
        }

        /// <summary>
        /// Delete Account (log out) for currently authenticated user
        /// </summary>
        public static void DeleteAccount()
        {
            var account = GetAccount();

            if (account == null)
            {
                return;
            }

            AuthStorageHelper.DeletePersistedAccount(account.UserName, account.UserId);

            LoggedInAccount = null;
        }

        public static Dictionary<string, Account> GetAccounts()
        {
            return AuthStorageHelper.RetrieveAllPersistedAccounts();
        }

        /// <summary>
        ///  Return Account for currently authenticated user
        /// </summary>
        /// <returns></returns>
        public static Account GetAccount()
        {
            return LoggedInAccount ?? (LoggedInAccount = AuthStorageHelper.RetrieveCurrentAccount());
        }

        public static async Task<bool> SwitchToAccount(Account newAccount)
        {
            var oldAccount = LoggedInAccount;

            if (newAccount?.UserId != null)
            {
                // save current user pin timer
                AuthStorageHelper.SavePinTimer();

                await AuthStorageHelper.PersistCurrentAccountAsync(newAccount);

                var client = SDKManager.GlobalClientManager.PeekRestClient();
                if (client != null)
                {
                    await AuthStorageHelper.ClearCookiesAsync(newAccount.GetLoginOptions());
                    var identity = await OAuth2.CallIdentityServiceAsync(newAccount.IdentityUrl, client);
                    if (identity != null)
                    {
                        newAccount.UserId = identity.UserId;
                        newAccount.UserName = identity.UserName;
                        newAccount.Policy = identity.MobilePolicy;
                        await AuthStorageHelper.PersistCurrentAccountAsync(newAccount);
                    }
                    AuthStorageHelper.RefreshCookies();
                    LoggingService.Log("switched accounts, result = true", LoggingLevel.Verbose);
                    return true;
                }

                // log new user in
                LoggedInAccount = newAccount;

                RaiseAuthenticatedAccountChangedEvent(oldAccount, newAccount);
            }

            LoggingService.Log("switched accounts, result = false", LoggingLevel.Verbose);
            return false;
        }

        public static void WipeAccounts()
        {
            AuthStorageHelper.DeleteAllPersistedAccounts();
            AuthStorageHelper.WipePincode();
            SwitchAccount();
        }

        public static void SwitchAccount()
        {
            AuthStorageHelper.StartLoginFlowAsync();
        }

        /// <summary>
        ///     Create and persist Account for newly authenticated user
        /// </summary>
        /// <param name="loginOptions"></param>
        /// <param name="authResponse"></param>
        public static async Task<Account> CreateNewAccount(LoginOptions loginOptions, AuthResponse authResponse)
        {
            LoggingService.Log("Create account object", LoggingLevel.Verbose);

            var account = new Account(
                loginOptions.LoginUrl, 
                loginOptions.ClientId, 
                loginOptions.CallbackUrl,
                loginOptions.Scopes,
                authResponse.InstanceUrl, 
                authResponse.IdentityUrl, 
                authResponse.AccessToken, 
                authResponse.RefreshToken)
            {
                CommunityId = authResponse.CommunityId,
                CommunityUrl = authResponse.CommunityUrl
            };

            var cm = new ClientManager();
            cm.PeekRestClient();
            IdentityResponse identity = null;
            try
            {
                identity = await OAuth2.CallIdentityServiceAsync(authResponse.IdentityUrl, authResponse.AccessToken);
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
                account.OrganizationId = identity.OrganizationId;

                await AuthStorageHelper.PersistCurrentAccountAsync(account);

                LoggedInAccount = account;
            }
            LoggingService.Log("Finished creating account", LoggingLevel.Verbose);
            return account;
        }
    }
}