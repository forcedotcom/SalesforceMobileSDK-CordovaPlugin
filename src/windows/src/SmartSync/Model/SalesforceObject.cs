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
    public class SalesforceObject
    {
        public SalesforceObject(JObject rawData)
        {
            if (rawData == null)
            {
                throw new SmartStoreException("rawData parameter cannot be null");
            }
            var data = rawData.ExtractValue<string>(Constants.Id);
            if (data == null || String.IsNullOrWhiteSpace(data))
            {
                ObjectId = rawData.ExtractValue<string>(Constants.Id.ToLower());
                ObjectType = rawData.ExtractValue<string>(Constants.Type.ToLower());
                Name = rawData.ExtractValue<string>(Constants.Name.ToLower());
            }
            else
            {
                ObjectId = data;
                Name = rawData.ExtractValue<string>(Constants.Name);
                var attributes = rawData.ExtractValue<JObject>(Constants.Attributes);
                if (attributes != null)
                {
                    ObjectType = attributes.ExtractValue<string>(Constants.Type.ToLower());
                    if (String.IsNullOrWhiteSpace(ObjectType) || Constants.RecentlyViewed.Equals(ObjectType) ||
                        Constants.NullString.Equals(ObjectType))
                    {
                        ObjectType = rawData.ExtractValue<string>(Constants.Type);
                    }
                }
            }
            RawData = rawData;
            if (ObjectId != null)
            {
                _hashcode = ObjectId.GetHashCode();
            }
            _hashcode ^= rawData.GetHashCode() + _hashcode * 37;
        }

        public string ObjectType { set; get; }
        public string Name { set; get; }
        public string ObjectId { set; get; }
        public JObject RawData { private set; get; } 
        private readonly int _hashcode;

        public override string ToString()
        {
            return String.Format("name: [{0}], objectId: [{1}], type: [{2}]", Name, ObjectId, ObjectType);
        }

        public override bool Equals(object obj)
        {
            if (obj == null || !(obj is SalesforceObject))
            {
                return false;
            }
            var salesforceObject = obj as SalesforceObject;
            if (ObjectId == null || !ObjectId.Equals(salesforceObject.ObjectId))
            {
                return false;
            }
            if (Name == null  || !Name.Equals(salesforceObject.Name))
            {
                return false;
            }
            if (ObjectType == null ||
                !ObjectType.Equals(salesforceObject.ObjectType))
            {
                return false;
            }
            return true;
        }

        public override int GetHashCode()
        {
            return _hashcode;
        }
    }
}