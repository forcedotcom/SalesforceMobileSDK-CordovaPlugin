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
using System.Reflection;
using System.Text;
using SQLitePCL;
using SQLitePCL.Extensions;

namespace Salesforce.SDK.SmartStore.Store
{
    public sealed class DBHelper : IDisposable
    {
        #region Statics

        // Some queries
        private const string CountSelect = "SELECT count(*) FROM {0} {1}";
        private const string SeqSelect = "SELECT seq FROM SQLITE_SEQUENCE WHERE name = ?";
        private const string LimitSelect = "SELECT * FROM ({0}) LIMIT {1}";
        private const string QueryStatement = "SELECT {0} FROM {1} {2} {3}{4}{5}";
        private const string InsertStatement = "INSERT INTO {0} ({1}) VALUES ({2});";
        private const string UpdateStatement = "UPDATE {0} SET {1} WHERE {2}";
        private const string DeleteStatement = "DELETE FROM {0} WHERE {1}";
        private const string DeleteAllStatement = "DELETE FROM {0}";
        private static Dictionary<string, DBHelper> _instances;
        private static Type _sqliteConnectionType = typeof(SQLiteConnection);
        #endregion

        #region DBHelper properties

        private readonly string DatabasePath;

        /// <summary>
        /// </summary>
        private readonly Dictionary<string, IndexSpec[]> SoupNameToIndexSpecsMap;

        /// <summary>
        ///     Cache of soup name to soup table names
        /// </summary>
        private readonly Dictionary<string, string> SoupNameToTableNamesMap;

        private ISQLiteConnection _sqlConnection;

        #endregion

        private DBHelper(string sqliteDb)
        {
            SoupNameToTableNamesMap = new Dictionary<string, string>();
            SoupNameToIndexSpecsMap = new Dictionary<string, IndexSpec[]>();
            DatabasePath = sqliteDb;
            _sqlConnection = (ISQLiteConnection) Activator.CreateInstance(_sqliteConnectionType, sqliteDb);

        }

        /// <summary>
        /// This static method exists if you need to extend SQLiteConnection to add more functionality.
        /// 
        /// This method will throw SmartStoreException if type isn't derived from SQLiteConnection.
        /// </summary>
        /// <param name="sqlConnectionType"></param>
        public static void SetSqliteConnectionClass(Type sqlConnectionType)
        {
            var sqlcontype = sqlConnectionType.GetTypeInfo();
            var iSqLiteConnectionTypeInfo = (typeof(ISQLiteConnection)).GetTypeInfo();
            // check to make sure whatever type is given implements the ISQLiteConnection interface
            var matches = iSqLiteConnectionTypeInfo.IsAssignableFrom(sqlcontype);
            if (sqlConnectionType == null || !matches &&
                sqlConnectionType != typeof (SQLiteConnection))
            {
                throw new SmartStoreException("sqlConnectionType must be SQLiteConnection or derived from SQLiteConnection");
            }
            _sqliteConnectionType = sqlConnectionType;
        }

        public static DBHelper GetInstance(string sqliteDbFile)
        {
            if (_instances == null)
            {
                _instances = new Dictionary<string, DBHelper>();
            }
            DBHelper instance;
            if (!_instances.TryGetValue(sqliteDbFile, out instance))
            {
                instance = new DBHelper(sqliteDbFile);
                _instances.Add(sqliteDbFile, instance);
            }
            return instance;
        }

        public void CacheTableName(string soupName, string tableName)
        {
            SoupNameToTableNamesMap.Add(soupName, tableName);
        }

        public string GetCachedTableName(string soupName)
        {
            string value;
            if (!SoupNameToTableNamesMap.TryGetValue(soupName, out value))
            {
                return null;
            }
            return value;
        }

        public void CacheIndexSpecs(string soupName, IndexSpec[] indexSpecs)
        {
            SoupNameToIndexSpecsMap.Add(soupName, indexSpecs);
        }

        public IndexSpec[] GetCachedIndexSpecs(string soupName)
        {
            IndexSpec[] value;
            if (!SoupNameToIndexSpecsMap.TryGetValue(soupName, out value))
            {
                return null;
            }
            return value;
        }

        public void RemoveFromCache(string soupName)
        {
            SoupNameToTableNamesMap.Remove(soupName);
            SoupNameToIndexSpecsMap.Remove(soupName);
        }

        public long GetNextId(string tableName)
        {
            using (ISQLiteStatement prog = _sqlConnection.Prepare((SeqSelect)))
            {
                prog.Bind(1, tableName);
                SQLiteResult result = prog.Step();
                long data = 0;
                if (prog.DataCount > 0)
                {
                     data = prog.GetInteger(0);
                }
                return data + 1;
            }
        }

        public ISQLiteStatement CountQuery(string table, string whereClause)
        {
            string selectionStr = (whereClause == null ? "" : " WHERE " + whereClause);
            string sql = String.Format(CountSelect, table, selectionStr);
            var stmt = _sqlConnection.Prepare(sql);
            if (stmt != null)
            {
                stmt.Step();
                return stmt;
            }
            throw new SmartStoreException("Invalid CountQuery statement");
        }

        public ISQLiteStatement LimitRawQuery(string sql, string limit, params string[] args)
        {
            string limitSql = String.Format(LimitSelect, sql, limit);
            var stmt = _sqlConnection.Prepare(limitSql);
            if (args != null)
            {
                for (int i = 0; i < args.Length; i++)
                {
                    stmt.Bind(i + 1, args[i]);
                }
            }
            if (stmt != null)
            {
                stmt.Step();
                return stmt;
            }
            throw new SmartStoreException("Invalid LimitRawQuery statement");
        }

        public long CountRawCountQuery(string countSql, params string[] args)
        {
            using (ISQLiteStatement prog = _sqlConnection.Prepare((countSql)))
            {
                if (args != null)
                {
                    for (int i = 0; i < args.Length; i++)
                    {
                        prog.Bind(i + 1, args[i]);
                    }
                }
                SQLiteResult result = prog.Step();
                if (result == SQLiteResult.ROW)
                {
                    return prog.GetInteger(0);
                }
            }
            return 0;
        }

        public long CountRawQuery(String sql, params string[] args)
        {
            string countSql = String.Format(CountSelect, "", "(" + sql + ")");
            return CountRawCountQuery(countSql, args);
        }

        public ISQLiteStatement Query(string table, string[] columns, string orderBy, string limit, string whereClause,
            params string[] args)
        {
            if (String.IsNullOrWhiteSpace(table) || columns == null || columns.Length == 0)
            {
                throw new InvalidOperationException("Must specify a table and columns to query");
            }
            if (String.IsNullOrWhiteSpace(whereClause))
            {
                whereClause = String.Empty;
            }
            else
            {
                whereClause = "WHERE " + whereClause;
            }
            string sql = String.Format(QueryStatement,
                String.Join(", ", columns),
                table,
                whereClause,
                orderBy,
                limit,
                String.Empty);
            ISQLiteStatement stmt = _sqlConnection.Prepare(sql);
            if (args != null)
            {
                for (int i = 0; i < args.Length; i++)
                {
                    stmt.Bind(i + 1, args[i]);
                }
            }
            SQLiteResult result = stmt.Step();
            return stmt;
        }

        public long Insert(string table, Dictionary<string, object> contentValues)
        {
            if (String.IsNullOrWhiteSpace(table) || contentValues == null || contentValues.Keys.Count == 0)
            {
                throw new InvalidOperationException("Must specify a table and provide content to insert");
            }
            string columns = String.Join(", ", contentValues.Keys);
            var valueBindingString = new StringBuilder();
            for (int i = 0, max = contentValues.Count; i < max; i++)
            {
                valueBindingString.Append("?");
                if ((i + 1) < max)
                {
                    valueBindingString.Append(", ");
                }
            }
            string sql = String.Format(InsertStatement,
                table,
                columns,
                valueBindingString);
            using (ISQLiteStatement stmt = _sqlConnection.Prepare(sql))
            {
                int count = 1;
                foreach (string key in contentValues.Keys)
                {
                    stmt.Bind(count, contentValues[key]);
                    count++;
                }
                var result = stmt.Step();
            }
            long insertId = _sqlConnection.LastInsertRowId();
            return insertId;
        }

        public bool Update(string table, Dictionary<string, object> contentValues, string whereClause,
            params string[] args)
        {
            if (String.IsNullOrWhiteSpace(table) || contentValues == null || contentValues.Keys.Count == 0)
            {
                throw new InvalidOperationException("Must specify a table and provide content to update");
            }
            if (String.IsNullOrWhiteSpace(whereClause))
            {
                whereClause = String.Empty;
            }
            string entries = String.Join("= ?, ", contentValues.Keys);
            if (contentValues.Keys.Count > 0)
            {
                entries += " = ?";
            }
            string sql = String.Format(UpdateStatement,
                table,
                entries,
                whereClause);
            int place = 1;
            using (ISQLiteStatement stmt = _sqlConnection.Prepare(sql))
            {
                foreach (object next in contentValues.Values)
                {
                    stmt.Bind(place, next);
                    place++;
                }
                if (!String.IsNullOrWhiteSpace(whereClause) && args != null && args.Length > 0)
                {
                    foreach (string next in args)
                    {
                        stmt.Bind(place, next);
                        place++;
                    }
                }
                var result = stmt.Step();
                return result == SQLiteResult.DONE;
            }
        }


        public bool Delete(string table, Dictionary<string, object> contentValues)
        {
            if (String.IsNullOrWhiteSpace(table) || contentValues == null || contentValues.Keys.Count == 0)
            {
                throw new InvalidOperationException("Must specify a table and provide content to delete");
            }
            string values = String.Join(" = ?, ", contentValues.Keys);
            if (contentValues.Keys.Count > 0)
            {
                values += " = ?";
            }
            string sql = String.Format(DeleteStatement,
                table,
                values);
            using (ISQLiteStatement stmt = _sqlConnection.Prepare(sql))
            {
                int place = 1;
                foreach (object next in contentValues.Values)
                {
                    stmt.Bind(place, next);
                    place++;
                }
                return stmt.Step() == SQLiteResult.DONE;
            }
        }

        public bool Delete(string table, string whereClause)
        {
            if (String.IsNullOrWhiteSpace(table) || String.IsNullOrWhiteSpace(whereClause))
            {
                throw new InvalidOperationException("Must specify a table and provide where clause to delete");
            }
            string sql = String.Format(DeleteStatement,
                table,
                whereClause);
            using (ISQLiteStatement stmt = _sqlConnection.Prepare(sql))
            {
                var result = stmt.Step();
                return result == SQLiteResult.DONE;
            }
        }

        public bool Delete(string table)
        {
            string sql = String.Format(DeleteAllStatement, table);
            using (ISQLiteStatement stmt = _sqlConnection.Prepare(sql))
            {
                return stmt.Step() == SQLiteResult.DONE;
            }
        }

        public string GetSoupTableName(string soupName)
        {
            string soupTableName = GetCachedTableName(soupName);
            if (String.IsNullOrWhiteSpace(soupTableName))
            {
                soupTableName = GetSoupTableNameFromDb(soupName);
                if (!String.IsNullOrWhiteSpace(soupTableName))
                {
                    CacheTableName(soupName, soupTableName);
                }
            }
            return soupTableName;
        }

        private string GetSoupTableNameFromDb(string soupName)
        {
            using (
                ISQLiteStatement stmt = Query(SmartStore.SoupNamesTable, new[] { SmartStore.IdCol }, String.Empty,
                    String.Empty,
                    SmartStore.SoupNamePredicate, soupName))
            {
                if (stmt.DataCount == 0)
                {
                    return null;
                }
                return SmartStore.GetSoupTableName(stmt.GetInteger(SmartStore.IdCol));
            }
        }

        internal void ResetConnection()
        {
            _sqlConnection.Dispose();
            _sqlConnection = new SQLiteConnection(DatabasePath);
        }

        public SQLiteResult Execute(string sql)
        {
            ISQLiteStatement statement;
            return Execute(sql, out statement);
        }

        public SQLiteResult Execute(string sql, out ISQLiteStatement statement)
        {
            statement = _sqlConnection.Prepare(sql);
            if (statement != null)
            {
                return statement.Step();
            }
            return SQLiteResult.ERROR;
        }

        public string GetColumnNameForPath(String soupName, String path)
        {
            IndexSpec[] indexSpecs = GetIndexSpecs(soupName);
            foreach (IndexSpec indexSpec in indexSpecs.Where(indexSpec => indexSpec.Path.Equals(path)))
            {
                return indexSpec.ColumnName;
            }
            throw new SmartStoreException(String.Format("{0} does not have an index on {1}", soupName, path));
        }

        public IndexSpec[] GetIndexSpecs(String soupName)
        {
            IndexSpec[] indexSpecs = GetCachedIndexSpecs(soupName);
            if (indexSpecs == null)
            {
                indexSpecs = GetIndexSpecsFromDb(soupName);
                CacheIndexSpecs(soupName, indexSpecs);
            }
            return indexSpecs;
        }

        private IndexSpec[] GetIndexSpecsFromDb(String soupName)
        {
            ISQLiteStatement statement = Query(SmartStore.SoupIndexMapTable,
                new[] { SmartStore.PathCol, SmartStore.ColumnNameCol, SmartStore.ColumnTypeCol }, null,
                null, SmartStore.SoupNamePredicate, soupName);

            if (statement.DataCount < 1)
            {
                throw new SmartStoreException(String.Format("{0} does not have any indices", soupName));
            }
            var indexSpecs = new List<IndexSpec>();
            do
            {
                String path = statement.GetText(SmartStore.PathCol);
                String columnName = statement.GetText(SmartStore.ColumnNameCol);
                var columnType = new SmartStoreType(statement.GetText(SmartStore.ColumnTypeCol));
                indexSpecs.Add(new IndexSpec(path, columnType, columnName));
            } while (statement.Step() == SQLiteResult.ROW);
            statement.ResetAndClearBindings();
            return indexSpecs.ToArray();
        }

        public bool BeginTransaction()
        {
            using (ISQLiteStatement stmt = _sqlConnection.Prepare("Begin Transaction"))
            {
                return SQLiteResult.DONE == stmt.Step();
            }
        }

        public bool CommitTransaction()
        {
            using (ISQLiteStatement stmt = _sqlConnection.Prepare("Commit Transaction"))
            {
                return SQLiteResult.DONE == stmt.Step();
            }
        }

        public bool RollbackTransaction()
        {
            using (ISQLiteStatement stmt = _sqlConnection.Prepare("Rollback Transaction"))
            {
                return SQLiteResult.DONE == stmt.Step();
            }
        }

        public void Dispose()
        {
            if (_sqlConnection != null)
            {
                _sqlConnection.Dispose();
            }
            _instances.Remove(DatabasePath);
            SoupNameToIndexSpecsMap.Clear();
            SoupNameToTableNamesMap.Clear();
        }
    }
}