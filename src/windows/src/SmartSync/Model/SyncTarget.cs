﻿/*
 * Copyright (c) 2014-2015, salesforce.com, inc.
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

using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Salesforce.SDK.SmartSync.Util;

namespace Salesforce.SDK.SmartSync.Model
{
    /// <summary>
    ///     Target for sync u i.e. set of objects to download from server
    /// </summary>
    public abstract class SyncTarget
    {
        public const string WindowsImpl = "windowsImpl";
        public const string WindowsImplType = "windowsImplType";
        public const string IdFieldNameStr = "idFieldName";
        public const string ModificationDateFieldNameStr = "modificationDateFieldName";

        protected SyncTarget()
        {
            IdFieldName = Constants.Id;
            ModificationDateFieldName = Constants.LastModifiedDate;
        }

        protected SyncTarget(JObject target)
        {
            IdFieldName = target != null ? target.ExtractValue<string>(IdFieldNameStr) : Constants.Id;
            ModificationDateFieldName = target != null ? target.ExtractValue<string>(ModificationDateFieldNameStr) : Constants.LastModifiedDate;
        }

        [JsonProperty]
        public string IdFieldName { get; }

        [JsonProperty]
        public string ModificationDateFieldName { get; }

        /// <summary>
        /// </summary>
        /// <returns>json representation of target</returns>
        public virtual JObject AsJson()
        {
            var target = new JObject();
            target[WindowsImpl] = GetType().GetTypeInfo().Assembly.FullName;
            target[WindowsImplType] = GetType().GetTypeInfo().FullName;
            target[IdFieldNameStr] = IdFieldName;
            target[ModificationDateFieldNameStr] = ModificationDateFieldName;

            return target;
        }

        public string GetId()
        {
            return IdFieldName;
        }

        public string GetModificationDate()
        {
            return ModificationDateFieldName;
        }
    }
}