/*
 * Copyright (c) 2012-present, salesforce.com, inc.
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
package com.salesforce.androidsdk.smartstore.store;

import android.text.TextUtils;

import com.salesforce.androidsdk.smartstore.store.SmartStore.SmartStoreException;
import com.salesforce.androidsdk.util.JSONObjectHelper;

import org.json.JSONException;
import org.json.JSONObject;

import java.util.ArrayList;
import java.util.List;
import java.util.regex.Matcher;
import java.util.regex.Pattern;

/**
 * Simple class to represent a query spec
 */
public class QuerySpec {
	// Constants
	private static final String SELECT = "SELECT ";
	private static final String FROM = "FROM ";
	private static final String WHERE = "WHERE ";
	private static final String ORDER_BY = "ORDER BY ";

    private static final String SELECT_COUNT = SELECT + "count(*) ";
    private static final String SELECT_COUNT_FROM = SELECT_COUNT + "FROM (%s)";
    private static final String SELECT_ID = SELECT + SmartStore.ID_COL + " ";
    private static final String SELECT_ID_FROM = SELECT_ID + "FROM (%s)";

    // Keys in json
    public static final String BEGIN_KEY = "beginKey";
    public static final String END_KEY = "endKey";
    public static final String INDEX_PATH = "indexPath";
    public static final String LIKE_KEY = "likeKey";
    public static final String MATCH_KEY = "matchKey";
    public static final String SMART_SQL = "smartSql";
    public static final String ORDER_PATH = "orderPath";
    public static final String ORDER = "order";
    public static final String PAGE_SIZE = "pageSize";
    public static final String QUERY_TYPE = "queryType";
    public static final String SELECT_PATHS = "selectPaths";

    // Key members
	public final QueryType queryType;
    public final int pageSize;
    public final String smartSql;
    public final String countSmartSql;
    public final String idsSmartSql;

    // Exact/Range/Like/Match
	public final String soupName;
    public final String[] selectPaths;
    public final String path;
    public final String orderPath;
    public final Order order;

    // Exact/Match
    public final String matchKey;
    // Range
    public final String beginKey;
    public final String endKey;
    // Like
    public final String likeKey;

    // Private constructor for soup query spec
    private QuerySpec(String soupName, String[] selectPaths, QueryType queryType, String matchKey, String beginKey, String endKey, String likeKey, String orderPath, Order order, int pageSize, String path) {
    	this.soupName = soupName;
        this.selectPaths = selectPaths;
        this.path = path;
        this.queryType = queryType;
        this.matchKey = matchKey;
        this.beginKey = beginKey;
        this.endKey = endKey;
        this.likeKey = likeKey;
        this.orderPath = orderPath;
        this.order = order;
        this.pageSize = pageSize;
        this.smartSql = computeSmartSql();
        this.countSmartSql = computeCountSmartSql();
        this.idsSmartSql = computeIdsSmartSql();
    }

    // Private constructor for smart query spec
    private QuerySpec(String smartSql, int pageSize) {
    	this.smartSql = smartSql;
        this.countSmartSql = computeCountSmartSql(smartSql);
        this.idsSmartSql = computeIdsSmartSql(smartSql);
    	this.pageSize = pageSize;
        this.queryType = QueryType.smart;
    	
    	// Not applicable
        this.soupName = null;
        this.selectPaths = null;
        this.path = null;
        this.matchKey = null;
        this.beginKey = null;
        this.endKey = null;
        this.likeKey = null;
        this.orderPath = null;
        this.order = null;    	
    }

    /**
     * Return query spec for an all query
     * @param soupName
     * @param orderPath
     * @param order
     * @param pageSize
     * @return
     */
    public static QuerySpec buildAllQuerySpec(String soupName, String orderPath, Order order, int pageSize) {
        return buildAllQuerySpec(soupName, null, orderPath, order, pageSize);
    }

    /**
     * Return query spec for an all query
     * @param soupName
     * @param selectPaths
     * @param orderPath
     * @param order
     * @param pageSize
     * @return
     */
    public static QuerySpec buildAllQuerySpec(String soupName, String[] selectPaths, String orderPath, Order order, int pageSize) {
        return new QuerySpec(soupName, selectPaths, QueryType.range, null, null, null, null, orderPath, order, pageSize, null);
    }

    /**
     * Return a query spec for an exact match query
     * @param soupName
     * @param path
     * @param exactMatchKey
     * @param orderPath
     * @param order
     * @param pageSize
     * @return
     */
    public static QuerySpec buildExactQuerySpec(String soupName, String path, String exactMatchKey, String orderPath, Order order, int pageSize) {
        return buildExactQuerySpec(soupName, null, path, exactMatchKey, orderPath, order, pageSize);
    }

    /**
     * Return a query spec for an exact match query
     * @param soupName
     * @param selectPaths
     * @param path
     * @param exactMatchKey
     * @param orderPath
     * @param order
     * @param pageSize
     * @return
     */
    public static QuerySpec buildExactQuerySpec(String soupName, String[] selectPaths, String path, String exactMatchKey, String orderPath, Order order, int pageSize) {
        return new QuerySpec(soupName, selectPaths, QueryType.exact, exactMatchKey, null, null, null, orderPath, order, pageSize, path);
    }

    /**
     * Return a query spec for a range query
     * @param soupName
     * @param path
     * @param beginKey
     * @param endKey
     * @param orderPath
     * @param order
     * @param pageSize
     * @return
     */
    public static QuerySpec buildRangeQuerySpec(String soupName, String path, String beginKey, String endKey, String orderPath, Order order, int pageSize) {
        return buildRangeQuerySpec(soupName, null, path, beginKey, endKey, orderPath, order, pageSize);
    }

    /**
     * Return a query spec for a range query
     * @param soupName
     * @param selectPaths
     * @param path
     * @param beginKey
     * @param endKey
     * @param orderPath
     * @param order
     * @param pageSize
     * @return
     */
    public static QuerySpec buildRangeQuerySpec(String soupName, String[] selectPaths, String path, String beginKey, String endKey, String orderPath, Order order, int pageSize) {
        return new QuerySpec(soupName, selectPaths, QueryType.range, null, beginKey, endKey, null, orderPath, order, pageSize, path);
    }

    /**
     * Return a query spec for a like query
     * @param soupName
     * @param path
     * @param likeKey
     * @param orderPath
     * @param order
     * @param pageSize
     * @return
     * */
    public static QuerySpec buildLikeQuerySpec(String soupName, String path, String likeKey, String orderPath, Order order, int pageSize) {
        return buildLikeQuerySpec(soupName, null, path, likeKey, orderPath, order, pageSize);
    }

    /**
     * Return a query spec for a like query
     * @param soupName
     * @param selectPaths
     * @param path
     * @param likeKey
     * @param orderPath
     * @param order
     * @param pageSize
     * @return
     * */
    public static QuerySpec buildLikeQuerySpec(String soupName, String[] selectPaths, String path, String likeKey, String orderPath, Order order, int pageSize) {
        return new QuerySpec(soupName, selectPaths, QueryType.like, null, null, null, likeKey, orderPath, order, pageSize, path);
    }

    /**
     * Return a query spec for a match query (full-text search)
     * @param soupName
     * @param path
     * @param matchKey
     * @param orderPath
     * @param order
     * @param pageSize
     * @return
     */
    public static QuerySpec buildMatchQuerySpec(String soupName, String path, String matchKey, String orderPath, Order order, int pageSize) {
        return buildMatchQuerySpec(soupName, null, path, matchKey, orderPath, order, pageSize);
    }

    /**
     * Return a query spec for a match query (full-text search)
     * @param soupName
     * @param selectPaths
     * @param path
     * @param matchKey
     * @param orderPath
     * @param order
     * @param pageSize
     * @return
     */
    public static QuerySpec buildMatchQuerySpec(String soupName, String[] selectPaths, String path, String matchKey, String orderPath, Order order, int pageSize) {
        return new QuerySpec(soupName, selectPaths, QueryType.match, matchKey, null, null, null, orderPath, order, pageSize, path);
    }

    /**
     * Return a query spec for a smart query
     * @param smartSql
     * @param pageSize
     * @return
     */
    public static QuerySpec buildSmartQuerySpec(String smartSql, int pageSize) {
    	return new QuerySpec(smartSql, pageSize);
    }

    /**
     * Compute smartSql for exact/like/range/match queries
     */
    private String computeSmartSql() {
        String selectClause = computeSelectClause();
        String fromClause = computeFromClause();
        String whereClause = computeWhereClause();
        String orderClause = computeOrderClause();
        return selectClause + fromClause + whereClause + orderClause;
    }

    /**
     * Compute countSmartSql for exact/like/range/match queries
     */
    private String computeCountSmartSql() {
    	String fromClause = computeFromClause();
    	String whereClause = computeWhereClause();
    	return SELECT_COUNT + fromClause + whereClause;
    }
    
    /**
     * Compute countSmartSql for smart queries
     */
    private String computeCountSmartSql(String smartSql) {
    	return String.format(SELECT_COUNT_FROM, smartSql);
    }

    /**
     * Compute idsSmartSql for exact/like/range/match queries
     */
    private String computeIdsSmartSql() {
        String fromClause = computeFromClause();
        String whereClause = computeWhereClause();
        String orderClause = computeOrderClause();
        return SELECT_ID + fromClause + whereClause + orderClause;
    }

    /**
     * Compute idsSmartSql for smart queries
     */
    private String computeIdsSmartSql(String smartSql) {
        return String.format(SELECT_ID_FROM, smartSql);
    }

    /**
     * @return select clause for exact/like/range/match queries
     */
    private String computeSelectClause() {
        List<String> fieldReferences = new ArrayList<>();
        for (String selectPath : (selectPaths != null ? selectPaths : new String[] {SmartSqlHelper.SOUP})) {
            fieldReferences.add(computeFieldReference(selectPath));
        }
        return SELECT + TextUtils.join(", ", fieldReferences) + " ";
    }

    /**
     * @return from clause for exact/like/range/match queries
     */
    private String computeFromClause() {
        return FROM + computeSoupReference() + " ";
    }
    
    /**
     * @return where clause for exact/like/range/match queries
     */
    private String computeWhereClause() {
        if (path == null && queryType != QueryType.match /* null path allowed for fts match query */) return "";

        String field = null;

        if (path != null) {
            field = computeFieldReference(path);
        }

        String pred = "";
        switch (queryType) {
            case exact:
                pred = field + " = ? ";
                break;
            case like:
                pred = field + " LIKE ? ";
                break;
            case range:
                if (beginKey == null && endKey == null) {
                    break;
                }
                if (endKey == null) {
                    pred = field + " >= ? ";
                    break;
                }
                if (beginKey == null) {
                    pred = field + " <= ? ";
                    break;
                }
                else {
                    pred = field + " >= ? AND " + field + " <= ? ";
                    break;
                }
            case match:
                pred = computeFieldReference(SmartStore.SOUP_ENTRY_ID) + " IN ("
                        + SELECT + SmartStore.ROWID_COL + " " + FROM + computeSoupFtsReference() + " " + WHERE
                        + computeSoupFtsReference() + " MATCH '" + qualifyMatchKey(field, matchKey) + "'"
                        // statement arg binding doesn't seem to work so inlining matchKey
                        + ") ";
                break;
            default:
                throw new SmartStoreException("Fell through switch: " + queryType);
        }
        return (pred.equals("") ? "" : WHERE + pred);
    }

    /**
     * fts5 doesn't allow WHERE column MATCH 'value' - only allows WHERE table MATCH 'column:value'
     * This method changes the matchKey to add field: in the right places
     * @param field
     * @param matchKey
     * @return
     */
    public static String qualifyMatchKey(String field, String matchKey) {
        if (field == null) {
            return matchKey;
        }

        StringBuffer qualifiedMatchKey = new StringBuffer();
        Pattern pattern = Pattern.compile("[^\\(\\) ]+");
        Matcher matcher = pattern.matcher(matchKey);
        while (matcher.find()) {
            String fullMatch = matcher.group();
            String fullMatchLowerCase = fullMatch.toLowerCase();

            if (fullMatchLowerCase.equals("and") || fullMatchLowerCase.equals("or") || fullMatchLowerCase.equals("not") // operator
                    || fullMatch.startsWith("{")) // already qualified
            {
                // Leaving unchanged
                matcher.appendReplacement(qualifiedMatchKey, fullMatch);
            }
            else {
                // Qualifying with {soup:path}: -- which turn into column: in sql
                matcher.appendReplacement(qualifiedMatchKey, field + ":" + fullMatch);
            }
        }
        matcher.appendTail(qualifiedMatchKey);

        return qualifiedMatchKey.toString();
    }

    /**
     * @return order clause for exact/like/range/match queries
     */
    private String computeOrderClause() {
    	if (orderPath == null || order == null) return "";

    	return ORDER_BY + computeFieldReference(orderPath) + " " + order.sql + " ";
    }
    
	/**
	 * @return soup reference for smart sql query
	 */
	private String computeSoupReference() {
		return "{" + soupName + "}";
	}

    /**
     * @return fts soup table reference
     */
    private String computeSoupFtsReference() {
        return computeSoupReference() + SmartStore.FTS_SUFFIX;
    }
    
    /**
     * @param field
	 * @return field reference for smart sql query
	 */
	private String computeFieldReference(String field) {
		return "{" + soupName + ":" + field + "}";
	}

    /**
     * @return args going with the sql predicate returned by getKeyPredicate
     */
    public String[] getArgs() {
        switch(queryType) {
        case exact:
            return new String[] {matchKey};
        case like:
            return new String[] {likeKey};
        case range:
            if (beginKey == null && endKey == null)
                return null;
            else if (endKey == null)
                return new String[] {beginKey};
            else if (beginKey == null)
                return new String[] {endKey};
            else
                return new String[] {beginKey, endKey};
        case match:
            return null; // baking matchKey into query
        case smart:
        	return null;
        default:
            throw new SmartStoreException("Fell through switch: " + queryType);
        }
    }

    /**
	 * @param soupName
	 * @param querySpecJson
	 * @return
	 * @throws JSONException
	 */
	public static QuerySpec fromJSON(String soupName, JSONObject querySpecJson)
			throws JSONException {
		QueryType queryType = QueryType.valueOf(querySpecJson.getString(QUERY_TYPE));
        String[] selectPaths = JSONObjectHelper.optStringArray(querySpecJson, SELECT_PATHS);
		String path = JSONObjectHelper.optString(querySpecJson, INDEX_PATH);
		String matchKey = JSONObjectHelper.optString(querySpecJson, MATCH_KEY);
		String beginKey = JSONObjectHelper.optString(querySpecJson, BEGIN_KEY);
		String endKey = JSONObjectHelper.optString(querySpecJson, END_KEY);
		String likeKey = JSONObjectHelper.optString(querySpecJson, LIKE_KEY);
		String smartSql = JSONObjectHelper.optString(querySpecJson, SMART_SQL);
		String orderPath = JSONObjectHelper.optString(querySpecJson, ORDER_PATH);
		Order order = Order.valueOf(JSONObjectHelper.optString(querySpecJson, ORDER, "ascending"));
		int pageSize = querySpecJson.getInt(PAGE_SIZE); 
	
		// Building query spec
		QuerySpec querySpec = null;
		switch (queryType) {
	    case exact:   querySpec = buildExactQuerySpec(soupName, selectPaths, path, matchKey, orderPath, order, pageSize); break;
	    case range:   querySpec = buildRangeQuerySpec(soupName, selectPaths, path, beginKey, endKey, orderPath, order, pageSize); break;
	    case like:    querySpec = buildLikeQuerySpec(soupName, selectPaths, path, likeKey, orderPath, order, pageSize); break;
        case match:   querySpec = buildMatchQuerySpec(soupName, selectPaths, path, matchKey, orderPath, order, pageSize); break;
	    case smart:   querySpec = buildSmartQuerySpec(smartSql, pageSize); break;
	    default: throw new RuntimeException("Fell through switch: " + queryType);
		}
		return querySpec;
	}

	/**
     * Query type enum
     */
    public enum QueryType {
        exact,
        range,
        like,
        match,
        smart
    }


    /**
     * Simple class to represent query order
     */
    public enum Order {
        ascending("ASC"), descending("DESC");

        public final String sql;

        Order(String sqlOrder) {
            this.sql = sqlOrder;
        }
    }

}
