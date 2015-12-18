﻿/*
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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Salesforce.SDK.Hybrid.SmartSync.Manager
{
    public sealed class SOSLBuilder
    {
        private static SDK.SmartSync.Manager.SOSLBuilder _soslBuilder;

        public static SOSLBuilder GetInstanceWithSearchTerm(string searchTerm)
        {
            _soslBuilder = SDK.SmartSync.Manager.SOSLBuilder.GetInstanceWithSearchTerm(searchTerm);
            var nativeBuilder = _soslBuilder;
            var hybridBuilder = JsonConvert.SerializeObject(nativeBuilder);
            return JsonConvert.DeserializeObject<SOSLBuilder>(hybridBuilder);
        }

        public SOSLBuilder SearchGroup(string searchGroup)
        {
            var nativeBuilder = _soslBuilder.SearchGroup(searchGroup);
            var hybridBuilder = JsonConvert.SerializeObject(nativeBuilder);
            return JsonConvert.DeserializeObject<SOSLBuilder>(hybridBuilder); 
        }

        public SOSLBuilder Returning(SOSLReturningBuilder returningSpec)
        {
            var returningSpecString = JsonConvert.SerializeObject(returningSpec);
            var nativeBuilder =
                _soslBuilder.Returning(
                    JsonConvert.DeserializeObject<SDK.SmartSync.Manager.SOSLReturningBuilder>(returningSpecString));
            var hybridBuilder = JsonConvert.SerializeObject(nativeBuilder);
            return JsonConvert.DeserializeObject<SOSLBuilder>(hybridBuilder);
        }

        public SOSLBuilder DivisionFilter(string filter)
        {
            var nativeBuilder = _soslBuilder.DivisionFilter(filter);
            var hybridBuilder = JsonConvert.SerializeObject(nativeBuilder);
            return JsonConvert.DeserializeObject<SOSLBuilder>(hybridBuilder);
        }

        public SOSLBuilder DataCategory(string dataCategory)
        {
            var nativeBuilder = _soslBuilder.DataCategory(dataCategory);
            var hybridBuilder = JsonConvert.SerializeObject(nativeBuilder);
            return JsonConvert.DeserializeObject<SOSLBuilder>(hybridBuilder);
        }

        public SOSLBuilder Limit(int limit)
        {
            var nativeBuilder = _soslBuilder.Limit(limit);
            var hybridBuilder = JsonConvert.SerializeObject(nativeBuilder);
            return JsonConvert.DeserializeObject<SOSLBuilder>(hybridBuilder);
        }

        public string BuildAndEncode()
        {
            return _soslBuilder.BuildAndEncode();
        }

        public string BuildAndEncodeWithPath(string path)
        {
            return _soslBuilder.BuildAndEncodeWithPath(path);
        }

        public string BuildWithPath(string path)
        {
            return _soslBuilder.BuildWithPath(path);
        }

        public string Build()
        {
            return _soslBuilder.Build();
        }
    }
}
