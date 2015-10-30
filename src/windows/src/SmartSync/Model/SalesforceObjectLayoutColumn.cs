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

using System;
using Newtonsoft.Json.Linq;
using Salesforce.SDK.SmartStore.Store;
using Salesforce.SDK.SmartSync.Util;

namespace Salesforce.SDK.SmartSync.Model
{
    public class SalesforceObjectLayoutColumn
    {
        public SalesforceObjectLayoutColumn(JObject rawData)
        {
            if (rawData == null)
            {
                throw new SmartStoreException("rawData parameter cannot be null");
            }
            Name = rawData.ExtractValue<string>(Constants.LayoutNameField);
            Field = rawData.ExtractValue<string>(Constants.LayoutFieldField);
            Format = rawData.ExtractValue<string>(Constants.LayoutFormatField);
            Label = rawData.ExtractValue<string>(Constants.LayoutLabelField);
            RawData = RawData;
        }

        public string Name { private set; get; }
        public string Field { private set; get; }
        public string Format { private set; get; }
        public string Label { private set; get; }
        public JObject RawData { private set; get; }

        public override string ToString()
        {
            return String.Format("name: [{0}], field: [{1}], format: [{2}], label: [{3}], rawData: [{4}]", Name, Field,
                Format, Label,
                RawData);
        }

        public override bool Equals(object obj)
        {
            if (obj == null || !(obj is SalesforceObjectLayoutColumn))
            {
                return false;
            }
            var salesforceObject = (SalesforceObjectLayoutColumn) obj;
            if (Name == null || salesforceObject.Name == null ||
                !Name.Equals(salesforceObject.Name))
            {
                return false;
            }
            if (Field == null || salesforceObject.Field == null ||
                !Field.Equals(salesforceObject.Field))
            {
                return false;
            }
            if (Format == null || salesforceObject.Format == null ||
                !Format.Equals(salesforceObject.Format))
            {
                return false;
            }
            if (Label == null || salesforceObject.Label == null ||
                !Label.Equals(salesforceObject.Label))
            {
                return false;
            }
            return true;
        }

        public override int GetHashCode()
        {
            int result = Name.GetHashCode();
            result ^= RawData.GetHashCode() + result*37;
            return result;
        }
    }
}