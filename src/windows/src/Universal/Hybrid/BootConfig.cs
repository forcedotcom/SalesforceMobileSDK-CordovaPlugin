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
using System.Threading.Tasks;
using Newtonsoft.Json;


namespace Salesforce.SDK.Hybrid
{
    /// <summary>
    ///     Object representing application configuration (read from www/bootconfig.json)
    /// </summary>
    public class BootConfig
    {
        private const String BootConfigJson = "www/bootconfig.json";
        private static BootConfig _instance;

        /// <summary>
        ///     OAuth client id
        /// </summary>
        [JsonProperty(PropertyName = "remoteAccessConsumerKey")]
        public string ClientId { get; set; }

        /// <summary>
        ///     Oauth callback url
        /// </summary>
        [JsonProperty(PropertyName = "oauthRedirectURI")]
        public string CallbackURL { get; set; }

        /// <summary>
        ///     OAuth scopes
        /// </summary>
        [JsonProperty(PropertyName = "oauthScopes")]
        public string[] Scopes { get; set; }

        /// <summary>
        ///     True for hybrid local application (meaning the html/js/css is bundled in the application)
        ///     False for hybrid remote application (menaing the html/js/css is served from a server)
        /// </summary>
        [JsonProperty(PropertyName = "isLocal")]
        public bool IsLocal { get; set; }

        /// <summary>
        ///     Start page
        /// </summary>
        [JsonProperty(PropertyName = "startPage")]
        public string StartPage { get; set; }

        /// <summary>
        ///     Error page
        /// </summary>
        [JsonProperty(PropertyName = "errorPage")]
        public string ErrorPage { get; set; }

        /// <summary>
        ///     When true, authentication is attempted when application first load
        /// </summary>
        [JsonProperty(PropertyName = "shouldAuthenticate")]
        public bool ShouldAuthenticate { get; set; }

        /// <summary>
        ///     When true and offline, application tries to load from cache
        /// </summary>
        [JsonProperty(PropertyName = "attemptOfflineLoad")]
        public bool AttemptOfflineLoad { get; set; }


        // Return  the singleton instance (build it the first time it is called)
        public static async Task<BootConfig> GetBootConfig()
        {
            if (_instance == null)
            {
                String configStr = await Extensions.Utilities.ReadFileFromApplicationAsync(BootConfigJson);
                _instance = JsonConvert.DeserializeObject<BootConfig>(configStr);
            }
            return _instance;
        }
    }
}