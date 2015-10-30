using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Salesforce.SDK.Hybrid.Auth
{
    public sealed class MobilePolicy
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
}
