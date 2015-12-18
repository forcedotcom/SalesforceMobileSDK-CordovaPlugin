/*
 * Copyright (c) 2014, salesforce.com, inc.
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

using Newtonsoft.Json;

namespace Salesforce.SDK.Auth
{
    internal class PincodeOptions
    {
        public enum PincodeScreen
        {
            Create,
            Confirm,
            Locked
        }

        public PincodeOptions(PincodeScreen screen, Account user, string passcode)
        {
            Screen = screen;
            User = user;
            Passcode = passcode;
            Policy = User.Policy;
        }

        public Account User { get; private set; }
        public PincodeScreen Screen { get; private set; }
        public string Passcode { get; private set; }
        public MobilePolicy Policy { get; set; }

        /// <summary>
        ///     Serialize PincodeOptions object as a JSON string
        /// </summary>
        /// <param name="pincodeOptions"></param>
        /// <returns></returns>
        public static string ToJson(PincodeOptions pincodeOptions)
        {
            return JsonConvert.SerializeObject(pincodeOptions);
        }

        /// <summary>
        ///     Deserialize PincodeOptions from a JSON string
        /// </summary>
        /// <param name="pincodeOptionsJson"></param>
        /// <returns></returns>
        public static PincodeOptions FromJson(string pincodeOptionsJson)
        {
            return JsonConvert.DeserializeObject<PincodeOptions>(pincodeOptionsJson);
        }
    }
}