using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading.Tasks;

namespace Salesforce.SDK.Hybrid.Auth
{
    public sealed class Account
    {
        private const string InternalCommunityId = "000000000000000000";

        /// <summary>
        ///     Constructor for Account
        ///     NB: the Account is not stored anywhere until we call PersistCurrentAccountAsync on the IAuthStorageHelper
        /// </summary>
        /// <param name="loginUrl"></param>
        /// <param name="clientId"></param>
        /// <param name="callbackUrl"></param>
        /// <param name="scopes"></param>
        /// <param name="instanceUrl"></param>
        /// <param name="identityUrl"></param>
        /// <param name="accessToken"></param>
        /// <param name="refreshToken"></param>
        public Account(string loginUrl, string clientId, string callbackUrl, [ReadOnlyArray()] string[] scopes, string instanceUrl,
            string identityUrl, string accessToken, string refreshToken)
        {
            LoginUrl = loginUrl;
            ClientId = clientId;
            CallbackUrl = callbackUrl;
            Scopes = scopes;
            InstanceUrl = instanceUrl;
            IdentityUrl = identityUrl;
            AccessToken = accessToken;
            RefreshToken = refreshToken;
        }

        public string LoginUrl { get; private set; }
        public string UserId { get; set; }
        public string UserName { get; set; }
        public string ClientId { get; private set; }
        public string CallbackUrl { get; private set; }
        public string[] Scopes { get; private set; }
        public string InstanceUrl { get; private set; }
        public string IdentityUrl { get; private set; }
        public string AccessToken { get; set; }
        public string RefreshToken { get; private set; }
        public string CommunityId { get; set; }
        public string CommunityUrl { get; set; }

        [JsonProperty]
        public MobilePolicy Policy { get; internal set; }


        /// <summary>
        ///     Serialize Account object as a JSON string
        /// </summary>
        /// <param name="account"></param>
        /// <returns></returns>
        public static string ToJson(Account account)
        {
            return JsonConvert.SerializeObject(account);
        }

        /// <summary>
        ///     Deserialize Account from a JSON string
        /// </summary>
        /// <param name="accountJson"></param>
        /// <returns></returns>
        public static Account FromJson(string accountJson)
        {
            return JsonConvert.DeserializeObject<Account>(accountJson);
        }

        internal SDK.Auth.Account ConvertToSDKAccount()
        {
            var account = JsonConvert.SerializeObject(this);
            return JsonConvert.DeserializeObject<SDK.Auth.Account>(account);
        }

        public sealed override string ToString()
        {
            return UserName;
        }

        public LoginOptions GetLoginOptions()
        {
            return new LoginOptions(LoginUrl, ClientId, CallbackUrl, SDK.Auth.LoginOptions.DefaultDisplayType, Scopes);
        }
    }
}
