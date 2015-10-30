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
    public class SalesforceObjectType
    {
        public SalesforceObjectType(JObject rawData)
        {
            if (rawData == null)
            {
                throw new SmartStoreException("rawData parameter cannot be null");
            }
            Name = rawData.ExtractValue<string>(Constants.NameField);
            KeyPrefix = rawData.ExtractValue<string>(Constants.KeyprefixField);
            Label = rawData.ExtractValue<string>(Constants.LabelField);
            LabelPlural = rawData.ExtractValue<string>(Constants.LabelpluralField);
            if (String.IsNullOrWhiteSpace(Label))
            {
                Label = Name;
            }
            if (String.IsNullOrWhiteSpace(LabelPlural))
            {
                LabelPlural = Label;
            }
            RawData = RawData;
            IsSearchable = !rawData.ExtractValue<bool>(Constants.HiddenField) &&
                           rawData.ExtractValue<bool>(Constants.SearchableField);
            IsLayoutable = !rawData.ExtractValue<bool>(Constants.HiddenField) &&
                           rawData.ExtractValue<bool>(Constants.LayoutableField);
            Fields = RawData.ExtractValue<JArray>(Constants.FieldsField);
            /*
            * Extracts a few flagship fields and sets them to instance variables
            * for easy retrieval.
            */
            if (Fields == null || Fields.Count <= 0) return;
            for (int i = 0, max = Fields.Count; i < max; i++)
            {
                var field = Fields[i].Value<JObject>();
                var nameFieldPresent = field.ExtractValue<bool>(Constants.NameFieldField);
                if (!nameFieldPresent) continue;
                /*
                * Some objects, such as 'Account', have more than one
                * name field, like 'Name', 'First Name', and 'Last Name'.
                * This check exists to ensure that we use the first
                * name field, which is the flagship name field, and
                * not the last one. If it is already set, we won't
                * overwrite it.
                */
                if (String.IsNullOrWhiteSpace(NameField) || Constants.NullString.Equals(NameField))
                {
                    NameField = field.ExtractValue<string>(Constants.NameField);
                }
                else
                {
                    // NameField is set, no need to continue.
                    break;
                }
            }
        }

        public string KeyPrefix { private set; get; }
        public string Name { private set; get; }
        public string Label { private set; get; }
        public string LabelPlural { private set; get; }
        public string NameField { private set; get; }
        public bool IsSearchable { private set; get; }
        public bool IsLayoutable { private set; get; }
        public JArray Fields { private set; get; }
        public JObject RawData { private set; get; }

        public override string ToString()
        {
            return String.Format("keyPrefix: [{0}], name: [{1}], label: [{2}], labelPlural: " +
                                 "[{3}], nameField: [{4}], rawData: [{5}]", KeyPrefix, Name, Label,
                LabelPlural, NameField, RawData);
        }

        public override bool Equals(object obj)
        {
            if (obj == null || !(obj is SalesforceObjectType))
            {
                return false;
            }
            var salesforceObject = (SalesforceObjectType) obj;
            if (Name == null || salesforceObject.Name == null || !Name.Equals(salesforceObject.Name))
            {
                return false;
            }
            if (KeyPrefix == null || salesforceObject.KeyPrefix == null || !KeyPrefix.Equals(salesforceObject.KeyPrefix))
            {
                return false;
            }
            if (Label == null || salesforceObject.Label == null || !Label.Equals(salesforceObject.Label))
            {
                return false;
            }
            if (LabelPlural == null || salesforceObject.LabelPlural == null ||
                !LabelPlural.Equals(salesforceObject.LabelPlural))
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