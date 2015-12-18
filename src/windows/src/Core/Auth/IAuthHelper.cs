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
using System.Threading.Tasks;

namespace Salesforce.SDK.Auth
{
    /// <summary>
    /// Interface for auth related operations that are implemented in the platform specific assemblies
    /// </summary>
    public interface IAuthHelper
    {
        /// <summary>
        /// Shows the login UI
        /// </summary>
        Task StartLoginFlowAsync();

        /// <summary>
        /// This should be called when login is complete. A new account is created
        /// in the AccountManager and the pincode screen is shown if needeed
        /// </summary>
        /// <param name="loginOptions"></param>
        /// <param name="authResponse"></param>
        Task OnLoginCompleteAsync(LoginOptions loginOptions, AuthResponse authResponse);

        /// <summary>
        /// Refresh webview cookies
        /// </summary>
        void RefreshCookies();

        /// <summary>
        /// Clears webview cookies
        /// </summary>
        /// <param name="options"></param>
        Task ClearCookiesAsync(LoginOptions options);

        /// <summary>
        /// Save the current user's credentials to secure storage
        /// 
        /// NOTE This should only be called by the AccountManager all login/logout
        /// functions should be done through the AccountManager
        /// </summary>
        /// <param name="account"></param>
        /// <returns></returns>
        Task PersistCurrentAccountAsync(Account account);

        /// <summary>
        /// Delete the specified user's credentials from secure storage
        /// 
        /// NOTE This should only be called by the AccountManager all login/logout
        /// functions should be done through the AccountManager
        /// </summary>
        /// <param name="userName"></param>
        /// <param name="userId"></param>
        void DeletePersistedAccount(string userName, string userId);

        /// <summary>
        /// Delete all persisted accounts
        /// 
        /// NOTE This should only be called by the AccountManager all login/logout
        /// functions should be done through the AccountManager
        /// </summary>
        void DeleteAllPersistedAccounts();

        /// <summary>
        /// Retrieves all accounts stored in secure storage
        /// </summary>
        /// <returns></returns>
        Dictionary<string, Account> RetrieveAllPersistedAccounts();

        /// <summary>
        /// Retrieves the currently logged in user from secure storage
        /// </summary>
        /// <returns></returns>
        Account RetrieveCurrentAccount();

        /// <summary>
        /// Saves the pin timer
        /// </summary>
        void SavePinTimer();

        /// <summary>
        /// Clear the pincode timer
        /// </summary>
        void WipePincode();
    }
}