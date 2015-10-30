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
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using Salesforce.SDK.SmartStore.Store;
using Salesforce.SDK.SmartSync.Util;

namespace Salesforce.SDK.SmartSync.Model
{
    public class SalesforceObjectTypeLayout
    {
        public SalesforceObjectTypeLayout(string objType, JObject rawData)
        {
            if (rawData == null)
            {
                throw new SmartStoreException("rawData parameter cannot be null");
            }
            ObjectType = objType;
            RawData = rawData;
            ParseFields();
        }

        public int Limit { private set; get; }
        public string ObjectType { private set; get; }
        public JObject RawData { private set; get; }
        public List<SalesforceObjectLayoutColumn> Columns { private set; get; }

        private void ParseFields()
        {
            Limit = RawData.ExtractValue<int>(Constants.LayoutLimitsField);
            var searchColumns = RawData.ExtractValue<JArray>(Constants.LayoutColumnsField);
            if (searchColumns != null)
            {
                for (int i = 0, max = searchColumns.Count; i < max; i++)
                {
                    var columnData = searchColumns[i].Value<JObject>();
                    if (columnData != null)
                    {
                        Columns.Add(new SalesforceObjectLayoutColumn(columnData));
                    }
                }
            }
        }

        public override string ToString()
        {
            return String.Format("objectType: [{0}], limit: [{1}], rawData: [{2}]", ObjectType, Limit, RawData);
        }

        public override bool Equals(object obj)
        {
            if (obj == null || !(obj is SalesforceObjectTypeLayout))
            {
                return false;
            }
            var salesforceObject = (SalesforceObjectTypeLayout) obj;
            if (ObjectType == null || !ObjectType.Equals(salesforceObject.ObjectType))
            {
                return false;
            }
            return CompareColumns(salesforceObject);
        }

        public override int GetHashCode()
        {
            return ObjectType.GetHashCode();
        }

        private bool CompareColumns(SalesforceObjectTypeLayout obj)
        {
            if (obj == null)
            {
                return false;
            }
            List<SalesforceObjectLayoutColumn> objColumns = obj.Columns;
            if ((objColumns == null || objColumns.Count == 0)
                && (Columns == null || Columns.Count == 0))
            {
                return true;
            }
            if (objColumns != null)
            {
                int objColumnSize = objColumns.Count;
                if (objColumnSize != Columns.Count)
                {
                    return false;
                }
            }
            return objColumns.All(objColumn => Columns.Contains(objColumn));
        }
    }
}