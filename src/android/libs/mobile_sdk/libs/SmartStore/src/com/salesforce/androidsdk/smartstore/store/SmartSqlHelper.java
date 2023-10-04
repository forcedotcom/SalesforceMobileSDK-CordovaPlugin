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

import com.salesforce.androidsdk.smartstore.store.SmartStore.SmartStoreException;

import net.sqlcipher.database.SQLiteDatabase;

import java.util.HashMap;
import java.util.Locale;
import java.util.Map;
import java.util.regex.Matcher;
import java.util.regex.Pattern;

/**
 * SmartSqlHelper "smart" sql Helper
 * 
 * Singleton class that provides helpful methods for converting/running "smart" sql
 */
public class SmartSqlHelper  {

	private static final String NO_STRINGS_OR_FULL_STRINGS_REGEXP = "^([^']|'[^']*')*";
	//  ^           # the start of the string, then
	//  ([^']       # either not a quote character
	//  |'[^']*'    # or a fully quoted string
	//  )*          # as many times as you want

	private static final String INSIDE_QUOTED_STRING_REGEXP = NO_STRINGS_OR_FULL_STRINGS_REGEXP + "'[^']*";

	private static final String INSIDE_QUOTED_STRING_FOR_FTS_MATCH_PREDICATE_REGEXP = NO_STRINGS_OR_FULL_STRINGS_REGEXP + "MATCH[ ]+'[^']*";

	public static final Pattern SOUP_PATH_PATTERN = Pattern.compile("\\{([^}]+)\\}");

	public static final String TABLE_DOT_JSON_EXTRACT_REGEXP = "(\\w+)\\.json_extract\\(soup";

	private static Map<SQLiteDatabase, SmartSqlHelper> INSTANCES;

	/**
	 * Returns the instance of this class associated with the database specified.
	 *
	 * @param db Database.
	 * @return Instance of this class.
	 */
	public static synchronized SmartSqlHelper getInstance(SQLiteDatabase db) {
		if (INSTANCES == null) {
			INSTANCES = new HashMap<>();
		}
		SmartSqlHelper instance = INSTANCES.get(db);
		if (instance == null) {
			instance = new SmartSqlHelper();
			INSTANCES.put(db, instance);
		}
		return instance;
	}

    public static final String SOUP = "_soup";
	
	/**
	 * Convert "smart" sql query to actual sql
	 * A "smart" sql query is a query where columns are of the form {soupName:path} and tables are of the form {soupName}
	 * 
	 * NB: only select's are allowed
	 *     only indexed path can be referenced (alternatively you can do {soupName:_soupEntryId} or {soupName:_soupLastModifiedDate}
	 *     to get an entire soup element back, do {soupName:_soup}
	 *
	 * @param db
	 * @param smartSql
	 * @return actual sql     
	 */
	public String convertSmartSql(SQLiteDatabase db, String smartSql) {

		// Select's only
		String smartSqlLowerCase = smartSql.toLowerCase(Locale.getDefault()).trim();
		if (smartSqlLowerCase.startsWith("insert") || smartSqlLowerCase.startsWith("update") || smartSqlLowerCase.startsWith("delete")) {
			throw new SmartSqlException("Only SELECT are supported");
		}

		// Replacing {soupName} and {soupName:path}
		StringBuffer sql = new StringBuffer();
		Matcher matcher = SOUP_PATH_PATTERN.matcher(smartSql);
		while (matcher.find()) {
			String fullMatch = matcher.group();
			String match = matcher.group(1);
			int position = matcher.start();

			String beforeStr = smartSql.substring(0, position);
			if (beforeStr.matches(INSIDE_QUOTED_STRING_REGEXP)
				&& !beforeStr.matches(INSIDE_QUOTED_STRING_FOR_FTS_MATCH_PREDICATE_REGEXP))
			{
				continue;
			}

			String[] parts = match.split(":");
			String soupName = parts[0];
			String soupTableName = getSoupTableNameForSmartSql(db, soupName, position);
			boolean tableQualified = smartSql.charAt(position-1) == '.';
			String tableQualifier = tableQualified ? "" : soupTableName + ".";

			// {soupName}
			if (parts.length == 1) {
				matcher.appendReplacement(sql, soupTableName);
			} else if (parts.length == 2) {
				String path = parts[1];

				// {soupName:_soup}
				switch (path) {
					case SOUP:
						matcher.appendReplacement(sql, tableQualifier + SmartStore.SOUP_COL);
						break;
					// {soupName:_soupEntryId}
					case SmartStore.SOUP_ENTRY_ID:
						matcher.appendReplacement(sql, tableQualifier + SmartStore.ID_COL);
						break;
					// {soupName:_soupCreatedDate}
					case SmartStore.SOUP_CREATED_DATE:
						matcher.appendReplacement(sql, tableQualifier + SmartStore.CREATED_COL);
						break;
					// {soupName:_soupLastModifiedDate}
					case SmartStore.SOUP_LAST_MODIFIED_DATE:
						matcher.appendReplacement(sql, tableQualifier + SmartStore.LAST_MODIFIED_COL);
						break;
					// {soupName:path}
					default:
						String columnName = getColumnNameForPathForSmartSql(db, soupName, path, position);
						matcher.appendReplacement(sql, columnName.replace("$", "\\$") /* treat any $ as litteral */);
						break;
				}
			} else if (parts.length > 2) {
				reportSmartSqlError("Invalid soup/path reference " + fullMatch, position);
			}
		}
		matcher.appendTail(sql);

        // SQL query as string
		String sqlStr = sql.toString();

		// With json1 support, the column name could be an expression of the form json_extract(soup, '$.x.y.z')
		// We can't have TABLE_x.json_extract(soup, ...) or table_alias.json_extract(soup, ...) in the sql query
        // Instead we should have json_extract(TABLE_x.soup, ...)
		sqlStr = sqlStr.replaceAll(TABLE_DOT_JSON_EXTRACT_REGEXP, "json_extract($1.soup");

		// Done
		return sqlStr;
	}
	
	private String getColumnNameForPathForSmartSql(SQLiteDatabase db, String soupName, String path, int position) {
		String columnName = null;
		boolean indexed = DBHelper.getInstance(db).hasIndexForPath(db, soupName, path);

		if (!indexed) {
			// Thanks to the json1 extension we can query the data even if it is not indexed
			columnName = "json_extract(" + SmartStore.SOUP_COL + ", '$." + path + "')";
		} else {
			try {
				columnName = DBHelper.getInstance(db).getColumnNameForPath(db, soupName, path);
			} catch (SmartStoreException e) {
				reportSmartSqlError(e.getMessage(), position);
			}
		}

		return columnName;
	}

	private String getSoupTableNameForSmartSql(SQLiteDatabase db, String soupName, int position) {
		String soupTableName = DBHelper.getInstance(db).getSoupTableName(db, soupName);
		if (soupTableName == null) {
			reportSmartSqlError("Unknown soup " + soupName, position);
		}
		return soupTableName;
	}
	
	private void reportSmartSqlError(String message, int position) {
		throw new SmartSqlException(message + " at character " + position);
	}
    
    /**
     * Exception thrown when smart sql failed to be parsed
     */
    public static class SmartSqlException extends SmartStoreException {

		private static final long serialVersionUID = -525130153073212701L;

		public SmartSqlException(String message) {
    		super(message);
    	}
    }
}
