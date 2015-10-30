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
    public class SOSLBuilder
    {
        private readonly Dictionary<string, object> _properties;
        private readonly List<SOSLReturningBuilder> _returning;

        private SOSLBuilder()
        {
            _properties = new Dictionary<string, object>();
            _returning = new List<SOSLReturningBuilder>();
        }

        /// <summary>
        ///     Returns an instance of this class based on the search term passed in.
        /// </summary>
        /// <param name="searchTerm"></param>
        /// <returns></returns>
        public static SOSLBuilder GetInstanceWithSearchTerm(string searchTerm)
        {
            var instance = new SOSLBuilder();
            instance.SearchTerm(searchTerm);
            instance.Limit(0);
            return instance;
        }

        /// <summary>
        ///     Adds the 'searchGroup' clause.
        /// </summary>
        /// <param name="searchGroup"></param>
        /// <returns></returns>
        public SOSLBuilder SearchGroup(string searchGroup)
        {
            _properties["searchGroup"] = searchGroup;
            return this;
        }

        /// <summary>
        ///     Adds the 'returningSpec' clause.
        /// </summary>
        /// <param name="returningSpec"></param>
        /// <returns></returns>
        public SOSLBuilder Returning(SOSLReturningBuilder returningSpec)
        {
            _returning.Add(returningSpec);
            return this;
        }

        /// <summary>
        ///     Adds the 'divisionFilter' clause.
        /// </summary>
        /// <param name="filter"></param>
        /// <returns></returns>
        public SOSLBuilder DivisionFilter(string filter)
        {
            _properties["divisionFilter"] = filter;
            return this;
        }

        /// <summary>
        ///     Adds the 'dataCategory' clause.
        /// </summary>
        /// <param name="dataCategory"></param>
        /// <returns></returns>
        public SOSLBuilder DataCategory(string dataCategory)
        {
            _properties["dataCategory"] = dataCategory;
            return this;
        }

        /// <summary>
        ///     Adds the 'limit' clause.
        /// </summary>
        /// <param name="limit"></param>
        /// <returns></returns>
        public SOSLBuilder Limit(int limit)
        {
            _properties["limit"] = limit;
            return this;
        }

        private SOSLBuilder SearchTerm(string searchTerm)
        {
            string searchValue = searchTerm ?? "";

            // Escapes special characters from search term.
            if (!String.IsNullOrWhiteSpace(searchTerm))
            {
                searchValue = searchValue.Replace("\\", "\\\\");
                searchValue = searchValue.Replace("+", "\\+");
                searchValue = searchValue.Replace("^", "\\^");
                searchValue = searchValue.Replace("~", "\\~");
                searchValue = searchValue.Replace("'", "\\'");
                searchValue = searchValue.Replace("-", "\\-");
                searchValue = searchValue.Replace("[", "\\[");
                searchValue = searchValue.Replace("]", "\\]");
                searchValue = searchValue.Replace("{", "\\{");
                searchValue = searchValue.Replace("}", "\\}");
                searchValue = searchValue.Replace("(", "\\(");
                searchValue = searchValue.Replace(")", "\\)");
                searchValue = searchValue.Replace("&", "\\&");
                searchValue = searchValue.Replace(":", "\\:");
                searchValue = searchValue.Replace("!", "\\!");
            }
            _properties["searchTerm"] = searchValue;
            return this;
        }

        /// <summary>
        ///     Builds and encodes the query.
        /// </summary>
        /// <returns></returns>
        public string BuildAndEncode()
        {
            return Uri.EscapeUriString(Build());
        }

        /// <summary>
        ///     Builds and encodes with path.
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public string BuildAndEncodeWithPath(string path)
        {
            string result = BuildWithPath(path);
            if (!String.IsNullOrWhiteSpace(result))
            {
                result = Uri.EscapeUriString(result);
            }
            return result;
        }

        /// <summary>
        ///     Builds with path.
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public string BuildWithPath(string path)
        {
            string result = null;
            if (!String.IsNullOrWhiteSpace(path))
            {
                result = path.EndsWith("/")
                    ? String.Format("{0}search/?q={1}", path, Build())
                    : String.Format("{0}/search/?q={1}", path, Build());
            }
            return result;
        }

        /// <summary>
        ///     Builds the query.
        /// </summary>
        /// <returns></returns>
        public string Build()
        {
            var query = new StringBuilder();
            var searchTerm = _properties.Get<string>("searchTerm");
            if (String.IsNullOrWhiteSpace(searchTerm))
            {
                return null;
            }
            query.Append(String.Format("find {0}", searchTerm));
            var searchGroup = _properties.Get<string>("searchGroup");
            if (!String.IsNullOrWhiteSpace(searchTerm))
            {
                query.Append(" in ");
                query.Append(searchGroup);
            }
            if (_returning != null && _returning.Count > 0)
            {
                query.Append(" returning ");
                query.Append(_returning[0].Build());
                for (int i = 1; i < _returning.Count; i++)
                {
                    query.Append(", ");
                    query.Append(_returning[i].Build());
                }
            }
            var divisionFilter = _properties.Get<string>("divisionFilter");
            if (!String.IsNullOrWhiteSpace(divisionFilter))
            {
                query.Append(" with ");
                query.Append(divisionFilter);
            }
            var dataCategory = _properties.Get<string>("dataCategory");
            if (!String.IsNullOrWhiteSpace(dataCategory))
            {
                query.Append(" with data category ");
                query.Append(dataCategory);
            }
            var limit = _properties.Get<int>("limit");
            if (limit > 0)
            {
                query.Append(" limit ");
                query.Append(String.Format("{0}", limit));
            }
            return query.ToString();
        }
    }
}