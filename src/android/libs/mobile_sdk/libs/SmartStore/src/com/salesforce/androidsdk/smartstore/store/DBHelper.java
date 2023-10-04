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

import android.content.ContentValues;
import android.content.Context;
import android.database.Cursor;
import android.database.SQLException;
import android.database.sqlite.SQLiteDoneException;
import android.util.LruCache;

import com.salesforce.androidsdk.accounts.UserAccount;
import com.salesforce.androidsdk.smartstore.app.SmartStoreSDKManager;
import com.salesforce.androidsdk.smartstore.store.SmartStore.SmartStoreException;
import com.salesforce.androidsdk.smartstore.store.SmartStore.Type;
import com.salesforce.androidsdk.smartstore.util.SmartStoreLogger;

import net.sqlcipher.DatabaseUtils.InsertHelper;
import net.sqlcipher.database.SQLiteDatabase;
import net.sqlcipher.database.SQLiteStatement;

import org.json.JSONArray;
import org.json.JSONException;
import org.json.JSONObject;

import java.util.ArrayList;
import java.util.Arrays;
import java.util.HashMap;
import java.util.List;
import java.util.Map;
import java.util.Map.Entry;

/**
 * SmartStore Database Helper
 * Singleton class that provides helpful methods for accessing the database underneath the SmartStore
 * It also caches a number of of things to speed things up (e.g. soup table name, index specs, insert helpers etc)
 */
public class DBHelper {

	// Explain support
	public static final String EXPLAIN_SQL = "sql";
	public static final String EXPLAIN_ARGS = "args";
	public static final String EXPLAIN_ROWS = "rows";
	public static final String EXPLAIN_TAG = "EXPLAIN";

	private static Map<SQLiteDatabase, DBHelper> INSTANCES;

	/**
	 * Returns the instance of this class associated with the database specified.
	 *
	 * @param db Database.
	 * @return Instance of this class.
	 */
	public static synchronized DBHelper getInstance(SQLiteDatabase db) {
		if (INSTANCES == null) {
			INSTANCES = new HashMap<>();
		}
		DBHelper instance = INSTANCES.get(db);
		if (instance == null) {
			instance = new DBHelper();
			INSTANCES.put(db, instance);
		}
		return instance;
	}

	// Some queries
	private static final String COUNT_SELECT = "SELECT count(*) FROM %s %s";
	private static final String SEQ_SELECT = "SELECT seq FROM SQLITE_SEQUENCE WHERE name = ?";
	private static final String LIMIT_SELECT = "SELECT * FROM (%s) LIMIT %s";

	// Caches count limit
	private static final int CACHES_COUNT_LIMIT = 1024;

	// Cache of soup name to boolean indicating existence
	private final LruCache<String, Boolean> soupNameToExistMap = new LruCache<>(CACHES_COUNT_LIMIT);

	// Cache of soup name to soup table names
	private final LruCache<String, String> soupNameToTableNamesMap = new LruCache<>(CACHES_COUNT_LIMIT);

	// Cache of soup name to index specs
	private final LruCache<String, IndexSpec[]> soupNameToIndexSpecsMap = new LruCache<>(CACHES_COUNT_LIMIT);

	// Cache of soup name to boolean indicating if soup uses FTS
	private final LruCache<String, Boolean> soupNameToHasFTS = new LruCache<>(CACHES_COUNT_LIMIT);

	// Cache of table name to get-next-id compiled statements
	private final LruCache<String, SQLiteStatement> tableNameToNextIdStatementsMap = new LruCache<String, SQLiteStatement>(CACHES_COUNT_LIMIT) {
		@Override
		protected void entryRemoved(boolean evicted, String key, SQLiteStatement oldValue, SQLiteStatement newValue) {
			oldValue.close();
		}
	};

	// Cache of table name to insert helpers
	private final LruCache<String, InsertHelper> tableNameToInsertHelpersMap = new LruCache<String, InsertHelper>(CACHES_COUNT_LIMIT) {
		@Override
		protected void entryRemoved(boolean evicted, String key, InsertHelper oldValue, InsertHelper newValue) {
			oldValue.close();
		}
	};

	// Cache of raw count sql to compiled statements
	private final LruCache<String, SQLiteStatement> rawCountSqlToStatementsMap = new LruCache<String, SQLiteStatement>(CACHES_COUNT_LIMIT) {
		@Override
		protected void entryRemoved(boolean evicted, String key, SQLiteStatement oldValue, SQLiteStatement newValue) {
			oldValue.close();
		}
	};

	// Boolean to turn explain query plan capture on or off
	private boolean captureExplainQueryPlan;

	// Last explain query plan
	private JSONObject lastExplainQueryPlan;

	/**
	 * @param soupName
	 * @param tableName
	 */
	public void cacheTableName(String soupName, String tableName) {
		soupNameToTableNamesMap.put(soupName, tableName);
	}

	/**
	 * @param soupName
	 * @param hasSoup
	 */
	public void cacheHasSoup(String soupName, boolean hasSoup) {
		soupNameToExistMap.put(soupName, hasSoup);
	}


	/**
	 * @param soupName
	 * @return
	 */
	public String getCachedTableName(String soupName) {
		return soupNameToTableNamesMap.get(soupName);
	}

	/**
	 * @param soupName
	 * @param indexSpecs
	 */
	public void cacheIndexSpecs(String soupName, IndexSpec[] indexSpecs) {
		soupNameToIndexSpecsMap.put(soupName, indexSpecs.clone());
		soupNameToHasFTS.put(soupName, IndexSpec.hasFTS(indexSpecs));
	}

	/**
	 * @param soupName
	 * @return
	 */
	public IndexSpec[] getCachedIndexSpecs(String soupName) {
		return soupNameToIndexSpecsMap.get(soupName);
	}

	/**
	 * @param soupName
	 * @return
	 */
	public Boolean getCachedHasFTS(String soupName) {
		return soupNameToHasFTS.get(soupName);
	}

	/**
	 * @param soupName
	 */
	public void removeFromCache(String soupName) {
		String tableName = soupNameToTableNamesMap.get(soupName);
		if (tableName != null) {
			InsertHelper ih = tableNameToInsertHelpersMap.remove(tableName);
			if (ih != null) 
				ih.close();
			
			SQLiteStatement prog = tableNameToNextIdStatementsMap.remove(tableName);
			if (prog != null) 
				prog.close();
			
			cleanupRawCountSqlToStatementMaps(tableName);
		}
		soupNameToExistMap.remove(soupName);
		soupNameToTableNamesMap.remove(soupName);
		soupNameToIndexSpecsMap.remove(soupName);
		soupNameToHasFTS.remove(soupName);
	}

	private void cleanupRawCountSqlToStatementMaps(String tableName) {
		List<String> countSqlToRemove = new ArrayList<>();
		for (Entry<String, SQLiteStatement>  entry : rawCountSqlToStatementsMap.snapshot().entrySet()) {
			String countSql = entry.getKey();
			if (countSql.contains(tableName)) {
				SQLiteStatement countProg = entry.getValue();
				if (countProg != null)
					countProg.close();
				countSqlToRemove.add(countSql);
			}
		}
		for (String countSql : countSqlToRemove) {
			rawCountSqlToStatementsMap.remove(countSql);
		}
	}

	/**
	 * Get next id for a table
	 * 
	 * @param db
	 * @param tableName
	 * @return long
	 */
	public long getNextId(SQLiteDatabase db, String tableName) {
		SQLiteStatement prog = tableNameToNextIdStatementsMap.get(tableName);
		if (prog == null) {
			prog = db.compileStatement(SEQ_SELECT);
			prog.bindString(1, tableName);
			tableNameToNextIdStatementsMap.put(tableName, prog);
		}
		try {
			return prog.simpleQueryForLong() + 1;
		} catch (SQLiteDoneException e) {
			// first time, we don't find any row for the table in the sequence table
			return 1L;
		}
	}

	/**
	 * Get insert helper for a table
	 * @param table
	 * @return
	 */
	public InsertHelper getInsertHelper(SQLiteDatabase db, String table) {
		InsertHelper insertHelper = tableNameToInsertHelpersMap.get(table);
		if (insertHelper == null) {
			insertHelper = new InsertHelper(db, table);
			tableNameToInsertHelpersMap.put(table, insertHelper);
		}
		return insertHelper;
	}

	/**
	 * Does a count query
	 * @param db
	 * @param table
	 * @param whereClause
	 * @param whereArgs
	 * @return
	 */
	public Cursor countQuery(SQLiteDatabase db, String table, String whereClause, String... whereArgs) {
		String selectionStr = (whereClause == null ? "" : " WHERE " + whereClause);
		String sql = String.format(COUNT_SELECT, table, selectionStr);
		return db.rawQuery(sql, whereArgs);
	}

	/**
	 * Does a limit for a raw query
	 * @param db
	 * @param sql
	 * @param limit
	 * @param whereArgs
	 * @return
	 */
	public Cursor limitRawQuery(SQLiteDatabase db, String sql, String limit, String... whereArgs) {
		String limitSql = String.format(LIMIT_SELECT, sql, limit);
		if (captureExplainQueryPlan) {
			runExplainQueryPlan(db, limitSql, whereArgs);
		}
		return db.rawQuery(limitSql, whereArgs);
	}

	private void runExplainQueryPlan(SQLiteDatabase db, String sql, String... whereArgs) {
		JSONObject lastExplain = new JSONObject();
		Cursor c = null;
		try {
			lastExplain.put(EXPLAIN_SQL, sql);
			if (whereArgs != null && whereArgs.length > 0) lastExplain.put(EXPLAIN_ARGS, new JSONArray(Arrays.asList(whereArgs)));
			JSONArray rows = new JSONArray();

			c = db.rawQuery("EXPLAIN QUERY PLAN " + sql, whereArgs);
			while (c.moveToNext()) {
				JSONObject row = new JSONObject();
				StringBuilder sb = new StringBuilder();
				for (int i = 0; i < c.getColumnCount(); i++) {
					row.put(c.getColumnName(i), c.getString(i));
				}
				rows.put(row);
			}
			lastExplain.put(EXPLAIN_ROWS, rows);
			SmartStoreLogger.d(EXPLAIN_TAG, lastExplain.toString(2));
		} catch (JSONException e) {
            SmartStoreLogger.d(EXPLAIN_TAG, "Exception", e);
		} finally {
			safeClose(c);
		}
		lastExplainQueryPlan = lastExplain;
	}

	/**
	 * Does a count for a raw count query
	 * @param db
	 * @param countSql
	 * @param whereArgs
	 * @return
	 */
	public int countRawCountQuery(SQLiteDatabase db, String countSql, String... whereArgs) {
		SQLiteStatement prog = rawCountSqlToStatementsMap.get(countSql);
		if (prog == null) {
			prog = db.compileStatement(countSql);
			rawCountSqlToStatementsMap.put(countSql, prog);
		}
		if (whereArgs != null) {
			for (int i=0; i<whereArgs.length; i++) {
				prog.bindString(i+1, whereArgs[i]);
			}
		}
		try {
			int count =  (int) prog.simpleQueryForLong();
			prog.clearBindings();
			return count;
		} catch (SQLiteDoneException e) {
			return -1;
		}
	}

	/**
	 * Does a count for a raw query
	 * @param db
	 * @param sql
	 * @param whereArgs
	 * @return
	 */
	public int countRawQuery(SQLiteDatabase db, String sql, String... whereArgs) {
		String countSql = String.format(COUNT_SELECT, "", "(" + sql + ")");
		return countRawCountQuery(db, countSql, whereArgs);
	}

	/**
	 * Runs a query
	 * @param db
	 * @param table
	 * @param columns
	 * @param orderBy
	 * @param limit
	 * @param whereClause
	 * @param whereArgs
	 * @return
	 */
	public Cursor query(SQLiteDatabase db, String table, String[] columns, String orderBy, String limit, String whereClause, String... whereArgs) {
		return db.query(table, columns, whereClause, whereArgs, null, null, orderBy, limit);
	}

	/**
	 * Does an insert
	 * @param db
	 * @param table
	 * @param contentValues
	 * @return row id of inserted row
	 */
	public long insert(SQLiteDatabase db, String table, ContentValues contentValues) {
		InsertHelper ih = getInsertHelper(db, table);
		long rowId = ih.insert(contentValues);
		if (rowId == -1) {
			// In case of failure InsertHelper.insert swallows the SQLException and returns -1
			throw new SQLException(String.format("Insert into %s failed", table));
		}
		return rowId;
	}

	/**
	 * Does an update
	 * @param db
	 * @param table
	 * @param contentValues
	 * @param whereClause
	 * @param whereArgs
	 * @return number of rows affected
	 */
	public int update(SQLiteDatabase db, String table, ContentValues contentValues, String whereClause, String... whereArgs) {
		return db.update(table, contentValues, whereClause, whereArgs);
	}

	/**
	 * Does a delete (after first logging the delete statement)
	 * @param db
	 * @param table
	 * @param whereClause
	 * @param whereArgs
	 */
	public void delete(SQLiteDatabase db, String table, String whereClause, String... whereArgs) {
		db.delete(table, whereClause, whereArgs == null ? new String[0] : whereArgs);
	}

	/**
	 * Resets all cached data and deletes the database for all users.
	 *
	 * @param ctx Context.
	 */
	public synchronized void reset(Context ctx) {
		clearMemoryCache();
		final List<UserAccount> accounts = SmartStoreSDKManager.getInstance().getUserAccountManager().getAuthenticatedUsers();
		if (accounts != null) {
			for (final UserAccount account : accounts) {
				reset(ctx, account);
			}
		}
	}

	/**
	 * Resets all cached data and deletes the database for the specified user.
	 *
	 * @param ctx Context.
	 * @param account User account.
	 */
	public synchronized void reset(Context ctx, UserAccount account) {
		clearMemoryCache();
		DBOpenHelper.deleteDatabase(ctx, account);
	}

	/**
	 * Resets all cached data from memory.
	 */
	public synchronized void clearMemoryCache() {
		soupNameToExistMap.evictAll();
		soupNameToTableNamesMap.evictAll();
		soupNameToIndexSpecsMap.evictAll();
		tableNameToInsertHelpersMap.evictAll();
		tableNameToNextIdStatementsMap.evictAll();
		rawCountSqlToStatementsMap.evictAll();
	}

    /**
     * Return column name in soup table that holds the soup projection for path
	 * @param db
     * @param soupName
     * @param path
     * @return
     */
    public String getColumnNameForPath(SQLiteDatabase db, String soupName, String path) {
        IndexSpec[] indexSpecs = getIndexSpecs(db, soupName);
        for (IndexSpec indexSpec : indexSpecs) {
            if (indexSpec.path.equals(path)) {
                return indexSpec.columnName;
            }
        }
        throw new SmartStoreException(String.format("%s does not have an index on %s", soupName, path));
    }

	/**
	 * Return true if the given path is indexed on the given soup
	 * @param db
	 * @param soupName
	 * @param path
	 * @return
	 */
	public boolean hasIndexForPath(SQLiteDatabase db, String soupName, String path) {
		IndexSpec[] indexSpecs = getIndexSpecs(db, soupName);
		if (indexSpecs != null) {
			for (IndexSpec indexSpec : indexSpecs) {
				if (indexSpec.path.equals(path)) {
					return true;
				}
			}
		}
		return false;
	}

    /**
     * Read index specs back from the soup index map table
     * @param db
     * @param soupName
     * @return
     */
    public IndexSpec[] getIndexSpecs(SQLiteDatabase db, String soupName) {
        IndexSpec[] indexSpecs = getCachedIndexSpecs(soupName);
        if (indexSpecs == null) {
            indexSpecs = getIndexSpecsFromDb(db, soupName);
            cacheIndexSpecs(soupName, indexSpecs);
        }
        return indexSpecs;
    }

    protected IndexSpec[] getIndexSpecsFromDb(SQLiteDatabase db, String soupName) {
        Cursor cursor = null;
        try {
            cursor = query(db, SmartStore.SOUP_INDEX_MAP_TABLE, new String[] {SmartStore.PATH_COL, SmartStore.COLUMN_NAME_COL, SmartStore.COLUMN_TYPE_COL}, null,
                    null, SmartStore.SOUP_NAME_PREDICATE, soupName);

            if (!cursor.moveToFirst()) {
                throw new SmartStoreException(String.format("%s does not have any indices", soupName));
            }
            List<IndexSpec> indexSpecs = new ArrayList<>();
            do {
                String path = cursor.getString(cursor.getColumnIndex(SmartStore.PATH_COL));
                String columnName = cursor.getString(cursor.getColumnIndex(SmartStore.COLUMN_NAME_COL));
                Type columnType = Type.valueOf(cursor.getString(cursor.getColumnIndex(SmartStore.COLUMN_TYPE_COL)));
                indexSpecs.add(new IndexSpec(path, columnType, columnName));
            } while (cursor.moveToNext());
            return indexSpecs.toArray(new IndexSpec[0]);
        }
        finally {
            safeClose(cursor);
        }
    }

	/**
	 * @param db
	 * @param soupName
	 * @return true if soup has full-text-search index
	 */
	public boolean hasFTS(SQLiteDatabase db, String soupName) {
		getIndexSpecs(db, soupName); // will populate cache if needed
		return getCachedHasFTS(soupName);
	}

    /**
     * Return table name for a given soup or null if the soup doesn't exist
     * @param db
     * @param soupName
     * @return 
    */
   public String getSoupTableName(SQLiteDatabase db, String soupName) {
       String soupTableName = getCachedTableName(soupName);
       if (soupTableName == null) {
           soupTableName = getSoupTableNameFromDb(db, soupName);
           if (soupTableName != null) {
               cacheTableName(soupName, soupTableName);
           }
           // Note: if you ask twice about a non-existing soup, we go to the database both times
           //       we could optimize for that scenario but it doesn't seem very important
       }
       return soupTableName;
   }

	/**
	 * Return true if given soup exists
	 * @param db
	 * @param soupName
	 * @return
	 */
   public boolean hasSoup(SQLiteDatabase db, String soupName) {
	   Boolean exist = soupNameToExistMap.get(soupName);
	   if (exist != null) {
		   return exist;
	   } else {
		   boolean hasSoup = getSoupTableName(db, soupName) != null;
		   cacheHasSoup(soupName, hasSoup);
		   return hasSoup;
	   }
   }

	/**
	 * If turned on, explain query plan is run before executing a query and stored in lastExplainQueryPlan
	 * and also get logged
	 * @param captureExplainQueryPlan true to turn capture on and false to turn off
     */
	public void setCaptureExplainQueryPlan(boolean captureExplainQueryPlan) {
		this.captureExplainQueryPlan = captureExplainQueryPlan;
	}

	/**
	 * @return explain query plan for last query run (if captureExplainQueryPlan is true)
     */
	public JSONObject getLastExplainQueryPlan() {
		return lastExplainQueryPlan;
	}


   protected String getSoupTableNameFromDb(SQLiteDatabase db, String soupName) {
       Cursor cursor = null;
       try {
           cursor = query(db, SmartStore.SOUP_ATTRS_TABLE, new String[] {SmartStore.ID_COL}, null, null, SmartStore.SOUP_NAME_PREDICATE, soupName);
           if (!cursor.moveToFirst()) {
               return null;
           }
           return SmartStore.getSoupTableName(cursor.getLong(cursor.getColumnIndex(SmartStore.ID_COL)));
       	}
       	finally {
           safeClose(cursor);
       	}
   	}

    /**
     * @param cursor
     */
    protected void safeClose(Cursor cursor) {
        if (cursor != null) {
            cursor.close();
        }
    }
}
