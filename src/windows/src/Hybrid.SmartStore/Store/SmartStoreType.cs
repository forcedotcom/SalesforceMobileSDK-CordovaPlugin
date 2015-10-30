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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Salesforce.SDK.Hybrid.SmartStore
{
    public sealed class SmartStoreType
    {
        private SDK.SmartStore.Store.SmartStoreType _smartStoreType;

        private const string SmartTypeInteger = "integer";
        private const string SmartTypeString = "string";
        private const string SmartTypeFloating = "floating";

        private static readonly SmartStoreType _smartInteger = new SmartStoreType(SmartTypeInteger);
        private static readonly SmartStoreType _smartString = new SmartStoreType(SmartTypeString);
        private static readonly SmartStoreType _smartFloating = new SmartStoreType(SmartTypeFloating);

        public string ColumnType {
            get
            {
                return _smartStoreType.ColumnType;
            }
            set
            {
                _smartStoreType = new SDK.SmartStore.Store.SmartStoreType(value);
            }
        }

        public static SmartStoreType SmartInteger
        {
            get { return _smartInteger; }
        }

        public static SmartStoreType SmartString
        {
            get { return _smartString; }
        }

        public static SmartStoreType SmartFloating
        {
            get { return _smartFloating; }
        }

        public SmartStoreType()
        {
            _smartStoreType = new SDK.SmartStore.Store.SmartStoreType(SmartTypeString);
        }

        public SmartStoreType(string columnType)
        {
            _smartStoreType = new SDK.SmartStore.Store.SmartStoreType(columnType);
        }
    }
}
