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
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Salesforce.SDK.Auth;
using Salesforce.SDK.Core;
using SQLitePCL;
using SQLitePCL.Extensions;
using Salesforce.SDK.Settings;

namespace Salesforce.SDK.SmartStore.Store
{
    public class SmartStore : ISmartStore
    {
        // Default
        public static readonly int DefaultPageSize = 10;
        public static readonly string Soup = "_soup";

        // Table to keep track of soup names
        internal static readonly string SoupNamesTable = "soup_names";

        // Table to keep track of soup's index specs
        internal static readonly string SoupIndexMapTable = "soup_index_map";

        // Table to keep track of status of Long operations in flight
        internal static readonly string LongOperationsStatusTable = "long_operations_status";

        // Columns of the soup index map table
        public static readonly string SoupNameCol = "soupName";
        public static readonly string PathCol = "path";
        public static readonly string ColumnNameCol = "columnName";
        public static readonly string ColumnTypeCol = "columnType";

        // Columns of a soup table
        public static readonly string IdCol = "id";
        public static readonly string CreatedCol = "created";
        public static readonly string LastModifiedCol = "lastModified";
        public static readonly string SoupCol = "soup";

        // Columns of Lon operations status table
        public static readonly string TypeCol = "type";
        public static readonly string DetailsCol = "details";
        public static readonly string StatusCol = "status";

        // JSON fields added to soup element on insert/update
        public static readonly string SoupEntryId = "_soupEntryId";
        public static readonly string SoupLastModifiedDate = "_soupLastModifiedDate";

        // Predicates
        public static readonly string SoupNamePredicate = SoupNameCol + " = ?";
        public static readonly string PathPredicate = PathCol + " = ?";
        public static readonly string IdPredicate = IdCol + " = ?";

        private static readonly object smartlock = new object();
        private static readonly Regex SmartSqlRegex = new Regex("\\{([^}]+)\\}", RegexOptions.IgnoreCase);
        private static readonly DateTime Jan1st1970 = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        // Backing database
        private string _databasePath;
        private static IApplicationInformationService AppInfoService => SDKServiceLocator.Get<IApplicationInformationService>();

        public string DatabasePath
        {
            get
            {
                return _databasePath;
            }
        }

        public DBHelper Database
        {
            get { return DBHelper.GetInstance(DatabasePath); }
        }

        public static long CurrentTimeMillis
        {
            get { return (long)DateTime.UtcNow.Ticks; }
        }

        public SmartStore()
        {
            _databasePath = GenerateDatabasePath(AccountManager.GetAccount());
            CreateMetaTables();
        }

        private SmartStore(Account account)
        {
            _databasePath = GenerateDatabasePath(account);
            CreateMetaTables();
        }

        public static SmartStore GetSmartStore()
        {
            var store = new SmartStore();
            return store;
        }

        public static SmartStore GetSmartStore(Account account)
        {
            var store = new SmartStore(account);
            return store;
        }

        public static SmartStore GetGlobalSmartStore()
        {
            var store = new SmartStore(null); // generate a "global" smartstore
            return store;
        }

        public static string GenerateDatabasePath(Account account)
        {
            DBOpenHelper open = DBOpenHelper.GetOpenHelper(account);
            var localPath = AppInfoService.GetApplicationLocalFolderPath();
            return Path.Combine(localPath, open.DatabaseFile);
        }

        public static async Task<bool> HasGlobalSmartStore()
        {
            return await HasSmartStore(null);
        }

        public static async Task<bool> HasSmartStore(Account account)
        {
            var path = DBOpenHelper.GetOpenHelper(account).DatabaseFile;
            return await AppInfoService.DoesFileExistAsync(path);
        }

        /// <summary>
        ///     Create soup index map table to keep track of soups' index specs
        ///     Create soup name map table to keep track of soup name to table name mappings
        ///     Called when the database is first created
        /// </summary>
        public void CreateMetaTables()
        {
            lock (smartlock)
            {
                DBHelper db = DBHelper.GetInstance(DatabasePath);
                // Create soup_index_map table
                var sb = new StringBuilder();
                db.BeginTransaction();
                sb.Append("CREATE TABLE IF NOT EXISTS ").Append(SoupIndexMapTable).Append(" (")
                    .Append(SoupNameCol).Append(" TEXT")
                    .Append(",").Append(PathCol).Append(" TEXT")
                    .Append(",").Append(ColumnNameCol).Append(" TEXT")
                    .Append(",").Append(ColumnTypeCol).Append(" TEXT")
                    .Append(")");
                db.Execute(sb.ToString());
                db.Execute(String.Format("CREATE INDEX IF NOT EXISTS {0} on {1} ( {2} )", SoupIndexMapTable + "_0",
                    SoupIndexMapTable, SoupNameCol));

                // Create soup_names table
                // The table name for the soup will simply be table_<soupId>
                sb = new StringBuilder();
                sb.Append("CREATE TABLE IF NOT EXISTS ").Append(SoupNamesTable).Append(" (")
                    .Append(IdCol).Append(" INTEGER PRIMARY KEY AUTOINCREMENT")
                    .Append(",").Append(SoupNameCol).Append(" TEXT")
                    .Append(")");
                db.Execute(sb.ToString());

                db.CommitTransaction();
                // Create alter_soup_status table
                CreateLongOperationsStatusTable(db);
            }
        }

        /// <summary>
        ///     This method will drop all tables from the current database, including soup index and soup names.
        /// </summary>
        public void ResetDatabase()
        {
            lock (smartlock)
            {
                DBHelper db = DBHelper.GetInstance(DatabasePath);
                try
                {
                    DropAllSoups();
                }
                catch (Exception)
                {
                    // it's ok
                }
                SQLiteResult result = db.Execute("DROP TABLE IF EXISTS " + SoupIndexMapTable);
                db.Execute("DROP TABLE IF EXISTS " + SoupNamesTable);
                CreateMetaTables();
            }
        }

        public static void DeleteAllDatabases(bool includeGlobal)
        {
            var accounts = AccountManager.GetAccounts();
            foreach (var accountKey in accounts.Keys)
            {
                var account = accounts[accountKey];
                var path = DBOpenHelper.GetOpenHelper(account).DatabaseFile;
                var helper = DBHelper.GetInstance(GenerateDatabasePath(account));
                DropAllSoups(path);
                SQLiteResult result = helper.Execute("DROP TABLE IF EXISTS " + SoupIndexMapTable);
                helper.Execute("DROP TABLE IF EXISTS " + SoupNamesTable);
                helper.Dispose();
            }
            if (includeGlobal)
            {
                var path = DBOpenHelper.GetOpenHelper(null).DatabaseFile;
                var helper = DBHelper.GetInstance(GenerateDatabasePath(null));
                DropAllSoups(path);
                SQLiteResult result = helper.Execute("DROP TABLE IF EXISTS " + SoupIndexMapTable);
                helper.Execute("DROP TABLE IF EXISTS " + SoupNamesTable);
                helper.Dispose();
            }
        }

        /// <summary>
        ///     Create long_operations_status table
        /// </summary>
        /// <param name="connection"></param>
        private static void CreateLongOperationsStatusTable(DBHelper db)
        {
            var sb = new StringBuilder();
            sb.Append("CREATE TABLE IF NOT EXISTS ").Append(LongOperationsStatusTable).Append(" (")
                .Append(IdCol).Append(" INTEGER PRIMARY KEY AUTOINCREMENT")
                .Append(",").Append(TypeCol).Append(" TEXT")
                .Append(",").Append(DetailsCol).Append(" TEXT")
                .Append(",").Append(StatusCol).Append(" TEXT")
                .Append(", ").Append(CreatedCol).Append(" INTEGER")
                .Append(", ").Append(LastModifiedCol).Append(" INTEGER")
                .Append(")");
            db.Execute(sb.ToString());
        }

        /// <summary>
        ///     Create table for soupName with a column for the soup itself and columns for paths specified in indexSpecs
        ///     Create indexes on the new table to make lookup faster
        ///     Create rows in soup index map table for indexSpecs
        /// </summary>
        /// <param name="soupName"></param>
        /// <param name="indexSpecs"></param>
        public void RegisterSoup(String soupName, IndexSpec[] indexSpecs)
        {
            lock (smartlock)
            {
                if (soupName == null) throw new SmartStoreException("Null soup name");
                if (indexSpecs.Length == 0)
                    throw new SmartStoreException("No indexSpecs specified for soup: " + soupName);
                if (HasSoup(soupName)) return; // soup already exist - do nothing

                // First get a table name
                String soupTableName = null;
                var soupMapValues = new Dictionary<string, object> { { SoupNameCol, soupName } };
                DBHelper db = DBHelper.GetInstance(DatabasePath);
                try
                {
                    db.BeginTransaction();
                    long soupId = db.Insert(SoupNamesTable, soupMapValues);
                    soupTableName = GetSoupTableName(soupId);
                }
                finally
                {
                    db.CommitTransaction();
                }

                if (!String.IsNullOrWhiteSpace(soupTableName))
                {
                    // Do the rest - create table / indexes
                    RegisterSoupUsingTableName(soupName, indexSpecs, soupTableName);
                }
            }
        }

        /// <summary>
        ///     Helper method for registerSoup
        /// </summary>
        /// <param name="soupName"></param>
        /// <param name="indexSpecs"></param>
        /// <param name="soupTableName"></param>
        private void RegisterSoupUsingTableName(string soupName, IndexSpec[] indexSpecs, string soupTableName)
        {
            DBHelper db = DBHelper.GetInstance(DatabasePath);
            // Prepare SQL for creating soup table and its indices
            var createTableStmt = new StringBuilder(); // to create new soup table
            var createIndexStmts = new List<String>(); // to create indices on new soup table
            var soupIndexMapInserts = new List<Dictionary<string, object>>(); // to be inserted in soup index map table

            createTableStmt.Append("CREATE TABLE ").Append(soupTableName).Append(" (")
                .Append(IdCol).Append(" INTEGER PRIMARY KEY AUTOINCREMENT")
                .Append(", ").Append(SoupCol).Append(" TEXT")
                .Append(", ").Append(CreatedCol).Append(" INTEGER")
                .Append(", ").Append(LastModifiedCol).Append(" INTEGER");

            int i = 0;
            var indexSpecsToCache = new IndexSpec[indexSpecs.Length];
            foreach (IndexSpec indexSpec in indexSpecs)
            {
                // for create table
                String columnName = soupTableName + "_" + i;
                String columnType = indexSpec.SmartType.ColumnType;
                createTableStmt.Append(", ").Append(columnName).Append(" ").Append(columnType);

                // for insert
                var values = new Dictionary<string, object>
                {
                    {SoupNameCol, soupName},
                    {PathCol, indexSpec.Path},
                    {ColumnNameCol, columnName},
                    {ColumnTypeCol, indexSpec.SmartType.ColumnType}
                };
                soupIndexMapInserts.Add(values);

                // for create index
                String indexName = soupTableName + "_" + i + "_idx";
                createIndexStmts.Add(String.Format("CREATE INDEX {0} on {1} ( {2} )", indexName, soupTableName,
                    columnName));

                // for the cache
                indexSpecsToCache[i] = new IndexSpec(indexSpec.Path, indexSpec.SmartType, columnName);

                i++;
            }
            createTableStmt.Append(")");

            // Run SQL for creating soup table and its indices
            db.Execute(createTableStmt.ToString());
            foreach (String createIndexStmt in createIndexStmts)
            {
                db.Execute(createIndexStmt);
            }
            try
            {
                db.BeginTransaction();
                foreach (var values in soupIndexMapInserts)
                {
                    db.Insert(SoupIndexMapTable, values);
                }

                // Add to soupNameToTableNamesMap
                db.CacheTableName(soupName, soupTableName);

                // Add to soupNameToIndexSpecsMap
                db.CacheIndexSpecs(soupName, indexSpecsToCache);
            }
            finally
            {
                db.CommitTransaction();
            }
        }

        /// <summary>
        ///     Re-index all soup elements for passed indexPaths
        /// </summary>
        /// <param name="soupName"></param>
        /// <param name="indexPaths"></param>
        /// <param name="handleTx"></param>
        public void ReIndexSoup(string soupName, string[] indexPaths, bool handleTx)
        {
            lock (smartlock)
            {
                DBHelper db = DBHelper.GetInstance(DatabasePath);
                string soupTableName = db.GetSoupTableName(soupName);
                if (String.IsNullOrWhiteSpace(soupTableName))
                {
                    throw new SmartStoreException("Soup: " + soupName + " does not exist");
                }
                Dictionary<string, IndexSpec> mapAllSpecs = IndexSpec.MapForIndexSpecs(GetSoupIndexSpecs(soupName));
                IndexSpec[] indexSpecs =
                    (from indexPath in indexPaths where mapAllSpecs.ContainsKey(indexPath) select mapAllSpecs[indexPath])
                        .ToArray();
                if (indexSpecs.Length == 0)
                {
                    return; // nothing to do
                }
                if (handleTx)
                {
                    db.BeginTransaction();
                }

                using (
                    ISQLiteStatement stmt = db.Query(soupTableName, new[] { IdCol, SoupCol }, String.Empty, String.Empty,
                        String.Empty))
                {
                    if (stmt.DataCount > 0)
                    {
                        try
                        {
                            do
                            {
                                string soupEntryId = stmt.GetText(0);
                                string soupRaw = stmt.GetText(1);

                                try
                                {
                                    JObject soupElt = JObject.Parse(soupRaw);
                                    var contentValues = new Dictionary<string, object>();
                                    foreach (IndexSpec indexSpec in indexSpecs)
                                    {
                                        ProjectIndexedPaths(soupElt, contentValues, indexSpec);
                                    }
                                    db.Update(soupTableName, contentValues, IdPredicate, soupEntryId + String.Empty);
                                }
                                catch (JsonException e)
                                {
                                    Debug.WriteLine("SmartStore.ReIndexSoup: Could not parse soup element " +
                                                    soupEntryId +
                                                    "\n" + e.Message);
                                }
                            } while (stmt.Step() == SQLiteResult.ROW);
                        }
                        finally
                        {
                            if (handleTx)
                            {
                                db.CommitTransaction();
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        ///     Return indexSpecs of soup
        /// </summary>
        /// <param name="soupName"></param>
        /// <returns></returns>
        public IndexSpec[] GetSoupIndexSpecs(string soupName)
        {
            lock (smartlock)
            {
                DBHelper db = DBHelper.GetInstance(DatabasePath);
                string soupTableName = db.GetSoupTableName(soupName);
                if (String.IsNullOrWhiteSpace(soupTableName))
                {
                    throw new SmartStoreException("Soup: " + soupName + " does not exist");
                }
                return db.GetIndexSpecs(soupName);
            }
        }

        /// <summary>
        ///     Clear all rows from a soup
        /// </summary>
        /// <param name="soupName"></param>
        public void ClearSoup(string soupName)
        {
            lock (smartlock)
            {
                DBHelper db = DBHelper.GetInstance(DatabasePath);
                string soupTableName = db.GetSoupTableName(soupName);
                if (String.IsNullOrWhiteSpace(soupTableName))
                {
                    throw new SmartStoreException("Soup: " + soupName + " does not exist");
                }
                db.BeginTransaction();
                try
                {
                    db.Delete(soupName);
                }
                finally
                {
                    db.CommitTransaction();
                }
            }
        }

        /// <summary>
        ///     Check if soup exists
        /// </summary>
        /// <param name="soupName"></param>
        /// <returns></returns>
        public bool HasSoup(string soupName)
        {
            lock (smartlock)
            {
                return DBHelper.GetInstance(DatabasePath).GetSoupTableName(soupName) != null;
            }
        }

        /// <summary>
        ///     Destroy a soup; cleanup of all entries in the soup index map table and drops the soup table.
        /// </summary>
        /// <param name="soupName"></param>
        public void DropSoup(string soupName)
        {
            DropSoup(DatabasePath, soupName);
        }

        private static void DropSoup(string databasePath, string soupName)
        {
            lock (smartlock)
            {
                DBHelper db = DBHelper.GetInstance(databasePath);
                string soupTableName = db.GetSoupTableName(soupName);
                if (!String.IsNullOrWhiteSpace(soupTableName))
                {
                    SQLiteResult result = db.Execute("DROP TABLE IF EXISTS " + soupTableName);
                    bool success = result == SQLiteResult.DONE;
                    var soupDrop = new Dictionary<string, object> { { SoupNameCol, soupName } };
                    try
                    {
                        db.BeginTransaction();
                        success &= db.Delete(SoupNamesTable, soupDrop);
                        success &= db.Delete(SoupIndexMapTable, soupDrop);
                    }
                    finally
                    {
                        if (success)
                        {
                            db.CommitTransaction();
                            db.RemoveFromCache(soupName);
                        }
                        else
                        {
                            db.RollbackTransaction();
                        }
                    }
                }
            }
        }

        /// <summary>
        ///     Clear all soups and clean the table indexes for all removed soups.
        /// </summary>
        public void DropAllSoups()
        {
            lock (smartlock)
            {
                List<string> soupNames = GetAllSoupNames();
                foreach (string soup in soupNames)
                {
                    DropSoup(soup);
                }
            }
        }

        public static void DropAllSoups(string databasePath)
        {
            lock (smartlock)
            {
                List<string> soupNames = GetAllSoupNames(databasePath);
                foreach (string soup in soupNames)
                {
                    DropSoup(databasePath, soup);
                }
            }
        }
        /// <summary>
        ///     Returns a list of all soup names.
        /// </summary>
        /// <returns></returns>
        public List<string> GetAllSoupNames()
        {
            return GetAllSoupNames(DatabasePath);
        }

        private static List<string> GetAllSoupNames(string databasePath)
        {
            lock (smartlock)
            {
                var soupNames = new List<string>();
                DBHelper db = DBHelper.GetInstance(databasePath);
                using (ISQLiteStatement stmt = db.Query(SoupNamesTable, new[] { SoupNameCol }, String.Empty, String.Empty,
                    String.Empty))
                {
                    if (stmt.DataCount > 0)
                    {
                        do
                        {
                            soupNames.Add(stmt.GetText(0));
                        } while (stmt.Step() == SQLiteResult.ROW);
                    }
                }
                return soupNames;
            }
        }

        /// <summary>
        ///     Run a query given by its query spec, only returning results from the selected page.
        /// </summary>
        /// <param name="querySpec"></param>
        /// <param name="pageIndex"></param>
        /// <returns></returns>
        public JArray Query(QuerySpec querySpec, int pageIndex)
        {
            lock (smartlock)
            {
                QuerySpec.SmartQueryType qt = querySpec.QueryType;
                var results = new JArray();
                string sql = ConvertSmartSql(querySpec.SmartSql);
                int offsetRows = querySpec.PageSize * pageIndex;
                int numberRows = querySpec.PageSize;
                string limit = offsetRows + "," + numberRows;

                using (ISQLiteStatement statement = DBHelper.GetInstance(DatabasePath)
                    .LimitRawQuery(sql, limit, querySpec.getArgs()))
                {
                    if (statement.DataCount > 0)
                    {
                        do
                        {
                            if (qt == QuerySpec.SmartQueryType.Smart)
                            {
                                results.Add(GetDataFromRow(statement));
                            }
                            else
                            {
                                results.Add(JObject.Parse(GetObject(statement, 0).ToString()));
                            }
                        } while (statement.Step() == SQLiteResult.ROW);
                    }
                    statement.ResetAndClearBindings();
                }
                return results;
            }
        }

        /// <summary>
        ///     count of results for a "smart" query
        /// </summary>
        /// <param name="querySpec"></param>
        /// <returns></returns>
        public long CountQuery(QuerySpec querySpec)
        {
            lock (smartlock)
            {
                String countSql = ConvertSmartSql(querySpec.CountSmartSql);
                return DBHelper.GetInstance(DatabasePath).CountRawCountQuery(countSql, querySpec.getArgs());
            }
        }

        /// <summary>
        ///     Helper to retrieve data.
        /// </summary>
        /// <param name="statement"></param>
        /// <param name="position"></param>
        /// <returns></returns>
        private object GetObject(ISQLiteStatement statement, int position)
        {
            try
            {
                return statement.GetText(position);
            }
            catch (Exception) { }

            try
            {
                return statement.GetInteger(position);
            }
            catch (Exception) { }

            try
            {
                return statement.GetFloat(position);
            }
            catch (Exception) { }

            return null;
        }

        /// <summary>
        ///     Return JSONArray for one row of data from the statement
        /// </summary>
        /// <param name="statement"></param>
        /// <returns></returns>
        private JArray GetDataFromRow(ISQLiteStatement statement)
        {
            var row = new JArray();
            int columnCount = statement.ColumnCount;
            for (int i = 0; i < columnCount; i++)
            {
                if (statement.ColumnName(i).EndsWith(SoupCol))
                {
                    string raw = statement.GetText(i);
                    row.Add(JObject.Parse(raw));
                }
                else
                {
                    object raw = GetObject(statement, i);
                    if (raw != null)
                    {
                        long value;
                        if (long.TryParse(raw.ToString(), out value))
                        {
                            row.Add(new JValue(value));
                        }
                        else
                        {
                            double dvalue;
                            if (double.TryParse(raw.ToString(), out dvalue))
                            {
                                row.Add(new JValue(dvalue));
                            }
                            else
                            {
                                row.Add(raw.ToString());
                            }
                        }
                    }
                }
            }
            return row;
        }

        public bool Delete(string soupName, long[] soupEntryIds, Boolean handleTx)
        {
            lock (smartlock)
            {
                bool success = false;
                DBHelper db = DBHelper.GetInstance(DatabasePath);
                string soupTableName = db.GetSoupTableName(soupName);
                if (String.IsNullOrWhiteSpace(soupTableName))
                    throw new SmartStoreException("Soup: " + soupName + " does not exist");
                if (handleTx)
                {
                    db.BeginTransaction();
                }
                try
                {
                    success = db.Delete(soupTableName, GetSoupEntryIdsPredicate(soupEntryIds));
                }
                finally
                {
                    if (handleTx)
                    {
                        db.CommitTransaction();
                    }
                }
                return success;
            }
        }

        private void ProjectIndexedPaths(JObject soupElt, Dictionary<string, object> contentValues, IndexSpec indexSpec)
        {
            object value = Project(soupElt, indexSpec.Path);
            if (value == null)
                return;
            if (contentValues == null)
            {
                contentValues = new Dictionary<string, object>();
            }
            if (SmartStoreType.SmartInteger.ColumnType.Equals(indexSpec.SmartType.ColumnType))
            {
                contentValues.Add(indexSpec.ColumnName, Int32.Parse(value.ToString()));
            }
            else if (SmartStoreType.SmartString.ColumnType.Equals(indexSpec.SmartType.ColumnType))
            {
                contentValues.Add(indexSpec.ColumnName, value.ToString());
            }
            else if (SmartStoreType.SmartFloating.ColumnType.Equals(indexSpec.SmartType.ColumnType))
            {
                contentValues.Add(indexSpec.ColumnName, Double.Parse(value.ToString()));
            }
        }

        public JObject Create(string soupName, JObject soupElt)
        {
            lock (smartlock)
            {
                return Create(soupName, soupElt, true);
            }
        }

        public JObject Create(string soupName, JObject soupElt, bool handleTx)
        {
            lock (smartlock)
            {
                DBHelper db = DBHelper.GetInstance(DatabasePath);
                string soupTableName = db.GetSoupTableName(soupName);
                IndexSpec[] indexSpecs = db.GetIndexSpecs(soupName);
                if (handleTx)
                {
                    db.BeginTransaction();
                }
                long now = CurrentTimeMillis;
                long soupEntryId = db.GetNextId(soupTableName);

                soupElt[SoupEntryId] = soupEntryId;
                soupElt[SoupLastModifiedDate] = now;
                var contentValues = new Dictionary<string, object>
                {
                    {IdCol, soupEntryId},
                    {CreatedCol, now},
                    {LastModifiedCol, now},
                    {SoupCol, soupElt.ToString()}
                };
                foreach (IndexSpec indexSpec in indexSpecs)
                {
                    ProjectIndexedPaths(soupElt, contentValues, indexSpec);
                }

                bool success = db.Insert(soupTableName, contentValues) == soupEntryId;

                if (success)
                {
                    if (handleTx)
                    {
                        db.CommitTransaction();
                    }
                    return soupElt;
                }
                if (handleTx)
                {
                    db.RollbackTransaction();
                }
                return null;
            }
        }

        public JObject Upsert(string soupName, JObject soupElt, string externalIdPath)
        {
            lock (smartlock)
            {
                return Upsert(soupName, soupElt, externalIdPath, false);
            }
        }

        public JObject Upsert(string soupName, JObject soupElt)
        {
            lock (smartlock)
            {
                return Upsert(soupName, soupElt, SoupEntryId);
            }
        }

        public JObject Upsert(string soupName, JObject soupElt, string externalIdPath, bool handleTx)
        {
            lock (smartlock)
            {
                long entryId = -1;
                if (SoupEntryId.Equals(externalIdPath, StringComparison.CurrentCultureIgnoreCase))
                {
                    JToken entryIdToken;
                    if (soupElt.TryGetValue(SoupEntryId, out entryIdToken))
                    {
                        entryId = entryIdToken.Value<long>();
                    }
                }
                else
                {
                    object externalIdObj = Project(soupElt, externalIdPath);
                    if (externalIdObj != null)
                    {
                        entryId = LookupSoupEntryId(soupName, externalIdPath, externalIdObj.ToString());
                    }
                }
                if (entryId != -1)
                {
                    return Update(soupName, soupElt, entryId, handleTx);
                }
                return Create(soupName, soupElt, handleTx);
            }
        }

        public long LookupSoupEntryId(string soupName, string fieldPath, string fieldValue)
        {
            lock (smartlock)
            {
                DBHelper db = Database;
                string soupTableName = db.GetSoupTableName(soupName);
                if (String.IsNullOrWhiteSpace(soupTableName))
                {
                    throw new SmartStoreException("Soup: " + soupName + " does not exist");
                }
                string columnName = db.GetColumnNameForPath(soupName, fieldPath);
                using (ISQLiteStatement statement = db.Query(soupTableName, new[] { IdCol }, String.Empty, String.Empty,
                    columnName + " = ?", fieldValue))
                {
                    if (statement.DataCount > 1)
                    {
                        throw new SmartStoreException(String.Format(
                            "There are more than one soup elements where {0} is {1}", fieldPath, fieldValue));
                    }
                    if (statement.DataCount == 1)
                    {
                        return statement.GetInteger(0);
                    }
                }
                return -1; // not found
            }
        }

        public JObject Update(String soupName, JObject soupElt, long soupEntryId, bool handleTx)
        {
            lock (smartlock)
            {
                DBHelper db = Database;
                string soupTableName = db.GetSoupTableName(soupName);
                if (String.IsNullOrWhiteSpace(soupTableName))
                {
                    throw new SmartStoreException("Soup: " + soupName + " does not exist");
                }
                IndexSpec[] indexSpecs = db.GetIndexSpecs(soupName);
                long now = CurrentTimeMillis;
                if (soupElt[SoupEntryId] == null)
                {
                    soupElt.Add(SoupEntryId, soupEntryId);
                }
                else
                {
                    soupElt[SoupEntryId] = soupEntryId;
                }
                if (soupElt[SoupLastModifiedDate] == null)
                {
                    soupElt.Add(SoupLastModifiedDate, now);
                }
                else
                {
                    soupElt[SoupLastModifiedDate] = now;
                }


                var contentValues = new Dictionary<string, object>
                {
                    {SoupCol, soupElt.ToString()},
                    {LastModifiedCol, now}
                };
                foreach (IndexSpec indexSpec in indexSpecs)
                {
                    ProjectIndexedPaths(soupElt, contentValues, indexSpec);
                }
                if (handleTx)
                {
                    db.BeginTransaction();
                }
                bool success = db.Update(soupTableName, contentValues, IdPredicate, soupEntryId.ToString());
                if (success)
                {
                    if (handleTx)
                    {
                        db.CommitTransaction();
                    }
                    return soupElt;
                }
                if (handleTx)
                {
                    db.RollbackTransaction();
                }
                return null;
            }
        }

        public JArray Retrieve(string soupName, params long[] soupEntryIds)
        {
            lock (smartlock)
            {
                DBHelper db = Database;
                string soupTableName = db.GetSoupTableName(soupName);
                var result = new JArray();
                if (String.IsNullOrWhiteSpace(soupTableName))
                {
                    throw new SmartStoreException("Soup: " + soupName + " does not exist");
                }
                using (ISQLiteStatement statement = db.Query(soupTableName, new[] { SoupCol }, String.Empty, String.Empty,
                    GetSoupEntryIdsPredicate(soupEntryIds)))
                {
                    if (statement.DataCount > 0)
                    {
                        do
                        {
                            string raw = statement.GetText(statement.ColumnIndex(SoupCol));
                            result.Add(JObject.Parse(raw));
                        } while (statement.Step() == SQLiteResult.ROW);
                    }
                }
                return result;
            }
        }

        private string GetSoupEntryIdsPredicate(IEnumerable<long> soupEntryIds)
        {
            return IdCol + " IN (" + String.Join(",", soupEntryIds) + ")";
        }

        public static object Project(JObject soup, string path)
        {
            if (soup == null)
            {
                return null;
            }
            if (String.IsNullOrWhiteSpace(path))
            {
                return soup;
            }
            string[] pathElements = path.Split(new[] { "[.]" }, StringSplitOptions.None);
            object o = soup;
            foreach (string pathElement in pathElements)
            {
                if (o != null)
                {
                    o = ((JObject)o).SelectToken(pathElement, false);
                }
            }
            return o;
        }

        public string ConvertSmartSql(string smartSql)
        {
            string lowered = smartSql.ToLower().Trim();
            if (lowered.StartsWith("insert") || lowered.StartsWith("update") || lowered.StartsWith("delete"))
            {
                throw new SmartStoreException("Only SELECT is supported");
            }
            string sql = SmartSqlRegex.Replace(smartSql, delegate (Match matcher)
            {
                string fullMatch = matcher.Value;
                string match = matcher.Groups[1].Value;
                int position = matcher.Index;
                string[] parts = match.Split(':');
                string soupName = parts[0];
                string soupTableName = GetSoupTableNameForSmartSql(soupName, position);
                bool tableQualified = smartSql.ToCharArray()[position - 1] == '.';
                string tableQualifier = tableQualified ? String.Empty : soupTableName + ".";
                if (parts.Length == 1)
                {
                    return soupTableName;
                }
                if (parts.Length == 2)
                {
                    string path = parts[1];
                    if (path.Equals(Soup))
                    {
                        return tableQualifier + SoupCol;
                    }
                    if (path.Equals(SoupEntryId))
                    {
                        return tableQualifier + IdCol;
                    }
                    if (path.Equals(SoupLastModifiedDate))
                    {
                        return tableQualifier + LastModifiedCol;
                    }
                    return GetColumnNameForPathForSmartSql(soupName, path, position);
                }
                if (parts.Length > 2)
                {
                    ReportSmartSqlError("Invalid soup/path reference " + fullMatch, position);
                }
                return String.Empty;
            });
            return sql;
        }

        private string GetColumnNameForPathForSmartSql(string soupName, string path, int position)
        {
            string columnName = null;
            try
            {
                columnName = DBHelper.GetInstance(DatabasePath).GetColumnNameForPath(soupName, path);
            }
            catch (SmartStoreException e)
            {
                ReportSmartSqlError(e.Message, position);
            }
            return columnName;
        }

        private string GetSoupTableNameForSmartSql(string soupName, int position)
        {
            string soupTableName = DBHelper.GetInstance(DatabasePath).GetSoupTableName(soupName);
            if (soupTableName == null)
            {
                ReportSmartSqlError("Unknown soup " + soupName, position);
            }
            return soupTableName;
        }

        private void ReportSmartSqlError(string message, int position)
        {
            throw new SmartStoreException(message + " at character " + position);
        }

        public static string GetSoupTableName(long soupId)
        {
            return "TABLE_" + soupId;
        }
    }
}