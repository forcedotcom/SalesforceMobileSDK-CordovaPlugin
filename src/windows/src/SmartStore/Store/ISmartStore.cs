using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace Salesforce.SDK.SmartStore.Store
{
    public interface ISmartStore
    {
        DBHelper Database { get; }

        /// <summary>
        /// This method will drop all tables from the current database, including soup index and soup names.
        /// </summary>
        void ResetDatabase();

        /// <summary>
        /// Create table for soupName with a column for the soup itself and columns for paths specified in indexSpecs
        /// Create indexes on the new table to make lookup faster
        /// Create rows in soup index map table for indexSpecs
        /// </summary>
        /// <param name="soupName"></param>
        /// <param name="indexSpecs"></param>
        void RegisterSoup(String soupName, IndexSpec[] indexSpecs);

        /// <summary>
        /// Re-index all soup elements for passed indexPaths
        /// </summary>
        /// <param name="soupName"></param>
        /// <param name="indexPaths"></param>
        /// <param name="handleTx"></param>
        void ReIndexSoup(string soupName, string[] indexPaths, bool handleTx);

        /// <summary>
        /// Return indexSpecs of soup
        /// </summary>
        /// <param name="soupName"></param>
        /// <returns></returns>
        IndexSpec[] GetSoupIndexSpecs(string soupName);

        /// <summary>
        /// Clear all rows from a soup
        /// </summary>
        /// <param name="soupName"></param>
        void ClearSoup(string soupName);

        /// <summary>
        /// Check if soup exists
        /// </summary>
        /// <param name="soupName"></param>
        /// <returns></returns>
        bool HasSoup(string soupName);

        /// <summary>
        /// Destroy a soup; cleanup of all entries in the soup index map table and drops the soup table.
        /// </summary>
        /// <param name="soupName"></param>
        void DropSoup(string soupName);

        /// <summary>
        /// Clear all soups and clean the table indexes for all removed soups.
        /// </summary>
        void DropAllSoups();

        /// <summary>
        /// Returns a list of all soup names.
        /// </summary>
        /// <returns></returns>
        List<string> GetAllSoupNames();

        /// <summary>
        /// Run a query given by its query spec, only returning results from the selected page.
        /// </summary>
        /// <param name="querySpec"></param>
        /// <param name="pageIndex"></param>
        /// <returns></returns>
        JArray Query(QuerySpec querySpec, int pageIndex);

        /// <summary>
        /// count of results for a "smart" query
        /// </summary>
        /// <param name="querySpec"></param>
        /// <returns></returns>
        long CountQuery(QuerySpec querySpec);

        /// <summary>
        /// Delete a set of soup entiest
        /// </summary>
        /// <param name="soupName"></param>
        /// <param name="soupEntryIds"></param>
        /// <param name="handleTx"></param>
        /// <returns></returns>
        bool Delete(string soupName, long[] soupEntryIds, Boolean handleTx);

        /// <summary>
        /// Create a soup entry
        /// </summary>
        /// <param name="soupName"></param>
        /// <param name="soupElt"></param>
        /// <returns></returns>
        JObject Create(string soupName, JObject soupElt);

        /// <summary>
        /// Create a soup entry
        /// </summary>
        /// <param name="soupName"></param>
        /// <param name="soupElt"></param>
        /// <param name="handleTx"></param>
        /// <returns></returns>
        JObject Create(string soupName, JObject soupElt, bool handleTx);

        /// <summary>
        /// Upsert a soup entry
        /// </summary>
        /// <param name="soupName"></param>
        /// <param name="soupElt"></param>
        /// <param name="externalIdPath"></param>
        /// <returns></returns>
        JObject Upsert(string soupName, JObject soupElt, string externalIdPath);

        /// <summary>
        /// Upsert a soup entry
        /// </summary>
        /// <param name="soupName"></param>
        /// <param name="soupElt"></param>
        /// <returns></returns>
        JObject Upsert(string soupName, JObject soupElt);

        /// <summary>
        /// Upsert a soup entry
        /// </summary>
        /// <param name="soupName"></param>
        /// <param name="soupElt"></param>
        /// <param name="externalIdPath"></param>
        /// <param name="handleTx"></param>
        /// <returns></returns>
        JObject Upsert(string soupName, JObject soupElt, string externalIdPath, bool handleTx);

        /// <summary>
        /// Look for a entry in a given soup
        /// </summary>
        /// <param name="soupName"></param>
        /// <param name="fieldPath"></param>
        /// <param name="fieldValue"></param>
        /// <returns></returns>
        long LookupSoupEntryId(string soupName, string fieldPath, string fieldValue);

        /// <summary>
        /// Update a soup entry
        /// </summary>
        /// <param name="soupName"></param>
        /// <param name="soupElt"></param>
        /// <param name="soupEntryId"></param>
        /// <param name="handleTx"></param>
        /// <returns></returns>
        JObject Update(String soupName, JObject soupElt, long soupEntryId, bool handleTx);

        /// <summary>
        /// Retrieve an array of soup entries
        /// </summary>
        /// <param name="soupName"></param>
        /// <param name="soupEntryIds"></param>
        /// <returns></returns>
        JArray Retrieve(string soupName, params long[] soupEntryIds);

        /// <summary>
        /// Convert a string to smartSql
        /// </summary>
        /// <param name="smartSql"></param>
        /// <returns></returns>
        string ConvertSmartSql(string smartSql);
    }
}