﻿/*
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
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Salesforce.SDK.Hybrid.SmartSync.Manager
{
    public sealed class SOSLReturningBuilder
    {
        private static SDK.SmartSync.Manager.SOSLReturningBuilder _soslReturningBuilder;

        public static SOSLReturningBuilder GetInstanceWithObjectName(string name)
        {
            _soslReturningBuilder = SDK.SmartSync.Manager.SOSLReturningBuilder.GetInstanceWithObjectName(name);
            var nativeBuilder = _soslReturningBuilder;
            var hybridBuilder = JsonConvert.SerializeObject(nativeBuilder);
            return JsonConvert.DeserializeObject<SOSLReturningBuilder>(hybridBuilder);
        }

        public SOSLReturningBuilder Fields(string fields)
        {
            var nativeBuilder = _soslReturningBuilder.Fields(fields);
            var hybridBuilder = JsonConvert.SerializeObject(nativeBuilder);
            return JsonConvert.DeserializeObject<SOSLReturningBuilder>(hybridBuilder); 
        }

        public SOSLReturningBuilder Where(string where)
        {
            var nativeBuilder = _soslReturningBuilder.Where(where);
            var hybridBuilder = JsonConvert.SerializeObject(nativeBuilder);
            return JsonConvert.DeserializeObject<SOSLReturningBuilder>(hybridBuilder);
        }

        public SOSLReturningBuilder OrderBy(string orderBy)
        {
            var nativeBuilder = _soslReturningBuilder.OrderBy(orderBy);
            var hybridBuilder = JsonConvert.SerializeObject(nativeBuilder);
            return JsonConvert.DeserializeObject<SOSLReturningBuilder>(hybridBuilder);
        }

        public SOSLReturningBuilder ObjectName(string objectName)
        {
            var nativeBuilder = _soslReturningBuilder.ObjectName(objectName);
            var hybridBuilder = JsonConvert.SerializeObject(nativeBuilder);
            return JsonConvert.DeserializeObject<SOSLReturningBuilder>(hybridBuilder);
        }

        public SOSLReturningBuilder Limit(int limit)
        {
            var nativeBuilder = _soslReturningBuilder.Limit(limit);
            var hybridBuilder = JsonConvert.SerializeObject(nativeBuilder);
            return JsonConvert.DeserializeObject<SOSLReturningBuilder>(hybridBuilder);
        }

        public SOSLReturningBuilder WithNetwork(string withNetwork)
        {
            var nativeBuilder = _soslReturningBuilder.WithNetwork(withNetwork);
            var hybridBuilder = JsonConvert.SerializeObject(nativeBuilder);
            return JsonConvert.DeserializeObject<SOSLReturningBuilder>(hybridBuilder);
        }

        public string Build()
        {
            return _soslReturningBuilder.Build();
        }
    }
}
