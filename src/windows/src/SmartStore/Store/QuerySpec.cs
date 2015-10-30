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

namespace Salesforce.SDK.SmartStore.Store
{
    public class QuerySpec
    {
        public enum SmartQueryType
        {
            Smart,
            Exact,
            Range,
            Like
        };

        public enum SqlOrder
        {
            ASC,
            DESC
        };

        private const string SelectCount = "SELECT count(*) ";
        private const string Select = "SELECT ";
        private const string From = "FROM ";
        private const string Where = "WHERE ";
        private const string OrderBy = "ORDER BY ";

        public readonly string BeginKey;
        public readonly string CountSmartSql;
        public readonly string EndKey;
        public readonly string LikeKey;
        public readonly string MatchKey;
        public readonly SqlOrder Order;
        public readonly int PageSize;
        public readonly string Path;
        public readonly SmartQueryType QueryType;
        public readonly string SmartSql;
        public readonly string SoupName;

        private QuerySpec(string soupName, string path, SmartQueryType queryType, string matchKey, string beginKey,
            string endKey, string likeKey, SqlOrder order, int pageSize)
        {
            SoupName = soupName;
            Path = path;
            QueryType = queryType;
            MatchKey = matchKey;
            BeginKey = beginKey;
            EndKey = endKey;
            LikeKey = likeKey;
            Order = order;
            PageSize = pageSize;
            SmartSql = ComputeSmartSql();
            CountSmartSql = ComputeCountSql();
        }

        private QuerySpec(string smartSql, int pageSize)
        {
            QueryType = SmartQueryType.Smart;
            SmartSql = smartSql;
            CountSmartSql = ComputeCountSql(smartSql);
            PageSize = pageSize;
            SoupName = null;
            Path = null;
            MatchKey = null;
            BeginKey = null;
            EndKey = null;
            LikeKey = null;
        }

        public static QuerySpec BuildAllQuerySpec(string soupName, string path, SqlOrder order, int pageSize)
        {
            return BuildRangeQuerySpec(soupName, path, null, null, order, pageSize);
        }

        public static QuerySpec BuildExactQuerySpec(string soupName, string path, string exactMatchKey, int pageSize)
        {
            return new QuerySpec(soupName, path, SmartQueryType.Exact, exactMatchKey, null, null, null, SqlOrder.ASC,
                pageSize);
        }

        public static QuerySpec BuildRangeQuerySpec(string soupName, string path, string beginKey, string endKey,
            SqlOrder order, int pageSize)
        {
            return new QuerySpec(soupName, path, SmartQueryType.Range, null, beginKey, endKey, null, order, pageSize);
        }

        public static QuerySpec BuildLikeQuerySpec(string soupName, string path, string likeKey, SqlOrder order,
            int pageSize)
        {
            return new QuerySpec(soupName, path, SmartQueryType.Like, null, null, null, likeKey, order, pageSize);
        }

        public static QuerySpec BuildSmartQuerySpec(string smartSql, int pageSize)
        {
            return new QuerySpec(smartSql, pageSize);
        }

        private string ComputeSmartSql()
        {
            string selectClause = ComputeSelectClause();
            string fromClause = ComputeFromClause();
            string whereClause = ComputeWhereClause();
            string orderClause = ComputeOrderClause();
            return selectClause + fromClause + whereClause + orderClause;
        }

        /// <summary>
        ///     Compute countSmartSql for exact/like/range queries
        /// </summary>
        /// <returns></returns>
        private String ComputeCountSql()
        {
            string fromClause = ComputeFromClause();
            string whereClause = ComputeWhereClause();
            return SelectCount + fromClause + whereClause;
        }

        /// <summary>
        ///     Compute countSmartSql for smart queries
        /// </summary>
        /// <param name="smartSql"></param>
        /// <returns></returns>
        private string ComputeCountSql(string smartSql)
        {
            int fromLocation = smartSql.ToLower().IndexOf(" from ", StringComparison.CurrentCultureIgnoreCase);
            return SelectCount + smartSql.Substring(fromLocation);
        }

        /// <summary>
        /// </summary>
        /// <returns>select clause for exact/like/range queries</returns>
        private string ComputeSelectClause()
        {
            return Select + ComputeFieldReference(SmartStore.Soup) + " ";
        }

        /// <summary>
        /// </summary>
        /// <returns>from clause for exact/like/range queries</returns>
        private string ComputeFromClause()
        {
            return From + ComputeSoupReference() + " ";
        }

        /// <summary>
        /// </summary>
        /// <returns>where clause for exact/like/range queries</returns>
        private string ComputeWhereClause()
        {
            if (Path == null) return "";

            string field = ComputeFieldReference(Path);
            string pred = "";
            switch (QueryType)
            {
                case SmartQueryType.Exact:
                    pred = field + " = ? ";
                    break;
                case SmartQueryType.Like:
                    pred = field + " LIKE ? ";
                    break;
                case SmartQueryType.Range:
                    if (BeginKey == null && EndKey == null)
                    {
                        break;
                    }
                    if (EndKey == null)
                    {
                        pred = field + " >= ? ";
                        break;
                    }
                    if (BeginKey == null)
                    {
                        pred = field + " <= ? ";
                        break;
                    }
                    pred = field + " >= ?  AND " + field + " <= ? ";
                    break;
                default:
                    throw new SmartStoreException("Fell through switch: " + QueryType);
            }
            return (pred.Equals("") ? "" : Where + pred);
        }

        /// <summary>
        /// </summary>
        /// <returns>order clause for exact/like/range queries</returns>
        private String ComputeOrderClause()
        {
            if (Path == null) return "";

            return OrderBy + ComputeFieldReference(Path) + " " + Order + " ";
        }

        /// <summary>
        /// </summary>
        /// <returns>soup reference for smart sql query</returns>
        private String ComputeSoupReference()
        {
            return "{" + SoupName + "}";
        }


        /// <summary>
        /// </summary>
        /// <param name="field"></param>
        /// <returns>field reference for smart sql query</returns>
        private String ComputeFieldReference(String field)
        {
            return "{" + SoupName + ":" + field + "}";
        }

        /// <summary>
        /// </summary>
        /// <returns>args going with the sql predicate returned by getKeyPredicate</returns>
        public String[] getArgs()
        {
            switch (QueryType)
            {
                case SmartQueryType.Exact:
                    return new[] {MatchKey};
                case SmartQueryType.Like:
                    return new[] {LikeKey};
                case SmartQueryType.Range:
                    if (BeginKey == null && EndKey == null)
                        return null;
                    if (EndKey == null)
                        return new[] {BeginKey};
                    if (BeginKey == null)
                        return new[] {EndKey};
                    return new[] {BeginKey, EndKey};
                case SmartQueryType.Smart:
                    return null;
                default:
                    throw new SmartStoreException("Fell through switch: " + QueryType);
            }
        }
    }
}