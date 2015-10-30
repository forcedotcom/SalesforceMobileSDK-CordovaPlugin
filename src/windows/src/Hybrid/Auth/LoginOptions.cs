using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading.Tasks;

namespace Salesforce.SDK.Hybrid.Auth
{
    public sealed class LoginOptions
    {
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
        public LoginOptions(string loginUrl, string clientId, string callbackUrl, [ReadOnlyArray()] string[] scopes)
            : this(loginUrl, clientId, callbackUrl, SDK.Auth.LoginOptions.DefaultStoreDisplayType, scopes)
        {
        }

        /// <summary>
        ///     Constructor for LoginOptions
        /// </summary>
        /// <param name="loginUrl"></param>
        /// <param name="clientId"></param>
        /// <param name="callbackUrl"></param>
        /// <param name="scopes"></param>
        public LoginOptions(string loginUrl, string clientId, string callbackUrl, string displayType, [ReadOnlyArray()] string[] scopes)
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

        internal SDK.Auth.LoginOptions ConvertToSDKLoginOptions()
        {
            var options = JsonConvert.SerializeObject(this);
            return JsonConvert.DeserializeObject<SDK.Auth.LoginOptions>(options);
        }
    }
}
