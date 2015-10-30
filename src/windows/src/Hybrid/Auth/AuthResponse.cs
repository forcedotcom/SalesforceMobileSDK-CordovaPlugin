using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Salesforce.SDK.Hybrid.Auth
{
    public sealed class AuthResponse
    {
        /// <summary>
        ///     Auth scopes as a string array
        /// </summary>
        public string[] Scopes { get; set; }

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
            get { return String.Join(" ", Scopes); }
        }

        [JsonProperty(PropertyName = "sfdc_community_id")]
        public string CommunityId { get; set; }

        [JsonProperty(PropertyName = "sfdc_community_url")]
        public string CommunityUrl { get; set; }

        internal SDK.Auth.AuthResponse ConvertToSDKResponse()
        {
            var json = JsonConvert.SerializeObject(this);
            return JsonConvert.DeserializeObject<SDK.Auth.AuthResponse>(json);
        }
    }
}
