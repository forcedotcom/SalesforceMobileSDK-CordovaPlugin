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
using System.Text;
using Salesforce.SDK.SmartSync.Util;

namespace Salesforce.SDK.SmartSync.Manager
{
    public class SOSLReturningBuilder
    {
        private readonly Dictionary<string, object> _properties;

        private SOSLReturningBuilder()
        {
            _properties = new Dictionary<string, object>();
        }

        /// <summary>
        ///     Returns an instance of this class based on the object name.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public static SOSLReturningBuilder GetInstanceWithObjectName(string name)
        {
            var instance = new SOSLReturningBuilder();
            instance.ObjectName(name);
            instance.Limit(0);
            return instance;
        }

        /// <summary>
        ///     Adds the 'fields' clause.
        /// </summary>
        /// <param name="fields"></param>
        /// <returns></returns>
        public SOSLReturningBuilder Fields(string fields)
        {
            _properties["fields"] = fields;
            return this;
        }

        /// <summary>
        ///     Adds the 'where' clause.
        /// </summary>
        /// <param name="where"></param>
        /// <returns></returns>
        public SOSLReturningBuilder Where(string where)
        {
            _properties["where"] = where;
            return this;
        }

        /// <summary>
        ///     Adds the 'orderBy' clause.
        /// </summary>
        /// <param name="orderBy"></param>
        /// <returns></returns>
        public SOSLReturningBuilder OrderBy(string orderBy)
        {
            _properties["orderBy"] = orderBy;
            return this;
        }

        /// <summary>
        ///     Adds the 'objectName' clause.
        /// </summary>
        /// <param name="objectName"></param>
        /// <returns></returns>
        public SOSLReturningBuilder ObjectName(string objectName)
        {
            _properties["objectName"] = objectName;
            return this;
        }

        /// <summary>
        ///     Adds the 'limit' clause.
        /// </summary>
        /// <param name="limit"></param>
        /// <returns></returns>
        public SOSLReturningBuilder Limit(int limit)
        {
            _properties["limit"] = limit;
            return this;
        }

        /// <summary>
        ///     Adds the 'withNetwork' clause.
        /// </summary>
        /// <param name="withNetwork"></param>
        /// <returns></returns>
        public SOSLReturningBuilder WithNetwork(string withNetwork)
        {
            _properties["withNetwork"] = withNetwork;
            return this;
        }

        /// <summary>
        ///     Builds the query.
        /// </summary>
        /// <returns></returns>
        public string Build()
        {
            var query = new StringBuilder();
            var objectName = _properties.Get<string>("objectName");
            if (String.IsNullOrWhiteSpace(objectName))
            {
                return null;
            }
            query.Append(" ");
            query.Append(objectName);
            var fields = _properties.Get<string>("fields");
            if (!String.IsNullOrWhiteSpace(objectName))
            {
                query.Append(String.Format("({0}", fields));
                var where = _properties.Get<string>("where");
                if (!String.IsNullOrWhiteSpace(where))
                {
                    query.Append(" where ");
                    query.Append(where);
                }
                var orderBy = _properties.Get<string>("orderBy");
                if (!String.IsNullOrWhiteSpace(orderBy))
                {
                    query.Append(" order by ");
                    query.Append(orderBy);
                }
                var withNetwork = _properties.Get<string>("withNetwork");
                if (!String.IsNullOrWhiteSpace(withNetwork))
                {
                    query.Append(" with network = ");
                    query.Append(withNetwork);
                }
                var limit = _properties.Get<int>("limit");
                if (limit > 0)
                {
                    query.Append(" limit ");
                    query.Append(String.Format("{0}", limit));
                }
                query.Append(")");
            }
            return query.ToString();
        }
    }
}