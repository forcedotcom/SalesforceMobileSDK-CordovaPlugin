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
using System.Threading.Tasks;
using Salesforce.SDK.Auth;
using Salesforce.SDK.Core;
using Salesforce.SDK.Exceptions;
using Salesforce.SDK.Logging;

namespace Salesforce.SDK.Rest
{
    /// <summary>
    ///     Factory class for RestClient
    ///     Credentials are stored via the AccountManager
    ///     When no account is found, it kicks off the login flow and creates a new Account
    /// </summary>
    public class ClientManager
    {
        private static ILoggingService LoggingService => SDKServiceLocator.Get<ILoggingService>();
        private static IAuthHelper AuthHelper => SDKServiceLocator.Get<IAuthHelper>();
        /// <summary>
        ///     Logs currently authenticated user out by deleting locally persisted credentials and invoking the server to revoke
        ///     the user auth tokens
        /// </summary>
        /// <returns>true if server logout was successful</returns>
        public async Task<bool> Logout()
        {
            Account account = AccountManager.GetAccount();
            if (account != null)
            {
                LoginOptions options = account.GetLoginOptions();
                AccountManager.DeleteAccount();
                await AuthHelper.ClearCookiesAsync(options);
                bool loggedOut = await OAuth2.RevokeAuthTokenAsync(options, account.RefreshToken);
                if (loggedOut)
                {
                    GetRestClient();
                }
                return loggedOut;
            }
            GetRestClient();
            return await Task.Factory.StartNew(() => true);
        }

        /// <summary>
        ///     Returns a RestClient if user is already authenticated or null
        /// </summary>
        /// <returns></returns>
        public IRestClient PeekRestClient()
        {
            Account account = AccountManager.GetAccount();
            if (account != null)
            {
                return new RestClient(account.InstanceUrl, account.AccessToken,
                    async () =>
                    {
                        account = AccountManager.GetAccount();

                        try
                        {
                            account = await OAuth2.RefreshAuthTokenAsync(account);
                        }
                        catch (DeviceOfflineException)
                        {
                            LoggingService.Log("Could not refresh the token because device is offline", LoggingLevel.Warning);
                            return null;
                        }
                        catch (OAuthException ex)
                        {
                            LoggingService.Log("Failed to refresh the token, logging the user out", LoggingLevel.Warning);
                            LoggingService.Log(ex, LoggingLevel.Error);

                            // we failed to refresh the token, we have to log the user out
                            await Logout();
                        }

                        return account.AccessToken;
                    }
                    );
            }
            return null;
        }

        /// <summary>
        ///     Returns a RestClient if user is already authenticated or otherwise kicks off a login flow
        /// </summary>
        /// <returns></returns>
        public IRestClient GetRestClient()
        {
            var restClient = PeekRestClient();
            if (restClient == null)
            {
                try
                {
                    AuthHelper.StartLoginFlowAsync();
                }
                catch (InvalidOperationException)
                {
                    SDKServiceLocator.Get<ILoggingService>().Log("Platform doesn't support native login flow", LoggingLevel.Information);
                }
               
            }
            return restClient;
        }

        public IRestClient GetUnAuthenticatedRestClient(string instanceUrl)
        {
            return new RestClient(instanceUrl);
        }
    }
}