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
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Salesforce.SDK.Core;
using Salesforce.SDK.Exceptions;
using Salesforce.SDK.Net;
using Salesforce.SDK.Rest;
using Salesforce.SDK.Logging;

namespace Salesforce.SDK.Auth
{
    /// <summary>
    ///     object representing conncted application oauth configuration (login host, client id, callback url, oauth scopes)
    /// </summary>
    public class LoginOptions
    {
        public static readonly string DefaultPhoneDisplayType = "touch";
        public static readonly string DefaultStoreDisplayType = "page";
        public static readonly string DefaultDisplayType = DefaultPhoneDisplayType;

        public LoginOptions()
        {
            
        }
        /// <summary>
        ///     Constructor for LoginOptions
        /// </summary>
        /// <param name="loginUrl"></param>
        /// <param name="clientId"></param>
        /// <param name="displayType"></param>
        /// <param name="scopes"></param>
        public LoginOptions(string loginUrl, string clientId, string callbackUrl, string[] scopes)
            : this(loginUrl, clientId, callbackUrl, DefaultDisplayType, scopes)
        {
        }

        /// <summary>
        ///     Constructor for LoginOptions
        /// </summary>
        /// <param name="loginUrl"></param>
        /// <param name="clientId"></param>
        /// <param name="callbackUrl"></param>
        /// <param name="scopes"></param>
        public LoginOptions(string loginUrl, string clientId, string callbackUrl, string displayType, string[] scopes)
        {
            LoginUrl = loginUrl;
            ClientId = clientId;
            CallbackUrl = callbackUrl;
            Scopes = scopes;
            DisplayType = displayType;
        }

        public string LoginUrl { get; set; }
        public string ClientId { get; set; }
        public string CallbackUrl { get; set; }
        public string DisplayType { get; set; }
        public string[] Scopes { get; set; }
    }

    /// <summary>
    ///     object representing the connected application mobile policy set by administrator
    /// </summary>
    public class MobilePolicy
    {
        /// <summary>
        ///     Pin length required
        /// </summary>
        [JsonProperty(PropertyName = "pin_length")]
        public int PinLength { get; set; }

        /// <summary>
        ///     Inactivite time after which the user should be prompted to enter her pin
        /// </summary>
        [JsonProperty(PropertyName = "screen_lock")]
        public int ScreenLockTimeout { get; set; }

        public string PincodeHash { get; set; }
    }

    /// <summary>
    ///     object representing response from identity service
    /// </summary>
    public class IdentityResponse
    {
        /// <summary>
        ///     URL for identity service
        /// </summary>
        [JsonProperty(PropertyName = "id")]
        public string IdentityUrl { get; set; }

        /// <summary>
        ///     Salesforce user id of authenticated user
        /// </summary>
        [JsonProperty(PropertyName = "user_id")]
        public string UserId { get; set; }

        /// <summary>
        ///     Salesforce organization id of authenticated user
        /// </summary>
        [JsonProperty(PropertyName = "organization_id")]
        public string OrganizationId { get; set; }

        /// <summary>
        ///     Salesforce username of authenticated user
        /// </summary>
        [JsonProperty(PropertyName = "username")]
        public string UserName { get; set; }

        /// <summary>
        ///     Mobile policy for connected application set by administrator
        /// </summary>
        [JsonProperty(PropertyName = "mobile_policy")]
        public MobilePolicy MobilePolicy { get; set; }

    }

    /// <summary>
    ///     object representing response from oauth service (during initial login flow or subsequent refresh flows)
    /// </summary>
    public class AuthResponse
    {
        /// <summary>
        ///     Auth scopes as a string array
        /// </summary>
        public string[] Scopes;

        /// <summary>
        ///     URL for identity service
        /// </summary>
        [JsonProperty(PropertyName = "id")]
        public string IdentityUrl { get; set; }

        /// <summary>
        ///     Instance URL
        /// </summary>
        [JsonProperty(PropertyName = "instance_url")]
        public string InstanceUrl { get; set; }

        /// <summary>
        ///     Date and time the oauth tokens were issued at
        /// </summary>
        [JsonProperty(PropertyName = "issued_at")]
        public string IssuedAt { get; set; }

        /// <summary>
        ///     Access token
        /// </summary>
        [JsonProperty(PropertyName = "access_token")]
        public string AccessToken { get; set; }

        /// <summary>
        ///     Refresh token
        /// </summary>
        [JsonProperty(PropertyName = "refresh_token")]
        public string RefreshToken { get; set; }

        /// <summary>
        ///     Auth scopes in a space delimited string
        /// </summary>
        [JsonProperty(PropertyName = "scope")]
        public string ScopesStr
        {
            set { Scopes = value.Split(' '); }
        }

        [JsonProperty(PropertyName = "sfdc_community_id")]
        public string CommunityId { get; set; }

        [JsonProperty(PropertyName = "sfdc_community_url")]
        public string CommunityUrl { get; set; }
    }

    /// <summary>
    ///     Utility class to interact with Salesforce oauth service
    /// </summary>
    public class OAuth2
    {
        // Refresh scope
        private const string RefreshScope = "refresh_token";

        // Authorization url
        private const string OauthAuthenticationPath = "/services/oauth2/authorize";

        private const string OauthAuthenticationQueryString =
            "?display={0}&response_type=token&client_id={1}&redirect_uri={2}&scope={3}";

        // Front door url
        private const string FrontDoorPath = "/secur/frontdoor.jsp";
        private const string FrontDoorQueryString = "?display={0}&sid={1}&retURL={2}";

        // Refresh url
        private const string OauthRefreshPath = "/services/oauth2/token";

        private const string OauthRefreshQueryString =
            "?grant_type=refresh_token&format=json&client_id={0}&refresh_token={1}";

        // Revoke url
        private const string OauthRevokePath = "/services/oauth2/revoke";
        private const string OauthRevokeQueryString = "?token={0}";

        private static ILoggingService LoggingService => SDKServiceLocator.Get<ILoggingService>();

        private static IAuthHelper AuthHelper => SDKServiceLocator.Get<IAuthHelper>();

        /// <summary>
        ///     Build the URL to the authorization web page for this login server
        ///     You need not provide refresh_token, as it is provided automatically
        /// </summary>
        /// <param name="loginOptions"></param>
        /// <returns>A URL to start the OAuth flow in a web browser/view.</returns>
        public static string ComputeAuthorizationUrl(LoginOptions loginOptions)
        {
            // Scope
            string scopeStr = string.Join(" ", loginOptions.Scopes.Concat(new[] {RefreshScope}).Distinct().ToArray());

            // Args
            string[] args = {loginOptions.DisplayType, loginOptions.ClientId, loginOptions.CallbackUrl, scopeStr};
            object[] urlEncodedArgs = args.Select(WebUtility.UrlEncode).ToArray();

            // Authorization url
            string authorizationUrl =
                string.Format(loginOptions.LoginUrl + OauthAuthenticationPath + OauthAuthenticationQueryString,
                    urlEncodedArgs);

            return authorizationUrl;
        }

        /// <summary>
        ///     Build the front-doored URL for a given URL with the default displaytype
        /// </summary>
        /// <param name="instanceUrl"></param>
        /// <param name="accessToken"></param>
        /// <param name="url"></param>
        /// <returns></returns>
        public static string ComputeFrontDoorUrl(string instanceUrl, string accessToken, string url)
        {
            return ComputeFrontDoorUrl(instanceUrl, LoginOptions.DefaultDisplayType, accessToken, url);
        }

        /// <summary>
        ///     Build the front-doored URL for a given URL
        /// </summary>
        /// <param name="instanceUrl"></param>
        /// <param name="displayType"></param>
        /// <param name="accessToken"></param>
        /// <param name="url"></param>
        /// <returns></returns>
        public static string ComputeFrontDoorUrl(string instanceUrl, string displayType, string accessToken, string url)
        {
            // Args
            string[] args = {displayType, accessToken, url};
            string[] urlEncodedArgs = args.Select(Uri.EscapeDataString).ToArray();

            // Authorization url
            string frontDoorUrl = string.Format(instanceUrl + FrontDoorPath + FrontDoorQueryString, urlEncodedArgs);

            return frontDoorUrl;
        }

        /// <summary>
        ///     Async method for refreshing the token, persisting the data in the encrypted settings and returning the updated
        ///     account
        ///     with the new access token.
        /// </summary>
        /// <param name="account"></param>
        /// <returns>Boolean based on if the refresh auth token succeeded or not</returns>
        public static async Task<Account> RefreshAuthTokenAsync(Account account)
        {
            LoggingService.Log("Atempting to refresh auth token", LoggingLevel.Verbose);

            if (account == null)
            {
                return null;
            }

            try
            {
                var loginOptions = account.GetLoginOptions();

                // args
                var argsStr = string.Format(OauthRefreshQueryString, loginOptions.ClientId, account.RefreshToken);

                // Refresh url
                var refreshUrl = loginOptions.LoginUrl + OauthRefreshPath;

                // Post
                var call = HttpCall.CreatePost(refreshUrl, argsStr);

                var response = await call.ExecuteAndDeserializeAsync<AuthResponse>();

                account.AccessToken = response.AccessToken;
                account.IdentityUrl = response.IdentityUrl;

                await SDKServiceLocator.Get<IAuthHelper>().PersistCurrentAccountAsync(account);
            }
            catch (DeviceOfflineException ex)
            {
                LoggingService.Log("Failed to refresh the token because we were offline", LoggingLevel.Warning);
                LoggingService.Log(ex, LoggingLevel.Warning);
                throw;
            }
            catch (WebException ex)
            {
                LoggingService.Log("Exception occurred when refreshing token:", LoggingLevel.Critical);
                LoggingService.Log(ex, LoggingLevel.Critical);
                Debug.WriteLine("Error refreshing token");
                throw new OAuthException(ex.Message, ex);
            }
            catch (Exception ex)
            {
                LoggingService.Log("Exception occurred when refreshing token:", LoggingLevel.Critical);
                LoggingService.Log(ex, LoggingLevel.Critical);
                Debug.WriteLine("Error refreshing token");
                throw new OAuthException(ex.Message, ex);
            }

            return account;
        }


        /// <summary>
        ///     Async method to revoke the user's refresh token (i.e. do a server-side logout for the authenticated user)
        /// </summary>
        /// <param name="loginOptions"></param>
        /// <param name="refreshToken"></param>
        /// <returns>true if successful</returns>
        public static async Task<bool> RevokeAuthTokenAsync(LoginOptions loginOptions, string refreshToken)
        {
            // Args
            string argsStr = string.Format(OauthRevokeQueryString, new[] {WebUtility.UrlEncode(refreshToken)});

            // Revoke url
            string revokeUrl = loginOptions.LoginUrl + OauthRevokePath;

            // Post
            HttpCall c = HttpCall.CreatePost(revokeUrl, argsStr);

            // ExecuteAsync post
            HttpCall result = await c.ExecuteAsync().ConfigureAwait(false);

            LoggingService.Log($"result.StatusCode = {result.StatusCode}", LoggingLevel.Verbose);

            return result.StatusCode == HttpStatusCode.OK;
        }

        public static void RefreshCookies()
        {
            AuthHelper.RefreshCookies();
        }

        public static void ClearCookies(LoginOptions loginOptions)
        {
            AuthHelper.ClearCookiesAsync(loginOptions);
        }

        /// <summary>
        ///     Async method to call the identity service (to get the mobile policy among other pieces of information)
        /// </summary>
        /// <param name="idUrl"></param>
        /// <param name="accessToken"></param>
        /// <returns></returns>
        public static async Task<IdentityResponse> CallIdentityServiceAsync(string idUrl, string accessToken)
        {
            LoggingService.Log("Calling identity service", LoggingLevel.Verbose);

            // Auth header
            var headers = new HttpCallHeaders(accessToken, new Dictionary<string, string>());
            // Get
            HttpCall c = HttpCall.CreateGet(headers, idUrl);

            // ExecuteAsync get
            return await c.ExecuteAndDeserializeAsync<IdentityResponse>();
        }

        public static async Task<IdentityResponse> CallIdentityServiceAsync(string idUrl, IRestClient client)
        {
            var request = new RestRequest(HttpMethod.Get, new Uri(idUrl).AbsolutePath);
            var response = await client.SendAsync(request);
            if (response.Success)
            {
                LoggingService.Log("success", LoggingLevel.Verbose);
                return JsonConvert.DeserializeObject<IdentityResponse>(response.AsString);
            }
            else
            {
                LoggingService.Log("Error occured:", LoggingLevel.Critical);
                LoggingService.Log(response.Error, LoggingLevel.Critical);
            }
            throw response.Error;
        }

        /// <summary>
        ///     Extract the authentication data from the fragment portion of a URL
        /// </summary>
        /// <param name="fragmentstring"></param>
        /// <returns></returns>
        public static AuthResponse ParseFragment(string fragmentstring)
        {
            var res = new AuthResponse();

            string[] parameters = fragmentstring.Split('&');
            foreach (string parameter in parameters)
            {
                string[] parts = parameter.Split('=');
                string name = Uri.UnescapeDataString(parts[0]);
                string value = parts.Length > 1 ? Uri.UnescapeDataString(parts[1]) : "";

                switch (name)
                {
                    case "id":
                        res.IdentityUrl = value;
                        break;
                    case "instance_url":
                        res.InstanceUrl = value;
                        break;
                    case "access_token":
                        res.AccessToken = value;
                        break;
                    case "refresh_token":
                        res.RefreshToken = value;
                        break;
                    case "issued_at":
                        res.IssuedAt = value;
                        break;
                    case "scope":
                        res.Scopes = value.Split('+');
                        break;
                    case "sfdc_community_id":
                        res.CommunityId = value;
                        break;
                    case "sfdc_community_url":
                        res.CommunityUrl = value;
                        break;
                    default:
                        Debug.WriteLine("Parameter not recognized {0}", name);
                        break;
                }
            }
            return res;
        }
    }
}